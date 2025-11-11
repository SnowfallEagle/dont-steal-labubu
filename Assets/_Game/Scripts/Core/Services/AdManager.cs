using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using MirraGames.SDK;
using MirraGames.SDK.MirraWeb;

public class AdManager : Singleton<AdManager>
{
    [Header("Sticky")]
    private Coroutine TryEnableBannerCoroutine;

    [Header("Inter")]
    public double InterTimer = 62f;
    public double LastCloseInterTime = 0f;
    [NonSerialized] public bool TurnOffAdsInappBought = false;

    [Header("Rewarded")]
    public string RewardedId = string.Empty;

    public void SetSticky(bool Enable)
    {
        if (!MirraSDK.Ads.IsAdsAvailable || !MirraSDK.Ads.IsBannerAvailable)
        {
            return;
        }

        if (Enable && !TurnOffAdsInappBought)
        {
            if (TryEnableBannerCoroutine == null)
            {
                TryEnableBannerCoroutine = StartCoroutine(TryEnableSticky());
            }
        }
        else
        {
            if (TryEnableBannerCoroutine != null)
            {
                StopCoroutine(TryEnableBannerCoroutine);
                TryEnableBannerCoroutine = null;
            }

            MirraSDK.Ads.DisableBanner();
        }
    }

    private IEnumerator TryEnableSticky()
    {
        for (;;)
        {
            if (TurnOffAdsInappBought)
            {
                TryEnableBannerCoroutine = null;
                break;
            }

            if (MirraSDK.Ads.IsBannerReady)
            {
                MirraSDK.Ads.InvokeBanner();
                TryEnableBannerCoroutine = null;
                break;
            }

            yield return new WaitForSecondsRealtime(5f);
        }
    }

    // @returns: <= 0 on ready to show. Time left to show ad + some small error, so it's safe to use it couroutines that depend on it.
    public double GetTimeLeftToShowInter()
    {
        double NextTimeToShowInter = LastCloseInterTime + InterTimer;
        double TimeLeft = NextTimeToShowInter - Time.timeAsDouble;

        return TimeLeft;
    }

    public bool CanShowInter()
    {
        return
            !TurnOffAdsInappBought &&
            GetTimeLeftToShowInter() <= 0d &&

            MirraSDK.Ads.IsAdsAvailable &&
            MirraSDK.Ads.IsInterstitialAvailable &&
            MirraSDK.Ads.IsInterstitialReady &&
            !MirraSDK.Ads.IsInterstitialVisible;
    }

    // @param: If there's error - only OnError (not OnSuccess) will be called. Make sure there's code to continue game in both callbacks 
    public void ShowInter(Action OnOpen, Action OnSuccess, Action OnError)
    {
        Assert.IsTrue(OnOpen != null && OnSuccess != null && OnError != null);
        
        if (!CanShowInter())
        {
            OnError();
            return;
        }

        MirraSDK.Ads.InvokeInterstitial(OnOpen, (Success) =>
        {
            LastCloseInterTime = Time.timeAsDouble;

            if (Success)
            {
                OnSuccess();
                return;
            }

            OnError();
        });
    }

    public bool CanShowRewarded()
    {
        return
            MirraSDK.Ads.IsAdsAvailable &&
            MirraSDK.Ads.IsRewardedAvailable &&
            MirraSDK.Ads.IsRewardedReady &&
            !MirraSDK.Ads.IsInterstitialVisible &&
            !MirraSDK.Ads.IsRewardedVisible;
    }

    // @param: If there's error - only OnError (not OnClose) will be called. Make sure there's code to continue game in both callbacks 
    public void ShowRewarded(string Id, Action OnOpen, Action<bool> OnClose)
    {
        Assert.IsTrue(OnOpen != null && OnOpen != null && OnClose != null);

        if (!CanShowRewarded())
        {
            return;
        }

        RewardedId = Id;

        MirraSDK.Ads.InvokeRewarded(OnOpen, OnClose, Id);
    }
}
