namespace AudioGen;

/// <summary>A mono float buffer with mixing helpers, written out as 16-bit PCM WAV.</summary>
public sealed class Buffer(int sampleRate, double seconds)
{
    public int Rate { get; } = sampleRate;

    public float[] Data { get; private set; } = new float[(int)(sampleRate * seconds)];

    public int Length => Data.Length;

    public double Seconds => Data.Length / (double)Rate;

    public void Add(float[] source, double atSeconds, float gain = 1f)
    {
        int start = (int)(atSeconds * Rate);
        for (int i = 0; i < source.Length; i++)
        {
            int j = start + i;
            if (j >= 0 && j < Data.Length)
            {
                Data[j] += source[i] * gain;
            }
        }
    }

    /// <summary>Adds wrapping around the end, so tails of a loop spill into its start.</summary>
    public void AddWrapped(float[] source, double atSeconds, float gain = 1f)
    {
        int start = (int)(atSeconds * Rate);
        for (int i = 0; i < source.Length; i++)
        {
            Data[(start + i) % Data.Length] += source[i] * gain;
        }
    }

    public void Normalize(float peak = 0.89f)
    {
        float max = 1e-6f;
        foreach (float s in Data)
        {
            max = MathF.Max(max, MathF.Abs(s));
        }

        float k = peak / max;
        for (int i = 0; i < Data.Length; i++)
        {
            Data[i] *= k;
        }
    }

    public void FadeEdges(double inSeconds, double outSeconds)
    {
        int a = (int)(inSeconds * Rate);
        int b = (int)(outSeconds * Rate);
        for (int i = 0; i < a && i < Data.Length; i++)
        {
            Data[i] *= i / (float)a;
        }

        for (int i = 0; i < b && i < Data.Length; i++)
        {
            Data[Data.Length - 1 - i] *= i / (float)b;
        }
    }

    public void SoftClip(float drive = 1.2f)
    {
        for (int i = 0; i < Data.Length; i++)
        {
            Data[i] = MathF.Tanh(Data[i] * drive) / MathF.Tanh(drive);
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using BinaryWriter w = new(File.Create(path));
        int bytes = Data.Length * 2;
        w.Write("RIFF"u8);
        w.Write(36 + bytes);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(Rate);
        w.Write(Rate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8);
        w.Write(bytes);
        foreach (float s in Data)
        {
            w.Write((short)Math.Clamp(s * 32767f, -32768f, 32767f));
        }
    }
}

/// <summary>Oscillators, envelopes and filters used by the instruments.</summary>
public static class Dsp
{
    public const float Tau = MathF.PI * 2f;

    private static readonly Random Rng = new(1234);

    public static float Noise() => (float)(Rng.NextDouble() * 2 - 1);

    public static float[] Make(int rate, double seconds) => new float[Math.Max(1, (int)(rate * seconds))];

    /// <summary>Exponential decay envelope with a short attack.</summary>
    public static float Env(double t, double attack, double decay) =>
        t < attack ? (float)(t / attack) : (float)Math.Exp(-(t - attack) / decay);

    public static float Adsr(double t, double length, double a, double d, float s, double r)
    {
        if (t < a)
        {
            return (float)(t / a);
        }

        if (t < a + d)
        {
            return 1f - ((1f - s) * (float)((t - a) / d));
        }

        if (t < length)
        {
            return s;
        }

        return s * (float)Math.Max(0, 1 - ((t - length) / r));
    }

    public static float Saw(double phase) => (float)((phase - Math.Floor(phase)) * 2 - 1);

    public static float Square(double phase) => (phase - Math.Floor(phase)) < 0.5 ? 1f : -1f;

    public static float Tri(double phase)
    {
        double p = phase - Math.Floor(phase);
        return (float)(p < 0.5 ? (p * 4) - 1 : 3 - (p * 4));
    }

    /// <summary>One pole low pass in place.</summary>
    public static void LowPass(float[] x, int rate, float cutoff)
    {
        float a = 1f - MathF.Exp(-Tau * cutoff / rate);
        float y = 0;
        for (int i = 0; i < x.Length; i++)
        {
            y += a * (x[i] - y);
            x[i] = y;
        }
    }

    public static void HighPass(float[] x, int rate, float cutoff)
    {
        float a = 1f - MathF.Exp(-Tau * cutoff / rate);
        float low = 0;
        for (int i = 0; i < x.Length; i++)
        {
            low += a * (x[i] - low);
            x[i] -= low;
        }
    }

    /// <summary>Resonant band pass (biquad, constant peak gain).</summary>
    public sealed class BandPass
    {
        private float _b0, _b2, _a1, _a2, _x1, _x2, _y1, _y2;

        public BandPass(int rate, float freq, float q) => Set(rate, freq, q);

        public void Set(int rate, float freq, float q)
        {
            float w = Tau * Math.Clamp(freq, 20f, rate * 0.45f) / rate;
            float alpha = MathF.Sin(w) / (2 * q);
            float a0 = 1 + alpha;
            _b0 = alpha / a0;
            _b2 = -alpha / a0;
            _a1 = -2 * MathF.Cos(w) / a0;
            _a2 = (1 - alpha) / a0;
        }

        public float Process(float x)
        {
            float y = (_b0 * x) + (_b2 * _x2) - (_a1 * _y1) - (_a2 * _y2);
            _x2 = _x1;
            _x1 = x;
            _y2 = _y1;
            _y1 = y;
            return y;
        }
    }

    /// <summary>Small Schroeder reverb (4 combs + 2 all-pass), returns a new wet+dry signal.</summary>
    public static float[] Reverb(float[] x, int rate, float mix = 0.3f, float room = 0.82f, double extraSeconds = 1.5)
    {
        float[] output = new float[x.Length + (int)(extraSeconds * rate)];
        int[] combs = [1557, 1617, 1491, 1422];
        float[] wet = new float[output.Length];
        foreach (int baseDelay in combs)
        {
            int delay = baseDelay * rate / 44100;
            float[] line = new float[delay];
            int idx = 0;
            float damp = 0;
            for (int i = 0; i < output.Length; i++)
            {
                float input = i < x.Length ? x[i] : 0f;
                float y = line[idx];
                damp = (y * 0.6f) + (damp * 0.4f);
                line[idx] = input + (damp * room);
                idx = (idx + 1) % delay;
                wet[i] += y * 0.25f;
            }
        }

        foreach (int baseDelay in new[] { 556, 441 })
        {
            int delay = baseDelay * rate / 44100;
            float[] line = new float[delay];
            int idx = 0;
            for (int i = 0; i < wet.Length; i++)
            {
                float buf = line[idx];
                float y = -wet[i] + buf;
                line[idx] = wet[i] + (buf * 0.5f);
                idx = (idx + 1) % delay;
                wet[i] = y;
            }
        }

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = ((i < x.Length ? x[i] : 0f) * (1 - mix)) + (wet[i] * mix);
        }

        return output;
    }

    public static float Midi(float note) => 440f * MathF.Pow(2f, (note - 69f) / 12f);
}
