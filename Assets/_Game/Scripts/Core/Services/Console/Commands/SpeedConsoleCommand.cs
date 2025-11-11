#if GAME_SING

using System;
using UnityEngine;
using static ConsoleCommand;

namespace Sing
{
    public sealed class SpeedConsoleCommand : ConsoleCommand
    {
        public SpeedConsoleCommand()
        {
            ArgumentsInfo = new ArgumentInfo[] { };
        }

        public override void Execute(object[] Args)
        {
            GameManager.Instance.Player.Owner.TogglePlayerDebugSpeed();
        }
    }
}

#endif
