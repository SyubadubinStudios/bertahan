using static AudioGen.Dsp;

namespace AudioGen;

/// <summary>Sound effects. Cartoon flavoured: bonks, boings and silly groans.</summary>
public static class Sfx
{
    public const int Rate = 44100;

    private static readonly Dictionary<char, float[]> Vowels = new()
    {
        ['a'] = [800, 1150, 2900],
        ['o'] = [450, 800, 2830],
        ['u'] = [325, 700, 2530],
        ['e'] = [400, 2100, 2800],
        ['i'] = [300, 2300, 3000],
    };

    /// <summary>Formant voice: pitch contour f0(t), vowel sequence over the duration.</summary>
    public static float[] Voice(double seconds, Func<double, double> f0, string vowels, float rough = 0.2f, float breath = 0.1f)
    {
        float[] x = Make(Rate, seconds);
        BandPass[] f = [new(Rate, 500, 6), new(Rate, 1500, 8), new(Rate, 2500, 10)];
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            double u = t / seconds;
            float pos = (float)(u * (vowels.Length - 1));
            int a = (int)MathF.Floor(pos);
            int bIdx = Math.Min(a + 1, vowels.Length - 1);
            float k = pos - a;
            float[] va = Vowels[vowels[a]], vb = Vowels[vowels[bIdx]];
            for (int j = 0; j < 3; j++)
            {
                f[j].Set(Rate, (va[j] * (1 - k)) + (vb[j] * k), 5 + (j * 3));
            }

            double pitch = f0(t) * (1 + (rough * 0.05 * Noise()));
            phase += pitch / Rate;
            float src = Saw(phase) + (breath * Noise());
            float s = (f[0].Process(src) * 1.0f) + (f[1].Process(src) * 0.6f) + (f[2].Process(src) * 0.3f);
            x[i] = s * Adsr(t, seconds - 0.08, 0.03, 0.05, 1f, 0.08);
        }

        return x;
    }

    private static Buffer Out(float[] x, double pad = 0.05)
    {
        Buffer b = new(Rate, (x.Length / (double)Rate) + pad);
        b.Add(x, 0);
        b.Normalize(0.9f);
        return b;
    }

    public static Buffer Swing()
    {
        float[] x = Make(Rate, 0.28);
        BandPass bp = new(Rate, 400, 2);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            bp.Set(Rate, (float)(300 + (2500 * Math.Sin(Math.PI * t / 0.28))), 2.5f);
            x[i] = bp.Process(Noise()) * (float)Math.Sin(Math.PI * t / 0.28);
        }

        return Out(x);
    }

    public static Buffer HitBlunt()
    {
        float[] x = Make(Rate, 0.3);
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            phase += (170 * Math.Exp(-t / 0.06) + 60) / Rate;
            x[i] = (MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.09)) + (Noise() * 0.6f * (float)Math.Exp(-t / 0.012));
        }

        LowPass(x, Rate, 3000);
        return Out(x);
    }

    public static Buffer HitBlade()
    {
        float[] x = Make(Rate, 0.3);
        BandPass bp = new(Rate, 3500, 3);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            x[i] = (bp.Process(Noise()) * 1.5f * (float)Math.Exp(-t / 0.05)) + (MathF.Sin(Tau * 110 * (float)t) * (float)Math.Exp(-t / 0.07));
        }

        return Out(x);
    }

    /// <summary>The frying pan: a proper cartoon BONNNG.</summary>
    public static Buffer HitPan()
    {
        float[] x = Make(Rate, 1.2);
        float[] ratios = [1f, 2.32f, 4.25f, 6.63f];
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            float s = 0;
            for (int k = 0; k < ratios.Length; k++)
            {
                s += MathF.Sin(Tau * 520 * ratios[k] * (float)t * (1 + (0.002f * MathF.Sin(Tau * 6 * (float)t)))) * (float)Math.Exp(-t / (0.5 / (k + 1))) / (k + 1);
            }

            x[i] = s + (Noise() * 0.5f * (float)Math.Exp(-t / 0.005));
        }

        return Out(x);
    }

    public static Buffer Gunshot()
    {
        float[] x = Make(Rate, 0.7);
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            phase += (120 * Math.Exp(-t / 0.05) + 45) / Rate;
            x[i] = (Noise() * (float)Math.Exp(-t / 0.04)) + (MathF.Sin(Tau * (float)phase) * 1.2f * (float)Math.Exp(-t / 0.12));
        }

        float[] wet = Reverb(x, Rate, 0.25f, 0.7f, 0.6);
        LowPass(wet, Rate, 6000);
        return Out(wet);
    }

    public static Buffer Reload()
    {
        Buffer b = new(Rate, 0.6);
        foreach (double at in new[] { 0.0, 0.25, 0.32 })
        {
            float[] c = Make(Rate, 0.06);
            BandPass bp = new(Rate, at > 0.2 ? 2400 : 1600, 8);
            for (int i = 0; i < c.Length; i++)
            {
                double t = i / (double)Rate;
                c[i] = bp.Process(Noise()) * 3f * (float)Math.Exp(-t / 0.01);
            }

            b.Add(c, at);
        }

        b.Normalize(0.7f);
        return b;
    }

    public static Buffer Empty()
    {
        float[] x = Make(Rate, 0.08);
        BandPass bp = new(Rate, 3000, 10);
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = bp.Process(Noise()) * 4f * (float)Math.Exp(-(i / (double)Rate) / 0.008);
        }

        return Out(x);
    }

    public static Buffer GlassBreak()
    {
        Buffer b = new(Rate, 0.9);
        Random r = new(5);
        for (int k = 0; k < 18; k++)
        {
            float freq = 2500 + (float)(r.NextDouble() * 4500);
            double at = r.NextDouble() * 0.25;
            float[] shard = Make(Rate, 0.3);
            for (int i = 0; i < shard.Length; i++)
            {
                double t = i / (double)Rate;
                shard[i] = MathF.Sin(Tau * freq * (float)t) * (float)Math.Exp(-t / 0.05) * 0.4f;
            }

            b.Add(shard, at);
        }

        float[] crash = Make(Rate, 0.3);
        for (int i = 0; i < crash.Length; i++)
        {
            crash[i] = Noise() * (float)Math.Exp(-(i / (double)Rate) / 0.05);
        }

        HighPass(crash, Rate, 2000);
        b.Add(crash, 0, 1.2f);
        b.Normalize(0.85f);
        return b;
    }

    public static Buffer FireWhoosh()
    {
        float[] x = Make(Rate, 1.2);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            x[i] = Noise() * Adsr(t, 0.5, 0.08, 0.2, 0.6f, 0.6);
        }

        LowPass(x, Rate, 900);
        LowPass(x, Rate, 1400);
        return Out(x);
    }

    public static Buffer FireLoop()
    {
        Buffer b = new(Rate, 2.0);
        float[] rumble = Make(Rate, 2.0);
        for (int i = 0; i < rumble.Length; i++)
        {
            rumble[i] = Noise();
        }

        LowPass(rumble, Rate, 500);
        b.AddWrapped(rumble, 0, 1.0f);
        Random r = new(7);
        for (int k = 0; k < 60; k++)
        {
            float[] crackle = Make(Rate, 0.02);
            for (int i = 0; i < crackle.Length; i++)
            {
                crackle[i] = Noise() * (float)Math.Exp(-(i / (double)Rate) / 0.003);
            }

            b.AddWrapped(crackle, r.NextDouble() * 2.0, 0.3f + (float)r.NextDouble() * 0.5f);
        }

        b.Normalize(0.6f);
        return b;
    }

    public static Buffer Explosion()
    {
        float[] x = Make(Rate, 1.6);
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            phase += (80 * Math.Exp(-t / 0.2) + 30) / Rate;
            x[i] = (Noise() * (float)Math.Exp(-t / 0.25)) + (MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.4));
        }

        LowPass(x, Rate, 1800);
        return Out(Reverb(x, Rate, 0.2f, 0.7f, 0.5));
    }

    public static Buffer Groan(int variant)
    {
        (double len, double baseF, string v) = variant switch
        {
            0 => (1.1, 95.0, "uoa"),
            1 => (0.9, 120.0, "aou"),
            _ => (1.3, 80.0, "ouuo"),
        };
        float[] x = Voice(len, t => baseF * (1 + (0.25 * Math.Sin(t * 5))) * (1 - (0.2 * t)), v, 0.6f, 0.25f);
        return Out(x);
    }

    public static Buffer ZombieDie()
    {
        float[] x = Voice(0.9, t => 160 * Math.Exp(-t * 1.2), "aou", 0.5f, 0.2f);
        Buffer b = Out(x, 0.3);
        // silly "pop" at the end
        float[] pop = Make(Rate, 0.12);
        double phase = 0;
        for (int i = 0; i < pop.Length; i++)
        {
            double t = i / (double)Rate;
            phase += (400 + (1400 * t / 0.12)) / Rate;
            pop[i] = MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.04);
        }

        b.Add(pop, 0.75, 0.5f);
        b.Normalize(0.9f);
        return b;
    }

    public static Buffer TuyulGiggle()
    {
        Buffer b = new(Rate, 1.0);
        for (int k = 0; k < 5; k++)
        {
            float[] hee = Voice(0.12, t => 700 + (k * 40) - (t * 800), "ie", 0.1f, 0.3f);
            b.Add(hee, k * 0.15, 0.8f);
        }

        b.Normalize(0.85f);
        return b;
    }

    public static Buffer KuntiLaugh()
    {
        Buffer b = new(Rate, 2.6);
        for (int k = 0; k < 8; k++)
        {
            float[] hi = Voice(0.16, t => 620 - (k * 25) + (60 * Math.Sin(t * 60)), "ii", 0.05f, 0.4f);
            b.Add(hi, k * 0.19, 0.7f);
        }

        float[] wet = Reverb(b.Data, Rate, 0.45f, 0.86f, 0.6);
        return Out(wet);
    }

    public static Buffer PocongHop()
    {
        float[] x = Make(Rate, 0.25);
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            phase += (140 * Math.Exp(-t / 0.03) + 55) / Rate;
            x[i] = (MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.07)) + (Noise() * 0.3f * (float)Math.Exp(-t / 0.01));
        }

        LowPass(x, Rate, 1500);
        return Out(x);
    }

    public static Buffer Roar()
    {
        float[] x = Voice(1.4, t => 65 * (1 + (0.1 * Math.Sin(t * 30))), "aao", 1.0f, 0.5f);
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = MathF.Tanh(x[i] * 3);
        }

        LowPass(x, Rate, 2000);
        return Out(x);
    }

    public static Buffer DukunCast()
    {
        float[] x = Make(Rate, 1.3);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            float f = 300 + (900 * (float)(t / 1.3));
            x[i] = (MathF.Sin(Tau * f * (float)t) * 0.4f) + (MathF.Sin(Tau * f * 1.5f * (float)t) * 0.3f * MathF.Sin(Tau * 9 * (float)t));
            x[i] *= Adsr(t, 1.1, 0.2, 0.2, 0.7f, 0.2);
        }

        float[] chant = Voice(1.2, t => 110, "ouao", 0.2f, 0.2f);
        Buffer b = new(Rate, 2.2);
        b.Add(Reverb(x, Rate, 0.4f, 0.85f, 0.8), 0);
        b.Add(chant, 0, 0.6f);
        b.Normalize(0.85f);
        return b;
    }

    public static Buffer PlayerHurt(bool high)
    {
        float[] x = Voice(0.28, t => (high ? 330 : 170) * (1 - (t * 0.8)), "ou", 0.2f, 0.15f);
        return Out(x);
    }

    public static Buffer PlayerDie()
    {
        float[] x = Voice(1.2, t => 220 * Math.Exp(-t * 0.9), "aaou", 0.3f, 0.2f);
        return Out(Reverb(x, Rate, 0.2f, 0.7f, 0.4));
    }

    public static Buffer Pickup()
    {
        Buffer b = new(Rate, 0.7);
        float[] notes = [659.25f, 830.61f, 987.77f, 1318.5f];
        for (int k = 0; k < notes.Length; k++)
        {
            b.Add(Inst.Saron(Rate, notes[k], 0.8f, 0.5), k * 0.06, 0.5f);
        }

        b.Normalize(0.8f);
        return b;
    }

    public static Buffer Heal()
    {
        Buffer b = new(Rate, 1.2);
        float[] notes = [523.25f, 659.25f, 783.99f];
        for (int k = 0; k < notes.Length; k++)
        {
            b.Add(Inst.Bonang(Rate, notes[k], 0.8f, 1.0), k * 0.09, 0.5f);
        }

        b.Normalize(0.75f);
        return b;
    }

    /// <summary>Kentongan alarm: the village warning that a wave is coming.</summary>
    public static Buffer WaveStart()
    {
        Buffer b = new(Rate, 2.2);
        double[] hits = [0, 0.18, 0.36, 0.8, 0.98, 1.16];
        foreach (double h in hits)
        {
            b.Add(Inst.Kentongan(Rate, 1f), h);
        }

        b.Normalize(0.85f);
        return b;
    }

    public static Buffer WaveClear()
    {
        Buffer b = new(Rate, 3.5);
        b.Add(Inst.Gong(Rate, 70, 1f, 3.2), 0, 0.8f);
        float[] notes = [392f, 440f, 523.25f, 587.33f];
        for (int k = 0; k < notes.Length; k++)
        {
            b.Add(Inst.Bonang(Rate, notes[k], 0.9f), 0.1 + (k * 0.12), 0.4f);
        }

        b.Normalize(0.85f);
        return b;
    }

    public static Buffer UiClick()
    {
        float[] x = Inst.Kentongan(Rate, 0.8f, 1.8f);
        return Out(x);
    }

    public static Buffer UiHover()
    {
        float[] x = Make(Rate, 0.06);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            x[i] = MathF.Sin(Tau * 1500 * (float)t) * (float)Math.Exp(-t / 0.015);
        }

        Buffer b = Out(x);
        b.Normalize(0.3f);
        return b;
    }

    public static Buffer Step()
    {
        float[] x = Make(Rate, 0.1);
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = Noise() * (float)Math.Exp(-(i / (double)Rate) / 0.02);
        }

        LowPass(x, Rate, 700);
        Buffer b = Out(x);
        b.Normalize(0.35f);
        return b;
    }

    public static Buffer Dodge()
    {
        Buffer b = Swing();
        float[] grunt = Voice(0.18, t => 200, "ah".Replace('h', 'a'), 0.2f, 0.3f);
        b.Add(grunt, 0, 0.3f);
        b.Normalize(0.8f);
        return b;
    }

    public static Buffer Fireball()
    {
        float[] x = Make(Rate, 0.8);
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            x[i] = (Noise() * 0.6f) + (MathF.Sin(Tau * (200 + (400 * (float)t)) * (float)t) * 0.3f);
            x[i] *= Adsr(t, 0.6, 0.05, 0.1, 0.7f, 0.2);
        }

        LowPass(x, Rate, 2000);
        return Out(x);
    }

    public static Buffer Boing()
    {
        float[] x = Make(Rate, 0.5);
        double phase = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = i / (double)Rate;
            phase += (180 + (120 * MathF.Sin(Tau * 12 * (float)t) * (float)Math.Exp(-t / 0.2))) / Rate;
            x[i] = MathF.Sin(Tau * (float)phase) * (float)Math.Exp(-t / 0.18);
        }

        return Out(x);
    }
}
