using MirraGames.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

#if GAME_COOK

namespace Cook
{
    public class UIShopMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform m_PanelTransform;
        [SerializeField] private RectTransform m_ContentTransform;

        // It's kinda stupid, but we update Items from UIShopMenu...
        // Better version would be having some Design key/value table and add UIShopElements automatically
        [SerializeField] private List<ItemBalanceData> m_BalanceList = new();
        public List<ItemBalanceData> BalanceList => m_BalanceList;
        private List<UIShopElement> m_ChildElements = new();
        public List<UIShopElement> ChildElements => m_ChildElements;

        public void OnOpenMenu()
        {
            if (!CookManager.Instance.GameLoaded)
            {
                return;
            }

            if (!m_PanelTransform.gameObject.activeSelf)
            {
                m_PanelTransform.gameObject.SetActive(true);

                if (MirraSDK.Analytics.IsGameplayReporterAvailable)
                {
                    MirraSDK.Analytics.GameplayStop();
                }

                SoundController.Instance.Play("PositiveClick");
                
                // Show cursor when opening shop menu
                InputGame.Instance.SetShopMenuState(true);
            }
        }

        public void OnCloseMenu()
        {
            if (!CookManager.Instance.GameLoaded)
            {
                return;
            }

            if (m_PanelTransform.gameObject.activeSelf)
            {
                m_PanelTransform.gameObject.SetActive(false);

                if (MirraSDK.Analytics.IsGameplayReporterAvailable)
                {
                    MirraSDK.Analytics.GameplayStart();
                }
                
                // Hide cursor when closing shop menu
                InputGame.Instance.SetShopMenuState(false);
            }
        }

        public void Init()
        {
            Assert.IsNotNull(m_PanelTransform);
            Assert.IsNotNull(m_ContentTransform);

            Assert.IsTrue(m_ContentTransform.childCount == m_BalanceList.Count);

            m_ChildElements.Capacity = m_BalanceList.Count;
            for (int i = 0; i < m_ContentTransform.childCount; i++)
            {
                UIShopElement Element = m_ContentTransform.GetChild(i)?.GetComponent<UIShopElement>();
                Assert.IsNotNull(Element);

                m_ChildElements.Add(Element);

                Element.Init();
            }

            UpdateBalanceAndUI();
        }

        public void UpdateBalanceAndUI()
        {
            for (int i = 0; i < m_ChildElements.Count; ++i)
            {
                m_ChildElements[i].UpdateUI();
            }
        }
    }
}

#endif
