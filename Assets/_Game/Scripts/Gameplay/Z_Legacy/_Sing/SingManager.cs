#if GAME_SING

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;
using YG;

namespace Sing
{
    /** === Begin AI === */
    [Flags]
    public enum ERandomPointAI : int
    {
        AIPlace     = 1 << 0,
        OthersPlace = 1 << 1,
        Street      = 1 << 2,

        Anywhere = int.MaxValue
    }

    [Serializable]
    public class AIData
    {
        [Header("AI Config")]
        public float MaxDistanceToWalk = 50f;

        [Header("Random Points")]
        public Transform[] StreetRandomPoints;

        [Header("Friendly Bots")]
        public float UnlockZoneChance = 0.75f;

        public float ZoneZeroSingProbability = 0.2f;

        public float ZoneZeroChillProbability = 0.5f;
        public float ZoneZeroLongChillProbability = 0.1f;
        public float ZoneZeroLongChillBaseTime = 5f;
        public float ZoneZeroLongChillDeviationTime = 2f;

        public float AdvanceNextZoneProbability = 0.2f;
        public float AdvanceNextZoneWhenUnlockedProbability = 0.4f;
        public float AdvanceNextZoneForwardProbability = 0.8f;
    }
    /** === End AI === */

    public class SingManager : Singleton<SingManager>
    {
        public Action OnPlayerUnlockedSkin;
        public Action OnPlayerLockedSkin;

        [Header("SDK")]
        private bool m_Loaded = false;
        public bool Loaded => m_Loaded;

        [Header("Save")]
        private float m_LastSaveTime = 0f;

        [Header("Auth")]
        private bool m_Authorized = false;
        private bool m_InitiallyWasAuthorized = false;

        [Header("Auth/UI")]
        [SerializeField] private RectTransform m_AuthMenu;

        [Header("Interstitial")]
        [SerializeField] private Text m_InterADTextOneSec;
        [SerializeField] private Text m_InterADTextTwoSec;
        [SerializeField] private Image m_InterADFadeImage;
        private Coroutine m_InterADCoroutine = null;

        [Header("Rewarded")]
        private Dictionary<string, Rewarded> m_RewardsDB = new();

        private float m_RewardCoinsMultiplier = 1f;
        public float RewardCoinsMultiplier => m_RewardCoinsMultiplier;

        [Header("In-app")]
        private Dictionary<string, Inapp> m_InappsDB = new();
        private string m_AuthQueuedInapp = null;

        [Header("Misc Ad related")]
        [SerializeField] private List<Outline> m_OutlinesToEnableNearSpawn = new();

        [Header("AI")]
        [SerializeField] private AIData m_AI = new();
        public AIData AI => m_AI;

        private List<Controller> m_Controllers = new();

        [Header("Spawn")]
        [SerializeField] private Transform m_SpawnPoint;

        [Header("Zone")]
        [SerializeField] private List<Zone> m_Zones = new();
        public List<Zone> Zones => m_Zones;
        private Zone m_ZoneZero;

        [SerializeField] private ParkourZone m_ParkourZone;

        [Header("Skins")]
        [SerializeField] private UINextSkinProgress m_UINextSkinProgress;
        public UINextSkinProgress UINextSkinProgress => m_UINextSkinProgress;
        [SerializeField] private List<SkinBuyingStand> m_SkinBuyingStands = new();
        public List<SkinBuyingStand> SkinBuyingStands => m_SkinBuyingStands;

        [Header("Rebirth")]
        [SerializeField] private RectTransform m_RebirthTransform;
        // private Vector3 m_RebirthInitialPosition;

        private bool m_PrevUpdateCanRebirth = false;

        [SerializeField] private float m_RebirthMoneyMultiplier = 1f;
        public float RebirthMoneyMultiplier => m_RebirthMoneyMultiplier;
        [SerializeField] private float m_RebirthMoneyNeededToProgress = 100f;

        [Header("Rebirth/UI")]
        [SerializeField] private RectTransform m_RebirthMenu;
        [SerializeField] private Button m_RebirthButton;
        [SerializeField] private Image m_RebirthReadySign;
        [SerializeField] private Image m_RebirthProgressBar;
        [SerializeField] private TextMeshProUGUI m_RebirthProgressBarText;
        [SerializeField] private TextMeshProUGUI m_RebirthMultiplier;
        [SerializeField] private TextMeshProUGUI m_RebirthMultiplierText;

        [Header("VFX")]
        [SerializeField] private GameObject m_CoinsVFXPrefab;
        [SerializeField] private Transform m_CoinsVFXTransform;
        [SerializeField] private float m_CoinsVFXTime = 1f;

        [Header("Music")]
        [SerializeField] private List<Transform> m_SingTransforms = new();
        [SerializeField] private List<bool> m_SingTransformsUsed = new();

        private bool m_MusicStarted = false;
        public bool MusicStarted => m_MusicStarted;

        [Header("Tutorial")]
        [SerializeField] private GuidanceLine.GuidanceLine m_GuideLine;

        [Header("HUD")]
        [SerializeField] private Canvas m_Canvas;
        [SerializeField] private CanvasScaler m_CanvasScaler;
        [SerializeField] private RectTransform m_HUDTransform;

        [Header("Settings/UI")]
        [SerializeField] private RectTransform m_SettingsTransform;

        [Header("HUD/Notification")]
        [SerializeField] private TextMeshProUGUI m_NotificationText;
        [SerializeField] private GameObject m_MoneyNotificationPrefab;

        [Header("Floating Stuff")]
        [SerializeField] private List<Transform> m_FloatingStuff = new();
        [SerializeField] private float m_FloatingStuffYOffset = 1f;
        [SerializeField] private float m_FloatingStuffAnimTime = 0.5f;

        [Header("Desktop")]
        [SerializeField] private List<Transform> m_DesktopOnlyTransforms = new();

        /** === Begin Settings === */
        public void ToggleSettingsMenu()
        {
            m_SettingsTransform.gameObject.SetActive(!m_SettingsTransform.gameObject.activeSelf);
        }

        /** === End Settings === */

        /** === Begin Song === */
        public Transform TakeFreeSingTransform()
        {
            int SingIndex = -1;

            for (int i = 0; i < m_SingTransformsUsed.Count; ++i)
            {
                if (!m_SingTransformsUsed[i])
                {
                    SingIndex = i;
                    break;
                }
            }

            if (SingIndex == -1)
            {
                return null;
            }

            m_SingTransformsUsed[SingIndex] = true;
            return m_SingTransforms[SingIndex];
        }

        public void ReturnSingTransform(Transform Transform)
        {
            if (Transform == null)
            {
                return;
            }

            for (int i = 0; i < m_SingTransforms.Count; ++i)
            {
                if (m_SingTransforms[i] == Transform)
                {
                    m_SingTransformsUsed[i] = false;
                    break;
                }
            }
        }

        public float GetSongPosition()
        {
            if (!m_MusicStarted)
            {
                return 0f;
            }
            
            Controller AIController = null;

            for (int i = 0; i < m_Controllers.Count; ++i)
            {
                if (m_Controllers[i].AIController && !m_Controllers[i].AI.Enemy) 
                {
                    AIController = m_Controllers[i];
                    break;
                }
            }

            if (AIController == null || AIController.AudioSource == null)
            {
                return 0f;
            }

            return AIController.AudioSource.time;
        }

        /** === End Song === */

        /** === Begin Player === */
        public void PlayPlayerCoinsVFX()
        {
            GameObject VFXObject = Instantiate(m_CoinsVFXPrefab, m_CoinsVFXTransform.position, m_CoinsVFXTransform.rotation, GameManager.Instance.Player.transform);
            Destroy(VFXObject, m_CoinsVFXTime);

            SoundController.Instance.Play("CashMoney");
        }

        public SkinBuyingStand GetStandBySkinId(string SkinId)
        {
            for (int i = 0; i < m_SkinBuyingStands.Count; ++i)
            {
                if (m_SkinBuyingStands[i].SkinId == SkinId)            
                {
                    return m_SkinBuyingStands[i];
                }
            }

            return null;
        }

        // @returns: True when there's next skin to buy. False hen there's no next skin to buy, skin and stand will be last player skin
        public bool GetNextSkinAndStandForPlayer(out SkinData Skin, out SkinBuyingStand Stand)
        {
            SkinBuyingStand LastStand = SkinBuyingStands[SkinBuyingStands.Count - 1];
            Assert.IsNotNull(LastStand);

            SkinData LastSkin = GameManager.Instance.Player.Owner.GetSkin(LastStand.SkinId);
            Assert.IsNotNull(LastSkin);

            if (LastSkin.Unlocked)
            {
                Skin = LastSkin;
                Stand = LastStand;

                return false;
            }

            int StandIndex = SkinBuyingStands.Count - 1;
            SkinData SkinIt = null;

            for ( ; StandIndex >= 0; --StandIndex)
            {
                SkinIt = GameManager.Instance.Player.Owner.GetSkin(SkinBuyingStands[StandIndex].SkinId);

                if (SkinIt.Unlocked)
                {
                    break;
                }
            }

            ++StandIndex;

            Stand = SkinBuyingStands[StandIndex];
            Skin = GameManager.Instance.Player.Owner.GetSkin(Stand.SkinId);

            return true;
        }

        public void StartKillingPlayer()
        {
            // @TODO: Maybe some time delay for player
            KillCharacter(GameManager.Instance.Player.Owner);
        }
        /** === End Player === */

        /** === Begin Character === */
        public void KillCharacter(Controller Controller)
        {
            Assert.IsNotNull(Controller);

            if (Controller.PlayerController)
            {
                Controller.ChangeSkin("skin_default");
            }
            TeleportCharacterOnSpawn(Controller);
        }

        public void TeleportPlayerOnSpawn()
        {
            TeleportCharacterOnSpawn(GameManager.Instance.Player.Owner);
        }

        public void TeleportCharacterOnSpawn(Controller Controller)
        {
            Assert.IsNotNull(Controller);

            if (Controller.PlayerController)
            {
                Controller.transform.position = m_SpawnPoint.position;
                Controller.transform.rotation = m_SpawnPoint.rotation;
                return;
            }

            Assert.IsNotNull(m_ZoneZero);
            Controller.transform.position = m_ZoneZero.GetRandomPositionForCharacter();
        }
        /** === End Character === */

        /** === Begin Rebirth === */
        public void OnOpenRebirthMenu()
        {
            m_RebirthMenu.gameObject.SetActive(true);
        }

        public void OnCloseRebirthMenu()
        {
            m_RebirthMenu.gameObject.SetActive(false);
        }

        public void OnRebirthClicked()
        {
            m_RebirthMoneyMultiplier += 0.5f;
            m_RebirthMoneyNeededToProgress *= 2f;

            GameManager.Instance.Player.SetMoney(0f);

            for (int i = 1; i < m_Zones.Count; ++i)
            {
                m_Zones[i].ActivateBorder();
            }

            for (int i = 0; i < m_SkinBuyingStands.Count; ++i)
            {
                m_SkinBuyingStands[i].LockSkinAndStand(i == 0);
                m_SkinBuyingStands[i].OnMoneyChanged();
            }

            KillCharacter(GameManager.Instance.Player.Owner);
            OnCloseRebirthMenu();

            SaveGameFully();
        }

        private void OnMoneyChangedRebirth()
        {
            UpdateRebirthMenu();
        }

        private void UpdateRebirthMenu()
        {
            m_RebirthMultiplierText.text = $"+x0.5 {LocalizationManager.Instance.GetTranslation("ui_rebirth_plus_multiplier")}";

            double Money = GameManager.Instance.Player.Money;

            m_RebirthProgressBar.fillAmount = (float)(Money / m_RebirthMoneyNeededToProgress);

            if (Money >= m_RebirthMoneyNeededToProgress)
            {
                m_RebirthProgressBarText.text = LocalizationManager.Instance.GetTranslation("ui_ready");
                m_RebirthButton.interactable = true;
                m_RebirthReadySign.gameObject.SetActive(true);

                if (!m_PrevUpdateCanRebirth)
                {
                    LeanTween.cancel(m_RebirthTransform.gameObject);

                    float ScaleFactor = 1.4f;

                    LeanTween.scale(m_RebirthTransform.gameObject, new Vector3(ScaleFactor, ScaleFactor, ScaleFactor), 1f)
                        .setEaseOutBounce()
                        .setOnComplete(() =>
                        {
                            LeanTween.scale(m_RebirthTransform.gameObject, Vector3.one, 1f);
                        });
                }

                m_PrevUpdateCanRebirth = true;
            }
            else
            {
                m_RebirthProgressBarText.text = $"{Money:F0} / {m_RebirthMoneyNeededToProgress:F0}";
                m_RebirthButton.interactable = false;
                m_RebirthReadySign.gameObject.SetActive(false);

                m_PrevUpdateCanRebirth = false;
            }

            m_RebirthMultiplier.text = $"{LocalizationManager.Instance.GetTranslation("ui_rebirth_current_multiplier")}: x{m_RebirthMoneyMultiplier:F1}";
        }
        /** === End Rebirth === */

        /** === Begin Zone */
        public Zone GetZoneById(int Id)
        {
            for (int i = 0; i < m_Zones.Count; ++i)
            {
                if (m_Zones[i].ZoneId == Id)
                {
                    return m_Zones[i];
                }
            }

            return null;
        }
        /** === End Zone */

        /** === Begin Tutorial === */
        private void AnimateTutorialButton(RectTransform Transform)
        {
            Assert.IsNotNull(Transform);

            const float ScaleFactor = 1.2f;
            LeanTween.scale(Transform, new Vector3(ScaleFactor, ScaleFactor, ScaleFactor), 1f)
                .setEaseOutElastic()
                .setLoopPingPong(1);
        }

        // @param Key: Key represents Translation Key. If null - disables overlay
        private void SetTutorialText(string Key)
        {
        /*
            if (Key == null)
            {
                m_Tutorial.TutorialOverlay.gameObject.SetActive(false);
                return;
            }

            m_Tutorial.TutorialOverlay.gameObject.SetActive(true);
            m_Tutorial.TutorialText.text = LocalizationManager.Instance.GetTranslation(Key);
        */
        }

        private void SetGuideLine(Transform Point = null)
        {
            if (!Point)
            {
                m_GuideLine.gameObject.SetActive(false);
                return;
            }

            m_GuideLine.gameObject.SetActive(true);
            m_GuideLine.endPoint = Point;
        }
        /** === End Tutorial === */

        /** === Begin Notify === */
        public void Notify(string Key, float TimeToShow, Color Color, bool NeedTranslation, string SoundKey = null)
        {
            LeanTween.cancel(m_NotificationText.rectTransform);

            RectTransform CanvasRect = m_NotificationText.canvas.GetComponent<RectTransform>();
            Assert.IsNotNull(CanvasRect);

            m_NotificationText.gameObject.SetActive(true);
            m_NotificationText.rectTransform.localScale = Vector3.one;
            m_NotificationText.text = NeedTranslation ? LocalizationManager.Instance.GetTranslation(Key) : Key;
            m_NotificationText.color = Color;

            // We need text size and fine scale immediatly
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_NotificationText.rectTransform);

            Vector3 OutOfScreenPos = m_NotificationText.rectTransform.localPosition;
            OutOfScreenPos.y = (CanvasRect.sizeDelta.y * 0.5f) + m_NotificationText.preferredHeight * 1.2f;
            m_NotificationText.rectTransform.localPosition = OutOfScreenPos;

            m_NotificationText.rectTransform.localScale = Vector3.zero;
            LeanTween.scale(m_NotificationText.rectTransform, Vector3.one, 0.5f)
                .setEase(LeanTweenType.easeOutCubic);

            LeanTween.move(m_NotificationText.rectTransform, Vector3.zero, 0.5f)
                .setEase(LeanTweenType.easeOutCubic)
                .setOnComplete(() =>
                {
                    LeanTween.scale(m_NotificationText.rectTransform, m_NotificationText.rectTransform.localScale, TimeToShow)
                        .setOnComplete(() =>
                        {
                            LeanTween.scale(m_NotificationText.rectTransform, Vector3.zero, 0.5f);
                            LeanTween.move(m_NotificationText.rectTransform, OutOfScreenPos, 0.5f);
                        });
                });

            if (!string.IsNullOrEmpty(SoundKey))
            {
                SoundController.Instance.Play(SoundKey);
            }
        }

        public void NotifyMoney(double Amount)
        {
            GameObject GO = Instantiate(m_MoneyNotificationPrefab);
            Assert.IsNotNull(GO);

            RectTransform Transform = GO.GetComponent<RectTransform>();
            Assert.IsNotNull(Transform);

            Image Image = GO.GetComponentInChildren<Image>();
            TextMeshProUGUI Text = GO.GetComponentInChildren<TextMeshProUGUI>();

            Text.text = $"+{Amount}";

            Transform.SetParent(m_HUDTransform, false);

            float XOffsetRange = 400f * (m_Canvas.renderingDisplaySize.x / m_CanvasScaler.referenceResolution.x);
            float YOffsetRange = 100f * m_CanvasScaler.scaleFactor;

            Transform.localPosition = new Vector3(
                UnityEngine.Random.Range(-XOffsetRange, XOffsetRange),
                UnityEngine.Random.Range(-YOffsetRange, YOffsetRange),
                0f
            );

            Vector3 AnimOffset = new Vector3(0, 100f, 0f);
            const float AnimOffsetTime = 0.25f;

            LeanTween.moveLocal(Transform.gameObject, Transform.localPosition + AnimOffset, AnimOffsetTime)
                .setEaseOutCubic()
                .setOnComplete(() =>
                {
                    LeanTween.value(1f, 0f, 1f)
                        .setOnUpdate((float Alpha) =>
                        {
                            if (Text != null)
                            {
                                Text.alpha = Alpha;
                            }

                            if (Image != null)
                            {
                                Color Color = Image.color;
                                Color.a = Alpha;
                                Image.color = Color;
                            }
                        })
                        .setOnComplete(() =>
                        {
                            if (Transform != null && Transform.gameObject != null)
                            {
                                Destroy(Transform.gameObject);
                            }
                        });
                });

            SoundController.Instance.Play("CoinPickup");
        }
        /** === End Notify === */

        /** === Begin Save === */
        // Should be somewhere else but fine for now
        public void SaveMoney(bool SaveToYandex = true)
        {
            YandexGame.savesData.Sing.Money = GameManager.Instance.Player.Money;

            if (SaveToYandex)
            {
                YandexGame.SaveProgress();
            }
        }

        public void SaveGameFully()
        {
            Debug.Log("Saving Game Fully..");

            var Player = GameManager.Instance.Player;
            var PlayerController = Player.Owner;

            // Money
            SaveMoney(false);

            // Inapps
            List<string> Inapps = new();

            // Previously bought inapps.
            // It's important, because thay may not be activated when function is called.
            // This function can be called from OnSDKLoaded before inapps activation.
            if (YandexGame.savesData.Sing.BoughtInapps != null)
            {
                Inapps.AddRange(YandexGame.savesData.Sing.BoughtInapps);
            }

            foreach (var kv in m_InappsDB)
            {
                // Usually it will represent newly activated Inapps that haven't been saved yet
                if (kv.Value.Activated && !Inapps.Exists((Id) => Id == kv.Key))
                {
                    Inapps.Add(kv.Key);
                }
            }

            YandexGame.savesData.Sing.BoughtInapps = Inapps.ToArray();

            // Rebirth
            YandexGame.savesData.Sing.RebirthMoneyMultiplier       = m_RebirthMoneyMultiplier;
            YandexGame.savesData.Sing.RebirthMoneyNeededToProgress = m_RebirthMoneyNeededToProgress;

            // Zones
            List<int> ActivatedZones = new();
            
            for (int i = 0; i < m_Zones.Count; ++i)
            {
                if (m_Zones[i].IsActivatedZone())
                {
                    ActivatedZones.Add(m_Zones[i].ZoneId);
                }
            }

            YandexGame.savesData.Sing.ActivatedZoneIds = ActivatedZones.Count > 0 ? ActivatedZones.ToArray() : null;

            // Skins
            List<string> UnlockedSkins = new();
            
            for (int i = 0; i < m_SkinBuyingStands.Count; ++i)
            {
                SkinBuyingStand Stand = m_SkinBuyingStands[i];
                if (Stand == null)
                {
                    continue;
                }

                SkinData Skin = PlayerController.GetSkin(Stand.SkinId);
                if (Skin == null)
                {
                    continue;
                }

                if (Skin.Unlocked)
                {
                    UnlockedSkins.Add(Skin.Id);
                }
            }

            YandexGame.savesData.Sing.UnlockedSkins = UnlockedSkins.Count > 0 ? UnlockedSkins.ToArray() : null;

            // Save
            YandexGame.SaveProgress();

            m_LastSaveTime = Time.time;
        }
        /** === End Save === */

        /** === Begin Localization === */
        private void OnLocalizationRefresh()
        {
            UpdateRebirthMenu();
        }
        /** === End Localization === */

        /** === Begin In-app === */
        private void OnInappPaymentSuccess(string Id)
        {
            // Inapps: Consume and activate bought, but not active inapps

            var Inapp = QueryInapp(Id);
            if (Inapp == null || Inapp.Activated)
            {
                return;
            }

            ActivateInapp(Inapp);
            SaveGameFully();

            Notify("ui_notify_inapp_success", 2f, Color.green, true, "Success");
        }

        private void OnInappPaymentFail(string Id)
        {
            Debug.Log("Inapp payment failed");

            Notify("ui_notify_inapp_failed", 2f, Color.red, true, "NegativeClick");
        }

        private void OnInappPaymentsUpdate()
        {
            foreach (var kv in m_InappsDB)
            {
                kv.Value.RefreshData();
            }
        }

        private void ActivateInapp(Inapp Inapp)
        {
            Assert.IsNotNull(Inapp);

            if (Inapp.Activated)
            {
                return;
            }

            Inapp.OnInappActivated();

            switch (Inapp.InappId)
            {
                case "inapp_turn_off_ads":
                    AdManager.Instance.TurnOffInter = true;

                    YandexGame.StickyAdActivity(false);

                    if (m_InterADCoroutine != null)
                    {
                        StopCoroutine(m_InterADCoroutine);
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown inapp {Inapp.InappId}");
                    break;
            }

            Debug.Log($"Inapp {Inapp.InappId} activated");
        }

        private void ActivateInapp(string InappId)
        {
            var InappButton = QueryInapp(InappId);
            if (InappButton == null)
            {
                return;
            }

            ActivateInapp(InappButton);
        }

        private Inapp QueryInapp(string Key)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return null;
            }

            if (!m_InappsDB.TryGetValue(Key, out Inapp Inapp))
            {
                return null;
            }

            return Inapp;
        }
        /** === End In-app === */

        /** === Begin Rewarded === */
        public void StartRewarded(string Id)
        {
            if (!AdManager.Instance.CanShowReward())
            {
                return;
            }

            AdManager.Instance.ShowRewarded(Id, 0, OnRewardRewarded, OnOpenRewarded, OnCloseRewarded, OnErrorRewarded);
        }

        public void OnRewardRewarded(int Id)
        {
            Debug.Log($"{nameof(OnRewardRewarded)}() called");

            ActivateReward(AdManager.Instance.RewardId);
        }

        public void OnOpenRewarded()
        {
            Debug.Log($"{nameof(OnOpenRewarded)}() called");

            PauseGame(true);
        }

        public void OnCloseRewarded()
        {
            PauseGame(false);

            Debug.Log($"{nameof(OnCloseRewarded)}() called");
        }

        public void OnErrorRewarded()
        {
            Debug.Log($"{nameof(OnErrorRewarded)}() called");

            OnCloseRewarded();
        }

        private void ActivateReward(Rewarded Reward)
        {
            Assert.IsNotNull(Reward);

            if (Reward.Activated)
            {
                return;
            }

            Reward.OnRewardActivated();

            switch (Reward.RewardId)
            {
                case "reward_coins_x2":
                    m_RewardCoinsMultiplier = 2f;
                    break;

                case "reward_speed_x2":
                    GameManager.Instance.Player.Owner.SetPlayerSpeedMultiplier(2f);
                    break;

                default:
                    Debug.LogWarning($"Unknown reward {Reward.RewardId}");
                    break;
            }

            Debug.Log($"Rewarded {Reward.RewardId} activated");
        }

        private void ActivateReward(string RewardId)
        {
            var Reward = QueryReward(RewardId);
            if (Reward == null)
            {
                return;
            }

            ActivateReward(Reward);
        }

        // It's better to just move Cooldown logic of Rewarded in SingManager.Update() and make it private but now it's fine
        public void DeactivateReward(Rewarded Reward)
        {
            Assert.IsNotNull(Reward);

            if (!Reward.Activated)
            {
                return;
            }

            Reward.OnRewardDeactivated();

            switch (Reward.RewardId)
            {
                case "reward_coins_x2":
                    m_RewardCoinsMultiplier = 1f;
                    break;

                case "reward_speed_x2":
                    GameManager.Instance.Player.Owner.SetPlayerSpeedMultiplier(1f);
                    break;

                default:
                    Debug.LogWarning($"Unknown reward {Reward.RewardId}");
                    break;
            }

            Debug.Log($"Rewarded {Reward.RewardId} deactivated");
        }

        public void DeactivateReward(string RewardId)
        {
            var Reward = QueryReward(RewardId);
            if (Reward == null)
            {
                return;
            }

            DeactivateReward(Reward);
        }

        private Rewarded QueryReward(string Key)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return null;
            }

            if (!m_RewardsDB.TryGetValue(Key, out Rewarded Reward))
            {
                return null;
            }

            return Reward;
        }
        /** === End Rewarded === */

        /** === Begin Ads === */
        public void OnBeforeInter()
        {
            PauseGame(true);

            Debug.Log($"{nameof(OnBeforeInter)}() called");
        }

        public void OnOpenInter()
        {
            Debug.Log($"{nameof(OnOpenInter)}() called");
        }

        public void OnCloseInter()
        {
            PauseGame(false);

            // Idk why, but it has to be there, otherwise null exceptions...
            if (m_InterADTextOneSec != null)
            {
                m_InterADTextOneSec.gameObject.SetActive(false);
            }
            if (m_InterADFadeImage != null)
            {
                m_InterADFadeImage.gameObject.SetActive(false);
            }

            Debug.Log($"{nameof(OnCloseInter)}() called");
        }

        public void OnErrorInter()
        {
            Debug.Log($"{nameof(OnErrorInter)}() called");

            OnCloseInter();
        }

        private IEnumerator ShowInterADCoroutine()
        {
            for (;;)
            {
                float TimeLeftForNextInter = AdManager.Instance.GetTimeLeftToShowAd();
                Debug.Log($"{nameof(Sing)}.{nameof(ShowInterADCoroutine)}(): Time left for next inter: {TimeLeftForNextInter}s");

                yield return new WaitForSeconds(TimeLeftForNextInter);

                // Start 2..1 countdown
                m_InterADFadeImage.gameObject.SetActive(true);
                m_InterADTextTwoSec.gameObject.SetActive(true);

                // Pause game
                OnBeforeInter();

                yield return new WaitForSecondsRealtime(1f);

                m_InterADTextTwoSec.gameObject.SetActive(false);
                m_InterADTextOneSec.gameObject.SetActive(true);

                yield return new WaitForSecondsRealtime(1f);

                AdManager.Instance.ShowInter(OnOpenInter, OnCloseInter, OnErrorInter);
            }
        }
        /** === End Ads === */

        /** === Begin Auth === */
        public void OpenSDKAuthDialog()
        {
            YandexGame.AuthDialog();
        }

        public void OpenAuthMenuAndQueueInapp(string Inapp)
        {
            Assert.IsTrue(!string.IsNullOrEmpty(Inapp));

            Debug.Log($"{nameof(OpenAuthMenuAndQueueInapp)}: Inapp={Inapp}");

            m_AuthMenu.gameObject.SetActive(true);
            m_AuthQueuedInapp = Inapp;
        }

        public void CloseAuthMenu()
        {
            m_AuthMenu.gameObject.SetActive(false);
        }

        private void OnTryToDetectAuthorization()
        {
            Assert.IsTrue(!m_InitiallyWasAuthorized && !m_Authorized);

            if (YandexGame.auth)
            {
                YandexGame.GetDataEvent -= OnTryToDetectAuthorization;

                m_Authorized = true;

                Debug.Log($"{nameof(OnTryToDetectAuthorization)}: {nameof(m_Authorized)}=true, {m_AuthQueuedInapp}={m_AuthQueuedInapp}");

                if (!string.IsNullOrEmpty(m_AuthQueuedInapp) && YandexGame.PurchaseByID(m_AuthQueuedInapp) != null)
                {
                    Debug.Log($"{nameof(OnTryToDetectAuthorization)}: {nameof(m_Authorized)}=true, {m_AuthQueuedInapp}={m_AuthQueuedInapp}");

                    YandexGame.BuyPayments(m_AuthQueuedInapp);
                    m_AuthQueuedInapp = null;
                }
            }
        }
        /** === End Auth === */

        /** === Begin Game Core === */
        private void LateUpdate()
        {
#if UNITY_EDITOR
            // Check if something is broken
            {
                for (int ControllerIdx = 0; ControllerIdx < m_Controllers.Count; ++ControllerIdx)
                {
                    int FoundZoneIdx = -1;

                    Controller Controller = m_Controllers[ControllerIdx];

                    for (int ZoneIdx = 0; ZoneIdx < m_Zones.Count; ++ZoneIdx)
                    {
                        if (!m_Zones[ZoneIdx].Controllers.Contains(Controller))
                        {
                            continue;
                        }

                        if (FoundZoneIdx != -1)
                        {
                            // Debug.Log($"Controller: {Controller.gameObject.name},FoundZoneIdx: {m_Zones[FoundZoneIdx].ZoneId}, next Idx: {m_Zones[ZoneIdx].ZoneId}");

                            if (Math.Abs(m_Zones[FoundZoneIdx].ZoneId - m_Zones[ZoneIdx].ZoneId) != 1)
                            {
                                NoEntry.Assert();
                            }
                        }
                        else
                        {
                            FoundZoneIdx = ZoneIdx;
                        }
                    }
                }
            }
#endif

            // Outlines
            {
                var ZoneZero = GetZoneById(0);
                var ZoneOne = GetZoneById(1);

                bool PlayerNearSpawn = false;

                if (ZoneZero != null && ZoneOne != null)
                {
                    PlayerNearSpawn =
                        ZoneZero.Controllers.Contains(GameManager.Instance.Player.Owner) ||
                        ZoneOne.Controllers.Contains(GameManager.Instance.Player.Owner);

                    for (int i = 0; i < m_OutlinesToEnableNearSpawn.Count; ++i)
                    {
                        if (m_OutlinesToEnableNearSpawn[i] != null)
                        {
                            m_OutlinesToEnableNearSpawn[i].enabled = PlayerNearSpawn;
                        }
                    }
                }
            }

            // Tick Zones
            for (int i = 0; i < m_Zones.Count; ++i)
            {
                m_Zones[i].Tick();
            }

            // Guiding Line
            {
                bool LineSet = false;
                bool AvailableZoneSet = false;
                Zone ZoneAvailable = null;

                // Current Available Zone
                for (int i = 1; i < m_Zones.Count; ++i)
                {
                    Zone Zone = GetZoneById(i);

                    if (Zone == null || Zone.Open)
                    {
                        continue;
                    }

                    Zone ZonePrev = GetZoneById(i - 1);
                    if (ZonePrev == null || !ZonePrev.Open)
                    {
                        break;
                    }

                    AvailableZoneSet = true;
                    ZoneAvailable = ZonePrev;

                    if (ZonePrev.Controllers.Contains(GameManager.Instance.Player.Owner))
                    {
                        break;
                    }

                    SetGuideLine(ZonePrev.CoinsArea.transform);
                    LineSet = true;
                    break;
                }

                // Coins in Available Zone
                if (AvailableZoneSet)
                {
                    Transform CoinTransform = ZoneAvailable.GetClosestCoinTransform(GameManager.Instance.Player.transform.position);

                    if (CoinTransform != null)
                    {
                        SetGuideLine(CoinTransform);
                        LineSet = true;
                    }
                }

                // Next zone
                for (int i = 1; i < m_Zones.Count; ++i)
                {
                    Zone Zone = GetZoneById(i);

                    if (Zone == null)
                    {
                        continue;
                    }

                    if (!Zone.Open && Zone.Cost <= GameManager.Instance.Player.Money)
                    {
                        SetGuideLine(Zone.CoinsArea.transform);
                        LineSet = true;
                        break;
                    }
                }

                // Parkour
                if (m_ParkourZone.Open)
                {
                    Zone ZoneFirst = GetZoneById(1);

                    if (ZoneFirst != null && ZoneFirst.Open)
                    {
                        if (m_ParkourZone.PlayerInZone)
                        {
                            // In case if we have Zone to open, we reset guide line
                            SetGuideLine(null);
                            LineSet = false;
                        }
                        else
                        {
                            SetGuideLine(m_ParkourZone.FailPoint);
                            LineSet = true;
                        }
                    }
                }

                // Better skin to wear
                var CurrentSkin = GameManager.Instance.Player.Owner.Skin;
                if (CurrentSkin != null)
                {
                    for (int i = m_SkinBuyingStands.Count - 1; i >= 0; --i)
                    {
                        var StandIt = m_SkinBuyingStands[i];

                        if (StandIt.SkinId == CurrentSkin.Id)
                        {
                            break;
                        }

                        if (StandIt.State == ESkinBuyingStandState.Bought)
                        {
                            SetGuideLine(StandIt.ButtonMesh.transform);
                            LineSet = true;
                            break;
                        }
                    }
                }

                // Next skin
                if (GetNextSkinAndStandForPlayer(out var Skin, out var Stand) && Stand.Cost <= GameManager.Instance.Player.Money)
                {
                    SetGuideLine(Stand.ButtonMesh.transform);
                    LineSet = true;
                }

                if (!LineSet)
                {
                    SetGuideLine(null);
                }

                m_GuideLine.Update();
            }

            // Save
            if (Time.time - m_LastSaveTime >= 30f)
            {
                SaveGameFully();
            }
        }

        public void PauseGame(bool Toggle)
        {
            if (Toggle)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
                SoundController.Instance.MuteGame("pause_game");
                return;
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
            SoundController.Instance.UnmuteGame("pause_game");
        }
        /** === End Game Core === */

        /** === Begin Initialization === */
        private void OnSDKLoaded()
        {
            YandexGame.GetDataEvent -= OnSDKLoaded;

            // Authorization
            m_InitiallyWasAuthorized = YandexGame.auth;
            m_Authorized = m_InitiallyWasAuthorized;

            if (!m_Authorized)
            {
                YandexGame.GetDataEvent += OnTryToDetectAuthorization;
            }

            Debug.Log($"{nameof(OnSDKLoaded)}(): {nameof(m_Authorized)}:{m_Authorized}");

            // Reload localization from here
            LocalizationManager.Instance.LoadLanguage(YandexGame.lang);

            // Sticky
            YandexGame.StickyAdActivity(true);

            var Player = GameManager.Instance.Player;

            // Check if version is incompatible
            // @NOTE: Be very careful with inapps and progress
            if (YandexGame.savesData.Sing.MajorVersion < 0)
            {
                YandexGame.savesData = new();
            }

            // Check for first lauch
            if (YandexGame.savesData.FirstLaunch)
            {
                YandexGame.savesData.Sing.Money = 0f;
                YandexGame.savesData.FirstLaunch = false;
                YandexGame.SaveProgress();
            }

            // === Loading Saved Progress ===

            // Money
            Player.SetMoney(YandexGame.savesData.Sing.Money);
            Debug.Log($"Loaded money: {YandexGame.savesData.Sing.Money}");

            // Controllers
            for (int i = 0; i < m_Controllers.Count; ++i)
            {
                var Controller = m_Controllers[i];
                Controller.Init();

                if (Controller.AIController && !Controller.AI.Enemy)
                {
                    Controller.PlayMusic();
                }
            }

            m_MusicStarted = true;

            // Player depends on AI music, so we start music for AI first
            GameManager.Instance.Player.Owner.PlayMusic();

            for (int i = 0; i < m_SkinBuyingStands.Count; ++i)
            {
                m_SkinBuyingStands[i].Init();
            }

            // Inapps
            Inapp[] Inapps = FindObjectsByType<Inapp>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < Inapps.Length; ++i)
            {
                m_InappsDB.Add(Inapps[i].InappId, Inapps[i]);
                Inapps[i].RefreshData();
            }

            Rewarded[] Rewards = FindObjectsByType<Rewarded>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < Rewards.Length; ++i)
            {
                m_RewardsDB.Add(Rewards[i].RewardId, Rewards[i]);
            }

            // Inapps: Activation of saved bought inapps
            string[] BoughtInapps = YandexGame.savesData.Sing.BoughtInapps;
            if (YandexGame.savesData.Sing.BoughtInapps != null)
            {
                for (int i = 0; i < BoughtInapps.Length; ++i)
                {
                    ActivateInapp(BoughtInapps[i]);
                }
            }

            // Inapps: Consume and activate bought, but not active inapps
            YandexGame.ConsumePurchases();

            if (!AdManager.Instance.TurnOffInter)
            {
                // Idk if it even does matter
                OnBeforeInter();
                AdManager.Instance.ShowInter(OnOpenInter, OnCloseInter, OnErrorInter);

                m_InterADCoroutine = StartCoroutine(ShowInterADCoroutine());
            }

            // Parkour Zone
            m_ParkourZone.Init();

            // Platform specific tweaking
            bool Mobile = GameManager.Instance.Mobile;
            if (!Mobile)
            {
                for (int i = 0; i < m_DesktopOnlyTransforms.Count; ++i)
                {
                    m_DesktopOnlyTransforms[i].gameObject.SetActive(true);
                }
            }

            var Camera = FindFirstObjectByType<MenteBacata.ScivoloCharacterControllerDemo.OrbitingCamera>();
            Camera?.SetMobile(Mobile);

            var TCK = FindFirstObjectByType<TouchControlsKit.TCKInput>();
            TCK?.gameObject.SetActive(Mobile);

            var JoystickInput = FindFirstObjectByType<JoystickInput>();
            JoystickInput?.SetMobile(Mobile);

            // Zones
            if (YandexGame.savesData.Sing.ActivatedZoneIds != null)
            {
                for (int i = 0; i < YandexGame.savesData.Sing.ActivatedZoneIds.Length; ++i)
                {
                    Zone Zone = GetZoneById(YandexGame.savesData.Sing.ActivatedZoneIds[i]);

                    if (Zone != null)
                    {
                        Zone.DeactivateBorder();
                    }
                }
            }

            // Skins
            var UnlockedSkins = YandexGame.savesData.Sing.UnlockedSkins;
            if (UnlockedSkins != null)
            {
                for (int i = 0; i < UnlockedSkins.Length; ++i)
                {
                    var Stand = GetStandBySkinId(UnlockedSkins[i]);

                    if (Stand != null)
                    {
                        Stand.UnlockSkinAndStand();
                    }
                }
            }

            // Next Skin Progress
            m_UINextSkinProgress.Init();
            m_UINextSkinProgress.UpdateUI();

            // Rebirth
            m_RebirthMoneyMultiplier       = YandexGame.savesData.Sing.RebirthMoneyMultiplier;
            m_RebirthMoneyNeededToProgress = YandexGame.savesData.Sing.RebirthMoneyNeededToProgress;
            UpdateRebirthMenu();

            // Set state
            m_Loaded = true;

            // Try get payments again
            YandexGame.GetPayments();
        }

        private void Awake()
        {
            // We need Game Manager to detect mobile devices 
            var _ = GameManager.Instance;

            LeanTween.init(800);

            Assert.IsTrue(m_SingTransforms.Count > 0);

            m_SingTransformsUsed.Capacity = m_SingTransforms.Count;

            for (int i = 0; i < m_SingTransforms.Count; ++i)
            {
                m_SingTransformsUsed.Add(false);
            }

            Assert.IsNotNull(m_AuthMenu);
            CloseAuthMenu();

            Assert.IsNotNull(m_InterADTextOneSec);
            Assert.IsNotNull(m_InterADTextTwoSec);

            Assert.IsNotNull(m_SpawnPoint);

            Assert.IsNotNull(m_RebirthTransform);
            // m_RebirthInitialPosition = m_RebirthTransform.localPosition;

            Assert.IsNotNull(m_RebirthMultiplierText);
            Assert.IsNotNull(m_RebirthButton);
            Assert.IsNotNull(m_RebirthReadySign);
            Assert.IsNotNull(m_RebirthProgressBar);
            Assert.IsNotNull(m_RebirthProgressBarText);
            Assert.IsNotNull(m_RebirthMultiplier);
            Assert.IsNotNull(m_RebirthMenu);
            OnCloseRebirthMenu();

            const float RebirthReadySignScale = 1.2f;
            LeanTween.scale(m_RebirthReadySign.rectTransform, new Vector3(RebirthReadySignScale, RebirthReadySignScale, RebirthReadySignScale), 0.5f)
                .setLoopPingPong();

            Assert.IsNotNull(m_CoinsVFXPrefab);
            Assert.IsNotNull(m_CoinsVFXTransform);

            Assert.IsNotNull(m_NotificationText);
            Assert.IsNotNull(m_GuideLine);

            Assert.IsNotNull(m_MoneyNotificationPrefab);
            Assert.IsNotNull(m_Canvas);
            Assert.IsNotNull(m_CanvasScaler);
            Assert.IsNotNull(m_HUDTransform);

            Assert.IsNotNull(m_SettingsTransform);
            m_SettingsTransform.gameObject.SetActive(false);

            Assert.IsNotNull(m_UINextSkinProgress);

            // m_Tutorial.TutorialArrow.gameObject.SetActive(false);

            SkinBuyingStand[] Stands = FindObjectsByType<SkinBuyingStand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < Stands.Length; ++i)
            {
                m_SkinBuyingStands.Add(Stands[i]);
            }
            m_SkinBuyingStands.Sort((x, y) => { return (int)(x.Cost - y.Cost); });

            Zone[] Zones = FindObjectsByType<Zone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            m_Zones.Capacity = Zones.Length;
            for (int i = 0; i < Zones.Length; ++i)
            {
                m_Zones.Add(Zones[i]);
            }

            m_ZoneZero = GetZoneById(0);
            Assert.IsNotNull(m_ZoneZero);

            Assert.IsNotNull(m_ParkourZone);

            Controller[] Controllers = FindObjectsByType<Controller>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < Controllers.Length; ++i)
            {
                var Controller = Controllers[i];

                m_Controllers.Add(Controller);
                Controller.PreInit();
            }

            TeleportCharacterOnSpawn(GameManager.Instance.Player.Owner);

            for (int i = 0; i < m_FloatingStuff.Count; ++i)
            {
                var Transform = m_FloatingStuff[i];
                if (Transform == null)
                {
                    continue;
                }

                LeanTween.moveY(Transform.gameObject, Transform.position.y + m_FloatingStuffYOffset, m_FloatingStuffAnimTime)
                    .setEaseLinear()
                    .setLoopPingPong();
            }

            for (int i = 0; i < m_DesktopOnlyTransforms.Count; ++i)
            {
                m_DesktopOnlyTransforms[i].gameObject.SetActive(false);
            }

            if (!YandexGame.SDKEnabled)
            {
                YandexGame.GetDataEvent += OnSDKLoaded;
            }

            YandexGame.PurchaseSuccessEvent += OnInappPaymentSuccess;
            YandexGame.PurchaseFailedEvent  += OnInappPaymentFail;
            YandexGame.GetPaymentsEvent     += OnInappPaymentsUpdate;
        }

        private void Start()
        {
            // Localization event
            LocalizationManager.Instance.OnRefresh += OnLocalizationRefresh;

            // In case we reload scene
            if (YandexGame.SDKEnabled && !m_Loaded)
            {
                OnSDKLoaded();
            }

            // Rebirth
            GameManager.Instance.Player.OnMoneyChanged += OnMoneyChangedRebirth;
            OnMoneyChangedRebirth();

            // Stuck
            GameManager.Instance.Player.CharMoveController.OnStuck += TeleportPlayerOnSpawn;
        }
        /** === End Initialization === */
    }
}

#endif
