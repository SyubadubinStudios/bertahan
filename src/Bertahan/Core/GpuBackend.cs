using System.Numerics;
using ThreeNet;

namespace Bertahan.Core;

/// <summary>
/// Picks the wgpu backend before the view creates its renderer. Some GPUs
/// (seen on an AMD RX 640) render only black through DX12 offscreen targets
/// with MSAA on, so "auto" renders a tiny multisampled probe frame and falls
/// back to Vulkan when the readback comes back empty.
/// </summary>
public static class GpuBackend
{
    /// <summary>Bump when the probe changes so cached results are re-checked.</summary>
    private const int ProbeVersion = 2;

    public static string Active { get; private set; } = "dx12";

    public static void Select(GameSettings settings)
    {
        string choice = settings.Backend;
        if (Environment.GetEnvironmentVariable("WGPU_BACKEND") is { Length: > 0 } forced)
        {
            Active = forced;
            return;
        }

        if (choice is "dx12" or "vulkan")
        {
            Use(choice);
            return;
        }

        if (settings.ProbedBackend is { Length: > 0 } probed && settings.ProbeVersion == ProbeVersion)
        {
            Use(probed);
            return;
        }

        string result = Probe("dx12") ? "dx12" : "vulkan";
        settings.ProbedBackend = result;
        settings.ProbeVersion = ProbeVersion;
        settings.Save();
        Use(result);
    }

    private static void Use(string backend)
    {
        Environment.SetEnvironmentVariable("WGPU_BACKEND", backend);
        Active = backend;
    }

    /// <summary>Renders a lit sphere on a coloured background (256 px with MSAA: smaller targets hide the bug) and checks the pixels are not all black.</summary>
    private static bool Probe(string backend)
    {
        try
        {
            Environment.SetEnvironmentVariable("WGPU_BACKEND", backend);
            using Scene scene = new();
            scene.Environment = scene.Environment with { Background = new Vector4(0.8f, 0.3f, 0.2f, 1f) };
            scene.AddMesh(scene.CreateSphereGeometry(), scene.CreateMaterial(MaterialOptions.Pbr(Colors.White)));
            Node light = scene.AddLight(Light.Directional(Vector3.One, 2f));
            light.LookAt(new Vector3(-1, -1, -1));
            Node camera = scene.AddCamera(Camera.Perspective(0.9f), new Vector3(0, 0, 4));
            using Renderer renderer = Renderer.CreateOffscreen(RendererOptions.Default with { Width = 256, Height = 256, BgraOutput = true, MsaaSamples = 4 });
            renderer.Render(scene, camera);
            byte[] pixels = renderer.ReadPixels();
            long sum = 0;
            foreach (byte b in pixels)
            {
                sum += b;
            }

            return sum > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
