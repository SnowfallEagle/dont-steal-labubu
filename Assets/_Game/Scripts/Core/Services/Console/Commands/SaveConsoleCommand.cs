using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConsoleCommand;

public sealed class SaveConsoleCommand : ConsoleCommand
{
    public SaveConsoleCommand()
    {
        ArgumentsInfo = new ArgumentInfo[] { };
    }

    public override void Execute(object[] Args)
    {
        Cook.CookManager.Instance.SaveGameFully();
    }
}

