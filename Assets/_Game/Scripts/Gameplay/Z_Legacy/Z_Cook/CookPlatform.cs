using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

#if GAME_COOK

namespace Cook
{
    public class CookPlatform : MonoBehaviour
    {
        public const string NoneId = "-";

        [SerializeField] private string m_Id = NoneId;
        public string Id => m_Id;

        private PlayerPlace m_PlayerPlace;
        [SerializeField] private Controller m_Owner;
        public Controller Owner => m_Owner;

        [SerializeField] private Transform m_AIPointToTakeFood;
        public Transform AIPointToTakeFood => m_AIPointToTakeFood;

        [SerializeField] private Transform m_ChefTransform;

        [SerializeField] private BoxCollider m_PlaceableArea;
        public BoxCollider PlaceableArea => m_PlaceableArea;

        private List<Item> m_Food = new();
        public List<Item> Food => m_Food;

        [NonSerialized] public Item Brainrot;

        private Item m_Chef;
        public Item Chef => m_Chef;

        private Outline m_Outline;
        private bool m_OutlineAlwaysMode = false;

        [NonSerialized] public bool PlayerHere = false;

        [NonSerialized] public double Money = 0f;
        public TextMeshPro MoneyText;

        public Transform LevelUpgradeWrapper;
        public TextMeshPro LevelUpgradePriceText;
        public TextMeshPro LevelUpgradeEnhancementText;

        public void OnMoneyButton()
        {
            if (Money <= 0f)
            {
                return;
            }

            GameManager.Instance.Player.AddMoney(Money);
            Money = 0f;

            OnMoneyUpdate(false);
            CookManager.Instance.PlaySellCoinsVFX();

            // Animation
            if (Brainrot != null)
            {
                Brainrot.transform.localPosition = Vector3.zero;
                Brainrot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                LeanTween.cancel(Brainrot.gameObject, false);
                LeanTween.moveLocalY(Brainrot.gameObject, Brainrot.gameObject.transform.localPosition.y + 2f, 0.25f)
                    .setEaseInOutCirc()
                    .setLoopPingPong(1)
                    .setOnComplete(() =>
                    {
                        /*
                        LeanTween.moveLocal(Brainrot.gameObject, Vector3.zero, 0.25f)
                            .setEaseInOutQuad();
                        LeanTween.rotateLocal(Brainrot.gameObject, new Vector3(0f, 180f, 0f), 0.25f)
                            .setEaseInOutQuad();
                        */
                    });
            }

            // Save
            CookManager.Instance.SaveGameFully();
        }

        public void OnMoneyUpdate(bool AddRevenue)
        {
            if (Owner == null || Owner.AIController || MoneyText == null)
            {
                MoneyText.enabled = false;
                return;
            }

            if (Brainrot != null && AddRevenue)
            {
                Money += Brainrot.GetBrainrotRevenuePerSec() * Time.deltaTime;
            }
        }

        public void OnMoneyUpdateUI()
        {
            if (Owner == null || Owner.AIController || MoneyText == null)
            {
                return;
            }

            MoneyText.text = $"${Item.RoundBigNumber(Money):F0}";
        }

        public void OnUpgradeButtonUpdate()
        {
            if (Owner == null || Owner.AIController || Brainrot == null)
            {
                LevelUpgradeWrapper.gameObject.SetActive(false);
                return;
            }

            LevelUpgradeWrapper.gameObject.SetActive(true);
            LevelUpgradePriceText.text = $"${Brainrot.GetBrainrotLevelUpgradePrice():F0}";

            string LevelShortLocalized = LocalizationManager.Instance.GetTranslation("level_short");
            int CurrentLevel = Brainrot.Food.Level;
            LevelUpgradeEnhancementText.text = $"{LevelShortLocalized} {CurrentLevel} > {LevelShortLocalized} {CurrentLevel + 1}";
        }

        public void TryUpgradeBrainrot()
        {
            if (Brainrot == null || Owner == null || Owner.AIController)
            {
                return;
            }

            double Price = Brainrot.GetBrainrotLevelUpgradePrice();
            var Player = GameManager.Instance.Player;

            if (Player.WithdrawMoney(Price) < 0f)
            {
                CookManager.Instance.Notify("ui_notify_not_enough_money", 2f, Color.red, true, "NegativeClick");
                return;
            }

            ++Brainrot.Food.Level;

            SoundController.Instance.Play("CashMoney");
            CookManager.Instance.PlaySellCoinsVFX();
            CookManager.Instance.SaveGameFully();
        }

        public void OnTriggerEnter(Collider Other)
        {
            if (Owner != null && Owner.PlayerController && Other != null && Other.gameObject == GameManager.Instance.Player.gameObject)
            {
                PlayerHere = true;
            }
        }

        public void OnTriggerExit(Collider Other)
        {
            if (Owner != null && Owner.PlayerController && Other != null && Other.gameObject == GameManager.Instance.Player.gameObject)
            {
                PlayerHere = false;
            }
        }

        public void Save(ref PlatformSaveData Data)
        {
            Data.Id = m_Id;

            if (Brainrot != null)
            {
                Brainrot.Save(ref Data.Brainrot);
            }
        }

        public void Load(in PlatformSaveData Data)
        {
            var Inventory = GameManager.Instance.Player.Inventory;

            if (!string.IsNullOrEmpty(Data.Brainrot.Id))
            {
                Inventory.LoadItem(in Data.Brainrot);
            }
        }

        public void AttachToPlayerPlace(PlayerPlace Place, int BotPlatformId = -1)
        {
            m_PlayerPlace = Place;
            m_Owner = Place.Owner;

            // Change id only for bots
            if (Place.IsOwnerAI)
            {
                m_Id = $"bot_{Place.Id}_{BotPlatformId}";
            }
        }

        public void AddPreparingFood(Item Food)
        {
            Assert.IsTrue(Food && Food.Type == EItemType.Food && Food.Food.Type == EFoodType.Preparing && !m_Food.Contains(Food));

            m_Food.Add(Food);
            Food.transform.SetParent(transform, true);
        }

        public void SetChef(Item ChefToSet)
        {
            Assert.IsTrue(!m_Chef && ChefToSet && ChefToSet.Type == EItemType.Chef);

            m_Chef = ChefToSet;
            m_Chef.Platform = this;

            m_Chef.transform.position = m_ChefTransform.position;
            m_Chef.transform.rotation = m_ChefTransform.rotation;
        }

        public void ToggleOutline(bool Enable, bool OutlineAlwaysMode = false)
        {
            // We skip if !OutlineAlwaysMode wants to do anything while m_OutlineAlwaysMode is true
            if (m_Outline.enabled && m_OutlineAlwaysMode && !OutlineAlwaysMode)
            {
                return;
            }

            m_Outline.enabled = Enable;
            m_OutlineAlwaysMode = OutlineAlwaysMode;
        }

        private void Awake()
        {
            Assert.IsNotNull(m_ChefTransform);
            Assert.IsNotNull(m_AIPointToTakeFood);
            Assert.IsNotNull(m_PlaceableArea);

            m_Outline = GetComponent<Outline>();
            Assert.IsNotNull(m_Outline);
        }
    }
}

#endif