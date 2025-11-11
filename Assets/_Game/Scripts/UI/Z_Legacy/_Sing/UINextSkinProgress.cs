#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

namespace Sing
{
    public class UINextSkinProgress : MonoBehaviour
    {
        private bool m_Initialized = false;

        [SerializeField] private Image m_SkinImage;
        [SerializeField] private Image m_ProgressBar;
        [SerializeField] private TextMeshProUGUI m_NameText;
        [SerializeField] private TextMeshProUGUI m_CostText;

        private bool m_PrevUpdateNotEnoughMoney = true;

        // private Vector3 m_InitialPosition;

        public void Init()
        {
            m_Initialized = true;
        }

        public void UpdateUI()
        {
            if (!m_Initialized)
            {
                return;
            }

            if (!SingManager.Instance.GetNextSkinAndStandForPlayer(out var Skin, out var Stand))
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            m_SkinImage.sprite = Stand.UISprite;
            m_NameText.text = LocalizationManager.Instance.GetTranslation(Skin.Id);
            m_CostText.text = $"{Stand.Cost:F0}";

            // Cost can be zero
            const float ThresholdToFillFully = 0.1f;
            m_ProgressBar.fillAmount = Stand.Cost <= ThresholdToFillFully ? 1f : (float)(GameManager.Instance.Player.Money / Stand.Cost);

            // If we bought skin or whatever, progress bar fill amount changed to less than 1f
            if (!m_PrevUpdateNotEnoughMoney && m_ProgressBar.fillAmount < 1f)
            {
                m_PrevUpdateNotEnoughMoney = true;
                return;
            }

            if (m_PrevUpdateNotEnoughMoney && m_ProgressBar.fillAmount >= 1f)
            {
                m_PrevUpdateNotEnoughMoney = false;

                LeanTween.cancel(gameObject);

                float ScaleFactor = 1.2f;

                LeanTween.scale(gameObject, new Vector3(ScaleFactor, ScaleFactor, ScaleFactor), 1f)
                    .setEaseOutBounce()
                    .setOnComplete(() =>
                    {
                        LeanTween.scale(gameObject, Vector3.one, 1f);
                    });
                return;
            }

            m_PrevUpdateNotEnoughMoney = m_ProgressBar.fillAmount < 1f;
        }

        private void Awake()
        {
            Assert.IsNotNull(m_SkinImage);
            Assert.IsNotNull(m_ProgressBar);
            Assert.IsNotNull(m_NameText);
            Assert.IsNotNull(m_CostText);

            // m_InitialPosition = transform.localPosition;

            GameManager.Instance.Player.OnMoneyChanged += OnMoneyChanged;
        }

        private void OnMoneyChanged()
        {
            UpdateUI();
        }
    }
}

#endif
