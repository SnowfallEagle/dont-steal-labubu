using System;
using UnityEngine;
using static ConsoleCommand;

public sealed class SomeMoneyConsoleCommand : ConsoleCommand
{
    public SomeMoneyConsoleCommand()
    {
        ArgumentsInfo = new ArgumentInfo[] { };
    }

    public override void Execute(object[] Args)
    {
#if GAME_COOK
        GameManager.Instance.Player.AddMoney(500000);
#else
        GameManager.Instance.Player.AddMoney(75000000f);
#endif
    }
}

