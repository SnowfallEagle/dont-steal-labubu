using System.Diagnostics;

public static class NotImplemented
{
    [Conditional("UNITY_ASSERTIONS")]
    public static void Assert(string Message = "")
    {
        UnityEngine.Assertions.Assert.IsTrue(false, $"Not implemented! { Message }");
    }
}

public static class NoEntry
{
    [Conditional("UNITY_ASSERTIONS")]
    public static void Assert(string Message = "")
    {
        UnityEngine.Assertions.Assert.IsTrue(false, $"No entry! { Message }");
    }
}

