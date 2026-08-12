using System;
using System.Collections.Generic;

namespace Mandate.Presentation
{
    /// <summary>
    /// Presentation-only sequencing for an already committed world result.
    /// Skipping or replay protection never changes domain state.
    /// </summary>
    public sealed class PlayerActionPresentationSequence
    {
        private readonly HashSet<string> _presentedResultIds =
            new HashSet<string>(StringComparer.Ordinal);

        public bool IsActive { get; private set; }
        public string ResultId { get; private set; } = string.Empty;
        public string Cue { get; private set; } = string.Empty;
        public string Summary { get; private set; } = string.Empty;
        public float StartedAt { get; private set; }
        public float DurationSeconds { get; private set; }

        public bool Begin(
            string resultId,
            string cue,
            string summary,
            float now,
            float durationSeconds = 1.8f)
        {
            if (string.IsNullOrWhiteSpace(resultId) ||
                _presentedResultIds.Contains(resultId))
            {
                return false;
            }

            _presentedResultIds.Add(resultId);
            ResultId = resultId;
            Cue = cue ?? string.Empty;
            Summary = summary ?? string.Empty;
            StartedAt = now;
            DurationSeconds = Math.Max(0.1f, durationSeconds);
            IsActive = true;
            return true;
        }

        public float Progress(float now)
        {
            if (!IsActive)
            {
                return 1f;
            }
            return Math.Min(1f, Math.Max(0f,
                (now - StartedAt) / DurationSeconds));
        }

        public void Update(float now)
        {
            if (IsActive && Progress(now) >= 1f)
            {
                Complete();
            }
        }

        public void Skip() => Complete();

        public void ResetActive()
        {
            IsActive = false;
            ResultId = string.Empty;
            Cue = string.Empty;
            Summary = string.Empty;
        }

        private void Complete()
        {
            IsActive = false;
        }
    }
}
