//#define USE_FLOAT  // Comment this out to use double instead

#if USE_FLOAT
global using SynthType = System.Single;
using Godot;

public static class SynthTypeHelper
{
    public static SynthType ModuloOne(SynthType val)
    {
        return (val + 100.0f) % 1.0f;
    }
    public const SynthType Zero = 0.0f;
    public const SynthType One = 1.0f;
    public const SynthType Two = 2.0f;
    public const SynthType Half = 0.5f;
    public const SynthType Pi = Mathf.Pi;
}
#else
global using SynthType = System.Double;
using System;
public static class SynthTypeHelper
{
    public static SynthType ModuloOne(SynthType val)
    {
        return (val + 100.0) % 1.0;
    }
    public const SynthType Zero = 0.0;
    public const SynthType One = 1.0;
    public const SynthType Two = 2.0;
    public const SynthType Half = 0.5;
    public const SynthType Pi = Math.PI;

}
#endif



