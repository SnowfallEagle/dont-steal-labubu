using MirraGames.SDK;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConsoleCommand;

public sealed class ResetConsoleCommand : ConsoleCommand
{
    public ResetConsoleCommand()
    {
        ArgumentsInfo = new ArgumentInfo[] { };
    }

    public override void Execute(object[] Args)
    {
        if (MirraSDK.IsInitialized)
        {
            MirraSDK.Data.DeleteAll();
            MirraSDK.Data.Save();

            if (MirraSDK.Analytics.IsGameplayReporterAvailable)
            {
                MirraSDK.Analytics.GameplayStop();
            }
        }

        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }
}

