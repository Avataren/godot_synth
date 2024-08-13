using System;
using Godot;
public class PhaseTransformer
{
    // Sine transformation of phase
    public static float SineTransform(float phase, float strength)
    {
        // Ensure phase is within [0, 1]
        if (phase < 0 || phase > 1) throw new ArgumentOutOfRangeException(nameof(phase), "Phase must be between 0 and 1.");
        if (strength < 0 || strength > 1) throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be between 0 and 1.");

        // Apply sine transformation to the phase
        float transformedPhase = (Mathf.Sin(2.0f * Mathf.Pi * phase) + 1.0f) / 2.0f;
        return (1.0f - strength) * phase + strength * transformedPhase;
    }

    // Exponential transformation of phase
    public static float ExponentialTransform(float phase, float strength)
    {
        // Ensure phase is within [0, 1]
        if (phase < 0 || phase > 1) throw new ArgumentOutOfRangeException(nameof(phase), "Phase must be between 0 and 1.");
        if (strength < 0 || strength > 1) throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be between 0 and 1.");

        // Apply exponential transformation to the phase
        float transformedPhase = (float)Math.Pow(phase, 2);
        return (1.0f - strength) * phase + strength * transformedPhase;
    }

    // Logarithmic transformation of phase
    public static float LogarithmicTransform(float phase, float strength)
    {
        // Ensure phase is within [0, 1]
        if (phase < 0 || phase > 1) throw new ArgumentOutOfRangeException(nameof(phase), "Phase must be between 0 and 1.");
        if (strength < 0 || strength > 1) throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be between 0 and 1.");

        // Apply logarithmic transformation to the phase
        float transformedPhase = Mathf.Log(phase + 1.0f) / Mathf.Log(2.0f); // Log base 2 for [0, 1] range
        return (1.0f - strength) * phase + strength * transformedPhase;
    }

    // Wavefolding transformation of phase
    public static float WavefoldingTransform(float phase, float strength)
    {
        // Ensure phase is within [0, 1]
        if (phase < 0 || phase > 1) throw new ArgumentOutOfRangeException(nameof(phase), "Phase must be between 0 and 1.");
        if (strength < 0 || strength > 1) throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be between 0 and 1.");

        // Apply wavefolding transformation to the phase
        float transformedPhase = 2.0f * phase;
        if (transformedPhase > 1.0f) transformedPhase = 2.0f - transformedPhase; // Fold at 1

        return (1.0f - strength) * phase + strength * transformedPhase;
    }

    // Hard Clip transformation of phase
    public static float HardClipTransform(float phase, float strength)
    {
        // Ensure phase is within [0, 1]
        if (phase < 0 || phase > 1) throw new ArgumentOutOfRangeException(nameof(phase), "Phase must be between 0 and 1.");
        if (strength < 0 || strength > 1) throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be between 0 and 1.");

        // Apply hard clipping transformation to the phase
        float transformedPhase = Mathf.Clamp(2.0f * phase - 1.0f, -1.0f, 1.0f) / 2.0f + 0.5f; // Clip to [-0.5, 0.5], then map back to [0, 1]
        return (1.0f - strength) * phase + strength * transformedPhase;
    }
}