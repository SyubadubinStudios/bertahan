using static AudioGen.Dsp;

namespace AudioGen;

/// <summary>Synthesised gamelan, dangdut and village sounds. Every method returns one note.</summary>
public static class Inst
{
    /// <summary>Saron / peking: a struck bronze bar with inharmonic partials.</summary>
    public static float[] Saron(int rate, float freq, float velocity = 1f, double length = 1.4)
    {
        float[] x = Make(rate, length);
        (float ratio, float amp, double decay)[] partials = [(1f, 1f, 0.55), (2.76f, 0.35f, 0.18), (5.4f, 0.12f, 0.07), (8.93f, 0.05f, 0.03)];
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            float s = 0;
            foreach (var (ratio, amp, decay) in partials)
            {
                s += amp * MathF.Sin(Tau * freq * ratio * (float)t) * (float)Math.Exp(-t / decay);
            }

            // mallet click
            s += t < 0.004 ? Noise() * 0.3f * (float)(1 - (t / 0.004)) : 0;
            x[i] = s * velocity * Env(t, 0.002, 10);
        }

        return x;
    }

    /// <summary>Bonang: small kettle gong, rounder with a slight beating.</summary>
    public static float[] Bonang(int rate, float freq, float velocity = 1f, double length = 1.2)
    {
        float[] x = Make(rate, length);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            float e = (float)Math.Exp(-t / 0.4);
            float s = MathF.Sin(Tau * freq * (float)t) + (0.5f * MathF.Sin(Tau * freq * 1.003f * (float)t))
                      + (0.25f * MathF.Sin(Tau * freq * 2.02f * (float)t) * (float)Math.Exp(-t / 0.15))
                      + (0.1f * MathF.Sin(Tau * freq * 3.01f * (float)t) * (float)Math.Exp(-t / 0.06));
            x[i] = s * e * velocity * 0.6f * Env(t, 0.003, 10);
        }

        return x;
    }

    /// <summary>Gong ageng / kempul / kenong: deep, beating, long.</summary>
    public static float[] Gong(int rate, float freq, float velocity = 1f, double length = 5.0)
    {
        float[] x = Make(rate, length);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            // the pitch sags a little right after the hit
            float f = freq * (1f + (0.03f * (float)Math.Exp(-t / 0.2)));
            float s = MathF.Sin(Tau * f * (float)t) + (0.8f * MathF.Sin(Tau * f * 1.012f * (float)t))
                      + (0.3f * MathF.Sin(Tau * f * 2.44f * (float)t) * (float)Math.Exp(-t / 0.8))
                      + (0.15f * MathF.Sin(Tau * f * 3.9f * (float)t) * (float)Math.Exp(-t / 0.3));
            x[i] = s * (float)Math.Exp(-t / (length * 0.35)) * velocity * 0.5f * Env(t, 0.01, 100);
        }

        return x;
    }

    /// <summary>Kendang strokes: "dhe" (open bass), "tak" (slap), "dut" (dangdut pitch bend), "tong".</summary>
    public static float[] Kendang(int rate, string stroke, float velocity = 1f)
    {
        float[] x = Make(rate, 0.45);
        BandPass slap = new(rate, 1800, 3);
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            float s;
            switch (stroke)
            {
                case "tak":
                    s = slap.Process(Noise()) * 2.5f * (float)Math.Exp(-t / 0.03)
                        + (0.3f * MathF.Sin(Tau * 420 * (float)t) * (float)Math.Exp(-t / 0.04));
                    break;
                case "dut":
                    phase += (140 - (60 * (1 - Math.Exp(-t / 0.08))) + (40 * Math.Exp(-t / 0.015))) / rate;
                    s = MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.22);
                    break;
                case "tong":
                    phase += (260 - (40 * t)) / rate;
                    s = MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.12);
                    break;
                default: // dhe
                    phase += (95 + (80 * Math.Exp(-t / 0.03))) / rate;
                    s = MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.18) + (Noise() * 0.1f * (float)Math.Exp(-t / 0.01));
                    break;
            }

            x[i] = s * velocity;
        }

        return x;
    }

    /// <summary>Suling (bamboo flute): sine with breath and vibrato.</summary>
    public static float[] Suling(int rate, float freq, double length, float velocity = 1f)
    {
        float[] x = Make(rate, length + 0.15);
        double phase = 0;
        BandPass breath = new(rate, freq * 2, 2);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            float vib = 1f + (0.012f * MathF.Sin(Tau * 5.5f * (float)t) * (float)Math.Min(1, t / 0.3));
            phase += freq * vib / rate;
            float s = MathF.Sin(Tau * (float)phase) + (0.18f * MathF.Sin(Tau * 2 * (float)phase)) + (0.06f * MathF.Sin(Tau * 3 * (float)phase));
            s += breath.Process(Noise()) * 0.25f;
            x[i] = s * velocity * 0.45f * Adsr(t, length, 0.06, 0.1, 0.85f, 0.12);
        }

        return x;
    }

    /// <summary>Round electric-ish bass (dangdut / boss track).</summary>
    public static float[] Bass(int rate, float freq, double length, float velocity = 1f)
    {
        float[] x = Make(rate, length + 0.1);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            double p = freq * t;
            float s = (Tri(p) * 0.7f) + (MathF.Sin(Tau * (float)p) * 0.6f) + (Saw(p) * 0.12f);
            x[i] = s * velocity * 0.6f * Adsr(t, length, 0.005, 0.2, 0.6f, 0.08);
        }

        LowPass(x, rate, 900);
        return x;
    }

    /// <summary>Soft pad / drone for the night levels.</summary>
    public static float[] Pad(int rate, float freq, double length, float velocity = 1f)
    {
        float[] x = Make(rate, length + 1.5);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            float s = 0;
            for (int k = -1; k <= 1; k++)
            {
                s += Saw(freq * (1 + (k * 0.004)) * t) * 0.3f;
            }

            x[i] = s * velocity * 0.4f * Adsr(t, length, 1.0, 0.5, 0.8f, 1.4);
        }

        LowPass(x, rate, 700);
        LowPass(x, rate, 1200);
        return x;
    }

    /// <summary>Kentongan: hollow bamboo/wood alarm block.</summary>
    public static float[] Kentongan(int rate, float velocity = 1f, float pitch = 1f)
    {
        float[] x = Make(rate, 0.35);
        BandPass body = new(rate, 780 * pitch, 12);
        BandPass body2 = new(rate, 1650 * pitch, 10);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            float exc = t < 0.003 ? Noise() : 0f;
            x[i] = ((body.Process(exc) * 9f) + (body2.Process(exc) * 4f) + (MathF.Sin(Tau * 780 * pitch * (float)t) * 0.4f * (float)Math.Exp(-t / 0.05)))
                   * velocity * (float)Math.Exp(-t / 0.12);
        }

        return x;
    }

    /// <summary>Rattling shaker/ tambourine-like hit for the dangdut groove.</summary>
    public static float[] Shaker(int rate, float velocity = 1f)
    {
        float[] x = Make(rate, 0.12);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)rate;
            x[i] = Noise() * velocity * 0.35f * Env(t, 0.004, 0.03);
        }

        HighPass(x, rate, 5000);
        return x;
    }
}
