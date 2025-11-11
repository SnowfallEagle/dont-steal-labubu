using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

#if GAME_COOK

namespace Cook
{
    public class UIInventorySlot : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private TextMeshProUGUI m_DigitInput;
        [SerializeField] private TextMeshProUGUI m_Text;
        [SerializeField] private Image m_Background;
        public Image Background => m_Background;

        [SerializeField] private Color m_EquippedColor = Color.magenta;
        [SerializeField] private Color m_DefaultColor = Color.black; 

        private int m_Slot = InventorySlot.NoneIdx;
        private Inventory m_Inventory;

        private void Awake()
        {
            Assert.IsNotNull(m_Icon);
            Assert.IsNotNull(m_DigitInput);
            Assert.IsNotNull(m_Text);
            Assert.IsNotNull(m_Background);
        }

        public void UpdateUI(in InventorySlot Slot)
        {
            m_Slot = Slot.Slot;
            m_Inventory = Slot.Inventory;

            if (!Slot.Item)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            m_Icon.sprite = Slot.Item.Icon;

            m_DigitInput.text = $"{(Slot.Slot < 9 ? Slot.Slot + 1 : 0)}";

            if (Slot.Count > 1)
            {
                m_Text.text = $"[x{Slot.Count}]";
            }
            else if (Slot.Item.Type == EItemType.Food && Slot.Item.Food.Type == EFoodType.Taken)
            {
                m_Text.text = $"[{Slot.Item.Food.WeightKg:F2} {LocalizationManager.Instance.GetTranslation("ui_inventory_slot_kg")}]";
            }
            else
            {
                m_Text.text = "";
            }

            m_Background.color = Slot.Slot == Slot.Inventory.EquippedSlot ? m_EquippedColor : m_DefaultColor;
        }

        private void Update()
        {
            if (m_DigitInput != null)
            {
                m_DigitInput.gameObject.SetActive(GameManager.Instance.Desktop);
            }
        }

        public void OnPressed()
        {
            if (m_Inventory != null && Inventory.IsValidSlot(m_Slot))
            {
                m_Inventory.Equip(m_Slot);
            }
        }
    }
}

#endif
