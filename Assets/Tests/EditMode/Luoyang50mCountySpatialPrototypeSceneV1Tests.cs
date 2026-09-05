using Mandate.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace Mandate.Tests
{
    public sealed class Luoyang50mCountySpatialPrototypeSceneV1Tests
    {
        [Test]
        public void ValidationMenuBuildsReviewSceneWithController()
        {
            var controller = Luoyang50mCountySpatialPrototypeMenu
                .BuildSceneAsset();
            Assert.That(controller, Is.Not.Null);
            Assert.That(EditorSceneManager.GetActiveScene().path,
                Is.EqualTo(Luoyang50mCountySpatialPrototypeMenu
                    .SceneAssetPath));
        }
    }
}
