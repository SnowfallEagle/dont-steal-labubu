using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;
using MirraGames.SDK;
using Cook;

[Serializable]
public struct InappButton2D
{
    public Transform Wrapper;

    public TextMeshProUGUI MoneyText;
    public Button Button;
}

[Serializable]
public struct InappButton3D
{
    public Transform Wrapper;

    public TextMeshPro NameText;
    public TextMeshPro MoneyText;

    public SpriteRenderer InappIcon;
}

public class Inapp : MonoBehaviour
{
    [SerializeField] private string m_InappId = "Unknown Inapp Id";
    public string InappId => m_InappId;

    private bool m_Activated = false;
    public bool Activated => m_Activated;

    [Header("UI")]
    public InappButton2D Button2D;
    public InappButton3D Button3D;

    public void OnInteract()
    {
        if (m_Activated)
        {
            return;
        }

        if (!Cook.CookManager.Instance.Authorized || !Cook.CookManager.Instance.GameLoaded)
        {
            return;
        }

        if (MirraSDK.Payments.GetProductData(m_InappId) != null)
        {
            MirraSDK.Payments.Purchase(m_InappId,
                // Success
                () =>
                {
                    Debug.Log($"INAPP: Success purchase {m_InappId}");
                    Cook.CookManager.Instance.OnInappPaymentSuccess(m_InappId);
                },

                // Fail
                () =>
                {
                    Debug.Log($"INAPP: Fail purchase {m_InappId}");
                    Cook.CookManager.Instance.OnInappPaymentFail(m_InappId);
                });
        }
    }

    public void OnInappActivated()
    {
        if (Activated)
        {
            return;
        }

        Button2D.Button.interactable = false;
        Button2D.MoneyText.enabled = false;
        Button3D.MoneyText.enabled = false;

        const float Alpha = 0.5f;
        Button3D.NameText.alpha = Alpha;
        Button3D.InappIcon.color = new Color(1f, 1f, 1f, Alpha);

        m_Activated = true;
        SetVisibility(false);

        Debug.Log($"INAPP {InappId}: {nameof(OnInappActivated)}");
    }

    public void RefreshData()
    {
        var Inapp = MirraSDK.Payments.GetProductData(m_InappId);
        if (Inapp == null)
        {
            SetVisibility(false);
            return;
        }

        string Price = Inapp.GetFullPriceInteger();
        Button2D.MoneyText.text = Price;
        Button3D.MoneyText.text = Price;

        SetVisibility(!m_Activated && Cook.CookManager.Instance.Authorized && GameManager.Instance.InappsAvailable);
    }

    private void Awake()
    {
        Assert.IsNotNull(Button2D.Wrapper);
        Assert.IsNotNull(Button2D.MoneyText);
        Assert.IsNotNull(Button2D.Button);

        Assert.IsNotNull(Button3D.Wrapper);
        Assert.IsNotNull(Button3D.InappIcon);
        Assert.IsNotNull(Button3D.NameText);
        Assert.IsNotNull(Button3D.MoneyText);

        OnLocalizationRefresh();
        LocalizationManager.Instance.OnRefresh += OnLocalizationRefresh;

        SetVisibility(false);
    }

    private void OnLocalizationRefresh()
    {
        Button3D.NameText.text = LocalizationManager.Instance.GetTranslation(InappId);
    }

    private void SetVisibility(bool Visible)
    {
        Button2D.Wrapper.gameObject.SetActive(Visible);
        Button3D.Wrapper.gameObject.SetActive(Visible);
    }
}
