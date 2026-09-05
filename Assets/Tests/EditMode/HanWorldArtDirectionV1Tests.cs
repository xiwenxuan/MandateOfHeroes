using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class HanWorldArtDirectionV1Tests
    {
        [Test]
        public void ArtProfiles_DefineSixStablePresentationOnlyCandidates()
        {
            var profiles = HanWorldArtProfileCatalog.All.ToArray();
            Assert.That(profiles, Has.Length.EqualTo(6));
            Assert.That(profiles.Select(value => value.ProfileId).Distinct().Count(), Is.EqualTo(6));
            Assert.That(profiles.Select(value => value.Style).Distinct().Count(), Is.EqualTo(6));
            Assert.That(profiles.All(value => value.WorldVerticalExaggeration > 0f), Is.True);
            Assert.That(profiles.All(value => value.RegionVerticalExaggeration > 0f), Is.True);
            Assert.That(profiles.All(value => value.WorldFogEnd > value.WorldFogStart), Is.True);
            Assert.That(profiles.All(value => value.RegionFogEnd > value.RegionFogStart), Is.True);
            Assert.That(HanWorldArtProfileCatalog.Get(HanWorldArtStyle.RealisticNatural)
                .WorldVerticalExaggeration, Is.LessThan(HanWorldArtProfileCatalog
                    .Get(HanWorldArtStyle.ChineseSemiRealistic).WorldVerticalExaggeration));
            Assert.That(HanWorldArtProfileCatalog.Get(HanWorldArtStyle.ChineseSemiRealistic)
                .WorldVerticalExaggeration, Is.LessThan(HanWorldArtProfileCatalog
                    .Get(HanWorldArtStyle.StrategicSandbox).WorldVerticalExaggeration));
        }

        [Test]
        public void ArtCameraRig_UsesOneFrozenCameraPerSampleAndViewAcrossStyles()
        {
            foreach (var sample in new[]
            {
                ArtDirectionSample.CentralPlain,
                ArtDirectionSample.MountainRiver,
                ArtDirectionSample.ForestHills
            })
            foreach (var view in new[] { HanNaturalMapView.World, HanNaturalMapView.Region })
            {
                var preset = HanWorldArtDirectionCameraRig.Get(sample, view);
                Assert.That(preset.Row, Is.InRange(0, GlobalSpatialFoundationV1.Rows - 1));
                Assert.That(preset.Column, Is.InRange(0, GlobalSpatialFoundationV1.Columns - 1));
                Assert.That(preset.Size, Is.GreaterThan(0f));
                Assert.That(preset.Pitch, Is.InRange(45f, 80f));
            }
        }

        [Test]
        public void ArtProfiles_DoNotOwnFrozenWorldFacts()
        {
            Assert.That(GlobalSpatialFoundationV1.Rows, Is.EqualTo(2176));
            Assert.That(GlobalSpatialFoundationV1.Columns, Is.EqualTo(3314));
            Assert.That((long)GlobalSpatialFoundationV1.Rows * GlobalSpatialFoundationV1.Columns,
                Is.EqualTo(7211264L));
            Assert.That(GlobalSpatialFoundationV1.CellSizeMetres, Is.EqualTo(2000));
            Assert.That(typeof(HanWorldArtProfile).GetFields().Select(value => value.Name),
                Has.None.Contains("GlobalCell"));
        }
    }
}
