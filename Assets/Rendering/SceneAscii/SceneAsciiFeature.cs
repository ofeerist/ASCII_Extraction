using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Extraction.Rendering
{
    [DisallowMultipleRendererFeature("Scene ASCII")]
    public sealed class SceneAsciiFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader shader;
        private Material material;
        private AsciiPass pass;

        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (shader == null)
                shader = Shader.Find("Hidden/Extraction/SceneAscii");
            if (shader != null)
                material = CoreUtils.CreateEngineMaterial(shader);
            pass = new AsciiPass(material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (material == null || !camera.resolveFinalTarget ||
                (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView))
                return;
            // This is the game's rendering style; Scene View still respects its effects toggle.
            if (camera.isSceneViewCamera && !camera.postProcessEnabled)
                return;
            var settings = VolumeManager.instance.stack.GetComponent<SceneAsciiVolume>();
            if (settings != null && settings.IsActive())
                renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        private sealed class AsciiPass : ScriptableRenderPass
        {
            private readonly Material material;
            private static readonly int Cell = Shader.PropertyToID("_Cell");
            private static readonly int Tone = Shader.PropertyToID("_Tone");
            private static readonly int Ink = Shader.PropertyToID("_Ink");
            private static readonly int SourceSize = Shader.PropertyToID("_SourceSize");

            public AsciiPass(Material material)
            {
                this.material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer || material == null)
                    return;
                var settings = VolumeManager.instance.stack.GetComponent<SceneAsciiVolume>();
                if (settings == null || !settings.IsActive())
                    return;

                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "Scene ASCII Color";
                descriptor.clearBuffer = false;
                var destination = graph.CreateTexture(descriptor);
                var camera = frameData.Get<UniversalCameraData>().cameraTargetDescriptor;

                // Snapshot volume values per camera: render graph execution is deferred.
                var properties = new MaterialPropertyBlock();
                float scale = Mathf.Max(1, Mathf.RoundToInt(settings.characterHeight.value / 10f));
                properties.SetVector(Cell, new Vector4(6f * scale, 10f * scale,
                    settings.backgroundBrightness.value, settings.intensity.value));
                properties.SetVector(Tone, new Vector4(Mathf.Pow(2f, settings.exposure.value),
                    settings.contrast.value, settings.adaptiveInk.value ? 1f : 0f, 0f));
                properties.SetColor(Ink, settings.ink.value);
                properties.SetVector(SourceSize, new Vector4(camera.width, camera.height,
                    1f / Mathf.Max(1, camera.width), 1f / Mathf.Max(1, camera.height)));

                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination,
                    material, 0, properties, 0, 0,
                    geometry: RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle);
                graph.AddBlitPass(parameters, passName: "Scene ASCII");
                resources.cameraColor = destination;
            }
        }
    }
}
