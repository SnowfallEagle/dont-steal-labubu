using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnumUtils
{
    public static bool Any<T>(T Flags) where T : System.Enum
    {
        return !Flags.Equals(default(T));
    }
}
