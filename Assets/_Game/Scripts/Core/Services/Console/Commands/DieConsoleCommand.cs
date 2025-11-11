using System;
using UnityEngine;

public sealed class DieConsoleCommand : ConsoleCommand
{
    public DieConsoleCommand()
    {
        ArgumentsInfo = new ArgumentInfo[] { };
    }

    public override void Execute(object[] Args)
    {
    }
}

