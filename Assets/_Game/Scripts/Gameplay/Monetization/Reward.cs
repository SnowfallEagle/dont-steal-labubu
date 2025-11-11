using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TMPro;
using MirraGames.SDK;
using Cook;

[Serializable]
public struct RewardButton2D
{
    public Transform Wrapper;
    public Image RewardIcon;
    public TextMeshProUGUI CooldownText;
    public Button Button;
}

[Serializable]
public struct RewardButton3D
{
    public Transform Wrapper;
    public SpriteRenderer RewardIcon;
    public TextMeshPro NameText;
    public TextMeshPro CooldownText;
    public TextMeshPro ButtonTypeText;
}

public class Reward : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private string m_RewardId = "reward_id";
    public string RewardId => m_RewardId;

    private bool m_Activated = false;
    public bool Activated => m_Activated;

    public bool PlayerHere = false;

    [Header("Cooldown")]
    [SerializeField] private float m_CooldownTime = 120f;
    private float m_CooldownLeft = 0f;

    [Header("Progress")]
    private float m_Progress = 0f;
    public float Progress => m_Progress;

    public float ProgressSpeed = 0.5f;

    [Header("UI")]
    public RewardButton2D Button2D;
    public RewardButton3D Button3D;

    public void OnInteract()
    {
        if (m_Activated)
        {
            return;
        }

        Cook.CookManager.Instance.ShowRewarded(m_RewardId);
    }

    public void OnRewardActivated()
    {
        if (Activated)
        {
            return;
        }

        m_CooldownLeft = m_CooldownTime;
        m_Activated = true;
        UpdateVisuals();
    }

    public void OnRewardDeactivated()
    {
        if (!Activated)
        {
            return;
        }

        m_CooldownLeft = 0f;
        m_Activated = false;
        UpdateVisuals();
    }

    public void OnPlayerEnter()
    {
        if (Activated)
        {
            return;
        }

        m_Progress = 0f;
        PlayerHere = true;

        // Debug.Log("Player Here");
    }

    public void OnPlayerExit()
    {
        if (Activated)
        {
            return;
        }

        m_Progress = 0f;
        PlayerHere = false;

        CookManager.Instance.RewardProgressWrapper.gameObject.SetActive(false);

        // Debug.Log("Player Not Here");
    }

    public void UpdateVisuals()
    {
        if (MirraSDK.IsInitialized && MirraSDK.Ads.IsRewardedAvailable)
        {
            Button2D.Wrapper.gameObject.SetActive(true);
            Button3D.Wrapper.gameObject.SetActive(true);
        }
        else
        {
            Button2D.Wrapper.gameObject.SetActive(false);
            Button3D.Wrapper.gameObject.SetActive(false);
        }

        ToggleVisuals(!m_Activated);
    }

    private void ToggleVisuals(bool Toggle)
    {
        float Alpha = Toggle ? 1f : 0.5f;
        float Color = Alpha;

        Button2D.Button.interactable = Toggle;
        Button2D.RewardIcon.color = new Color(Color, Color, Color, Alpha);

        Button3D.NameText.alpha = Alpha;
        Button3D.RewardIcon.color = new Color(Color, Color, Color, Alpha);
        Button3D.ButtonTypeText.enabled = Toggle;

        UpdateCooldownText();
    }

    private void UpdateCooldownText()
    {
        string Text = string.Empty;

        if (m_Activated)
        {
            m_CooldownLeft = Mathf.Max(m_CooldownLeft, 0f);

            int Minutes = (int)(m_CooldownLeft / 60f);
            int Seconds = (int)(m_CooldownLeft) % 60;

            Text = $"{Minutes:D2}:{Seconds:D2}";
        }
        else
        {
            int Minutes = (int)((m_CooldownTime / 60f) + 0.5f);

            Text = $"{Minutes} {LocalizationManager.Instance.GetTranslation("ui_minutes_short")}";
        }

        Button2D.CooldownText.text = Text;
        Button3D.CooldownText.text = Text;
    }

    private void Update()
    {
        if (PlayerHere)
        {
            m_Progress += Time.deltaTime * ProgressSpeed;
            m_Progress = Mathf.Clamp01(m_Progress);

            Cook.CookManager.Instance.RewardProgressImage.fillAmount = m_Progress;
            Cook.CookManager.Instance.RewardProgressWrapper.gameObject.SetActive(true);

            if (m_Progress >= 1f)
            {
                m_Progress = 0f;
                PlayerHere = false;

                Cook.CookManager.Instance.RewardProgressImage.fillAmount = 0f;
                Cook.CookManager.Instance.RewardProgressWrapper.gameObject.SetActive(false);

                Cook.CookManager.Instance.ShowRewarded(m_RewardId);
            }
        }

        if (!m_Activated)
        {
            return;
        }

        m_CooldownLeft -= Time.deltaTime;

        UpdateCooldownText();

        if (m_CooldownLeft <= 0f)
        {
            Cook.CookManager.Instance.DeactivateReward(this);
        }
    }

    private void OnLocalizationRefresh()
    {
        Button3D.NameText.text = LocalizationManager.Instance.GetTranslation(RewardId);
        UpdateVisuals();
    }

    private void Awake()
    {
        Assert.IsNotNull(Button2D.Wrapper);
        Assert.IsNotNull(Button2D.CooldownText);
        Assert.IsNotNull(Button2D.Button);
        Assert.IsNotNull(Button2D.RewardIcon);

        Assert.IsNotNull(Button3D.Wrapper);
        Assert.IsNotNull(Button3D.RewardIcon);
        Assert.IsNotNull(Button3D.NameText);
        Assert.IsNotNull(Button3D.CooldownText);
        Assert.IsNotNull(Button3D.ButtonTypeText);

        OnLocalizationRefresh();
        LocalizationManager.Instance.OnRefresh += OnLocalizationRefresh;

        UpdateVisuals();
    }
}
