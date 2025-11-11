#if GAME_SING

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TMPro;

namespace Sing
{
    public enum ESkinBuyingStandState
    {
        CanBuy,
        CantBuy,
        Bought
    }

    public class SkinBuyingStand : MonoBehaviour
    {
        [Header("Skin")]
        [SerializeField] private string m_SkinId = "skin_unknown";
        public string SkinId => m_SkinId;
        [SerializeField] private double m_Cost = 100f;
        public double Cost => m_Cost;

        [SerializeField] private Sprite m_UISprite;
        public Sprite UISprite => m_UISprite;

        [Header("References")]
        [SerializeField] private TextMeshPro m_SkinNameText;
        [SerializeField] private TextMeshPro m_MoneyMultiplierText;
        [SerializeField] private TextMeshPro m_SpeedText;
        [SerializeField] private TextMeshPro m_PriceText;
        [SerializeField] private Transform m_PriceToHideTransform;

        [SerializeField] private MeshRenderer m_ButtonMesh;
        public MeshRenderer ButtonMesh => m_ButtonMesh;

        [Header("Materials")]
        [SerializeField] private Material m_ButtonMaterialCanBuy;
        [SerializeField] private Material m_ButtonMaterialCantBuy;
        [SerializeField] private Material m_ButtonMaterialBought;

        private ESkinBuyingStandState m_State = ESkinBuyingStandState.CantBuy;
        public ESkinBuyingStandState State => m_State;

        public void Init()
        {
            OnLocalizationRefresh();
        }

        public void OnMoneyChanged()
        {
            if (m_State == ESkinBuyingStandState.Bought)
            {
                return;
            }

            SetState(GameManager.Instance.Player.Money >= m_Cost ? ESkinBuyingStandState.CanBuy : ESkinBuyingStandState.CantBuy);
        }

        public void OnInteract()
        {
            var Player = GameManager.Instance.Player;
            var PlayerController = Player.Owner;

            switch (m_State)
            {
                case ESkinBuyingStandState.CanBuy:
                    if (Player.WithdrawMoney(m_Cost) > 0f)
                    {
                        break;
                    }

                    UnlockSkinAndStand();
                    PlayerController.ChangeSkin(m_SkinId);

                    SoundController.Instance.Play("SkinChange");
                    SingManager.Instance.PlayPlayerCoinsVFX();

                    SingManager.Instance.SaveGameFully();
                    break;

                case ESkinBuyingStandState.CantBuy:
                    SingManager.Instance.Notify("ui_notify_not_enough_money", 2f, Color.red, true, "NegativeClick");
                    break;

                case ESkinBuyingStandState.Bought:
                    PlayerController.ChangeSkin(m_SkinId);
                    SoundController.Instance.Play("SkinChange");
                    break;

                default:
                    break;
            }
        }

        public void UnlockSkinAndStand()
        {
            GameManager.Instance.Player.Owner.UnlockSkin(m_SkinId);
            SetState(ESkinBuyingStandState.Bought);

            SingManager.Instance.OnPlayerUnlockedSkin?.Invoke();
        }

        public void LockSkinAndStand(bool CanBuy = false)
        {
            GameManager.Instance.Player.Owner.LockSkin(m_SkinId);
            SetState(CanBuy ? ESkinBuyingStandState.CanBuy : ESkinBuyingStandState.CantBuy);

            SingManager.Instance.OnPlayerLockedSkin?.Invoke();
        }

        private void SetState(ESkinBuyingStandState State)
        {
            m_State = State;

            switch (m_State)
            {
                case ESkinBuyingStandState.CanBuy:
                    SetMaterial(m_ButtonMaterialCanBuy);
                    m_PriceText.color = Color.green;
                    break;

                case ESkinBuyingStandState.CantBuy:
                    SetMaterial(m_ButtonMaterialCantBuy);
                    m_PriceText.color = Color.red;
                    break;

                case ESkinBuyingStandState.Bought:
                    SetMaterial(m_ButtonMaterialBought);
                    m_PriceToHideTransform.gameObject.SetActive(false);
                    break;

                default:
                    break;
            }
        }

        private void SetMaterial(Material Material)
        {
            List<Material> Materials = new();
            m_ButtonMesh.GetMaterials(Materials);

            if (Materials.Count >= 2)
            {
                Materials[1] = Material;
            }

            m_ButtonMesh.SetMaterials(Materials);
        }

        private void OnLocalizationRefresh()
        {
            float MoneyMultiplier = 1f;
            float SpeedMultiplier = 1f;

            if (GameManager.Instance.Player.Owner != null)
            {
                var Skin = GameManager.Instance.Player.Owner.GetSkin(m_SkinId);
                if (Skin != null)
                {
                    MoneyMultiplier = Skin.MoneyMultiplier;
                    SpeedMultiplier = Skin.Speed;
                }
            }

            m_SkinNameText.text = LocalizationManager.Instance.GetTranslation(m_SkinId);
            m_MoneyMultiplierText.text = $"x{MoneyMultiplier:F0}";
            m_SpeedText.text = $"{(SpeedMultiplier*10f):F0} {LocalizationManager.Instance.GetTranslation("ui_skin_stand_speed")}";
            m_PriceText.text = $"{m_Cost:F0}";
        }

        private void Awake()
        {
            Assert.IsNotNull(m_SkinNameText);
            Assert.IsNotNull(m_MoneyMultiplierText);
            Assert.IsNotNull(m_SpeedText);
            Assert.IsNotNull(m_PriceText);
            Assert.IsNotNull(m_PriceToHideTransform);

            Assert.IsNotNull(m_UISprite);

            Assert.IsNotNull(m_ButtonMesh);
            Assert.IsNotNull(m_ButtonMaterialCanBuy);
            Assert.IsNotNull(m_ButtonMaterialCantBuy);
            Assert.IsNotNull(m_ButtonMaterialBought);

            GameManager.Instance.Player.OnMoneyChanged += OnMoneyChanged;

            LocalizationManager.Instance.OnRefresh += OnLocalizationRefresh;
            OnLocalizationRefresh();
        }
    }
}

#endif
