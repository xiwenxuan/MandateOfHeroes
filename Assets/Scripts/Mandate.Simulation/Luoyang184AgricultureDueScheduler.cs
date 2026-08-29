using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// Persistent due index for the Luoyang living-world crop states. Growth is
    /// sampled at bounded ten-day milestones and at exact eligibility/maturity
    /// boundaries. World ticks only dispatch entries whose due coordinate has
    /// arrived; they do not rescan the agriculture source list.
    /// </summary>
    public sealed class Luoyang184AgricultureDueScheduler
    {
        private const int GrowthSampleDays = 10;

        public void Initialize(Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            runtime.AgricultureDueEntries.Clear();
            for (var index = 0; index < runtime.Crops.Count; index++)
                Register(runtime, index,
                    CalculateNextDueDay(runtime, runtime.Crops[index]));
        }

        public void EnsureInitialized(Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (runtime.Crops.Count == 0) return;
            if (runtime.AgricultureDueEntries.Count == 0)
            {
                Initialize(runtime);
                return;
            }
            for (var index = 1; index < runtime.AgricultureDueEntries.Count;
                 index++)
                if (Compare(runtime, runtime.AgricultureDueEntries[index - 1],
                        runtime.AgricultureDueEntries[index]) > 0)
                    throw new InvalidOperationException(
                        "Agriculture due index is not in deterministic order.");
        }

        public void DispatchDue(Luoyang184LivingWorldRuntimeState runtime,
            Action<LuoyangCropRuntimeState> advanceCrop)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (advanceCrop == null) throw new ArgumentNullException(
                nameof(advanceCrop));
            EnsureInitialized(runtime);
            var due = new List<LuoyangAgricultureDueEntryState>();
            while (runtime.AgricultureDueEntries.Count > 0 &&
                   runtime.AgricultureDueEntries[0].DueDay <=
                   runtime.AbsoluteDay)
            {
                due.Add(runtime.AgricultureDueEntries[0]);
                runtime.AgricultureDueEntries.RemoveAt(0);
            }
            for (var index = 0; index < due.Count; index++)
            {
                var entry = due[index];
                if (entry.CropIndex < 0 || entry.CropIndex >= runtime.Crops.Count)
                    throw new InvalidOperationException(
                        "Agriculture due entry references a missing crop.");
                var crop = runtime.Crops[entry.CropIndex];
                if (entry.ScheduleRevision != crop.ScheduleRevision ||
                    entry.DueDay != crop.NextDueDay)
                    continue;
                advanceCrop(crop);
                runtime.AgricultureScheduleDispatchCount++;
                Register(runtime, entry.CropIndex,
                    CalculateNextDueDay(runtime, crop));
            }
        }

        public void Reschedule(Luoyang184LivingWorldRuntimeState runtime,
            LuoyangCropRuntimeState crop)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (crop == null) throw new ArgumentNullException(nameof(crop));
            var index = runtime.Crops.IndexOf(crop);
            if (index < 0)
                throw new InvalidOperationException(
                    "Cannot schedule a crop outside the runtime.");
            runtime.AgricultureDueEntries.RemoveAll(item =>
                item.CropIndex == index);
            Register(runtime, index, CalculateNextDueDay(runtime, crop));
        }

        public static long CalculateNextDueDay(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangCropRuntimeState crop)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (crop == null) throw new ArgumentNullException(nameof(crop));
            if (crop.Phase == LuoyangCropPhase.Harvested)
                return Math.Max(runtime.AbsoluteDay + 1,
                    crop.NextPlantingDay);
            if (crop.Phase == LuoyangCropPhase.Fallow)
                return runtime.AbsoluteDay + GrowthSampleDays;
            var duration = Math.Max(1, crop.CycleDurationDays);
            var earlyDay = checked(crop.PlantingDay +
                DivideRoundUp((long)duration *
                    crop.EarlyHarvestMinimumBasisPoints, 10_000));
            var maturityDay = Math.Max(crop.PlantingDay + 1,
                crop.FullMaturityDay);
            var sampleDay = runtime.AbsoluteDay + GrowthSampleDays;
            if (runtime.AbsoluteDay < earlyDay)
                return Math.Min(sampleDay, earlyDay);
            if (runtime.AbsoluteDay < maturityDay)
                return Math.Min(sampleDay, maturityDay);
            return runtime.AbsoluteDay + 1;
        }

        private static long DivideRoundUp(long value, long divisor) =>
            checked((value + divisor - 1) / divisor);

        private static void Register(
            Luoyang184LivingWorldRuntimeState runtime,
            int cropIndex,
            long dueDay)
        {
            var crop = runtime.Crops[cropIndex];
            crop.ScheduleRevision++;
            crop.NextDueDay = dueDay;
            var entry = new LuoyangAgricultureDueEntryState
            {
                DueDay = dueDay,
                CropIndex = cropIndex,
                ScheduleRevision = crop.ScheduleRevision
            };
            var low = 0;
            var high = runtime.AgricultureDueEntries.Count;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (Compare(runtime, runtime.AgricultureDueEntries[middle],
                        entry) <= 0)
                    low = middle + 1;
                else
                    high = middle;
            }
            runtime.AgricultureDueEntries.Insert(low, entry);
        }

        private static int Compare(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangAgricultureDueEntryState left,
            LuoyangAgricultureDueEntryState right)
        {
            var day = left.DueDay.CompareTo(right.DueDay);
            if (day != 0) return day;
            var field = string.CompareOrdinal(
                runtime.Crops[left.CropIndex].FieldId,
                runtime.Crops[right.CropIndex].FieldId);
            return field != 0
                ? field
                : left.ScheduleRevision.CompareTo(right.ScheduleRevision);
        }
    }
}
