using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class M26P1PresentationTests
    {
        [Test]
        public void MerchantHouseholdContentResource_LoadsAndValidates()
        {
            var asset = Resources.Load<TextAsset>(
                "Content/Core/Gameplay/merchant-household-p1");

            Assert.That(asset, Is.Not.Null);
            var registry = MerchantHouseholdContentRegistry.FromJson(asset.text);
            Assert.That(registry.GetGoal(
                MerchantHouseholdContentIds.FirstGoal), Is.Not.Null);
        }

        [Test]
        public void ActionPresentation_SkipAndDuplicateDoNotReplayResult()
        {
            var sequence = new PlayerActionPresentationSequence();

            Assert.That(sequence.Begin(
                "life_event.m26p1.test", "交谈", "已结算", 10f), Is.True);
            Assert.That(sequence.IsActive, Is.True);
            sequence.Skip();
            Assert.That(sequence.IsActive, Is.False);
            Assert.That(sequence.Begin(
                "life_event.m26p1.test", "交谈", "重复", 12f), Is.False);
        }
    }
}
