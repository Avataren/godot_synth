using System;
public class Undenormaliser
{
    public static void Undenormalise(ref float sample)
    {
        if ((BitConverter.ToUInt32(BitConverter.GetBytes(sample), 0) & 0x7f800000) == 0)
        {
            sample = 0.0f;
        }
    }    
}