using ThreeNet;

namespace Bertahan.Core;

/// <summary>Maps the Rendah / Sedang / Tinggi quality setting onto renderer options.</summary>
public static class Graphics
{
    public static readonly string[] Names = ["Rendah", "Sedang", "Tinggi"];

    public static RendererOptions Options(int quality) => quality switch
    {
        0 => RendererOptions.Default with
        {
            BgraOutput = true,
            MsaaSamples = 1,
            Shadows = true,
            ShadowMapSize = 1024,
            ShadowCascades = 1,
            ShadowDistance = 35,
            ShadowSoftness = 0,
            Bloom = false,
        },
        2 => RendererOptions.Default with
        {
            BgraOutput = true,
            MsaaSamples = 4,
            Shadows = true,
            ShadowMapSize = 4096,
            ShadowCascades = 3,
            ShadowDistance = 70,
            ShadowSoftness = 2,
            Ssao = true,
            Bloom = true,
            BloomIntensity = 0.35f,
            BloomThreshold = 1.2f,
        },
        _ => RendererOptions.Default with
        {
            BgraOutput = true,
            MsaaSamples = 4,
            Shadows = true,
            ShadowMapSize = 2048,
            ShadowCascades = 2,
            ShadowDistance = 55,
            ShadowSoftness = 1,
            Bloom = true,
            BloomIntensity = 0.3f,
            BloomThreshold = 1.3f,
        },
    };

    /// <summary>Resolution multiplier of the 3D view.</summary>
    public static double Scale(int quality) => quality == 0 ? 0.7 : 1.0;
}
