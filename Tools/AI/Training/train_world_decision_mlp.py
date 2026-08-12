#!/usr/bin/env python3
"""Deterministic offline teacher-policy experiment for V72.

This tool never runs in the game.  It creates a small, versioned MLP that
approximates UtilityDecisionPolicy candidate scores and writes every input,
split, metric and hash needed to audit or reproduce the experiment.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import random
from pathlib import Path


FEATURE_IDS = [
    "feature.food_pressure",
    "feature.housing_pressure",
    "feature.population_pressure",
    "feature.profit_opportunity",
    "feature.route_risk",
    "feature.security_risk",
    "feature.expected_benefit",
    "feature.cost",
    "feature.action_risk",
    "feature.agent_risk_tolerance",
    "feature.goal_affinity",
    "feature.validation_feasibility",
]
ACTION_IDS = [
    "mandate.action.none",
    "mandate.action.market_buy_order",
    "mandate.action.trade_order",
    "mandate.action.invest",
    "mandate.action.build_facility",
    "mandate.action.migrate_household",
    "mandate.action.government_purchase",
]


def canonical_json(value: object) -> str:
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def sha256(value: object) -> str:
    return hashlib.sha256(canonical_json(value).encode("utf-8")).hexdigest()


def teacher_score(features: list[int]) -> float:
    food, housing, population, profit, route, security, benefit, cost, risk, tolerance, goal, feasible = features
    need = max(food, housing, population)
    risk_penalty = risk * (10_000 - tolerance) / 10_000
    raw = (
        benefit * 0.82
        + need * 0.33
        + profit * 0.20
        + goal * 0.56
        + feasible * 0.92
        - cost * 0.54
        - route * 0.14
        - security * 0.18
        - risk_penalty * 0.62
    )
    return max(-20_000.0, min(30_000.0, raw))


def make_dataset() -> list[dict]:
    rows: list[dict] = []
    for scenario_seed in range(1, 401):
        rng = random.Random(184_000 + scenario_seed)
        split = "test" if scenario_seed % 5 == 0 else "train"
        for candidate_index in range(6):
            action_id = ACTION_IDS[(scenario_seed + candidate_index) % len(ACTION_IDS)]
            features = [rng.randrange(0, 10_001) for _ in FEATURE_IDS]
            if action_id == "mandate.action.none":
                features[6] = 0
                features[7] = 0
                features[8] = 0
                features[10] = 0
                features[11] = 10_000
            elif candidate_index == 5:
                features[11] = 0
            score = teacher_score(features)
            rows.append(
                {
                    "row_id": f"train.{scenario_seed:04d}.{candidate_index}",
                    "scenario_seed": scenario_seed,
                    "split": split,
                    "agent_kind": ["household", "family_organization", "merchant", "settlement", "county_government"][scenario_seed % 5],
                    "action_type_id": action_id,
                    "feature_schema_version": "ai.features.v1",
                    "action_schema_version": "ai.actions.v1",
                    "features": features,
                    "teacher_score_basis_points": round(score, 6),
                }
            )
    return rows


def forward(x: list[float], hidden_weights: list[list[float]], hidden_biases: list[float], output_weights: list[float], output_bias: float) -> tuple[list[float], float]:
    hidden = [max(0.0, hidden_biases[h] + sum(hidden_weights[h][f] * x[f] for f in range(len(x)))) for h in range(len(hidden_biases))]
    output = output_bias + sum(hidden[h] * output_weights[h] for h in range(len(hidden)))
    return hidden, output


def train(rows: list[dict], epochs: int, hidden_size: int, learning_rate: float) -> tuple[dict, list[dict]]:
    rng = random.Random(72)
    hidden_weights = [[rng.uniform(-0.08, 0.08) for _ in FEATURE_IDS] for _ in range(hidden_size)]
    hidden_biases = [0.01 for _ in range(hidden_size)]
    output_weights = [rng.uniform(-0.08, 0.08) for _ in range(hidden_size)]
    output_bias = 0.0
    training = [row for row in rows if row["split"] == "train"]
    history: list[dict] = []
    for epoch in range(epochs):
        order = list(range(len(training)))
        random.Random(72 + epoch).shuffle(order)
        squared_error = 0.0
        for index in order:
            row = training[index]
            x = [value / 10_000.0 for value in row["features"]]
            target = row["teacher_score_basis_points"] / 10_000.0
            hidden, output = forward(x, hidden_weights, hidden_biases, output_weights, output_bias)
            error = output - target
            squared_error += error * error
            previous_output_weights = list(output_weights)
            for h in range(hidden_size):
                output_weights[h] -= learning_rate * error * hidden[h]
            output_bias -= learning_rate * error
            for h in range(hidden_size):
                if hidden[h] <= 0:
                    continue
                hidden_gradient = error * previous_output_weights[h]
                for f in range(len(FEATURE_IDS)):
                    hidden_weights[h][f] -= learning_rate * hidden_gradient * x[f]
                hidden_biases[h] -= learning_rate * hidden_gradient
        if epoch == 0 or (epoch + 1) % 10 == 0 or epoch + 1 == epochs:
            history.append({"epoch": epoch + 1, "train_mse_normalized": squared_error / len(training)})
    return {
        "hidden_weights": hidden_weights,
        "hidden_biases": hidden_biases,
        "output_weights": output_weights,
        "output_bias": output_bias,
    }, history


def evaluate(rows: list[dict], parameters: dict) -> dict:
    by_split: dict[str, list[tuple[float, float]]] = {"train": [], "test": []}
    for row in rows:
        x = [value / 10_000.0 for value in row["features"]]
        _, output = forward(x, parameters["hidden_weights"], parameters["hidden_biases"], parameters["output_weights"], parameters["output_bias"])
        predicted = output * 10_000.0
        by_split[row["split"]].append((row["teacher_score_basis_points"], predicted))
        row["neural_score_basis_points"] = round(predicted, 6)
        row["absolute_error_basis_points"] = round(abs(predicted - row["teacher_score_basis_points"]), 6)
    result: dict[str, object] = {}
    for split, pairs in by_split.items():
        mse = sum((expected - actual) ** 2 for expected, actual in pairs) / len(pairs)
        mae = sum(abs(expected - actual) for expected, actual in pairs) / len(pairs)
        result[split] = {"rows": len(pairs), "rmse_basis_points": math.sqrt(mse), "mae_basis_points": mae}
    test_rows = [row for row in rows if row["split"] == "test"]
    groups: dict[int, list[dict]] = {}
    for row in test_rows:
        groups.setdefault(row["scenario_seed"], []).append(row)
    agreements = 0
    for candidates in groups.values():
        teacher = max(candidates, key=lambda row: (row["teacher_score_basis_points"], row["action_type_id"]))
        neural = max(candidates, key=lambda row: (row["neural_score_basis_points"], row["action_type_id"]))
        agreements += int(teacher["row_id"] == neural["row_id"])
    result["test_top_action_agreement"] = agreements / len(groups)
    result["test_seed_groups"] = len(groups)
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--epochs", type=int, default=80)
    parser.add_argument("--hidden-size", type=int, default=8)
    parser.add_argument("--learning-rate", type=float, default=0.003)
    args = parser.parse_args()
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    rows = make_dataset()
    config = {
        "trainer": "deterministic_sgd_relu_mlp_v1",
        "seed": 72,
        "epochs": args.epochs,
        "hidden_size": args.hidden_size,
        "learning_rate": args.learning_rate,
        "feature_schema_version": "ai.features.v1",
        "action_schema_version": "ai.actions.v1",
        "dataset_version": "world-decision-teacher-v1",
        "split_rule": "scenario_seed modulo 5; remainder 0 is test",
        "forbidden_features": ["year", "scenario_year", "historical_outcome"],
    }
    parameters, history = train(rows, args.epochs, args.hidden_size, args.learning_rate)
    evaluation = evaluate(rows, parameters)
    runtime_parameters = {
        "hidden_weights": [value for row in parameters["hidden_weights"] for value in row],
        "hidden_biases": parameters["hidden_biases"],
        "output_weights": [value * 10_000.0 for value in parameters["output_weights"]],
        "output_bias": parameters["output_bias"] * 10_000.0,
    }
    model = {
        "ModelId": "model.world_decision_utility_teacher_mlp_v1",
        "ModelVersion": "world-decision-mlp-v1.0.0",
        "FeatureSchemaVersion": "ai.features.v1",
        "ActionSchemaVersion": "ai.actions.v1",
        "DatasetVersion": "world-decision-teacher-v1",
        "ConfigHash": sha256(config),
        "WeightHash": sha256(runtime_parameters),
        "FeatureIds": FEATURE_IDS,
        "FeatureMinimums": [0.0 for _ in FEATURE_IDS],
        "FeatureMaximums": [10_000.0 for _ in FEATURE_IDS],
        "HiddenSize": args.hidden_size,
        "HiddenWeights": runtime_parameters["hidden_weights"],
        "HiddenBiases": runtime_parameters["hidden_biases"],
        "OutputWeights": runtime_parameters["output_weights"],
        "OutputBias": runtime_parameters["output_bias"],
    }
    (output / "model.json").write_text(json.dumps(model, ensure_ascii=False, indent=2), encoding="utf-8")
    (output / "training_config.json").write_text(json.dumps(config, ensure_ascii=False, indent=2), encoding="utf-8")
    (output / "feature_schema.json").write_text(json.dumps({"version": "ai.features.v1", "features": FEATURE_IDS, "normalization": "clamp((x-min)/(max-min),0,1)", "year_feature_present": False}, ensure_ascii=False, indent=2), encoding="utf-8")
    (output / "evaluation.json").write_text(json.dumps({"metrics": evaluation, "history": history}, ensure_ascii=False, indent=2), encoding="utf-8")
    with (output / "training_dataset.jsonl").open("w", encoding="utf-8", newline="\n") as handle:
        for row in rows:
            handle.write(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n")
    files = ["model.json", "training_config.json", "feature_schema.json", "evaluation.json", "training_dataset.jsonl"]
    hashes = {name: hashlib.sha256((output / name).read_bytes()).hexdigest() for name in files}
    manifest = {
        "model_id": model["ModelId"],
        "model_version": model["ModelVersion"],
        "runtime_mode": "inference_only",
        "online_learning": False,
        "safe_fallback": ["utility", "rule", "safe_no_action"],
        "files": hashes,
        "reproduce": "python Tools/AI/Training/train_world_decision_mlp.py --output <MODEL>",
    }
    (output / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(output), "rows": len(rows), "evaluation": evaluation, "model_weight_hash": model["WeightHash"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
