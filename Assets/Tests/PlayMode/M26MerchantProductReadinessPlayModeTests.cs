using System.Collections;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class M26MerchantProductReadinessPlayModeTests
    {
        [UnityTest]
        public IEnumerator OrdinaryPlayerRoute_StartsPreviewsBuysAndDeparts()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;

            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            Assert.That(dashboard.StartRecommendedMerchantExperience(),
                Is.True);

            Assert.That(dashboard.ShowFormalWorldMap(), Is.True);
            yield return null;
            var formalMap = dashboard.FormalWorldMap;
            Assert.That(formalMap, Is.Not.Null);
            Assert.That(formalMap.IsReady, Is.True, formalMap.LastError);
            Assert.That(formalMap.UsesHanWorldV1, Is.True);
            Assert.That(formalMap.IsWorldView, Is.True);
            Assert.That(formalMap.CellGridStep,
                Is.EqualTo(ExplicitStrategicCellMapV1.NationwideOverviewStepCells));
            Assert.That(formalMap.RouteProjection, Is.Not.Null);
            Assert.That(formalMap.RouteProjection.AssetRouteId,
                Is.EqualTo("R003"));
            Assert.That(formalMap.RouteProjection.Status,
                Is.EqualTo(PlayableWorldMapRouteStatus.Planned));
            Assert.That(formalMap.Render(640, 360), Is.Not.Null);

            var opening = dashboard.InspectCurrentMerchantGoal();
            Assert.That(opening.ProductReadiness, Is.Not.Null);
            Assert.That(opening.ProductReadiness.RecommendedNextStep,
                Does.Contain("行动页"));
            Assert.That(opening.MarketOpportunity.SourceName, Is.Not.Empty);

            Assert.That(dashboard.ExecuteCurrentPlayerAction(
                PlayerActionIds.MerchantUseOwnCapital).Success, Is.True);
            var purchase = dashboard.InspectCurrentMerchantGoal()
                .ProductReadiness.Purchase;
            Assert.That(purchase.CanPurchase, Is.True);
            Assert.That(purchase.TotalCost, Is.GreaterThan(0));
            Assert.That(purchase.CargoWeightAfter,
                Is.LessThanOrEqualTo(purchase.CargoCapacity));

            Assert.That(dashboard.ExecuteCurrentPlayerAction(
                PlayerActionIds.MerchantBuyJourneyCargo).Success, Is.True);
            var journey = dashboard.InspectCurrentMerchantGoal()
                .ProductReadiness.Journey;
            Assert.That(journey.CanDepart, Is.True);
            Assert.That(journey.RequiredProvisions, Is.GreaterThan(0));
            Assert.That(journey.KnownRisk, Is.Not.Empty);

            Assert.That(dashboard.ExecuteCurrentPlayerAction(
                PlayerActionIds.MerchantStartJourney).Success, Is.True);
            var departed = dashboard.InspectCurrentMerchantGoal()
                .ProductReadiness.Journey;
            Assert.That(departed.IsInTransit, Is.True);
            Assert.That(departed.RoadStatus, Does.Contain("全国格路"));
            Assert.That(departed.RemainingKilometers, Is.GreaterThan(0));
            formalMap.RefreshFromWorld();
            Assert.That(formalMap.RouteProjection.Status,
                Is.EqualTo(PlayableWorldMapRouteStatus.InTransit));
            Assert.That(formalMap.RouteProjection.CurrentCellId64,
                Is.Not.Zero);
        }
    }
}
