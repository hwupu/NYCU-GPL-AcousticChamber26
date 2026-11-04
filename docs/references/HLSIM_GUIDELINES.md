# Handoff: Build the NIOSH Hearing Loss Simulator (HLSim) in Unity

**Audience:** an AI coding agent (or developer) implementing a Unity port.
**Source being ported:** NIOSH/CDC *Hearing Loss Simulator* web app, v4.0.29.0 (Blazor WebAssembly + Web Audio).
**Core model:** ANSI S3.44 (the American counterpart of ISO 1999) noise-induced and age-related hearing threshold prediction.

> **Provenance.** Everything in this document was recovered directly from the compiled app (`HLSimWeb.wasm`, SHA-256 `m9H2wW4XFWcSj858xytGg6xrrCHy2zzec3Vz2QzzPlw=`, listed in the app's `blazor.boot.json`), from the saved page HTML, and from `hlsim.js`. The .NET IL of `HLSimWeb.Logic.ANSIS344` was decoded and every constant extracted programmatically, not typed from memory. Section 12 lists what was *not* recovered.

---

## 0. TL;DR for the implementer

1. The simulator plays up to **two looping sounds** (foreground, background), passes each through **10 narrow peaking EQ filters** (125 Hz … 8 kHz), and mixes them.
2. The EQ gain of each band **is the hearing level in dB HL** (negative = loss, range **−80 … +12**).
3. Band values come from one of: **None**, **3 presets** (Mild/Moderate/Severe), **Manual sliders**, or **Predicted** (ANSI S3.44 from sex, age, years of exposure, exposure dB(A), fractile).
4. A foreground/background **ratio slider** (−10 … +10) sets two linear gains.
5. Two spectrum displays show the *post-filter* spectrum of each track.
6. Section 5 contains a **complete, ready-to-compile C# `ANSIS344` class**. Section 14 has a float32-faithful **Python reference** and **test vectors** to validate it.

---

## 1. Scope and deliverables

| # | Deliverable | Notes |
|---|---|---|
| 1 | `ANSIS344` model class (pure C#, no Unity dependencies) | Section 5. Must reproduce test vectors (Section 9). |
| 2 | Presets / state model (`HearingLossLevel`, `PredictionParameters`, `SimulatorSettings`) | Sections 3, 4, 6. |
| 3 | Audio engine: 2 tracks × 10 peaking biquads, looping playback | Section 7. |
| 4 | Foreground/background ratio control | Section 7.4. |
| 5 | Spectrum visualizers | Section 8. |
| 6 | UI (sound pickers, Play/Stop, hearing-loss mode, prediction controls, 10 band sliders) | Section 10. |
| 7 | Unit tests for the model and the filter | Section 9. |

**Suggested Unity version:** 2022.3 LTS or newer. UI: uGUI or UI Toolkit (agent's choice). No third-party packages are required.

---

## 2. System overview

```
              ┌────────────────────────── Track 0: FOREGROUND ───────────────────────────┐
 Sound file → │ loop → Gain(fg) → Biquad[0..9] in series ─┬→ Analyser(fg) → fg canvas      │
              └───────────────────────────────────────────┼───────────────────────────────┘
                                                          ├──────────────┐
              ┌────────────────────────── Track 1: BACKGROUND ────────────┼───────────────┐
 Sound file → │ loop → Gain(bg) → Biquad[0..9] in series ─┬→ Analyser(bg) → bg canvas      │
              └───────────────────────────────────────────┼───────────────────────────────┘
                                                          └──────────────┴→ Output (summed)

 Hearing-loss state (None / Mild / Moderate / Severe / Manual / Predicted)
        └→ int[10] band dB values → SAME values applied to BOTH tracks' filters
```

Key facts:

- The same 10 band values are applied to **both** tracks (the foreground and the background both "go through the same ears").
- The visualizer taps the signal **after** the last filter, so it shows the hearing-loss effect.
- There is no master limiter, compressor, or reverb.

---

## 3. Bands

Index → center frequency (Hz):

| Index | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|---|---|---|
| **Hz** | 125 | 250 | 500 | 1000 | 1500 | 2000 | 3000 | 4000 | 6000 | 8000 |

- UI slider range: **−80 … +12 dB HL**, integer values, default 0.
- Slider element ids in the original: `f125`, `f250`, … `f8000`.
- The 10-element arrays in the ANSI tables use this same indexing. Bands 0–2 (125/250/500 Hz) reuse the 500 Hz-type values, and bands 8–9 (6000/8000 Hz) reuse the 6000 Hz-type values for the noise constants (see tables below).

---

## 4. Enums, parameters, defaults

### 4.1 `HearingLossLevel` (numeric values matter)

| Value | Name | Radio id | UI label |
|---|---|---|---|
| 0 | `None` | `hlRadio1` | No hearing loss |
| 1 | `Mild` | `hlRadio3` | Mild (20 dB HL at 4000 Hz) |
| 2 | `Moderate` | `hlRadio4` | Moderate (30 dB HL at 4000 Hz) |
| 3 | `Severe` | `hlRadio5` | Moderate/severe (40 dB HL at 4000 Hz) |
| 4 | `Predicted` | `hlRadio2` | Predicted loss based on exposure and age |
| 5 | `Manual` | `hlRadio6` | Manual adjustment by frequency |

The original app's initial selection is **None**.

### 4.2 `PredictionParameters`

| Field | Type | UI control | Range / options | Default (`ResetPrediction`) |
|---|---|---|---|---|
| `UseEffects` | bool | checkbox "Include noise exposure effects" (`hlCheckbox1`) | – | **true** |
| `UseAge` | bool | checkbox "Include age effects" (`hlCheckbox2`) | – | **true** |
| `Male` | bool | radio "Male" (`hlRadio7`) / "Female" (`hlRadio8`) | – | **true (Male)** |
| `Age` | int | slider (`age`) | **18 … 70** | **55** |
| `Years` | int | slider "Years of Exposure" (`yearsExp`) | **0 … 50**, suffix " years" | **25** |
| `ExposureLevel` | int | slider "Exposure level" (`expLevel`) | **70 … 130**, suffix " dB(A)" | **95** |
| `Fractile` | double | select "Fractile (Population Distribution)" | **0.1, 0.25, 0.5, 0.75, 0.9** | **0.5** |

A **"Reset default values"** button restores the defaults above. Step size is not specified in the markup (HTML default step = 1); all four numeric inputs are integers in the model.

### 4.3 Meaning of the inputs (from the app's own help text)

- **Years:** a noisy period in the simulated person's life, in working years (not continuous exposure).
- **Exposure level:** a single dB(A) value that represents the time-weighted average (TWA) over that period.
- **Fractile 0.1:** this person has *more* loss than 90% of equally exposed people. **0.75:** more loss than only 25%. **0.5** is the population median and the recommended default.
- **UseEffects unchecked:** shows age alone. **UseAge unchecked:** shows noise alone.

---

## 5. ANSIS344: full implementation specification

### 5.1 Algorithm (per band `i = 0..9`)

**Inputs:** `useAge, male, age, useEffects, years, level, fractile`.

**Step 0: normalize inputs**

```
sex = male ? 0 : 1                     // column index into the a/bu/bi tables
if (!useAge)     age = 18
if (!useEffects) { years = 0; level = 85 }
```

**Step 1: age-related loss ("normal" arrays), median and percentiles**

```
normal50[i] = a[i][sex] * (age - 18)^2
normal10[i] = normal50[i] + K10 * ( bu[i][sex] + 0.445 * normal50[i] )     // more loss
normal25[i] = normal50[i] + K25 * ( bu[i][sex] + 0.445 * normal50[i] )
normal75[i] = normal50[i] - K25 * ( bi[i][sex] + 0.356 * normal50[i] )     // less loss
normal90[i] = normal50[i] - K10 * ( bi[i][sex] + 0.356 * normal50[i] )
K10 = 1.282   K25 = 0.675
```

**Step 2: noise-induced loss ("noise" arrays)**

If `years <= 0`, all five noise arrays are 0. Otherwise:

```
l1 = log10(years)
l2 = log10(years + 1) / log10(11)
l0 = (level < L0[i]) ? 0 : (level - L0[i])        // dB above the per-band threshold L0

if (1 <= years <= 9)  noise50[i] = ( l2 * (u[i] + l1 * v[i]) ) * l0^2
if (years >= 10)      noise50[i] = (      (u[i] + l1 * v[i]) ) * l0^2

du = ( Xu[i] + Yu[i] * l1 ) * l0^2
di = ( Xi[i] + Yi[i] * l1 ) * l0^2

noise10[i] = noise50[i] + K10 * du
noise25[i] = noise50[i] + K25 * du
noise75[i] = noise50[i] - K25 * di
noise90[i] = noise50[i] - K10 * di
```

**Step 3: choose the fractile arrays** (exact comparisons in the original; the UI only offers these five values)

| fractile | normal array | noise array |
|---|---|---|
| 0.5 | `normal50` | `noise50` |
| 0.1 | `normal10` | `noise10` |
| 0.9 | `normal90` | `noise90` |
| 0.25 | `normal25` | `noise25` |
| anything else (0.75) | `normal75` | `noise75` |

**Step 4: combine, round, clamp**

```
if (useAge && useEffects)  loss = N + Z - (N * Z) / 120      // N = normal[i], Z = noise[i]
else if (useEffects)       loss = Z                          // noise only
else if (useAge)           loss = N                          // age only
else                       loss = 0

step[i]  = -1 * (int) Round(loss)          // Round = banker's rounding (round half to even)
step[i]  = clamp(step[i], -80, +12)
band[i]  = (int) Round(step[i])            // already an integer; output int[10]
```

The result `band[i]` (an integer dB HL, negative = loss) is the gain applied to the band-`i` filter.

### 5.2 Numeric fidelity notes (important for matching the original)

- **Table values are 32-bit floats** in the original. `K10`, `K25`, and the literals `0.445`, `0.356` are doubles.
- Arithmetic in the original mixes the two: the table values are widened to double for the products and `Pow`, and the results are narrowed back to float at the points shown in the reference code. The C# below mirrors those cast points.
- Using plain `double` everywhere differs from the float32-faithful version in about **0.025%** of band values, always by **1 dB** (59 of 235,200 values in a grid comparison). Acceptable tolerance for tests against a double implementation is ±1 dB; the float32-faithful code should match exactly.
- Rounding must be **round-half-to-even** (`Math.Round(double)` default in .NET). Note `Mathf.RoundToInt` also rounds half to even, but use `System.Math` to keep the model free of Unity dependencies.
- The original keeps the intermediate arrays in `static` fields, so values leak between calls. One side effect: `0 < years < 1` would reuse stale noise values. The UI only produces integers (0 or ≥ 1), so a port should simply use fresh zeroed arrays.

### 5.3 Constant tables

**Noise-induced loss constants (per band)**

| Index | Hz | L0 (dB(A)) | u | v | Xu | Yu | Xi | Yi |
|---|---|---|---|---|---|---|---|---|
| 0 | 125 | 93 | −0.033 | 0.110 | 0.044 | 0.016 | 0.033 | 0.002 |
| 1 | 250 | 93 | −0.033 | 0.110 | 0.044 | 0.016 | 0.033 | 0.002 |
| 2 | 500 | 93 | −0.033 | 0.110 | 0.044 | 0.016 | 0.033 | 0.002 |
| 3 | 1000 | 89 | −0.020 | 0.070 | 0.022 | 0.016 | 0.020 | 0.000 |
| 4 | 1500 | 84 | −0.0325 | 0.068 | 0.0265 | 0.007 | **0.070** | 0.000 |
| 5 | 2000 | 80 | −0.045 | 0.066 | 0.031 | −0.002 | 0.016 | 0.000 |
| 6 | 3000 | 77 | 0.012 | 0.037 | 0.007 | 0.016 | 0.029 | −0.010 |
| 7 | 4000 | 75 | 0.025 | 0.025 | 0.005 | 0.009 | 0.016 | −0.002 |
| 8 | 6000 | 77 | 0.019 | 0.024 | 0.013 | 0.008 | 0.028 | −0.007 |
| 9 | 8000 | 77 | 0.019 | 0.024 | 0.013 | 0.008 | 0.028 | −0.007 |

**Age-related loss constants (columns: male / female)**

| Index | Hz | a (M / F) | bu (M / F) | bi (M / F) |
|---|---|---|---|---|
| 0 | 125 | 0.003 / 0.003 | 7.23 / 6.67 | 5.78 / 5.34 |
| 1 | 250 | 0.003 / 0.003 | 6.67 / 6.12 | 5.34 / 4.89 |
| 2 | 500 | 0.0035 / 0.0035 | 6.12 / 6.12 | 4.89 / 4.89 |
| 3 | 1000 | 0.004 / 0.004 | 6.12 / 6.12 | 4.89 / 4.89 |
| 4 | 1500 | 0.0055 / 0.005 | 6.67 / 6.67 | 5.34 / 5.34 |
| 5 | 2000 | 0.007 / 0.006 | 7.23 / 6.67 | 5.78 / 5.34 |
| 6 | 3000 | 0.0115 / 0.0075 | 7.78 / 7.23 | 6.23 / 5.78 |
| 7 | 4000 | 0.016 / 0.009 | 8.34 / 7.78 | 6.67 / 6.23 |
| 8 | 6000 | 0.018 / 0.012 | 9.45 / 8.90 | 7.56 / 7.12 |
| 9 | 8000 | 0.022 / 0.015 | 10.56 / 10.56 | 8.45 / 8.45 |

**Fractile constants:** `K10 = 1.282` (fractiles 0.1 and 0.9), `K25 = 0.675` (fractiles 0.25 and 0.75), median 0.5 uses no offset.

### 5.4 Known quirk: the 1500 Hz `Xi` value

`Xi[4] = 0.070` is far above its neighbors (0.020 and 0.016). It makes the *below-median* fractiles (0.75, 0.9) at 1500 Hz behave strangely: for example, test vector 14 in Section 9 yields **+12 dB at 1500 Hz while every other band is −80**, and vector 12 shows only −3 dB at 1500 Hz next to −14 and −19 on either side.

It is possibly a typo in the original. It has not been verified against the published standard. **Recommendation:** port it faithfully by default, keep the table in a single place, and expose a `bool clampGainToZero` option (default `false`) so product owners can decide later. Do not silently "fix" it.

### 5.5 Complete C# reference implementation

```csharp
// File: Assets/HLSim/Model/ANSIS344.cs
// Port of HLSimWeb.Logic.ANSIS344 (NIOSH HLSim v4.0.29.0). Pure C#, no Unity dependencies.
using System;

namespace HLSim.Model
{
    public enum HearingLossLevel
    {
        None = 0, Mild = 1, Moderate = 2, Severe = 3, Predicted = 4, Manual = 5
    }

    [Serializable]
    public sealed class PredictionParameters
    {
        public bool UseAge = true;
        public bool Male = true;
        public int Age = 55;            // 18..70
        public bool UseEffects = true;
        public int Years = 25;          // 0..50
        public int ExposureLevel = 95;  // 70..130 dB(A) (TWA)
        public double Fractile = 0.5;   // 0.1, 0.25, 0.5, 0.75, 0.9

        public void ResetToDefaults()
        {
            UseAge = true; Male = true; Age = 55;
            UseEffects = true; Years = 25; ExposureLevel = 95; Fractile = 0.5;
        }
    }

    public static class ANSIS344
    {
        public const int BandCount = 10;
        public const int MinBandDb = -80;
        public const int MaxBandDb = 12;

        public static readonly int[] BandHz = { 125, 250, 500, 1000, 1500, 2000, 3000, 4000, 6000, 8000 };

        const double K10 = 1.282;
        const double K25 = 0.675;

        // ---- noise-induced loss constants (float32 in the original) ----
        static readonly float[] L0 = { 93f, 93f, 93f, 89f, 84f, 80f, 77f, 75f, 77f, 77f };
        static readonly float[] U  = { -0.033f, -0.033f, -0.033f, -0.02f, -0.0325f, -0.045f, 0.012f, 0.025f, 0.019f, 0.019f };
        static readonly float[] V  = { 0.11f, 0.11f, 0.11f, 0.07f, 0.068f, 0.066f, 0.037f, 0.025f, 0.024f, 0.024f };
        static readonly float[] XU = { 0.044f, 0.044f, 0.044f, 0.022f, 0.0265f, 0.031f, 0.007f, 0.005f, 0.013f, 0.013f };
        static readonly float[] YU = { 0.016f, 0.016f, 0.016f, 0.016f, 0.007f, -0.002f, 0.016f, 0.009f, 0.008f, 0.008f };
        static readonly float[] XI = { 0.033f, 0.033f, 0.033f, 0.02f, 0.07f, 0.016f, 0.029f, 0.016f, 0.028f, 0.028f };
        static readonly float[] YI = { 0.002f, 0.002f, 0.002f, 0.0f, 0.0f, 0.0f, -0.01f, -0.002f, -0.007f, -0.007f };

        // ---- age-related loss constants: [band, sex]  sex 0 = male, 1 = female ----
        static readonly float[,] A = {
            {0.003f, 0.003f},   {0.003f, 0.003f},   {0.0035f, 0.0035f}, {0.004f, 0.004f},  {0.0055f, 0.005f},
            {0.007f, 0.006f},   {0.0115f, 0.0075f}, {0.016f, 0.009f},   {0.018f, 0.012f},  {0.022f, 0.015f} };
        static readonly float[,] BU = {
            {7.23f, 6.67f},  {6.67f, 6.12f},  {6.12f, 6.12f},  {6.12f, 6.12f},  {6.67f, 6.67f},
            {7.23f, 6.67f},  {7.78f, 7.23f},  {8.34f, 7.78f},  {9.45f, 8.9f},   {10.56f, 10.56f} };
        static readonly float[,] BI = {
            {5.78f, 5.34f},  {5.34f, 4.89f},  {4.89f, 4.89f},  {4.89f, 4.89f},  {5.34f, 5.34f},
            {5.78f, 5.34f},  {6.23f, 5.78f},  {6.67f, 6.23f},  {7.56f, 7.12f},  {8.45f, 8.45f} };

        public static int[] Apply(PredictionParameters p) =>
            Apply(p.UseAge, p.Male, p.Age, p.UseEffects, p.Years, p.ExposureLevel, p.Fractile);

        /// <returns>int[10] of dB HL band values (negative = loss), clamped to [-80, +12].</returns>
        public static int[] Apply(bool useAge, bool male, int age, bool useEffects,
                                  int years, int level, double fractile)
        {
            int sex = male ? 0 : 1;
            if (!useAge) age = 18;
            if (!useEffects) { years = 0; level = 85; }

            var n50 = new float[BandCount]; var n10 = new float[BandCount]; var n25 = new float[BandCount];
            var n75 = new float[BandCount]; var n90 = new float[BandCount];
            var z50 = new float[BandCount]; var z10 = new float[BandCount]; var z25 = new float[BandCount];
            var z75 = new float[BandCount]; var z90 = new float[BandCount];

            for (int i = 0; i < BandCount; i++)
            {
                // ---- age-related ("normal") ----
                double a = A[i, sex], bu = BU[i, sex], bi = BI[i, sex];
                double dAge = age - 18;
                n50[i] = (float)(a * Math.Pow(dAge, 2.0));
                n10[i] = (float)(n50[i] + K10 * (bu + 0.445 * n50[i]));
                n25[i] = (float)(n50[i] + K25 * (bu + 0.445 * n50[i]));
                n75[i] = (float)(n50[i] - K25 * (bi + 0.356 * n50[i]));
                n90[i] = (float)(n50[i] - K10 * (bi + 0.356 * n50[i]));

                // ---- noise-induced ----
                if (years > 0)
                {
                    float l1 = (float)Math.Log10(years);
                    float l2 = (float)(Math.Log10(years + 1) / Math.Log10(11.0));
                    float l0 = (float)level < L0[i] ? 0f : (float)level - L0[i];
                    double p = Math.Pow(l0, 2.0);

                    if (years <= 9) { float t = l2 * (U[i] + l1 * V[i]); z50[i] = (float)(t * p); }
                    else            { float t = U[i] + l1 * V[i];        z50[i] = (float)(t * p); }

                    float tu = XU[i] + YU[i] * l1;
                    float ti = XI[i] + YI[i] * l1;
                    float du = (float)(tu * p);
                    float di = (float)(ti * p);

                    z10[i] = (float)(z50[i] + K10 * du);
                    z25[i] = (float)(z50[i] + K25 * du);
                    z75[i] = (float)(z50[i] - K25 * di);
                    z90[i] = (float)(z50[i] - K10 * di);
                }
            }

            float[] nl, nz;
            if (Near(fractile, 0.5))       { nl = n50; nz = z50; }
            else if (Near(fractile, 0.1))  { nl = n10; nz = z10; }
            else if (Near(fractile, 0.9))  { nl = n90; nz = z90; }
            else if (Near(fractile, 0.25)) { nl = n25; nz = z25; }
            else                           { nl = n75; nz = z75; }   // 0.75

            var bands = new int[BandCount];
            for (int i = 0; i < BandCount; i++)
            {
                float loss;
                if (useAge && useEffects) loss = (nl[i] + nz[i]) - (nl[i] * nz[i]) / 120f;
                else if (useEffects)      loss = nz[i];
                else if (useAge)          loss = nl[i];
                else                      loss = 0f;

                float step = -1 * (int)Math.Round((double)loss);      // banker's rounding
                if (step < MinBandDb) step = MinBandDb;
                if (step > MaxBandDb) step = MaxBandDb;
                bands[i] = (int)Math.Round((double)step);
            }
            return bands;
        }

        static bool Near(double x, double y) => Math.Abs(x - y) < 1e-9;
    }

    /// <summary>Fixed band presets used by Mild / Moderate / Severe (dB HL per band).</summary>
    public static class Presets
    {
        //                                    125  250  500  1k   1.5k  2k   3k   4k   6k   8k
        public static readonly int[] None     = {  0,   0,   0,   0,    0,   0,   0,   0,   0,   0 };
        public static readonly int[] Mild     = {  0,   0,   0,   0,   -5, -10, -10, -20, -10,   0 };
        public static readonly int[] Moderate = {  0,   0, -10, -10,  -15, -20, -20, -30, -20, -10 };
        public static readonly int[] Severe   = {  0,   0, -10, -20,  -25, -30, -30, -40, -30, -20 };
    }
}
```

---

## 6. Hearing-loss state machine (`SetHearingLoss`)

State: `Bands` (list of 10 ints), `PreviousManualBands` (list of 10 ints, initially all 0), `SelectedLevel`.

```
SetHearingLoss(level, predictionParameters):
    if level == Predicted and predictionParameters == null:
        throw ArgumentException("Prediction parameters cannot be null when hearing loss level is Predicted.")

    switch level:
        None:      Bands = Presets.None
        Mild:      Bands = Presets.Mild
        Moderate:  Bands = Presets.Moderate
        Severe:    Bands = Presets.Severe
        Manual:    Bands = copy of PreviousManualBands
        Predicted: Bands = ANSIS344.Apply(predictionParameters)

    SyncBands()    // push all 10 Bands values to the audio filters (both tracks)
```

Manual-mode behavior:

- The 10 band sliders are **disabled except in Manual mode**. In every other mode they display (read-only) the values currently in `Bands`.
- Moving a slider in Manual mode: sets `Bands[i]`, sets `PreviousManualBands[i]`, and applies the filter immediately.
- Switching *away* from Manual and back restores `PreviousManualBands`.
- A **"Reset all to zero"** button is visible in Manual mode and sets all bands to 0.
- Changing any prediction control while in Predicted mode re-runs `SetHearingLoss(Predicted, currentParameters)`.

**Presets as a picture:** all three show a "noise notch": loss grows from low frequencies to a maximum at **4000 Hz** (−20 / −30 / −40 dB), then recovers at 6000–8000 Hz.

Not recovered: a timer (`startTimer`/`Timer_Elapsed`) is started on manual slider changes, and the settings object holds a local-storage handle. This is probably debounced persistence of the manual values. The exact behavior was not decoded. In Unity, `PlayerPrefs` (or JSON in `persistentDataPath`) is a reasonable optional equivalent.

---

## 7. Audio engine

### 7.1 Per-track signal chain (original Web Audio graph)

```
AudioBufferSource (loop = true)  →  GainNode  →  Biquad[0] → Biquad[1] → … → Biquad[9]  ┬→ destination
                                                                                         └→ AnalyserNode
```

- Playing creates a **new** source and starts it at offset 0 (it restarts from the beginning each Play). Stop stops the source.
- Both tracks connect to the destination and are summed by the audio system.
- The two tracks have independent buffers and gain nodes, but filter *values* are set on both tracks' filters at the same time (`setFilterValue(index, dB)` loops over both tracks).

### 7.2 The filters

All 10 are Web Audio `BiquadFilterNode`s of type **peaking**, in series, ordered by frequency (index 0 = 125 Hz … index 9 = 8000 Hz).

| Parameter | Value |
|---|---|
| Type | peaking |
| Frequencies | 125, 250, 500, 1000, 1500, 2000, 3000, 4000, 6000, 8000 Hz |
| Gain (dB) | the band value in dB HL (−80 … +12), initial 0 |
| Q | **12** for every band, except band 0 (125 Hz) = **24** |
| Gain updates | instantaneous (no smoothing) |

**Q at 8000 Hz:** the original code *intends* 24 for the last band too, but it assigns `filters[last].Q = 24` (to the parameter object, not `.Q.value`), which silently does nothing. The effective value is **12**. Make the Q table a config array, default it to the *effective* values above (`{24,12,12,12,12,12,12,12,12,12}`), and note the intended alternative in a comment.

**Narrowness:** Q = 12 is a *very* narrow bell (about 0.12 octave wide). Each band attenuates a thin slice around its center; frequencies between centers are mostly unaffected. This is the original behavior. Do not widen it as a "fix". Expose Q as a tunable so it can be changed later.

**Biquad coefficients** (Audio EQ Cookbook peaking EQ, identical to the Web Audio spec; `Q` is linear, not dB):

```
A      = 10^(gainDb / 40)
w0     = 2π · f0 / fs
alpha  = sin(w0) / (2 · Q)

b0 = 1 + alpha·A        a0 = 1 + alpha/A
b1 = -2·cos(w0)         a1 = -2·cos(w0)
b2 = 1 - alpha·A        a2 = 1 - alpha/A
(all divided by a0)
y[n] = b0·x[n] + b1·x[n-1] + b2·x[n-2] − a1·y[n-1] − a2·y[n-2]
```

Recompute the coefficients when a gain changes or the output sample rate changes (use `AudioSettings.outputSampleRate`).

### 7.3 Unity implementation sketch

Use **one `AudioSource` per track**, each with a filter component that processes in `OnAudioFilterRead` (runs on the audio thread, so keep it allocation-free and lock-light). Set the AudioSources to `loop = true`, `playOnAwake = false`, `spatialBlend = 0` (2D).

```csharp
// File: Assets/HLSim/Audio/PeakingBiquad.cs
using System;

namespace HLSim.Audio
{
    public sealed class PeakingBiquad
    {
        double b0 = 1, b1, b2, a1, a2;

        public void Set(double fs, double f0, double q, double gainDb)
        {
            double A = Math.Pow(10.0, gainDb / 40.0);
            double w0 = 2.0 * Math.PI * f0 / fs;
            double cos = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2.0 * q);
            double a0 = 1.0 + alpha / A;
            b0 = (1.0 + alpha * A) / a0;
            b1 = (-2.0 * cos) / a0;
            b2 = (1.0 - alpha * A) / a0;
            a1 = (-2.0 * cos) / a0;
            a2 = (1.0 - alpha / A) / a0;
        }

        // Transposed Direct Form II. State is kept per channel by the caller.
        public float Process(float x, ref double z1, ref double z2)
        {
            double y = b0 * x + z1;
            z1 = b1 * x - a1 * y + z2;
            z2 = b2 * x - a2 * y;
            return (float)y;
        }
    }
}
```

```csharp
// File: Assets/HLSim/Audio/HearingLossFilterBank.cs
using UnityEngine;
using HLSim.Model;

namespace HLSim.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class HearingLossFilterBank : MonoBehaviour
    {
        // Effective Q values of the original app (see Section 7.2).
        public double[] q = { 24, 12, 12, 12, 12, 12, 12, 12, 12, 12 };

        const int MaxChannels = 8;
        readonly double[] gainDb = new double[ANSIS344.BandCount];
        readonly object gate = new object();
        volatile int version;
        int appliedVersion = -1;
        double fs;

        PeakingBiquad[] bq;
        double[,] z1 = new double[MaxChannels, ANSIS344.BandCount];
        double[,] z2 = new double[MaxChannels, ANSIS344.BandCount];

        // Optional tap for the spectrum display (mono mix, post-filter).
        public readonly float[] tap = new float[8192];
        int tapPos;

        void Awake()
        {
            fs = AudioSettings.outputSampleRate;
            bq = new PeakingBiquad[ANSIS344.BandCount];
            for (int i = 0; i < bq.Length; i++) bq[i] = new PeakingBiquad();
            version++;
        }

        public void SetBandDb(int index, double db)
        {
            lock (gate) { gainDb[index] = db; }
            version++;
        }

        public void SetAllBandsDb(int[] values)
        {
            lock (gate) { for (int i = 0; i < values.Length; i++) gainDb[i] = values[i]; }
            version++;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (bq == null) return;
            if (appliedVersion != version)
            {
                lock (gate)
                {
                    for (int i = 0; i < bq.Length; i++)
                        bq[i].Set(fs, ANSIS344.BandHz[i], q[i], gainDb[i]);
                }
                appliedVersion = version;
            }
            int ch = Mathf.Min(channels, MaxChannels);
            for (int n = 0; n < data.Length; n += channels)
            {
                float mono = 0f;
                for (int c = 0; c < ch; c++)
                {
                    float x = data[n + c];
                    for (int b = 0; b < ANSIS344.BandCount; b++)
                        x = bq[b].Process(x, ref z1[c, b], ref z2[c, b]);
                    data[n + c] = x;
                    mono += x;
                }
                tap[tapPos] = mono / ch;
                tapPos = (tapPos + 1) & (tap.Length - 1);
            }
        }

        public int TapWritePosition => tapPos;
    }
}
```

Notes for the implementer:

- The filter commutes with linear gain, so applying `AudioSource.volume` before/after `OnAudioFilterRead` makes no difference.
- Avoid clicks: the original applies gain changes instantly. An optional 5–10 ms ramp on coefficient changes is a reasonable improvement; keep it behind a flag and default to the original behavior.
- With a −80 dB gain a peaking filter's `A` is 1e-4; this is numerically safe in double precision.
- Sample-rate note: the 8000 Hz band is well below Nyquist at 44.1 kHz and 48 kHz.

### 7.4 Foreground/background ratio (S/N slider)

- Control: integer slider **−10 … +10**, default **0** (labeled "Foreground ← → Background").
- Gains are **linear amplitude** (not dB), set directly on the per-track gain stage:

```
v = sliderValue                                   // -10 .. +10
if (v > 0)  { fgGain = 1 - |v| / 10;  bgGain = 1 }    // toward Background: foreground fades out
if (v < 0)  { fgGain = 1;             bgGain = 1 - |v| / 10 }   // toward Foreground: background fades out
if (v == 0) { fgGain = 1;             bgGain = 1 }
```

So **−10 = foreground only**, **+10 = background only** and **0 = both at full level**. The help text agrees ("slid all the way to the Foreground end … only the foreground sound will be heard").

> Correction: in an earlier summary of this tool I had these two ends reversed. This section, taken from the decoded `SNRatioChanged` method, is correct.

### 7.5 Sound library

Two track slots: `foreground` (initial selection **`sounds/Male.flac`**) and `background` (initial selection: none). Each has a drop-down, a Play/Stop button, and a "Choose your own…" option with a file picker. Entries, in order:

| Label | Original file |
|---|---|
| Select a sound | (none) |
| Choose your own… | user file (accepts `audio/*`) |
| Human speech – Male | `sounds/Male.flac` |
| Human speech – Female | `sounds/Female.flac` |
| Human speech babble – Male | `sounds/Babble-Male.flac` |
| Human speech babble – Female | `sounds/Babble-Female.flac` |
| Human speech babble – Male and female | `sounds/Babble-Male-Female.flac` |
| Machinery – Air track drill | `sounds/Air Track Drill.flac` |
| Machinery – Blower | `sounds/Blower.flac` |
| Machinery – Brake press | `sounds/Brake Press.flac` |
| Machinery – Bulldozer | `sounds/Bulldozer.flac` |
| Machinery – Continuous haulage machine | `sounds/Continuous Haulage.flac` |
| Machinery – Continuous mining machine | `sounds/Continuous Miner with Continuous Haulage.flac` |
| Machinery – Grinder | `sounds/Grinder.flac` |
| Machinery – Jet airliner takeoff | `sounds/Jet Takeoff.flac` |
| Machinery – Silica mill | `sounds/Silica Mill.flac` |
| Random noise – High frequency | `sounds/Generic-High.flac` |
| Random noise – Low frequency | `sounds/Generic-Low.flac` |
| Music – Classical | `sounds/Music-Classical.flac` |
| Music – Pop | `sounds/Music-Pop.flac` |
| Simulated tinnitus | `sounds/Simulated Tinnitus (4KHz sawtooth).flac` |
| Warning - Backup alarm | `sounds/Warning - Backup alarm.flac` |

**The audio files are not part of the archive that was analyzed.** They must be obtained from NIOSH (contact on the app page: `mining@cdc.gov`) or replaced with licensed/recorded equivalents.

Format guidance: Unity's reliable import formats are WAV, OGG, MP3 and AIFF (FLAC import support depends on the Unity version; verify, or convert to OGG/WAV). "Choose your own…" needs runtime decoding of user files (`UnityWebRequestMultimedia` or a runtime decoder) and may be limited to formats the target platform supports. The original supports mp3, wav, flac, ogg, opus, oga, webm, weba, m4a, mid, aiff, wma and au (browser-dependent).

---

## 8. Spectrum visualizers

Two canvases, one per track (foreground: green bars; background: red bars). Drawn every frame from an analyser attached **after** the last filter.

### 8.1 Original analyser/drawing parameters

| Item | Value |
|---|---|
| FFT size | 1024 (512 bins) |
| Smoothing time constant | 0.5 |
| Min / max decibels | Web Audio defaults (−100 / −30 dB); the app does not change them |
| Canvas size | 600 × 75 px (scaled to width by CSS) |
| Background fill | `rgb(51, 51, 51)` |
| Bar width / gap | 6 px bar + 1 px gap (7 px per bin) |
| Bar height | `h = byteValue / 2` |
| Bar draw | rectangle at `y = 75 − h/2`, height `h` (the lower half falls below the canvas, so the visible part is about `h/2`) |
| Foreground bar color | `rgb(50, h + 100, 50)` |
| Background bar color | `rgb(h + 100, 50, 50)` |
| Label overlays | "Foreground" and "Background" over each canvas |

**Observed limitation (from reading the code, not confirmed in the live app):** the loop draws all 512 bins at 7 px each (3,584 px) onto a 600 px canvas, so only about the first 86 bins are visible, roughly **0 to 3.7 kHz at 44.1 kHz** (bin width = fs/1024). That conflicts with the help text, which talks about high-frequency bars on the right. A port should decide between *faithful* (truncate) and *useful* (map the full spectrum to the display width). **Recommendation:** map the full 0–Nyquist range (or a log-frequency axis) to the display, and keep the original colors and the dB mapping.

### 8.2 Unity implementation

- Use the `tap` ring buffer from each `HearingLossFilterBank` (post-filter, mono mix), read on the main thread.
- Take the latest 1024 samples, apply a **Blackman window** (what the Web Audio analyser uses), run an FFT, and take `|X[k]| / N`.
- Smoothing, per bin: `S[k] = 0.5·S_prev[k] + 0.5·|X[k]|/N`.
- Convert: `dB = 20·log10(S[k])`; byte value `= clamp(floor(255 · (dB + 100) / 70), 0, 255)`.
- Draw as a `RawImage` texture or a UI Toolkit `generateVisualContent` element.
- Avoid `AudioSource.GetSpectrumData` for this: it may not reflect the output of `OnAudioFilterRead`, and its scaling differs.

---

## 9. Test plan

### 9.1 Model unit tests (must pass exactly with the float32-faithful C# above)

Columns: `UseAge, Male, Age, UseEffects, Years, Level, Fractile` → 10 band values (dB HL). Generated by the Python reference in Section 14.

| # | UseAge | Male | Age | UseEff | Years | Level | Fractile | 125 | 250 | 500 | 1k | 1.5k | 2k | 3k | 4k | 6k | 8k |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | T | T | 55 | T | 25 | 95 | 0.5 | -5 | -5 | -5 | -8 | -15 | -19 | -34 | -42 | -38 | -43 |
| 2 | T | F | 55 | T | 25 | 95 | 0.5 | -5 | -5 | -5 | -8 | -14 | -18 | -29 | -34 | -31 | -35 |
| 3 | F | T | 55 | T | 25 | 95 | 0.5 | 0 | 0 | 0 | -3 | -8 | -11 | -21 | -24 | -17 | -17 |
| 4 | T | T | 55 | F | 0 | 0 | 0.5 | -4 | -4 | -5 | -5 | -8 | -10 | -16 | -22 | -25 | -30 |
| 5 | T | F | 55 | F | 0 | 0 | 0.5 | -4 | -4 | -5 | -5 | -7 | -8 | -10 | -12 | -16 | -21 |
| 6 | T | T | 18 | T | 1 | 85 | 0.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | -1 | 0 | 0 |
| 7 | F | T | 30 | T | 5 | 100 | 0.5 | -2 | -2 | -2 | -3 | -3 | 0 | -15 | -20 | -14 | -14 |
| 8 | F | T | 30 | T | 10 | 90 | 0.5 | 0 | 0 | 0 | 0 | -1 | -2 | -8 | -11 | -7 | -7 |
| 9 | F | T | 30 | T | 10 | 90 | 0.1 | 0 | 0 | 0 | 0 | -3 | -6 | -13 | -15 | -12 | -12 |
| 10 | F | T | 30 | T | 10 | 90 | 0.9 | 0 | 0 | 0 | 0 | +2 | 0 | -4 | -7 | -3 | -3 |
| 11 | T | T | 45 | T | 20 | 105 | 0.25 | -28 | -28 | -28 | -32 | -42 | -45 | -70 | -72 | -65 | -68 |
| 12 | T | T | 45 | T | 20 | 105 | 0.75 | -10 | -11 | -11 | -14 | -3 | -19 | -40 | -46 | -33 | -34 |
| 13 | T | F | 70 | T | 50 | 130 | 0.1 | -80 | -80 | -80 | -80 | -80 | -80 | -80 | -80 | -80 | -80 |
| 14 | T | T | 70 | T | 50 | 130 | 0.9 | -80 | -80 | -80 | -80 | +12 | -80 | -80 | -80 | -80 | -80 |
| 15 | F | F | 30 | T | 3 | 70 | 0.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 16 | T | T | 40 | T | 9 | 100 | 0.5 | -5 | -5 | -5 | -7 | -10 | -10 | -28 | -35 | -28 | -30 |
| 17 | T | T | 40 | T | 10 | 100 | 0.5 | -5 | -5 | -5 | -8 | -12 | -12 | -30 | -37 | -30 | -31 |
| 18 | F | T | 30 | T | 40 | 120 | 0.5 | -80 | -80 | -80 | -80 | -80 | -80 | -80 | -80 | -80 | -80 |
| 19 | T | T | 18 | F | 0 | 0 | 0.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 20 | F | T | 30 | F | 0 | 0 | 0.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Interpretation checks:

- **Vector 1 (defaults)** is the app's out-of-the-box "Predicted" result.
- **Vectors 3 vs 4** show noise-only has a 3–6 kHz notch with partial recovery at 8 kHz, while age-only keeps worsening toward 8 kHz.
- **Vectors 16 vs 17** cross the `years = 9 → 10` boundary (the `l2` factor is dropped at 10 years or more).
- **Vectors 13, 14, 18** hit the −80 clamp. **Vectors 10, 12, 14** show the 1500 Hz anomaly from Section 5.4.

### 9.2 Additional model tests

- Defaults object (`ResetToDefaults`): male, age 55, years 25, 95 dB(A), fractile 0.5, both effects on.
- `Apply` never returns values outside −80 … +12.
- With `useAge = false` the age input is ignored (age treated as 18). With `useEffects = false`, years and level are ignored.
- Fractile ordering sanity for typical inputs: loss(0.1) ≥ loss(0.25) ≥ loss(0.5) ≥ loss(0.75) ≥ loss(0.9) in magnitude, **except** where the 1500 Hz anomaly interferes.
- For a regression suite, generate a grid with the Python reference (sex × age 18..70 step 4 × years {0,1,3,9,10,20,35,50} × level 70..130 step 10 × fractile × the 3 checkbox combinations) and compare exactly.

### 9.3 Preset and state tests

- `Presets.Mild/Moderate/Severe` match Section 6.
- Switching Manual → other → Manual restores the last manual values.
- Predicted mode with `null` parameters throws the documented exception.

### 9.4 Audio tests

- **Filter response:** feed a sine at each center frequency; with band gain `g` dB the steady-state output level should change by `g` dB (±0.1 dB). Feed a sine at the *midpoint* between two centers (for example 1250 Hz) and confirm that it is nearly unaffected (this verifies the Q = 12 narrowness).
- **Q table:** band 0 uses 24, all other bands use 12.
- **S/N gains:** `v = −10 → (fg 1, bg 0)`, `v = −5 → (1, 0.5)`, `v = 0 → (1, 1)`, `v = +5 → (0.5, 1)`, `v = +10 → (0, 1)`.
- **Looping and restart:** Play restarts from time 0; Stop halts playback.
- Both tracks receive identical band values.

---

## 10. UI specification

Original layout (Bootstrap cards, responsive; a header "Hearing Loss Simulator", a "Help" button, and a beta disclaimer):

1. **Header:** title "Hearing Loss Simulator" with the version string; a Help button opens a documentation page ("Back to simulator" returns).
2. **Foreground sound card:** drop-down, (file picker if "Choose your own…"), Play/Stop button. Default: Male speech. The button shows "Please wait…" while a file loads.
3. **Background sound card:** same controls, default none.
4. **Foreground/Background ratio:** slider −10 … +10, labels "Foreground" (left) and "Background" (right).
5. **Visualizers:** foreground canvas above background canvas.
6. **Frequency bands (Hz):** ten vertical sliders, −80 … +12 dB HL, labelled 125 … 8000, with scale labels "+12" at top and "−80" at bottom and "dB HL" caption. Disabled except in Manual mode. A "Reset all to zero" button appears in Manual mode.
7. **Hearing loss panel:** radio group (Section 4.1), with "Common patterns" as a sub-heading for the three presets.
8. **Predicted loss panel** (activated by the Predicted radio): checkboxes "Include noise exposure effects" and "Include age effects"; a "Reset default values" button; an "Exposure effects" group (Years of exposure, Exposure level) and an "Age effects" group (Age, Sex, Fractile).
9. **Footer/disclaimer:** the original shows: *"Beta Release: This software has not fully completed NIOSH's rigorous testing but is considered fully functional. … Send comments to mining@cdc.gov."*

Accessibility labels used in the original: "Foreground sound", "Background sound", "Foreground and Background Sound", "Background/foreground ratio", "Frequency Bands", "Hearing Loss", "Years of Exposure", "Exposure level", "Age", "Sex", "Fractile (Population Distribution)".

Contextual help (info popups) exists for: Foreground/Background sound, Foreground/Background ratio, graphical displays, frequency bands, hearing loss options. The full help text lives in the original app's Help page and in the saved HTML. Reuse or rewrite as appropriate after checking CDC content terms.

---

## 11. Suggested project structure and milestones

```
Assets/HLSim/
  Model/      ANSIS344.cs, HearingLossLevel, PredictionParameters, Presets.cs, SimulatorSettings.cs
  Audio/      PeakingBiquad.cs, HearingLossFilterBank.cs, TrackController.cs, SignalNoiseMixer.cs
  Analysis/   SpectrumAnalyzer.cs (Blackman + FFT + smoothing), SpectrumView.cs
  UI/         MainScreen, FrequencySliders, HearingLossPanel, PredictionPanel, TrackPanel
  Sounds/     (assets to be supplied; see Section 7.5)
Tests/EditMode/  ANSIS344Tests.cs, PresetTests.cs, BiquadTests.cs, MixerTests.cs
```

| Milestone | Content | Exit criterion |
|---|---|---|
| M1 | `ANSIS344`, enums, presets, `SimulatorSettings` state machine | All Section 9.1–9.3 tests pass |
| M2 | Biquad + filter bank + two-track playback + S/N mixer | Section 9.4 tests pass; audible hearing-loss effect |
| M3 | UI wiring (all controls, enabling rules, defaults) | Every control in Section 10 works and matches the defaults |
| M4 | Spectrum displays | Both displays update live and show the post-filter spectrum |
| M5 | Polish: persistence, help content, attribution, platform builds | Release checklist |

---

## 12. What was *not* recovered, and open decisions

| Item | Status | Suggested handling |
|---|---|---|
| Sound files (`sounds/*.flac`) | Not in the analyzed archive | Obtain from NIOSH or substitute |
| Exact step size of the numeric sliders | Not in markup (HTML default 1) | Use 1 |
| Settings persistence details | Not decoded | Optional `PlayerPrefs` |
| `resetBands`, `TrackSNChanged`, `Timer_Elapsed` internals | Not decoded (analytics/persistence helpers) | Implement from the behavior described here |
| Help-page full text | Present in the app, not reproduced here | Copy from source, subject to CDC content terms |

**Decisions for the product owner (the agent should surface these rather than decide silently):**

1. **1500 Hz `Xi = 0.070` anomaly** (Section 5.4): faithful vs. corrected vs. clamped.
2. **8 kHz Q:** effective 12 (default here) vs. intended 24.
3. **Analyzer width:** faithful truncation vs. full-spectrum mapping (Section 8.1).
4. **Licensing and attribution:** the app is a CDC/NIOSH product and the ANSI S3.44 standard is a copyrighted publication. The constants here were read out of the NIOSH tool itself. Confirm reuse permissions, and label the port as *unofficial and not endorsed by NIOSH/CDC*. Keep the beta-status notice and a pointer to the original tool.
5. **Target platforms:** affects audio-file import and the "Choose your own…" feature.

---

## 13. Provenance and how this was derived

- `blazor.boot.json` lists `HLSimWeb.wasm` (the .NET assembly, delivered as a WebAssembly "Webcil" container), plus the runtime and common libraries. Its SHA-256 matches the file analyzed.
- The Webcil container was unwrapped, its .NET metadata tables were parsed, and the IL bodies of these methods were decoded:
  - `HLSimWeb.Logic.ANSIS344::DecANSI` (all table constants) and `::ApplyANSI` (the algorithm in Section 5).
  - `HLSimWeb.Logic.SimulatorSettings::SetHearingLoss` (state machine: presets and mode switch), `::setPredictedHearingLoss`, `::getParameters`, `::ResetPrediction` (defaults), `::setBand`, `::SyncBands`.
  - `HLSimWeb.Components.SignalNoiseRatio::SNRatioChanged` (gain mapping).
  - `HLSimWeb.Pages.Main::BuildRenderTree` (UI ranges and initial sound selection) and `TrackFiles::.cctor`.
- `hlsim.js` (Web Audio graph, filters, Q values, analyser, drawing) was read directly.
- The Python reference in Section 14 reproduces the IL arithmetic, including float32 narrowing points, and was cross-checked against a straightforward double-precision version of the formulas (59 of 235,200 band values differ by exactly 1 dB, as noted in Section 5.2).
- The C# in Section 5.5 and 7.3 was written against that specification. **It has not been compiled or run** in this environment, so treat it as a careful draft and rely on the tests above.

---

## 14. Appendix: Python reference (float32-faithful) for cross-checking

```python
"""Float32-faithful Python port of HLSimWeb.Logic.ANSIS344.ApplyANSI (NIOSH HLSim v4.0.29.0).
Requires numpy. Use to generate regression vectors for the Unity tests."""
import math
import numpy as np
f32 = np.float32

U   = [-0.033,-0.033,-0.033,-0.02,-0.0325,-0.045,0.012,0.025,0.019,0.019]
V   = [0.11,0.11,0.11,0.07,0.068,0.066,0.037,0.025,0.024,0.024]
L0  = [93,93,93,89,84,80,77,75,77,77]
XU  = [0.044,0.044,0.044,0.022,0.0265,0.031,0.007,0.005,0.013,0.013]
YU  = [0.016,0.016,0.016,0.016,0.007,-0.002,0.016,0.009,0.008,0.008]
XI  = [0.033,0.033,0.033,0.02,0.07,0.016,0.029,0.016,0.028,0.028]
YI  = [0.002,0.002,0.002,0.0,0.0,0.0,-0.01,-0.002,-0.007,-0.007]
A   = [[0.003,0.003],[0.003,0.003],[0.0035,0.0035],[0.004,0.004],[0.0055,0.005],
       [0.007,0.006],[0.0115,0.0075],[0.016,0.009],[0.018,0.012],[0.022,0.015]]
BU  = [[7.23,6.67],[6.67,6.12],[6.12,6.12],[6.12,6.12],[6.67,6.67],
       [7.23,6.67],[7.78,7.23],[8.34,7.78],[9.45,8.9],[10.56,10.56]]
BI  = [[5.78,5.34],[5.34,4.89],[4.89,4.89],[4.89,4.89],[5.34,5.34],
       [5.78,5.34],[6.23,5.78],[6.67,6.23],[7.56,7.12],[8.45,8.45]]
K10, K25 = 1.282, 0.675   # doubles

def apply_ansi(use_age, male, age, use_noise, years, level, fractile):
    sex = 0 if male else 1
    if not use_age: age = 18
    if not use_noise: years, level = 0, 85
    n50=[0]*10; n10=[0]*10; n25=[0]*10; n75=[0]*10; n90=[0]*10
    z50=[0]*10; z10=[0]*10; z25=[0]*10; z75=[0]*10; z90=[0]*10
    for i in range(10):
        a=float(f32(A[i][sex])); bu=float(f32(BU[i][sex])); bi=float(f32(BI[i][sex]))
        n50[i]=f32(a*float(age-18)**2)
        n10[i]=f32(float(n50[i])+K10*(bu+0.445*float(n50[i])))
        n25[i]=f32(float(n50[i])+K25*(bu+0.445*float(n50[i])))
        n75[i]=f32(float(n50[i])-K25*(bi+0.356*float(n50[i])))
        n90[i]=f32(float(n50[i])-K10*(bi+0.356*float(n50[i])))
        if years>0:
            l1=f32(math.log10(years))
            l2=f32(math.log10(years+1)/math.log10(11.0))
            l0=f32(0) if f32(level)<f32(L0[i]) else f32(f32(level)-f32(L0[i]))
            p=float(l0)**2
            if 1<=years<=9:
                z50[i]=f32(float(f32(l2*f32(f32(U[i])+f32(l1*f32(V[i])))))*p)
            elif years>=10:
                z50[i]=f32(float(f32(f32(U[i])+f32(l1*f32(V[i]))))*p)
            du=f32(float(f32(f32(XU[i])+f32(f32(YU[i])*l1)))*p)
            di=f32(float(f32(f32(XI[i])+f32(f32(YI[i])*l1)))*p)
            z10[i]=f32(float(z50[i])+K10*float(du)); z25[i]=f32(float(z50[i])+K25*float(du))
            z75[i]=f32(float(z50[i])-K25*float(di)); z90[i]=f32(float(z50[i])-K10*float(di))
    nl,nz={0.5:(n50,z50),0.1:(n10,z10),0.9:(n90,z90),0.25:(n25,z25)}.get(fractile,(n75,z75))
    out=[]
    for i in range(10):
        if use_noise and use_age:
            t=f32(f32(nl[i]+nz[i])-f32(f32(nl[i]*nz[i])/f32(120.0))); s=-int(round(float(t)))
        elif use_noise: s=-int(round(float(nz[i])))
        elif use_age:   s=-int(round(float(nl[i])))
        else:           s=0
        s=max(-80.0,min(12.0,float(s)))
        out.append(int(round(s)))
    return out

if __name__ == '__main__':
    # Defaults: UseAge, Male, Age 55, UseEffects, 25 years, 95 dB(A), fractile 0.5
    print(apply_ansi(True, True, 55, True, 25, 95, 0.5))
    # -> [-5, -5, -5, -8, -15, -19, -34, -42, -38, -43]
```
