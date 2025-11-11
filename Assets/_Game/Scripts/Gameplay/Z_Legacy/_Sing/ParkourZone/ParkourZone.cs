#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace Sing
{
    public class ParkourZone : MonoBehaviour
    {
        [Header("Core")]
        private bool m_Open = true;
        public bool Open => m_Open;

        private bool m_Initialized = false;

        private bool m_PlayerInZone = false;
        public bool PlayerInZone => m_PlayerInZone;

        [Header("Time")]
        [SerializeField] private float m_ParkourCloseTime = 120f;
        private float m_ParkourCloseTimeLeft = 0f;

        [Header("Reward")]
        [SerializeField] private double m_SkinCostMultiplierForReward = 1.1f;
        [SerializeField] private double m_MinReward = 100f;
        private double m_Reward = 0f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer m_ParkourIcon;
        [SerializeField] private TextMeshPro m_OpenText;
        [SerializeField] private TextMeshPro m_TimeText;
        [SerializeField] private TextMeshPro m_RewardText;
        [SerializeField] private SpriteRenderer m_MoneyIcon;
        [SerializeField] private TextMeshPro m_MoneyText;

        [Header("Border")]
        [SerializeField] private Transform m_BorderTransform;

        [Header("Transform Points")]
        [SerializeField] private Transform m_FailPoint;
        public Transform FailPoint => m_FailPoint;

        public void OnZoneEntered()
        {
            m_PlayerInZone = true;

            GameManager.Instance.Player.Owner.SetPlayerDefaultSpeed(true);
        }

        public void OnZoneLeft()
        {
            m_PlayerInZone = false;

            GameManager.Instance.Player.Owner.SetPlayerDefaultSpeed(false);
        }

        public void OnPlayerFailed()
        {
            GameManager.Instance.Player.transform.position = m_FailPoint.position;
            GameManager.Instance.Player.transform.rotation = m_FailPoint.rotation;
        }

        public void OnPlayerWon()
        {
            GameManager.Instance.Player.AddMoney(m_Reward, false);
            SingManager.Instance.Notify($"+{m_Reward:F0}", 3f, Color.green, false, "CashMoney");

            SingManager.Instance.TeleportPlayerOnSpawn();
            ActivateBorder();

            SingManager.Instance.SaveGameFully();
        }

        private void Update()
        {
            if (!m_Initialized || m_Open)
            {
                return;
            }

            m_ParkourCloseTimeLeft -= Time.deltaTime;

            if (m_ParkourCloseTimeLeft > 0f)
            {
                UpdateOpenTime();
                return;
            }

            DeactivateBorder();
        }

        private void UpdateOpenTime()
        {
            m_TimeText.text = CoreUtils.GetMinutesSecondsText(m_ParkourCloseTimeLeft);
        }

        private void UpdateReward()
        {
            SingManager.Instance.GetNextSkinAndStandForPlayer(out var Skin, out var Stand);
            Assert.IsNotNull(Skin);
            Assert.IsNotNull(Stand);

            m_Reward = System.Math.Max(m_MinReward, Stand.Cost * m_SkinCostMultiplierForReward);
            m_MoneyText.text = $"{m_Reward:F0}";
        }

        private void ActivateBorder()
        {
            m_Open = false;

            ToggleBorderElements(true);

            m_ParkourCloseTimeLeft = m_ParkourCloseTime;

            UpdateReward();

            UpdateOpenTime();
        }

        private void DeactivateBorder()
        {
            m_Open = true;

            ToggleBorderElements(false);
        }

        private void ToggleBorderElements(bool Toggle)
        {
            m_OpenText.enabled = Toggle;
            m_TimeText.enabled = Toggle;

            /*
            m_RewardText.enabled = Toggle;
            m_MoneyIcon.enabled = Toggle;
            m_MoneyText.enabled = Toggle;
            */

            m_BorderTransform.gameObject.SetActive(Toggle);
        }

        public void Init()
        {
            m_Initialized = true;

            ActivateBorder();
        }

        private void Awake()
        {
            Assert.IsNotNull(m_ParkourIcon);
            Assert.IsNotNull(m_OpenText);
            Assert.IsNotNull(m_TimeText);
            Assert.IsNotNull(m_RewardText);
            Assert.IsNotNull(m_MoneyIcon);
            Assert.IsNotNull(m_MoneyText);

            Assert.IsNotNull(m_BorderTransform);

            Assert.IsNotNull(m_FailPoint);

            SingManager.Instance.OnPlayerUnlockedSkin += UpdateReward;
            SingManager.Instance.OnPlayerLockedSkin += UpdateReward;
        }
    }
}

#endif