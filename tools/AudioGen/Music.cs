using static AudioGen.Dsp;

namespace AudioGen;

/// <summary>Loops for the menu and the levels, composed on slendro and pelog scales.</summary>
public static class Music
{
    public const int Rate = 32000;

    /// <summary>Slendro: five roughly equal steps per octave. Index 0 = D4-ish.</summary>
    public static float Slendro(int n, float baseFreq = 293.66f) => baseFreq * MathF.Pow(2f, n / 5f);

    private static readonly int[] PelogCents = [0, 120, 258, 540, 675, 785, 943];

    public static float Pelog(int n, float baseFreq = 261.63f)
    {
        int octave = (int)Math.Floor(n / 7.0);
        int step = ((n % 7) + 7) % 7;
        return baseFreq * MathF.Pow(2f, octave + (PelogCents[step] / 1200f));
    }

    private static Buffer Loop(double beats, double bpm, out double beat)
    {
        beat = 60.0 / bpm;
        return new Buffer(Rate, beats * beat);
    }

    // ------------------------------------------------------------------ menu

    /// <summary>"Kampung Damai": a cheerful lancaran-style gamelan loop.</summary>
    public static Buffer Menu()
    {
        int[][] gongan =
        [
            [2, 3, 4, 3, 2, 1, 0, 1, 2, 3, 2, 1, 0, -1, 0, 1],
            [3, 4, 5, 4, 3, 2, 3, 4, 5, 4, 3, 2, 1, 2, 1, 0],
            [2, 3, 4, 3, 2, 1, 0, 1, 2, 3, 2, 1, 0, -1, 0, 1],
            [2, 3, 4, 3, 5, 4, 3, 2, 1, 0, 1, 2, 3, 2, 1, 0],
        ];
        Buffer b = Loop(64, 100, out double beat);
        int index = 0;
        foreach (int[] line in gongan)
        {
            for (int k = 0; k < 16; k++, index++)
            {
                double t = index * beat;
                int note = line[k];
                int next = line[(k + 1) % 16];
                b.AddWrapped(Inst.Saron(Rate, Slendro(note), 0.8f), t, 0.5f);
                // peking: two eighth notes an octave up, anticipating the next note
                b.AddWrapped(Inst.Saron(Rate, Slendro(note + 5), 0.5f, 0.6), t, 0.22f);
                b.AddWrapped(Inst.Saron(Rate, Slendro(next + 5), 0.45f, 0.6), t + (beat / 2), 0.2f);
                // bonang mipil: gentle eighths
                b.AddWrapped(Inst.Bonang(Rate, Slendro(note), 0.5f), t + (beat / 2), 0.18f);
                if (k % 4 == 3)
                {
                    b.AddWrapped(Inst.Bonang(Rate, Slendro(note - 5), 0.9f, 2.0), t, 0.3f); // kenong
                }

                if (k is 5 or 9 or 13)
                {
                    b.AddWrapped(Inst.Gong(Rate, Slendro(note - 10) * 2, 0.7f, 2.5), t, 0.3f); // kempul
                }

                if (k == 15)
                {
                    b.AddWrapped(Inst.Gong(Rate, 62f, 1f, 6.0), t, 0.7f); // gong ageng
                }

                // kendang: dhe . tak . / tak dhe tak .
                string[] pattern = (k % 2 == 0) ? ["dhe", "", "tak", ""] : ["tak", "dhe", "", "tak"];
                for (int s = 0; s < 4; s++)
                {
                    if (pattern[s] != "")
                    {
                        b.AddWrapped(Inst.Kendang(Rate, pattern[s], 0.7f), t + (s * beat / 4), 0.35f);
                    }
                }
            }
        }

        Finish(b, 0.12f);
        return b;
    }

    // ------------------------------------------------------------------ day level

    /// <summary>"Goyang Kampung": dangdut groove with suling melody for the daytime levels.</summary>
    public static Buffer Day()
    {
        Buffer b = Loop(64, 124, out double beat);
        int[] roots = [0, 0, 3, 3, 4, 4, 0, 0, 5, 5, 3, 3, 4, 4, 0, 0];
        int[] melody =
        [
            7, -1, 9, 8, 7, -1, 5, -1, 4, 5, 7, -1, 8, 7, 5, 4,
            3, -1, 4, 5, 7, -1, 8, -1, 9, 8, 7, 5, 4, -1, -1, -1,
            7, -1, 9, 8, 7, -1, 5, -1, 4, 5, 7, -1, 8, 7, 5, 4,
            5, -1, 4, 3, 2, -1, 3, 4, 5, 4, 3, 2, 0, -1, -1, -1,
        ];
        for (int beatIndex = 0; beatIndex < 64; beatIndex++)
        {
            double t = beatIndex * beat;
            int bar = beatIndex / 4;
            int root = roots[bar % roots.Length];
            // kendang dangdut: dhe on 1, tak on the & , dut on the "and" of 2 and on 4
            int pos = beatIndex % 4;
            string[] groove = pos switch
            {
                0 => ["dhe", "tak"],
                1 => ["tak", "dut"],
                2 => ["dhe", "tak"],
                _ => ["dut", "tak"],
            };
            b.AddWrapped(Inst.Kendang(Rate, groove[0], 0.9f), t, 0.45f);
            b.AddWrapped(Inst.Kendang(Rate, groove[1], 0.75f), t + (beat / 2), 0.4f);
            for (int s = 0; s < 4; s++)
            {
                b.AddWrapped(Inst.Shaker(Rate, s % 2 == 0 ? 0.6f : 1f), t + (s * beat / 4), 0.25f);
            }

            // bass: root then fifth-ish in eighths
            b.AddWrapped(Inst.Bass(Rate, Pelog(root - 14), beat * 0.45), t, 0.5f);
            b.AddWrapped(Inst.Bass(Rate, Pelog(root - 14 + (pos % 2 == 0 ? 4 : 3)), beat * 0.4), t + (beat / 2), 0.4f);
            // saron arpeggio
            b.AddWrapped(Inst.Saron(Rate, Pelog(root + ((pos * 2) % 5)), 0.6f, 0.8), t, 0.18f);
            if (melody[beatIndex] >= 0)
            {
                int length = 1;
                while (beatIndex + length < 64 && melody[beatIndex + length] == -1 && length < 3)
                {
                    length++;
                }

                b.AddWrapped(Inst.Suling(Rate, Pelog(melody[beatIndex]), (beat * length) - 0.05), t, 0.34f);
            }

            if (beatIndex % 16 == 15)
            {
                b.AddWrapped(Inst.Gong(Rate, 65f, 0.9f, 4.0), t + (beat / 2), 0.45f);
            }
        }

        Finish(b, 0.1f);
        return b;
    }

    // ------------------------------------------------------------------ night level

    /// <summary>"Malam Jumat": slow, eerie pelog for the rice fields and the graveyard.</summary>
    public static Buffer Night()
    {
        Buffer b = Loop(32, 72, out double beat);
        int[] chords = [0, 0, 1, 1, 5, 5, 4, 3];
        for (int bar = 0; bar < 8; bar++)
        {
            double t = bar * 4 * beat;
            int r = chords[bar];
            b.AddWrapped(Inst.Pad(Rate, Pelog(r - 14), beat * 4), t, 0.35f);
            b.AddWrapped(Inst.Pad(Rate, Pelog(r - 11), beat * 4), t, 0.22f);
            if (bar % 2 == 0)
            {
                b.AddWrapped(Inst.Gong(Rate, 55f, 0.8f, 7.0), t, 0.55f);
            }
        }

        int[] gender = [7, -1, 8, 5, -1, -1, 4, -1, 7, 8, 10, -1, 8, -1, 5, -1, 4, -1, 3, 4, -1, -1, 1, -1, 3, -1, 4, 5, 4, -1, -1, -1];
        for (int i = 0; i < 32; i++)
        {
            if (gender[i] >= 0)
            {
                b.AddWrapped(Inst.Bonang(Rate, Pelog(gender[i]), 0.6f, 2.0), i * beat, 0.3f);
            }

            if (i % 8 == 6)
            {
                b.AddWrapped(Inst.Kendang(Rate, "dhe", 0.6f), i * beat, 0.3f);
                b.AddWrapped(Inst.Kendang(Rate, "dhe", 0.4f), (i + 0.5) * beat, 0.25f);
            }
        }

        // a distant kentongan now and then
        b.AddWrapped(Inst.Kentongan(Rate, 0.4f, 0.9f), 11.2 * beat, 0.2f);
        b.AddWrapped(Inst.Kentongan(Rate, 0.3f, 0.9f), 11.7 * beat, 0.15f);

        // night wind
        float[] wind = Make(Rate, b.Seconds);
        BandPass bp = new(Rate, 500, 1.2f);
        for (int i = 0; i < wind.Length; i++)
        {
            double tt = i / (double)Rate;
            bp.Set(Rate, 350 + (250 * MathF.Sin((float)(tt * 0.35))), 1.5f);
            wind[i] = bp.Process(Noise()) * (0.5f + (0.5f * MathF.Sin((float)(tt * 0.6))));
        }

        b.AddWrapped(wind, 0, 0.12f);
        Finish(b, 0.28f);
        return b;
    }

    // ------------------------------------------------------------------ boss

    /// <summary>"Serangan Dukun": fast and driving for the boss waves.</summary>
    public static Buffer Boss()
    {
        Buffer b = Loop(64, 146, out double beat);
        int[] riff = [0, 0, 1, 0, 3, 0, 1, 0, 0, 0, 1, 0, 4, 3, 1, -1];
        int[] roots = [0, 0, 1, 1, 0, 0, -2, -1];
        for (int i = 0; i < 64; i++)
        {
            double t = i * beat;
            int bar = i / 4;
            int root = roots[(bar / 2) % roots.Length];
            for (int s = 0; s < 4; s++)
            {
                string stroke = s switch { 0 => "dhe", 2 => (i % 2 == 0 ? "tak" : "dhe"), _ => "tong" };
                b.AddWrapped(Inst.Kendang(Rate, stroke, s == 0 ? 1f : 0.55f), t + (s * beat / 4), 0.4f);
            }

            int r = riff[(i * 2) % 16];
            b.AddWrapped(Inst.Bass(Rate, Pelog(root + r - 14), beat * 0.45), t, 0.55f);
            b.AddWrapped(Inst.Bass(Rate, Pelog(root + riff[((i * 2) + 1) % 16] - 14), beat * 0.4), t + (beat / 2), 0.5f);
            for (int s = 0; s < 4; s++)
            {
                int n = root + new[] { 7, 8, 7, 10 }[(s + i) % 4];
                b.AddWrapped(Inst.Bonang(Rate, Pelog(n), 0.5f, 0.5), t + (s * beat / 4), 0.14f);
            }

            if (i % 8 == 0)
            {
                b.AddWrapped(Inst.Gong(Rate, 58f, 1f, 3.0), t, 0.6f);
            }

            if (i % 4 == 2)
            {
                b.AddWrapped(Inst.Saron(Rate, Pelog(root + 3), 0.9f), t, 0.3f);
            }

            if (i >= 32 && i % 8 == 4)
            {
                b.AddWrapped(Inst.Suling(Rate, Pelog(root + 12), beat * 1.8), t, 0.3f);
            }
        }

        Finish(b, 0.12f);
        return b;
    }

    // ------------------------------------------------------------------ stingers

    public static Buffer Victory()
    {
        Buffer b = new(Rate, 4.5);
        int[] notes = [0, 2, 4, 5, 7, 9];
        for (int i = 0; i < notes.Length; i++)
        {
            b.Add(Inst.Saron(Rate, Pelog(notes[i]), 1f), i * 0.13, 0.5f);
            b.Add(Inst.Bonang(Rate, Pelog(notes[i] + 7), 0.7f), i * 0.13, 0.25f);
        }

        b.Add(Inst.Gong(Rate, 65f, 1f, 3.5), 0.8, 0.6f);
        b.Add(Inst.Suling(Rate, Pelog(9), 1.6), 0.85, 0.4f);
        b.Add(Inst.Kendang(Rate, "dhe", 1f), 0.78, 0.5f);
        b.Add(Inst.Kendang(Rate, "tak", 1f), 0.9, 0.4f);
        b.Normalize(0.8f);
        return b;
    }

    public static Buffer Defeat()
    {
        Buffer b = new(Rate, 5.0);
        int[] notes = [7, 5, 4, 3, 1, 0];
        for (int i = 0; i < notes.Length; i++)
        {
            b.Add(Inst.Bonang(Rate, Pelog(notes[i]), 0.8f, 1.6), i * 0.32, 0.45f);
        }

        b.Add(Inst.Gong(Rate, 50f, 1f, 4.0), 1.9, 0.8f);
        b.Add(Inst.Pad(Rate, Pelog(-14), 2.0), 1.9, 0.4f);
        b.Normalize(0.8f);
        return b;
    }

    private static void Finish(Buffer b, float reverb)
    {
        float[] wet = Reverb(b.Data, Rate, reverb, 0.8f, 2.0);
        // fold the reverb tail back to the start so the loop is seamless
        Buffer folded = new(Rate, b.Seconds);
        folded.AddWrapped(wet, 0);
        Array.Copy(folded.Data, b.Data, b.Length);
        LowPass(b.Data, Rate, 9000);
        b.Normalize(0.85f);
        b.SoftClip(1.1f);
    }
}
