using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Extraction.Rendering
{
    [Serializable, VolumeComponentMenu("Extraction/Scene ASCII")]
    public sealed class SceneAsciiVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Text cell height in rendered pixels. Rounded to a whole 5x7 font scale.")]
        public ClampedIntParameter characterHeight = new ClampedIntParameter(20, 10, 60);
        [Tooltip("Brightness of the scene color sampled behind each character. 1 preserves the sampled color.")]
        public ClampedFloatParameter backgroundBrightness = new ClampedFloatParameter(0.65f, 0f, 1f);
        public ColorParameter ink = new ColorParameter(new Color(0.85f, 0.9f, 0.8f, 1f));
        [Tooltip("Use dark ink on bright backgrounds to keep characters readable.")]
        public BoolParameter adaptiveInk = new BoolParameter(true);
        [Tooltip("Exposure applied only when choosing character density, in stops.")]
        public ClampedFloatParameter exposure = new ClampedFloatParameter(0f, -4f, 4f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1f, 0.5f, 2f);

        public bool IsActive() => active && enabled.value && intensity.value > 0f;
        public bool IsTileCompatible() => false;
    }
}
