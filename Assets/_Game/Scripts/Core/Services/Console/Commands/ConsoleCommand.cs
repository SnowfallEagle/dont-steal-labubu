using System;
using UnityEngine;

public abstract class ConsoleCommand
{
    public struct ArgumentInfo
    {
        public string Name;
        public Type Type;
        public object Default;
    }

    public ArgumentInfo[] ArgumentsInfo = new ArgumentInfo[0];

    public abstract void Execute(object[] Args);

    public void LogInfo(string CommandName)
    {
        string Info = ArgumentsInfo.Length > 0 ?
            $"{ CommandName }: " :
            $"{ CommandName }: no arguments";

        foreach (var Argument in ArgumentsInfo)
        {
            Info += Argument.Default != null ?
                $"<{ Argument.Name }: { Argument.Type.Name } = { Argument.Default }>" :
                $"<{ Argument.Name }: { Argument.Type.Name }> ";
        }

        Debug.Log(Info);
    }
}

