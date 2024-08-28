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
    public static SynthType Zero = 0.0f;
    public static SynthType One = 1.0f;
    public static SynthType NegativeOne = 1.0f;
    public static SynthType Half = 0.5f;
    public static SynthType Clamp(SynthType val, SynthType min, SynthType max)
    {
        return Mathf.Max(min, Mathf.Min(max, val));
    }    
    public static SynthType Max (SynthType a, SynthType b)
    {
        return Mathf.Max(a, b);
    }
    public static SynthType Min (SynthType a, SynthType b)
    {
        return Mathf.Min(a, b);
    }
    public static SynthType Exp(SynthType val)
    {
        return Mathf.Exp(val);
    }
    public static SynthType Pow(SynthType val, SynthType exp)
    {
        return Mathf.Pow(val, exp);
    }
    public static SynthType Abs(SynthType val)
    {
        return Mathf.Abs(val);
    }
    public static SynthType Sin(SynthType val)
    {
        return Mathf.Sin(val);
    }
    public static SynthType Cos(SynthType val)
    {
        return Mathf.Cos(val);
    }
    public static SynthType Sqrt(SynthType val)
    {
        return Mathf.Sqrt(val);
    }
    public static SynthType Log(SynthType val)
    {
        return Mathf.Log(val);
    }
    public static SynthType Pi = Mathf.Pi;
}
#else
global using SynthType = System.Double;
using System;
using System.Reflection.Emit;
public static class SynthTypeHelper
{
    public static SynthType ModuloOne(SynthType val)
    {
        return (val + 100.0) % 1.0;
    }
    public static SynthType Zero = 0.0;
    public static SynthType One = 1.0;
    public static SynthType NegativeOne = -1.0;
    public static SynthType Half = 0.5;
    public static SynthType Clamp(SynthType val, SynthType min, SynthType max)
    {
        return Math.Max(min, Math.Min(max, val));
    }
    public static SynthType Max(SynthType a, SynthType b)
    {
        return Math.Max(a, b);
    }
    public static SynthType Min(SynthType a, SynthType b)
    {
        return Math.Min(a, b);
    }
    public static SynthType Exp(SynthType val)
    {
        return Math.Exp(val);
    }
    public static SynthType Pow(SynthType val, SynthType exp)
    {
        return Math.Pow(val, exp);
    }
    public static SynthType Abs(SynthType val)
    {
        return Math.Abs(val);
    }
    public static SynthType Sin(SynthType val)
    {
        return Math.Sin(val);
    }
    public static SynthType Cos(SynthType val)
    {
        return Math.Cos(val);
    }
    public static SynthType Sqrt(SynthType val)
    {
        return Math.Sqrt(val);
    }
    public static SynthType Log(SynthType val)
    {
        return Math.Log(val);
    }
    public static SynthType Pi = Math.PI;

}
#endif


