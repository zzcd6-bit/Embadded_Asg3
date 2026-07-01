using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ShenBi.Rendering
{
    public sealed class ColdXianxiaPostFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class Settings
        {
            public bool enabled = true;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

            [Range(0f, 1f)]
            public float blend = 0.65f;

            [Range(0f, 2f)]
            public float saturation = 0.72f;

            [Range(0f, 2f)]
            public float contrast = 0.92f;

            public Color coolTint = new(0.62f, 0.77f, 0.82f, 1f);
            public Color shadowTint = new(0.32f, 0.46f, 0.52f, 1f);
            public Color fogTint = new(0.72f, 0.82f, 0.84f, 1f);

            [Range(0f, 1f)]
            public float tintStrength = 0.22f;

            [Range(0f, 1f)]
            public float shadowStrength = 0.28f;

            [Range(0f, 1f)]
            public float fogStrength = 0.16f;

            [Range(0f, 1f)]
            public float whiteLift = 0.06f;
        }

        [SerializeField]
        private Settings settings = new();

        [SerializeField, HideInInspector]
        private Shader shader;

        private Material material;
        private ColdXianxiaPostPass pass;

        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int CoolTintId = Shader.PropertyToID("_CoolTint");
        private static readonly int ShadowTintId = Shader.PropertyToID("_ShadowTint");
        private static readonly int FogTintId = Shader.PropertyToID("_FogTint");
        private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        private static readonly int ShadowStrengthId = Shader.PropertyToID("_ShadowStrength");
        private static readonly int FogStrengthId = Shader.PropertyToID("_FogStrength");
        private static readonly int WhiteLiftId = Shader.PropertyToID("_WhiteLift");

        public override void Create()
        {
            shader = shader != null ? shader : Shader.Find("Hidden/ShenBi/ColdXianxiaPost");

            CoreUtils.Destroy(material);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;

            pass = new ColdXianxiaPostPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.enabled || settings.blend <= 0.001f || material == null)
            {
                return;
            }

            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            {
                return;
            }

            pass.renderPassEvent = settings.renderPassEvent;
            pass.Setup(material, settings);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        private sealed class ColdXianxiaPostPass : ScriptableRenderPass
        {
            private const string PassName = "Cold Xianxia Post";

            private Material material;
            private Settings settings;

            public ColdXianxiaPostPass()
            {
                profilingSampler = new ProfilingSampler(PassName);
            }

            public void Setup(Material passMaterial, Settings passSettings)
            {
                material = passMaterial;
                settings = passSettings;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                material.SetFloat(BlendId, settings.blend);
                material.SetFloat(SaturationId, settings.saturation);
                material.SetFloat(ContrastId, settings.contrast);
                material.SetColor(CoolTintId, settings.coolTint);
                material.SetColor(ShadowTintId, settings.shadowTint);
                material.SetColor(FogTintId, settings.fogTint);
                material.SetFloat(TintStrengthId, settings.tintStrength);
                material.SetFloat(ShadowStrengthId, settings.shadowStrength);
                material.SetFloat(FogStrengthId, settings.fogStrength);
                material.SetFloat(WhiteLiftId, settings.whiteLift);

                var source = resourceData.activeColorTexture;
                var destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "_ColdXianxiaPostColor";
                destinationDesc.clearBuffer = false;

                var destination = renderGraph.CreateTexture(destinationDesc);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, PassName);

                resourceData.cameraColor = destination;
            }
        }
    }
}
