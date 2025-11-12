using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using System.Collections;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class GameManager : Singleton<GameManager>
{
    private Player m_Player;
    public Player Player
    {
        get
        {
            if (m_Player == null)
            {
                m_Player = FindFirstObjectByType<Player>();
            }
            return m_Player;
        }
    }

#if UNITY_EDITOR
    public bool DebugMobile = false;
#endif
    public bool Mobile
    {
        get
        {
#if UNITY_EDITOR
            return DebugMobile;
#else
            return MirraSDK.IsInitialized ? MirraSDK.Device.IsMobile : false;
#endif
        }
    }
    public bool Desktop => !Mobile;

    public bool InappsAvailable => false;
    //public bool InappsAvailable => MirraSDK.IsInitialized && (MirraSDK.Payments.IsPaymentsAvailable && MirraSDK.Platform.Current != PlatformType.CrazyGames);

    private void Awake()
    {
        var Console = ConsoleService.Instance;
    }
}
