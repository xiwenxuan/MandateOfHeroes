using System;
using UnityEngine;

namespace Mandate.Presentation
{
    /// <summary>
    /// Unity-serializable identity and stable-anchor contract baked into each
    /// Luoyang final-art prefab. This MonoBehaviour intentionally lives in a
    /// same-named source file so Unity can persist a non-null script reference.
    /// </summary>
    public sealed class LuoyangFinalAssetPrefabMetadata : MonoBehaviour
    {
        public string AssetVariantId;
        public string SourceProfileId;
        public int ReviewOrder;
        public string[] StableAnchorIds = Array.Empty<string>();
    }
}
