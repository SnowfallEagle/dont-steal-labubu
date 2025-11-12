#if GAME_COOK

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;
using MirraGames.SDK;
using MirraGames.SDK.Common;
using JetBrains.Annotations;

namespace Cook
{
    [Serializable]
    public class GameSave
    {
        public bool FirstLaunch = true;

        public double Money = 0d;
        public int PlayerSpeedLevel = 1;

        public int CookVersion = 4;
        public int TutorialVersion = 1;

        public Cook.PlatformSaveData[] Platforms = null;
        public Cook.ItemSlotSaveData[] Inventory = null;
        public string TutorialStage = "None";

        public string[] BoughtInapps = null;
    }

    [Serializable]
    public struct ItemFoodSaveData
    {
        public string Type;
        public float Progress;
        public float WeightKg;

        public Vector3 LocalPositionOnPlatform;

        public string BrainrotState;
        public int Level;
    }

    [Serializable]
    public struct ItemSaveData
    {
        public string Id;
        public string PlatformId;
        public float ValueToSell;
        public ItemFoodSaveData Food;
    }

    [Serializable]
    public struct ItemSlotSaveData
    {
        public ItemSaveData Item;
        public int Count;
    }

    [Serializable]
    public struct PlatformSaveData
    {
        public string Id;
        public ItemSaveData Chef;
        public ItemSaveData[] Food;
        public ItemSaveData Brainrot;
    }

    public struct ItemDBEntry
    {
        public GameObject Prefab;
        public Item Item;
        public ItemBalanceData Balance;
    }

    public enum ETutorialStage : int
    {
        None,
        EquipFoodInInventory,
        PlaceFood,
        WaitPreparing,
        TakeFood,
        Sell,
        BuyNew,
        Completed
    }

    [Serializable]
    public class TutorialData
    {
        [Header("Stage")]
        [SerializeField] public ETutorialStage Stage = ETutorialStage.None;

        [Header("UI")]
        [SerializeField] public RectTransform TutorialOverlay;
        [SerializeField] public TextMeshProUGUI TutorialText;
        [SerializeField] public RectTransform TutorialArrow;

        [SerializeField] public RectTransform TeleportButtonSellTransform;
        [SerializeField] public RectTransform TeleportButtonBuyTransform;
        [SerializeField] public RectTransform TeleportButtonCookTransform;

        [Header("Prefabs")]
        [SerializeField] public GameObject FirstFoodPrefab;

        [Header("Guide Objects")]
        [SerializeField] public CookPlatform GuidePlacePlatform;
        [SerializeField] public Transform GuidePlace;
        [SerializeField] public Transform GuideSell;
        [SerializeField] public Transform GuideBuy;

        [Header("Temprorary Data")]
        [NonSerialized] public RectTransform EquipItemSlotTransform;
    }

    /** Begin AI */
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

        [Header("Shop Points")]
        public Transform SellButtonTransform;
        public Transform ChefButtonTransform;
        public Transform FoodButtonTransform;

        [Header("Random Points")]
        public Transform[] StreetRandomPoints;
    }
    /** End AI */

    public class CookManager : Singleton<CookManager>
    {
        public Action OnInventoryItemEquipped;
        public Action OnFoodPlaced;
        public Action OnFoodPrepared;
        public Action OnFoodTaken;
        public Action OnFoodSold;
        public Action OnFoodBought;

        [Header("Wake")]
        public BoxCollider BrainrotSpawnZone;
        public BoxCollider BrainrotZone;
        [NonSerialized] public List<Item> BrainrotsToSteal = new();
        public int BrainrotsToStealCount = 25;
        public List<GameObject> BrainrotPrefabsUsual = new();
        public List<GameObject> BrainrotPrefabsSpecial = new();
        public float BrainrotSpecialProbability = 0.1f;

        public RectTransform InventoryWrapper;
        public RectTransform DropButtonWrapper;

        public float DefaultPlayerSpeed = 7.5f;
        public int PlayerSpeedLevel = 1;

        public ZoneSteal StealZone;

        public GameObject AlarmPrefab;
        public float AlarmScale = 10f;

        public float SavedLastTime = 0f;
        public float SaveTimer = 30f;

        public float UpdatePlatformRevenueUITimer = 0f;

        public RectTransform PlaceBrainrotOverlay;
        public CookPlatform PlatformToPlaceBrainrot;

        [Header("HUD/Level Upgrade")]
        [SerializeField] private RectTransform m_LevelUpgradeOverlay;

        [SerializeField] private float m_LevelUpgradeDotThreshold = 0.8f;
        [SerializeField] private float m_LevelUpgradeDistanceThreshold = 5f;
        [SerializeField] private float m_LevelUpgradeOverlayMinScale = 0.5f;

        private CookPlatform m_PlatformToUpgrade;

        [Header("AI")]
        [SerializeField] private AIData m_AI = new();
        public AIData AI => m_AI;

        private List<Controller> m_Controllers = new();

        [Header("Items")]
        [SerializeField] private List<GameObject> m_AllItemPrefabs = new();
        private Dictionary<string, ItemDBEntry> m_ItemDB = new();
        public Dictionary<string, ItemDBEntry> ItemDB => m_ItemDB;

        [Header("Growth")]
        private Dictionary<string, float> m_FoodGrowthMultiplier = new();
        [SerializeField] private RectTransform m_FoodGrowthMultiplierTransform;
        [SerializeField] private TextMeshProUGUI m_FoodGrowthMultiplierText;

        [Header("Platforms")]
        private Dictionary<string, CookPlatform> m_PlatformDB = new();

        [Header("Player Places")]
        private Dictionary<int, PlayerPlace> m_PlayerPlaceDB = new();

        [Header("Shop & Sell")]
        [SerializeField] private List<UIShopMenu> m_ShopMenus = new ();
        private UIShopMenu m_ShopMenuFood;
        public UIShopMenu ShopMenuFood => m_ShopMenuFood;
        private UIShopMenu m_ShopMenuChef;
        public UIShopMenu ShopMenuChef => m_ShopMenuChef;
        [SerializeField] private TextMeshPro m_SellFoodMoneyText;

        [Header("HUD/Hovered Item")]
        [SerializeField] private TextMeshProUGUI m_ItemHoverText;
        [SerializeField] private Vector2 m_ItemHoverTextOffset = new Vector2(100f, 100f);

        private Item m_HoveredItem;

        [Header("HUD/Collect Item")]
        [SerializeField] private RectTransform m_CollectItemOverlay;
        [SerializeField] private TextMeshProUGUI m_CollectItemName;

        [SerializeField] private float m_CollectItemDotThreshold = 0.8f;
        [SerializeField] private float m_CollectItemDistanceThreshold = 5f;
        [SerializeField] private float m_CollectItemOverlayMinScale = 0.5f;

        private Item m_ItemToCollect;

        [Header("HUD/Notification")]
        [SerializeField] private TextMeshProUGUI m_NotificationText;

        [Header("HUD/Settings")]
        [SerializeField] private RectTransform m_SettingsMenu;

        [Header("VFX")]
        [SerializeField] private GameObject m_CoinsVFXPrefab;
        [SerializeField] private Transform m_CoinsVFXTransform;
        [SerializeField] private float m_CoinsVFXTime = 1f;

        [Header("Tutorial")]
        [SerializeField] private TutorialData m_Tutorial;
        public ref TutorialData Tutorial => ref m_Tutorial;
        [SerializeField] private GuidanceLine.GuidanceLine m_GuidanceLine;

        public RectTransform TutorialPanelWrapper;
        public RectTransform TutorialDesktopWrapper;
        public RectTransform TutorialMobileWrapper;
        public RectTransform TutorialMobileRUWrapper;
        public RectTransform TutorialMobileENWrapper;

        [Header("Auth")]
        private bool m_Authorized = false;
        public bool Authorized => m_Authorized;
        private bool m_InitiallyWasAuthorized = false;

        [SerializeField] private RectTransform m_AuthMenu;
        private string m_AuthQueuedInapp = null;

        [Header("SDK")]
        public bool GameLoaded = false;
        public GameSave Save = new();

        [Header("AD")]
        [SerializeField] private TextMeshProUGUI m_InterADText;
        [SerializeField] private Image m_InterADFadeImage;
        private Coroutine m_InterADCoroutine = null;

        [Header("Rewards")]
        private Dictionary<string, Reward> m_RewardsDB = new();

        public float RewardSpeedMultiplier = 1f;
        public float RewardMoneyMultiplier = 1f;

        [Header("Reward Progress")]
        [SerializeField] private RectTransform m_RewardProgressWrapper;
        public RectTransform RewardProgressWrapper => m_RewardProgressWrapper;
        [SerializeField] private Image m_RewardProgressImage;
        public Image RewardProgressImage => m_RewardProgressImage;

        [Header("Inapp")]
        private Dictionary<string, Inapp> m_InappsDB = new();

        [Header("Platform Specific")]
        public List<GameObject> DesktopOnly = new();
        public List<GameObject> MobileOnly = new();

        /** Begin Reward */
        public void ShowRewarded(string Id)
        {
            if (!AdManager.Instance.CanShowRewarded())
            {
                return;
            }

            Reward Reward = QueryReward(Id);
            if (Reward == null)
            {
                return;
            }

            if (Reward.Activated)
            {
                return;
            }

            AdManager.Instance.ShowRewarded(Id, OnOpenRewarded, OnCloseRewarded);
        }

        public void OnOpenRewarded()
        {
            Debug.Log($"{nameof(OnOpenRewarded)}() called");

            OnBeforeInter();
        }

        public void OnCloseRewarded(bool Success)
        {
            Debug.Log($"{nameof(OnCloseRewarded)}(Success={Success}) called");

            if (Success)
            {
                ActivateReward(AdManager.Instance.RewardedId);
            }

            OnCloseInter();
        }

        private void ActivateReward(Reward Reward)
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
                    RewardMoneyMultiplier = 2f;
                    break;

                case "reward_speed_x2":
                    RewardSpeedMultiplier = 2f;
                    UpdatePlayerSpeed();
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
        public void DeactivateReward(Reward Reward)
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
                    RewardMoneyMultiplier = 1f;
                    break;

                case "reward_speed_x2":
                    RewardSpeedMultiplier = 1f;
                    UpdatePlayerSpeed();
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

        private Reward QueryReward(string Key)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return null;
            }

            if (!m_RewardsDB.TryGetValue(Key, out Reward Reward))
            {
                return null;
            }

            return Reward;
        }
        /** End Reward */

        /** Begin Overlay Place */
        public void OnPlaceBrainrot()
        {
            if (PlatformToPlaceBrainrot == null)
            {
                return;
            }

            var Player = GameManager.Instance.Player;
            var Inv = Player.Inventory;

            if (!Inv.HasEquippedItem)
            {
                return;
            }

            if (Inv.Slots[Inv.EquippedSlot].Item.Type != EItemType.Food)
            {
                return;
            }

            var Items = Inv.RemoveFully(Inv.EquippedSlot);
            if (Items.Length <= 0)
            {
                return;
            }

            var Item = Items[0];
            Item.transform.localPosition = Vector3.zero;
            Item.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Item.transform.SetParent(PlatformToPlaceBrainrot.transform, false);
            Item.Platform = PlatformToPlaceBrainrot;

            PlatformToPlaceBrainrot.Brainrot = Item;
            SoundController.Instance.Play("ButtonClick");


            // Animation
            LeanTween.cancel(Item.gameObject, false);

            LeanTween.moveLocalY(Item.gameObject, Item.gameObject.transform.localPosition.y + 1f, 0.5f)
                .setEaseInBounce()
                .setLoopPingPong(1);

            float Rot = 360f;
            LeanTween.rotateAroundLocal(Item.gameObject, Vector3.up, Rot, 1f).setEaseOutCubic();

            // Save
            SaveGameFully();
        }

        public void ShowPlaceBrainrot(CookPlatform Platform)
        {
            if (Platform == null || Platform.Brainrot != null)
            {
                HidePlaceBrainrot();
                return;
            }

            Vector3 Position = Platform.transform.position;
            Vector3 ScreenPos = Camera.main.WorldToScreenPoint(Position);
            ScreenPos.z = 0f;
            PlaceBrainrotOverlay.transform.position = ScreenPos;

            float Distance = (Position - Camera.main.transform.position).magnitude;

            // Scale X / Scale Max = Distance X / Distance Max
            // Scale X = (Distance X * ScaleMax) / Distance Max

            const float MaxScale = 1f;
            float ScaleFactor = (Distance * MaxScale) / m_CollectItemDistanceThreshold;

            ScaleFactor = Mathf.Clamp(ScaleFactor, m_CollectItemOverlayMinScale, MaxScale);
            PlaceBrainrotOverlay.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

            PlaceBrainrotOverlay.gameObject.SetActive(true);
            PlatformToPlaceBrainrot = Platform;
        }

        public void HidePlaceBrainrot()
        {
            PlaceBrainrotOverlay.gameObject.SetActive(false);
            PlatformToPlaceBrainrot = null;
        }
        /** End Overlay Place */

        /** Begin Overlay Level Upgrade */
        public void OnLevelUpgradeButton()
        {
            if (m_PlatformToUpgrade == null)
            {
                return;
            }

            m_PlatformToUpgrade.TryUpgradeBrainrot();
            HideLevelUpgrade();
        }

        public void ShowLevelUpgrade(CookPlatform Platform)
        {
            if (Platform == null || Platform.Brainrot == null || Platform.Owner == null || Platform.Owner.AIController)
            {
                HideLevelUpgrade();
                return;
            }

            Vector3 Position = Platform.LevelUpgradeWrapper.position;
            Vector3 ScreenPos = Camera.main.WorldToScreenPoint(Position);
            ScreenPos.z = 0f;
            m_LevelUpgradeOverlay.transform.position = ScreenPos;

            float Distance = (Position - Camera.main.transform.position).magnitude;

            // Scale X / Scale Max = Distance X / Distance Max
            // Scale X = (Distance X * ScaleMax) / Distance Max

            const float MaxScale = 1f;
            float ScaleFactor = (Distance * MaxScale) / m_LevelUpgradeDistanceThreshold;

            ScaleFactor = Mathf.Clamp(ScaleFactor, m_LevelUpgradeOverlayMinScale, MaxScale);
            m_LevelUpgradeOverlay.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

            m_LevelUpgradeOverlay.gameObject.SetActive(true);
            m_PlatformToUpgrade = Platform;
        }

        public void HideLevelUpgrade()
        {
            m_LevelUpgradeOverlay.gameObject.SetActive(false);
            m_PlatformToUpgrade = null;
        }
        /** End Overlay Level Upgrade */

        /** Begin AI */
        public Transform GetRandomPointAI(Controller AI, ERandomPointAI Selection)
        {
            List<Transform> Points = new();

            void Fill(Transform[] PointsToFill)
            {
                for (int i = 0; i < PointsToFill.Length; ++i)
                {
                    Points.Add(PointsToFill[i]);
                }
            }

            if (EnumUtils.Any(Selection & ERandomPointAI.AIPlace))
            {
                Fill(AI.PlayerPlace.RandomPoints);
            }
            if (EnumUtils.Any(Selection & ERandomPointAI.OthersPlace))
            {
                foreach (var kv in m_PlayerPlaceDB)
                {
                    if (kv.Value.Owner.PlayerController)
                    {
                        // ~1% chance to go to player
                        if (UnityEngine.Random.value < 0.99f)
                        {
                            continue;
                        }
                    }

                    Fill(kv.Value.RandomPoints);
                }
            }
            if (EnumUtils.Any(Selection & ERandomPointAI.Street))
            {
                Fill(m_AI.StreetRandomPoints);
            }

            return Points.Count <= 0 ? null : Points[UnityEngine.Random.Range(0, Points.Count)];
        }
        /** End AI */

        // Should be somewhere else but fine for now
        public void SaveMoney(bool SaveGame = true)
        {
            Save.Money = GameManager.Instance.Player.Money;

            if (SaveGame)
            {
                SaveGameFully();
            }
        }

        public void SaveGameFully()
        {
            Debug.Log("Saving Game Fully..");

            // Money
            SaveMoney(false);

            // Speed
            Save.PlayerSpeedLevel = PlayerSpeedLevel;

            // Inventory
            var Inventory = GameManager.Instance.Player.Inventory;
            Inventory.Save(out Save.Inventory);

            // Platforms
            PlayerPlace PlayerPlace = null;
            foreach (var kv in m_PlayerPlaceDB)
            {
                if (kv.Value.Owner.PlayerController)
                {
                    PlayerPlace = kv.Value;
                    break;
                }
            }

            Assert.IsNotNull(PlayerPlace);

            Save.Platforms = new PlatformSaveData[PlayerPlace.Platforms.Length];

            for (int i = 0; i < PlayerPlace.Platforms.Length; ++i)
            {
                PlayerPlace.Platforms[i].Save(ref Save.Platforms[i]);
            }

            // Tutorial
            Save.TutorialStage = m_Tutorial.Stage.ToString();

            // Inapps
            List<string> Inapps = new();

            // Previously bought inapps.
            // It's important, because thay may not be activated when function is called.
            // This function can be called from OnSDKLoaded before inapps activation.
            if (Save.BoughtInapps != null)
            {
                Inapps.AddRange(Save.BoughtInapps);
            }

            foreach (var kv in m_InappsDB)
            {
                // Usually it will represent newly activated Inapps that haven't been saved yet
                if (kv.Value.Activated && !Inapps.Exists((Id) => Id == kv.Key))
                {
                    Inapps.Add(kv.Key);
                }
            }

            Save.BoughtInapps = Inapps.ToArray();

            // Save
            if (MirraSDK.IsInitialized)
            {
                MirraSDK.Data.SetObject("Save", Save, true);
            }
            else
            {
                Debug.Log($"SaveGameFully: MirraSDK is not initialized but save called....");
            }
            // Debug.Log($"SaveGameFully: SAVE OBJ: {UnityEngine.JsonUtility.ToJson(Save, true)}");
            Debug.Log($"SaveGameFully: SAVE");

            SavedLastTime = Time.time;
        }

        public void UpdateSellFoodMoneyText()
        {
            var Inventory = GameManager.Instance.Player.Inventory;

            double SellTotal = 0f;

            for (int i = 0; i < Inventory.MaxSlots; ++i)
            {
                var Item = Inventory.Slots[i].Item;

                if (Item && Item.Type == EItemType.Food && Item.Food.BrainrotState == EBrainrotState.Stolen)
                {
                    SellTotal += Item.GetBrainrotRevenuePerSec();
                }
            }

            m_SellFoodMoneyText.text = $"${SellTotal:F0}";
        }

        public float ComputeGrowthSpeed()
        {
            float Growth = 1f;
#if UNITY_EDITOR && false
            Growth *= 100f;
#endif

            foreach (var kv in m_FoodGrowthMultiplier)
            {
                Growth *= kv.Value;
            }

            return Growth;
        }

        public void OpenSDKAuthDialog()
        {
            Assert.IsTrue(!m_InitiallyWasAuthorized && !m_Authorized);

            MirraSDK.Player.InvokeLogin(
                // Success
                () =>
                {
                    m_Authorized = true;

                    /*
                    if (!string.IsNullOrEmpty(m_AuthQueuedInapp) && YandexGame.PurchaseByID(m_AuthQueuedInapp) != null)
                    {
                        Debug.Log($"{nameof(OnTryToDetectAuthorization)}: {nameof(m_Authorized)}=true, {m_AuthQueuedInapp}={m_AuthQueuedInapp}");

                        YandexGame.BuyPayments(m_AuthQueuedInapp);
                        m_AuthQueuedInapp = null;
                    }
                    */

                    Debug.Log($"{nameof(OpenSDKAuthDialog)}: Succeed to auth player");
                },

                // Failure
                () =>
                {
                    Debug.Log($"{nameof(OpenSDKAuthDialog)}: Failed to auth player");
                });
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

        public void PlaySellCoinsVFX()
        {
            var Player = GameManager.Instance.Player.transform;
            GameObject VFXObject = Instantiate(m_CoinsVFXPrefab, Player.position, m_CoinsVFXTransform.rotation);
            Destroy(VFXObject, m_CoinsVFXTime);
        }

        private void UpdateShopElementsAvailability()
        {
            for (int i = 0; i < m_ShopMenus.Count; ++i)
            {
                m_ShopMenus[i].UpdateBalanceAndUI();
            }
        }

        public bool QueryItem(string Id, out ItemDBEntry Entry)
        {
            if (Id == null || Id == Item.NoneId)
            {
                Entry = new();
                return false;
            }

            if (!m_ItemDB.TryGetValue(Id, out Entry))
            {
                return false;
            }

            return true;
        }

        public void SetItem(string Id, in ItemDBEntry Entry)
        {
            if (Id == null || !m_ItemDB.ContainsKey(Id))
            {
                return;
            }

            m_ItemDB[Id] = Entry;
        }

        public CookPlatform QueryPlatform(string Id)
        {
            if (Id == null || Id == CookPlatform.NoneId)
            {
                return null;
            }

            if (!m_PlatformDB.TryGetValue(Id, out CookPlatform Platform))
            {
                return null;
            }

            return Platform;
        }

        public void OnItemHovered(Item HoveredItem, Vector2 ScreenPos)
        {
            if (!HoveredItem)
            {
                OnItemUnhovered();
                return;
            }

            if (HoveredItem == m_ItemToCollect)
            {
                return;
            }

            // We try not to call EnableOutline so we don't do anything with shader material properties
            if (m_HoveredItem != HoveredItem)
            {
                OnItemUnhovered();
                HoveredItem.EnableOutline(Color.white);
            }

            // We need this offset so EventsSystem.IsPointerOverGameObject() doesn't react to this UI Text
            ScreenPos += m_ItemHoverTextOffset;

            m_ItemHoverText.gameObject.SetActive(true);
            m_ItemHoverText.rectTransform.position = new Vector3(ScreenPos.x, ScreenPos.y, 0f);

            if (HoveredItem.Type == EItemType.Food)
            {
                m_ItemHoverText.text = $"{LocalizationManager.Instance.GetTranslation(HoveredItem.Id)}";
            }
            else if (HoveredItem.Type == EItemType.Chef)
            {
                m_ItemHoverText.text = $"{LocalizationManager.Instance.GetTranslation(HoveredItem.Id)}<br>{($"+{(HoveredItem.Chef.SpeedMultiplier-1f)*100f}% {LocalizationManager.Instance.GetTranslation("ui_hover_overlay_chef_boost")}"):F0}";
            }

            m_HoveredItem = HoveredItem;
        }

        public void OnItemUnhovered()
        {
            m_ItemHoverText.gameObject.SetActive(false);

            if (m_HoveredItem)
            {
                if (m_HoveredItem != m_ItemToCollect)
                {
                    // We don't own the outline when Item can be Collected
                    m_HoveredItem.DisableOutline();
                }
                m_HoveredItem = null;
            }
        }

        private void OnShowItemToCollect(Item ItemToCollect)
        {
            if (ItemToCollect == null || !InventoryWrapper.gameObject.activeSelf)
            {
                OnHideItemToCollect();
                return;
            }

            Vector3 Position = ItemToCollect.transform.position;
            Vector3 ScreenPos = Camera.main.WorldToScreenPoint(Position);
            ScreenPos.z = 0f;
            m_CollectItemOverlay.transform.position = ScreenPos;

            float Distance = (Position - Camera.main.transform.position).magnitude;

            // Scale X / Scale Max = Distance X / Distance Max
            // Scale X = (Distance X * ScaleMax) / Distance Max

            const float MaxScale = 1f;
            float ScaleFactor = (Distance * MaxScale) / m_CollectItemDistanceThreshold;

            ScaleFactor = Mathf.Clamp(ScaleFactor, m_CollectItemOverlayMinScale, MaxScale);
            m_CollectItemOverlay.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

            if (ItemToCollect == m_ItemToCollect)
            {
                return;
            }

            OnHideItemToCollect();

            m_ItemToCollect = ItemToCollect;
            m_ItemToCollect.EnableOutline(Color.green);

            m_CollectItemOverlay.gameObject.SetActive(true);
            m_CollectItemName.text = LocalizationManager.Instance.GetTranslation(ItemToCollect.Id);
        }

        private void OnHideItemToCollect()
        {
            m_CollectItemOverlay.gameObject.SetActive(false);

            if (m_ItemToCollect)
            {
                m_ItemToCollect.DisableOutline();
                m_ItemToCollect = null;
            }
        }

        public void OnCollectItem()
        {
            if (m_ItemToCollect == null)
            {
                return;
            }

            if (m_ItemToCollect.Platform != null)
            {
                m_ItemToCollect.Platform.Brainrot = null;
                m_ItemToCollect.Platform = null;
            }

            int ItemSlot = GameManager.Instance.Player.Inventory.Add(m_ItemToCollect);
            if (InventorySlot.NoneIdx == ItemSlot)
            {
                return;
            }

            GameManager.Instance.Player.Inventory.Equip(ItemSlot);
            if (m_ItemToCollect.Food.BrainrotState == EBrainrotState.Steal)
            {
                m_ItemToCollect.Food.BrainrotState = EBrainrotState.Stealing;
                InventoryWrapper.gameObject.SetActive(false);
                DropButtonWrapper.gameObject.SetActive(true);

                UpdatePlayerSpeed();

                var PlayerTransform = GameManager.Instance.Player.transform;

                GameObject GO = Instantiate(AlarmPrefab, PlayerTransform.position, Quaternion.identity);

                Vector3 OriginalScale = GO.transform.localScale;
                Vector3 Scale = OriginalScale * AlarmScale;
                Scale.y = OriginalScale.y;
    
                LeanTween.scale(GO, Scale, 2f).setOnComplete(() =>
                {
                    Destroy(GO);
                });
            }

            OnHideItemToCollect();

            SoundController.Instance.Play("ButtonClick");
        }

        public void UpdatePlayerSpeed()
        {
            var Player = GameManager.Instance.Player;
            var Inv = Player.Inventory;

            float EquippedItemSpeedMul = 1f;
            if (Inv.HasEquippedItem && Inv.Slots[Inv.EquippedSlot].Item.Food.BrainrotState == EBrainrotState.Stealing)
            {
                EquippedItemSpeedMul *= Inv.Slots[Inv.EquippedSlot].Item.Food.SpeedMultiplier;
            }

            Player.CharMoveController.moveSpeed = DefaultPlayerSpeed * RewardSpeedMultiplier * EquippedItemSpeedMul + (0.25f * (PlayerSpeedLevel-1));
        }

        public void OnRevertBrainrotStealing(bool SaveItem, bool DeleteItem = false)
        {
            var Inv = GameManager.Instance.Player.Inventory;
            var Item = Inv.HasEquippedItem ? Inv.Slots[Inv.EquippedSlot].Item : null;

            if (SaveItem)
            {
                if (Item != null && Item.Type == EItemType.Food && Item.Food.BrainrotState == EBrainrotState.Stealing)
                {
                    Item.Food.BrainrotState = EBrainrotState.Stolen;
                    BrainrotsToSteal.Remove(Item);
                    SaveGameFully();

                    SoundController.Instance.Play("Success");
                }
            }
            else
            {
                if (Item != null && Item.Type == EItemType.Food && Item.Food.BrainrotState == EBrainrotState.Stealing)
                {
                    Item[] Items = Inv.RemoveFully(Inv.EquippedSlot);

                    if (DeleteItem)
                    {
                        if (Items.Length > 0)
                        {
                            Destroy(Items[0].gameObject);
                        }
                    }
                    else
                    {
                        if (Items.Length > 0)
                        {
                            Vector3 Pos = GameManager.Instance.Player.transform.position;
                            Pos.y = StealZone.Collider.bounds.min.y;
                            Items[0].transform.position = Pos;
                            Items[0].Food.BrainrotState = EBrainrotState.Steal;
                            Items[0].Food.RespawnTimeLeft = Items[0].Food.RespawnTimer;
                        }
                    }
                }
            }

            InventoryWrapper.gameObject.SetActive(true);
            DropButtonWrapper.gameObject.SetActive(false);
            UpdatePlayerSpeed();
        }

        public void OnDropButton()
        {
            OnRevertBrainrotStealing(false);
        }

        public void OnPlayerDied()
        {
            OnRevertBrainrotStealing(false, true);

            TeleportManager.Instance.OnHome();

            SoundController.Instance.Play("Death");
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F1))
            {
                OnInappPaymentSuccess("inapp_turn_off_ads");
            }
#endif

            if (MirraSDK.Time.Scale <= 0f)
            {
                return;
            }

            if (GameLoaded && GameManager.Instance.Desktop)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    ToggleSettingsMenu();
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    ShowRewarded("reward_coins_x2");
                }
                else if (Input.GetKeyDown(KeyCode.T))
                {
                    ShowRewarded("reward_speed_x2");
                }
            }

            if (Time.time - SavedLastTime > SaveTimer)
            {
                SaveGameFully();
            }

            var Player = GameManager.Instance.Player;
            Item Item = null;
            Item BestItemToCollect = null;

            // Wake stuff
            List<Item> FoodToSteal = new();
            bool FirstUpdate = BrainrotsToSteal.Count <= 0;

            for (int i = 0; i < BrainrotsToSteal.Count; ++i)
            {
                var Brainrot = BrainrotsToSteal[i];

                if (Brainrot == null)
                {
                    continue;
                }

                Brainrot.UpdateBrainrotText();

                if (Brainrot.Food.BrainrotState != EBrainrotState.Steal)
                {
                    continue;
                }

                FoodToSteal.Add(Brainrot);

                Brainrot.Food.RespawnTimeLeft -= Time.deltaTime;

                if (Brainrot.Food.RespawnTimeLeft <= 0f)
                {
                    Destroy(Brainrot.gameObject);
                    BrainrotsToSteal[i] = null;
                }
            }

            BrainrotsToSteal.RemoveAll((x) => x == null);

            int NewBrainrots = BrainrotsToStealCount - BrainrotsToSteal.Count;
            for (int i = 0; i < NewBrainrots; ++i)
            {
                Vector3 Pos = new Vector3(
                    UnityEngine.Random.Range(BrainrotSpawnZone.bounds.min.x, BrainrotSpawnZone.bounds.max.x),
                    BrainrotSpawnZone.bounds.min.y,
                    UnityEngine.Random.Range(BrainrotSpawnZone.bounds.min.z, BrainrotSpawnZone.bounds.max.z)
                );
                Quaternion Rot = Quaternion.Euler(0f, UnityEngine.Random.value * 180f, 0f);

                List<GameObject> PrefabList = UnityEngine.Random.value < BrainrotSpecialProbability ?
                    BrainrotPrefabsSpecial :
                    BrainrotPrefabsUsual;

                GameObject Prefab = PrefabList[UnityEngine.Random.Range(0, PrefabList.Count)];
                GameObject GO = Instantiate(Prefab, Pos, Rot);

                Item = GO.GetComponent<Item>();
                if (Item != null)
                {
                    var Stage = Item.Food.Stages[Item.Food.Stages.Count - 1];
                    Item.UpdateFoodProgress(Stage.Threshold / Item.Food.ProgressRate + 1f, true);

                    if (FirstUpdate)
                    {
                        Item.Food.RespawnTimeLeft = UnityEngine.Random.Range(0f, Item.Food.RespawnTimer);
                    }
                    else
                    {
                        Item.Food.RespawnTimeLeft = Item.Food.RespawnTimer;
                    }

                    BrainrotsToSteal.Add(Item);
                }
                else
                {
                    Destroy(GO);
                }
            }

            CookPlatform FreePlatform = null;
            foreach (var p in Player.Owner.PlayerPlace.Platforms)
            {
                if (p != null && p.Brainrot == null)
                {
                    FreePlatform = p;
                    break;
                }
            }

            bool HasBrainrotInInv = false;

            for (int i = 0; i < Inventory.MaxSlots; ++i)
            {
                if (!Player.Inventory.Slots[i].Filled)
                {
                    continue;
                }

                Item = Player.Inventory.Slots[i].Item;

                if (Item.Type == EItemType.Food)
                {
                    HasBrainrotInInv = true;
                    break;
                }
            }

            /*
            bool IsStealing = false;

            var Inv = Player.Inventory;
            if (Inv.HasEquippedItem && Inv.Slots[Inv.EquippedSlot].Item.Type == EItemType.Food && Inv.Slots[Inv.EquippedSlot].Item.Food.BrainrotState == EBrainrotState.Stealing)
            {
                IsStealing = true;
            }
            */

            if (HasBrainrotInInv)
            {
                if (FreePlatform != null)
                {
                    SetGuidingLine(FreePlatform.transform);
                }
                else
                {
                    SetGuidingLine(AI.SellButtonTransform);
                }
            }
            else
            {
                Item BestB = null;
                float BestD = float.MaxValue;

                Vector3 PlayerPos = Player.transform.position;

                foreach (var b in BrainrotsToSteal)
                {
                    if (b == null)
                    {
                        continue;
                    }

                    Vector3 BPos = b.transform.position;
                    float Dist = (BPos - PlayerPos).sqrMagnitude;

                    if (Dist < BestD)
                    {
                        BestB = b;
                        BestD = Dist;
                    }
                }

                SetGuidingLine(BestB.transform);
            }

            // Update platforms
            CookPlatform PlatformToPlace = null;

            foreach (var kv in m_PlatformDB)
            {
                var Platform = kv.Value;

                if (Platform.PlayerHere)
                {
                    if (Platform.Brainrot != null)
                    {
                        BestItemToCollect = Platform.Brainrot;
                    }
                    else if (Player.Inventory.HasEquippedItem && Player.Inventory.Slots[Player.Inventory.EquippedSlot].Item.Type == EItemType.Food)
                    {
                        PlatformToPlace = Platform;
                    }
                }

                if (Platform.Brainrot != null)
                {
                    Platform.Brainrot.UpdateBrainrotText();
                }

                Platform.OnMoneyUpdate(true);
                Platform.OnUpgradeButtonUpdate();

                Platform.OnMoneyUpdateUI();
                /*
                if (Time.time - UpdatePlatformRevenueUITimer > 1f)
                {
                    Platform.OnMoneyUpdateUI();
                    UpdatePlatformRevenueUITimer = Time.time;
                }
                */
                /*
                if (Food.Food.Type == EFoodType.Preparing)
                {
                    Food.UpdateFoodProgress(Time.deltaTime * (Platform.Chef ? Platform.Chef.Chef.SpeedMultiplier : 1f));
                }
                */
            }

            Item = Player.Inventory.HasEquippedItem ? Player.Inventory.Slots[Player.Inventory.EquippedSlot].Item : null;

            ShowPlaceBrainrot(PlatformToPlace);
            if (PlatformToPlaceBrainrot != null && GameManager.Instance.Desktop && Input.GetKeyDown(KeyCode.E))
            {
                // Only place brainrot if we're not currently stealing one
                if (Item != null && Item.Food.BrainrotState != EBrainrotState.Stealing)
                {
                    OnPlaceBrainrot();
                }
            }

            // Check if we are out of stuff
            bool InZone = StealZone.Controllers.Contains(GameManager.Instance.Player.Owner);

            if (Item != null && Item.Type == EItemType.Food && Item.Food.BrainrotState == EBrainrotState.Stealing && !InZone)
            {
                OnRevertBrainrotStealing(true);
            }
            
            // Check for drop brainrot input on desktop
            if (Item != null &&
                Item.Type == EItemType.Food &&
                Item.Food.BrainrotState == EBrainrotState.Stealing && 
                GameManager.Instance.Desktop &&
                Input.GetKeyDown(KeyCode.E))
            {
                OnDropButton();
            }

            UpdateSellFoodMoneyText();

            // Update ItemToCollect
            if (FoodToSteal.Count <= 0)
            {
                OnHideItemToCollect();
                return;
            }

            if (BestItemToCollect == null)
            {
                float BestItemToCollectDistance = float.MaxValue;

                for (int i = 0; i < FoodToSteal.Count; ++i)
                {
                    var Food = FoodToSteal[i];

                    Vector3 FoodPosition = Food.transform.position;
                    float SqrDistance = (Camera.main.transform.position - FoodPosition).sqrMagnitude;

                    if (SqrDistance > m_CollectItemDistanceThreshold * m_CollectItemDistanceThreshold ||
                        SqrDistance > BestItemToCollectDistance)
                    {
                        continue;
                    }

                    Vector3 CameraToFood = (FoodPosition - Camera.main.transform.position).normalized;
                    float Dot = Vector3.Dot(CameraToFood, Camera.main.transform.forward);

                    if (Dot < m_CollectItemDotThreshold)
                    {
                        continue;
                    }

                    BestItemToCollect = Food;
                    BestItemToCollectDistance = SqrDistance;
                }
            }

            OnShowItemToCollect(BestItemToCollect);

            if (m_ItemToCollect != null && GameManager.Instance.Desktop && Input.GetKeyDown(KeyCode.E))
            {
                OnCollectItem();
            }

            // Update LevelUpgrade - only if Place and Collect are not available
            if (PlatformToPlaceBrainrot == null && m_ItemToCollect == null)
            {
                CookPlatform BestPlatformToUpgrade = null;
                float BestPlatformToUpgradeDistance = float.MaxValue;

                Vector3 CameraDirection = Camera.main.transform.forward;
                Vector3 CameraPosition = Camera.main.transform.position;

                foreach (var kv in m_PlatformDB)
                {
                    var Platform = kv.Value;

                    if (Platform.Brainrot == null || Platform.Owner == null || Platform.Owner.AIController)
                    {
                        continue;
                    }

                    // Check if player has enough money for upgrade
                    double UpgradePrice = Platform.Brainrot.GetBrainrotLevelUpgradePrice();
                    if (GameManager.Instance.Player.Money < UpgradePrice)
                    {
                        continue;
                    }

                    Vector3 PlatformPosition = Platform.LevelUpgradeWrapper.position;
                    float SqrDistance = (CameraPosition - PlatformPosition).sqrMagnitude;

                    if (SqrDistance > m_LevelUpgradeDistanceThreshold * m_LevelUpgradeDistanceThreshold ||
                        SqrDistance > BestPlatformToUpgradeDistance)
                    {
                        continue;
                    }

                    Vector3 CameraToPlatform = (PlatformPosition - CameraPosition).normalized;
                    float Dot = Vector3.Dot(CameraToPlatform, CameraDirection);

                    if (Dot < m_LevelUpgradeDotThreshold)
                    {
                        continue;
                    }

                    BestPlatformToUpgrade = Platform;
                    BestPlatformToUpgradeDistance = SqrDistance;
                }

                ShowLevelUpgrade(BestPlatformToUpgrade);

                if (m_PlatformToUpgrade != null && GameManager.Instance.Desktop && Input.GetKeyDown(KeyCode.E))
                {
                    OnLevelUpgradeButton();
                }
            }
            else
            {
                // Hide upgrade overlay if Place or Collect is available
                HideLevelUpgrade();
            }
        }

        public void OnBeforeInter()
        {
            MirraSDK.Time.Scale = 0f;

            if (MirraSDK.Analytics.IsGameplayReporterAvailable)
            {
                MirraSDK.Analytics.GameplayStop();
            }

            SoundController.Instance.MuteGame("ad_inter");

            Debug.Log($"{nameof(OnBeforeInter)}() called");
        }

        public void OnOpenInter()
        {
            Debug.Log($"{nameof(OnOpenInter)}() called");
        }

        public void OnCloseInter()
        {
            MirraSDK.Time.Scale = 1f;

            if (MirraSDK.Analytics.IsGameplayReporterAvailable)
            {
                MirraSDK.Analytics.GameplayStart();
            }

            SoundController.Instance.UnmuteGame("ad_inter");

            // Idk why, but it has to be there, otherwise null exceptions...
            if (m_InterADText != null)
            {
                m_InterADText.gameObject.SetActive(false);
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

        private void Awake()
        {
            LeanTween.init();

            OnTutorialClose();

            Assert.IsNotNull(m_AI.SellButtonTransform);
            Assert.IsNotNull(m_AI.ChefButtonTransform);
            Assert.IsNotNull(m_AI.FoodButtonTransform);
            Assert.IsTrue(m_AI.StreetRandomPoints.Length > 0);

            Assert.IsNotNull(m_FoodGrowthMultiplierTransform);
            Assert.IsNotNull(m_FoodGrowthMultiplierText);
            m_FoodGrowthMultiplierTransform.gameObject.SetActive(false);

            Assert.IsNotNull(m_InterADText);

            Assert.IsNotNull(m_SellFoodMoneyText);

            Assert.IsNotNull(m_ItemHoverText);
            Assert.IsNotNull(m_CollectItemOverlay);
            Assert.IsNotNull(m_CollectItemName);
            Assert.IsNotNull(m_LevelUpgradeOverlay);
            Assert.IsNotNull(m_NotificationText);
            Assert.IsNotNull(m_SettingsMenu);

            Assert.IsNotNull(m_CoinsVFXPrefab);
            Assert.IsNotNull(m_CoinsVFXTransform);
            
            Assert.IsNotNull(m_RewardProgressWrapper);
            Assert.IsNotNull(m_RewardProgressImage);
            
            Assert.IsNotNull(m_GuidanceLine);
            Assert.IsNotNull(m_Tutorial.TutorialOverlay);
            Assert.IsNotNull(m_Tutorial.TutorialText);
            Assert.IsNotNull(m_Tutorial.TutorialArrow);
            Assert.IsNotNull(m_Tutorial.FirstFoodPrefab);
            Assert.IsNotNull(m_Tutorial.GuidePlacePlatform);
            Assert.IsNotNull(m_Tutorial.GuidePlace);
            Assert.IsNotNull(m_Tutorial.GuideSell);
            Assert.IsNotNull(m_Tutorial.GuideBuy);

            Assert.IsNotNull(m_AuthMenu);
            CloseAuthMenu();

            m_Tutorial.TutorialArrow.gameObject.SetActive(false);

            for (int i = 0; i < m_AllItemPrefabs.Count; ++i)
            {
                Assert.IsNotNull(m_AllItemPrefabs[i]);

                var Item = m_AllItemPrefabs[i].GetComponent<Item>();

                Assert.IsNotNull(Item);

                ItemDBEntry Entry = new();
                Entry.Prefab = m_AllItemPrefabs[i];
                Entry.Item = Item;

                if (!m_ItemDB.TryAdd(Item.Id, Entry))
                {
                    Debug.LogWarning($"{nameof(CookManager)}.{nameof(Awake)}: Item Id [{Item.Id}] already exists. Its index is [{i}] in {nameof(m_AllItemPrefabs)}");
                }
            }

            Inapp[] Inapps = FindObjectsByType<Inapp>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < Inapps.Length; ++i)
            {
                m_InappsDB.Add(Inapps[i].InappId, Inapps[i]);
            }

            Reward[] Rewards = FindObjectsByType<Reward>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < Rewards.Length; ++i)
            {
                m_RewardsDB.Add(Rewards[i].RewardId, Rewards[i]);
            }

            Controller[] Controllers = FindObjectsByType<Controller>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < Controllers.Length; ++i)
            {
                var Controller = Controllers[i];

                m_Controllers.Add(Controller);
                Controller.PreInit();
            }

            PlayerPlace[] PlayerPlaces = FindObjectsByType<PlayerPlace>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < PlayerPlaces.Length; ++i)
            {
                var Place = PlayerPlaces[i];

                Place.Init(i);

                if (!m_PlayerPlaceDB.TryAdd(Place.Id, PlayerPlaces[i]))
                {
                    Debug.LogWarning($"[{PlayerPlaces[i].Id}] ID used in multiple PlayerPlaces [{PlayerPlaces[i].gameObject.name}]");
                }
            }

            CookPlatform[] Platforms = FindObjectsByType<CookPlatform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < Platforms.Length; ++i)
            {
                var Platform = Platforms[i];

                if (Platforms[i].Id == CookPlatform.NoneId)
                {
                    Debug.LogWarning($"{Platforms[i].Id} ID used in multiple Platforms [{Platforms[i].gameObject.name}]");
                }

                if (!m_PlatformDB.TryAdd(Platforms[i].Id, Platforms[i]))
                {
                    Debug.LogWarning($"[{Platforms[i].Id}] ID used in multiple Platforms [{Platforms[i].gameObject.name}]");
                }
            }

            // Initialize shop menus for balance
            for (int i = 0; i < m_ShopMenus.Count; ++i)
            {
                m_ShopMenus[i].Init();

                if (m_ShopMenus[i].ChildElements.Count > 0 &&
                    QueryItem(m_ShopMenus[i].ChildElements[0].ItemId, out var Entry))
                {
                    if (Entry.Item.Type == EItemType.Food)
                    {
                        m_ShopMenuFood = m_ShopMenus[i];
                    }
                    else if (Entry.Item.Type == EItemType.Chef)
                    {
                        m_ShopMenuChef = m_ShopMenus[i];
                    }
                }
            }
        }

        private void Start()
        {
            if (MirraSDK.IsInitialized)
            {
                if (!GameLoaded)
                {
                    OnSDKLoaded();
                }
            }
            else
            {
                MirraSDK.WaitForProviders(OnSDKLoaded);
            }
        }

        private void OnSDKLoaded()
        {
            Assert.IsTrue(MirraSDK.IsInitialized);

            // Game Ready
            if (MirraSDK.Analytics.IsGameplayReporterAvailable)
            {
                MirraSDK.Analytics.GameIsReady();
                MirraSDK.Analytics.GameplayStart();
            }

            // Get save data
            Save = MirraSDK.Data.GetObject<GameSave>("Save", null);

            // Debug.Log($"{nameof(OnSDKLoaded)}: Save data loaded: {(Save != null ? UnityEngine.JsonUtility.ToJson(Save, true) : "null")}");
            Debug.Log($"{nameof(OnSDKLoaded)}: Save data loaded");

            if (Save == null)
            {
                Debug.Log($"{nameof(OnSDKLoaded)}: Save data generated new: {UnityEngine.JsonUtility.ToJson(Save, true)}");
                Save = new();
            }

            // Some mobile / not mobile initialization stuff
            var Camera = FindFirstObjectByType<MenteBacata.ScivoloCharacterControllerDemo.OrbitingCamera>();
            if (Camera != null)
            {
                Camera.SetMobile(GameManager.Instance.Mobile);
            }

            var TCK = FindFirstObjectByType<TouchControlsKit.TCKInput>();
            if (TCK != null)
            {
                TCK.gameObject.SetActive(GameManager.Instance.Mobile);
            }

            var JoystickInput = FindFirstObjectByType<JoystickInput>();
            if (JoystickInput != null)
            {
                JoystickInput.SetMobile(GameManager.Instance.Mobile);
            }

            foreach (var go in DesktopOnly)
            {
                go.SetActive(GameManager.Instance.Desktop);
            }
            foreach (var go in MobileOnly)
            {
                go.SetActive(GameManager.Instance.Mobile);
            }

            // Authorization
            m_InitiallyWasAuthorized = MirraSDK.Player.IsLoggedIn;
            m_Authorized = m_InitiallyWasAuthorized;

            Debug.Log($"{nameof(OnSDKLoaded)}(): {nameof(m_Authorized)}:{m_Authorized}");

            // Reload localization from here
            if (MirraSDK.Language.IsLanguageInfoAvailable && MirraSDK.Language.IsLanguageInfoInitialized)
            {
                LocalizationManager.Instance.LoadLanguage(MirraSDK.Language.Current);
            }

            var Player = GameManager.Instance.Player;

            // Check if version is incompatible
            // @NOTE: Be very careful with inapps and progress
            if (Save.CookVersion < 3)
            {
                Save = new();
            }

            // Check for first lauch
            if (Save.FirstLaunch)
            {
                Save.Money = 0f;

                Save.Inventory = new ItemSlotSaveData[Inventory.MaxSlots];
                for (int i = 0; i < Save.Inventory.Length; ++i)
                {
                    Save.Inventory[i].Item.Id = Item.NoneId;
                    Save.Inventory[i].Count = 0;
                }

                //OnTutorialOpen();

                Save.FirstLaunch = false;
                MirraSDK.Data.SetObject("Save", Save, true);
                // Debug.Log($"FIRST LAUNCH: SAVE OBJ: {UnityEngine.JsonUtility.ToJson(Save, true)}");
                Debug.Log($"FIRST LAUNCH: SAVE");
            }

            // Load progress

            // Money
            Player.SetMoney(Save.Money);
            Debug.Log($"Loaded money: {Save.Money}");

            // Speed
            PlayerSpeedLevel = Save.PlayerSpeedLevel;
            UpdatePlayerSpeed();

            // Shop Menu initialization after Money
            UpdateShopElementsAvailability();
            Player.OnMoneyChanged += UpdateShopElementsAvailability;

            // Inventory
            Player.Inventory.Load(Save.Inventory);

            // Platforms
            if (Save.Platforms != null)
            {
                for (int i = 0; i < Save.Platforms.Length; ++i)
                {
                    ref var PlatformSaveData = ref Save.Platforms[i];

                    var Platform = QueryPlatform(PlatformSaveData.Id);
                    if (Platform)
                    {
                        Platform.Load(in PlatformSaveData);
                    }
                }
            }

            // Tutorial
            /*
            if (!Enum.TryParse(YandexGame.savesData.TutorialStage, out m_Tutorial.Stage))
            {
                m_Tutorial.Stage = ETutorialStage.None;
            }
            */
            m_Tutorial.Stage = ETutorialStage.None;

            // @DEBUG
            SwitchTutorialStage(ETutorialStage.Completed, true);
            // SwitchTutorialStage(m_Tutorial.Stage, true);

            // Update how much money we can get from selling food
            UpdateSellFoodMoneyText();

            // Controllers
            for (int i = 0; i < m_Controllers.Count; ++i)
            {
                var Controller = m_Controllers[i];
                Controller.Init();
            }

            // Inapps

            // Activation of saved bought inapps
            if (Save.BoughtInapps == null)
            {
                Save.BoughtInapps = new string[0];
            }

            bool NewInappFound = false;

            if (GameManager.Instance.InappsAvailable)
            {
                foreach (var ia in m_InappsDB)
                {
                    ia.Value.RefreshData();
                }

                for (int i = 0; i < Save.BoughtInapps.Length; ++i)
                {
                    ActivateInapp(Save.BoughtInapps[i]);
                }

                foreach (var ia in m_InappsDB)
                {
                    bool Purchased = MirraSDK.Payments.IsAlreadyPurchased(ia.Key);

                    Debug.Log($"{nameof(OnSDKLoaded)}: Inapp {ia.Key} Purchased={Purchased}");

                    if (Purchased && !Save.BoughtInapps.Contains(ia.Key))
                    {
                        NewInappFound = true;

                        Debug.Log($"{nameof(OnSDKLoaded)}: Inapp {ia.Key} was purchased without saving! Restoring...");

                        List<string> InappsList = Save.BoughtInapps.ToList();
                        InappsList.Add(ia.Key);
                        Save.BoughtInapps = InappsList.ToArray();

                        ActivateInapp(ia.Key);
                    }
                }

                // Inapps: Consume and activate bought, but not active inapps
                MirraSDK.Payments.RestorePurchases((Data) =>
                {
                    Debug.Log($"{nameof(OnSDKLoaded)}: Restore Purchases callback called");

                    string[] PendingProducts = Data.PendingProducts;

                    if (PendingProducts != null)
                    {
                        foreach (var p in PendingProducts)
                        {
                            Debug.Log($"{nameof(OnSDKLoaded)}: Pending Product {p}");

                            Data.RestoreProduct(p, () =>
                            {
                                OnInappPaymentSuccess(p);
                                Debug.Log($"{nameof(OnSDKLoaded)}: Inapp restored: {p}");
                            });
                        }
                    }

                    bool NewInapp = false;
                    string[] AllPurchases = Data.AllPurchases;

                    if (AllPurchases != null)
                    {
                        foreach (var a in Data.AllPurchases)
                        {
                            Debug.Log($"{nameof(OnSDKLoaded)}: All Purchases contains {a}");

                            if (!Save.BoughtInapps.Contains(a))
                            {
                                NewInapp = true;

                                List<string> InappsList = Save.BoughtInapps.ToList();
                                InappsList.Add(a);
                                Save.BoughtInapps = InappsList.ToArray();

                                ActivateInapp(a);
                            }
                        }
                    }

                    if (NewInapp)
                    {
                        SaveGameFully();
                    }
                });
            }

            foreach (var kv in m_RewardsDB)
            {
                kv.Value.UpdateVisuals();
            }

            if (!AdManager.Instance.TurnOffAdsInappBought)
            {
                // SetSticky() has checks if ads available
                AdManager.Instance.SetSticky(true);

                // Since coroutine doesn't check stuff, we do it from here
                if (MirraSDK.Ads.IsAdsAvailable && MirraSDK.Ads.IsInterstitialAvailable)
                {
                    m_InterADCoroutine = StartCoroutine(ShowInterADCoroutine());
                }
            }

            OnRevertBrainrotStealing(true);

            GameLoaded = true;

            if (NewInappFound && GameManager.Instance.InappsAvailable)
            {
                SaveGameFully();
            }
        }

        private void SwitchTutorialStage(ETutorialStage Stage, bool OnLoading = false)
        {
            m_Tutorial.Stage = Stage;

            var Player = GameManager.Instance.Player;

            switch (Stage)
            {
                case ETutorialStage.None:
                    Player.Inventory.SpawnAndAdd(m_Tutorial.FirstFoodPrefab);
                    SwitchTutorialStage(ETutorialStage.EquipFoodInInventory);
                    break;

                case ETutorialStage.EquipFoodInInventory:
                    SetTutorialText("ui_tutorial_equip_food_in_inventory");
                    OnInventoryItemEquipped += TutorialEquipFoodInInventory_OnInventoryItemEquipped;

                    RectTransform SlotTransform = null;
                    for (int i = 0; i < Inventory.MaxSlots; ++i)
                    {
                        if (Player.Inventory.Slots[i].Filled)
                        {
                            SlotTransform = Player.Inventory.Slots[i].UIView.GetComponent<RectTransform>();
                            Assert.IsNotNull(SlotTransform);
                            break;
                        }
                    }

                    Assert.IsNotNull(SlotTransform);
                    m_Tutorial.EquipItemSlotTransform = SlotTransform;

                    const float ScaleFactor = 1.2f;
                    if (SlotTransform)
                    {
                        LeanTween.scale(SlotTransform, new Vector3(ScaleFactor, ScaleFactor, ScaleFactor), 1f)
                            .setEaseOutElastic()
                            .setLoopPingPong();
                    }

                    m_Tutorial.TutorialArrow.gameObject.SetActive(true);
                    Vector3 AnimArrowScale = m_Tutorial.TutorialArrow.localScale * ScaleFactor;

                    LeanTween.scale(m_Tutorial.TutorialArrow, AnimArrowScale, 1f)
                        .setEaseOutElastic()
                        .setLoopPingPong();

                    break;

                case ETutorialStage.PlaceFood:
                    SetTutorialText("ui_tutorial_place_food");
                    SetGuidingLine(m_Tutorial.GuidePlace);
                    OnFoodPlaced += TutorialPlaceFood_OnPlacedFood;

                    m_Tutorial.GuidePlacePlatform.ToggleOutline(true, true);
                    break;

                case ETutorialStage.WaitPreparing:
                    SetTutorialText("ui_tutorial_wait_preparing");
                    SetGuidingLine();
                    OnFoodPrepared += TutorialWaitPreparing_OnFoodPrepared;
                    break;

                case ETutorialStage.TakeFood:
                    SetTutorialText("ui_tutorial_take_food");

                    Item Food = null;

                    foreach (var Platform in Player.Owner.PlayerPlace.Platforms)
                    {
                        for (int i = 0; i < Platform.Food.Count; ++i)
                        {
                            if (Platform.Food[i] != null && Platform.Food[i].Food.Type == EFoodType.Prepared)
                            {
                                Food = Platform.Food[i];
                                break;
                            }
                        }
                    }

                    SetGuidingLine(Food ? Food.transform : null);

                    OnFoodTaken += TutorialTakeFood_OnFoodTaken;
                    break;

                case ETutorialStage.Sell:
                    SetTutorialText("ui_tutorial_sell");
                    SetGuidingLine(m_Tutorial.GuideSell);
                    AnimateTutorialButton(m_Tutorial.TeleportButtonSellTransform);

                    OnFoodSold += TutorialSell_OnFoodSold;
                    break;

                case ETutorialStage.BuyNew:
                    SetTutorialText("ui_tutorial_buy_new");
                    SetGuidingLine(m_Tutorial.GuideBuy);
                    AnimateTutorialButton(m_Tutorial.TeleportButtonBuyTransform);

                    OnFoodBought += TutorialBuyNew_OnFoodBought;
                    break;

                case ETutorialStage.Completed:
                    SetGuidingLine();
                    SetTutorialText(null);
                    if (!OnLoading)
                    {
                        AnimateTutorialButton(m_Tutorial.TeleportButtonCookTransform);
                    }

                    m_FoodGrowthMultiplierTransform.gameObject.SetActive(true);

                    break;

                default:
                    Assert.IsTrue(false, $"{nameof(SwitchTutorialStage)}(): {Stage} is not handled in Switch Case scenario");
                    break;
            }

            if (!OnLoading)
            {
                SoundController.Instance.Play("Success");
            }

            Debug.Log($"Tutorial Stage: {m_Tutorial.Stage}");

            if (!OnLoading)
            {
                SaveGameFully();
            }
        }

        private void TutorialEquipFoodInInventory_OnInventoryItemEquipped()
        {
            OnInventoryItemEquipped -= TutorialEquipFoodInInventory_OnInventoryItemEquipped;

            LeanTween.cancel(m_Tutorial.TutorialArrow);
            m_Tutorial.TutorialArrow.gameObject.SetActive(false);

            LeanTween.cancel(m_Tutorial.EquipItemSlotTransform);
            LeanTween.scale(m_Tutorial.EquipItemSlotTransform, Vector3.one, 0.5f).setEaseInOutCubic();
            m_Tutorial.EquipItemSlotTransform = null;

            SwitchTutorialStage(ETutorialStage.PlaceFood);
        }

        private void TutorialPlaceFood_OnPlacedFood()
        {
            OnFoodPlaced -= TutorialPlaceFood_OnPlacedFood;

            m_Tutorial.GuidePlacePlatform.ToggleOutline(false, true);

            SwitchTutorialStage(ETutorialStage.WaitPreparing);
        }

        private void TutorialWaitPreparing_OnFoodPrepared()
        {
            OnFoodPrepared -= TutorialWaitPreparing_OnFoodPrepared;

            SwitchTutorialStage(ETutorialStage.TakeFood);
        }

        private void TutorialTakeFood_OnFoodTaken()
        {
            OnFoodTaken -= TutorialTakeFood_OnFoodTaken;

            SwitchTutorialStage(ETutorialStage.Sell);
        }

        private void TutorialSell_OnFoodSold()
        {
            OnFoodSold -= TutorialSell_OnFoodSold;

            SwitchTutorialStage(ETutorialStage.BuyNew);
        }

        private void TutorialBuyNew_OnFoodBought()
        {
            OnFoodBought -= TutorialBuyNew_OnFoodBought;

            SwitchTutorialStage(ETutorialStage.Completed);

            Notify("ui_notify_tutorial_completed", 3f, Color.green, true);
        }

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
            if (Key == null)
            {
                m_Tutorial.TutorialOverlay.gameObject.SetActive(false);
                return;
            }

            m_Tutorial.TutorialOverlay.gameObject.SetActive(true);
            m_Tutorial.TutorialText.text = LocalizationManager.Instance.GetTranslation(Key);
        }

        private void SetGuidingLine(Transform Point = null)
        {
            if (!Point)
            {
                m_GuidanceLine.gameObject.SetActive(false);
                return;
            }

            m_GuidanceLine.gameObject.SetActive(true);
            m_GuidanceLine.endPoint = Point;
        }

        public void ToggleSettingsMenu()
        {
            if (!MirraSDK.IsInitialized)
            {
                return;
            }

            bool Shown = !IsSettingsMenuOpen();

            if (MirraSDK.Analytics.IsGameplayReporterAvailable)
            {
                if (Shown)
                {
                    MirraSDK.Analytics.GameplayStop();
                }
                else
                {
                    MirraSDK.Analytics.GameplayStart();
                }
            }

            m_SettingsMenu.gameObject.SetActive(Shown);
            
            // Update cursor visibility based on settings menu state
            InputGame.Instance.SetPauseState(Shown);
        }
        
        public bool IsSettingsMenuOpen()
        {
            return m_SettingsMenu.gameObject.activeSelf;
        }

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

        private IEnumerator ShowInterADCoroutine()
        {
            for (;;)
            {
                double TimeLeftForNextInter = AdManager.Instance.GetTimeLeftToShowInter();
                Debug.Log($"{nameof(CookManager)}.{nameof(ShowInterADCoroutine)}(): Time left for next inter: {TimeLeftForNextInter}s");

                const double SmallError = 0.05f;
                double WaitTime = TimeLeftForNextInter + SmallError;

                if (WaitTime > 0f)
                {
                    yield return new WaitForSeconds((float)WaitTime);
                }

                if (!AdManager.Instance.CanShowInter())
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // Start countdown
                m_InterADFadeImage.gameObject.SetActive(true);

                m_InterADText.gameObject.SetActive(true);
                m_InterADText.text = LocalizationManager.Instance.GetTranslation("ad_in_2");

                // Pause game
                OnBeforeInter();

                yield return new WaitForSecondsRealtime(1f);

                m_InterADText.text = LocalizationManager.Instance.GetTranslation("ad_in_1");

                yield return new WaitForSecondsRealtime(1f);

                AdManager.Instance.ShowInter(OnOpenInter, OnCloseInter, OnErrorInter);
            }
        }

        public void OnInappPaymentSuccess(string Id)
        {
            // Inapps: Consume and activate bought, but not active inapps

            Debug.Log($"INAPP {Id}: {nameof(OnInappPaymentSuccess)} called");

            var Inapp = QueryInapp(Id);
            if (Inapp == null || Inapp.Activated)
            {
                Debug.Log($"INAPP {Id}: Query={(Inapp == null ? "false" : "true")}, Activated={(Inapp != null && Inapp.Activated ? "true" : "false")}");
                return;
            }

            ActivateInapp(Inapp);
            SaveGameFully();

            Notify("ui_notify_inapp_success", 2f, Color.green, true, "Success");
        }

        public void OnInappPaymentFail(string Id)
        {
            Debug.Log($"INAPP {Id}: {nameof(OnInappPaymentFail)} called");

            Notify("ui_notify_inapp_failed", 2f, Color.red, true, "NegativeClick");
        }

        /* Was used with Yandex SDK, but with Mirra I guess it doesn't matter
        private void OnInappPaymentsUpdate()
        {
            foreach (var kv in m_InappsDB)
            {
                kv.Value.RefreshData();
            }
        }
        */

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
                    AdManager.Instance.TurnOffAdsInappBought = true;
                    AdManager.Instance.SetSticky(false);

                    if (m_InterADCoroutine != null)
                    {
                        StopCoroutine(m_InterADCoroutine);
                        m_InterADCoroutine = null;
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

        public void OnTutorialOpen()
        {
            TutorialPanelWrapper.gameObject.SetActive(true);

            TutorialDesktopWrapper.gameObject.SetActive(GameManager.Instance.Desktop);
            TutorialMobileWrapper.gameObject.SetActive(GameManager.Instance.Mobile);

            if (GameManager.Instance.Mobile)
            {
                TutorialMobileRUWrapper.gameObject.SetActive(LocalizationManager.Instance.CurrentLanguage == LanguageType.Russian);
                TutorialMobileENWrapper.gameObject.SetActive(LocalizationManager.Instance.CurrentLanguage == LanguageType.English);
            }

            // Update cursor visibility for tutorial
            InputGame.Instance.SetTutorialState(true);
        }

        public void OnTutorialClose()
        {
            // TutorialPanelWrapper.gameObject.SetActive(false);
            
            // Update cursor visibility for tutorial
            InputGame.Instance.SetTutorialState(false);
        }
    }
}

#endif