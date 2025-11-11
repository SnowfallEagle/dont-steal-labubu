#if GAME_COOK

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TMPro;

namespace Cook
{
    public enum EShopElementInteraction : int
    {
        Buy,
        Sell,
        Speed
    }

    public enum EShopElementRarity : int
    {
        Common,
        Uncommon,
        Epic
    }

    public class UIShopElement : MonoBehaviour
    {
        [Header("Element")]
        [SerializeField] private EShopElementInteraction m_Interaction = EShopElementInteraction.Buy;

        [SerializeField] private string m_ItemId = Item.NoneId;
        public string ItemId => m_ItemId;

        [Header("References")]
        private Button m_Button;
        [SerializeField] private Image m_Icon;
        [SerializeField] private TextMeshProUGUI m_NameText;
        [SerializeField] private TextMeshProUGUI m_RarityText;
        [SerializeField] private TextMeshProUGUI m_MoneyText;

        [Header("Speed")]
        public Sprite Icon;
        public float SpeedUpgradeAmount = 0.25f;
        public double BasePriceToBuy = 150f;

        public double ComputePrice()
        {
            double Price = BasePriceToBuy;

            for (int i = 0; i < CookManager.Instance.PlayerSpeedLevel - 1; ++i)
            {
                Price *= 1.5f;
            }

            return Item.RoundBigNumber(Price);
        }

        public void OnInteract()
        {
            var Player = GameManager.Instance.Player;

            if (Player.WithdrawMoney(ComputePrice()) < 0f)
            {
                return;
            }

            CookManager.Instance.PlayerSpeedLevel++;
            CookManager.Instance.UpdatePlayerSpeed();
            UpdateUI();
            CookManager.Instance.SaveGameFully();
        }

        public void Init()
        {
            Assert.IsNotNull(m_NameText);
            Assert.IsNotNull(m_RarityText);
            Assert.IsNotNull(m_MoneyText);

            m_Button = GetComponent<Button>();
            Assert.IsNotNull(m_Button);

            LocalizationManager.Instance.OnRefresh += UpdateUI;
        }

        public void UpdateUI()
        {
            bool HasEnoughMoney = GameManager.Instance.Player.Money - ComputePrice() >= 0f;

            m_Button.interactable = HasEnoughMoney;

            if (Icon != null)
            {
                m_Icon.sprite = Icon;
            }

            // Set texts
            m_NameText.text = LocalizationManager.Instance.GetTranslation("shop_speed_multiplier");
            m_RarityText.text = $"x{(1f + 0.25f * (CookManager.Instance.PlayerSpeedLevel - 1)):F2} > x{(1f + 0.25f * (CookManager.Instance.PlayerSpeedLevel)):F2}";
            m_RarityText.color = RarityToColor(EShopElementRarity.Common);

            m_MoneyText.text   = $"${ComputePrice():F0}";
            m_MoneyText.color  = HasEnoughMoney ? Color.green : Color.red;
        }

        public static string RarityToText(EShopElementRarity Rarity)
        {
            switch (Rarity)
            {
                case EShopElementRarity.Common:   return LocalizationManager.Instance.GetTranslation("ui_shop_element_rarity_common");
                case EShopElementRarity.Uncommon: return LocalizationManager.Instance.GetTranslation("ui_shop_element_rarity_uncommon");
                case EShopElementRarity.Epic:     return LocalizationManager.Instance.GetTranslation("ui_shop_element_rarity_epic");
                default:                          return LocalizationManager.Instance.GetTranslation("error");
            }
        }

        public static Color RarityToColor(EShopElementRarity Rarity)
        {
            switch (Rarity)
            {
                case EShopElementRarity.Common:   return Color.white;
                case EShopElementRarity.Uncommon: return new Color(0f, 120f, 0f);
                case EShopElementRarity.Epic:     return new Color(150f, 0f, 150f);
                default:                          return Color.white;
            }
        }
    }
}

#endif
