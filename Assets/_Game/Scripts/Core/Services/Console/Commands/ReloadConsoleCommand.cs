using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConsoleCommand;
using MirraGames.SDK;

public sealed class ReloadConsoleCommand : ConsoleCommand
{
    public ReloadConsoleCommand()
    {
        ArgumentsInfo = new ArgumentInfo[] { };
    }

    public override void Execute(object[] Args)
    {
        if (MirraSDK.IsInitialized && MirraSDK.Analytics.IsGameplayReporterAvailable)
        {
            MirraSDK.Analytics.GameplayStop();
        }

        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }
}

