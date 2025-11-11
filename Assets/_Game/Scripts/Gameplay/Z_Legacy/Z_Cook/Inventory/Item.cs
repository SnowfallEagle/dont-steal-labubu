using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;
using TMPro;

#if GAME_COOK

namespace Cook
{
    /** Enums */
    public enum EItemType : int
    {
        Food,
        Chef
    }

    public enum EFoodType : int
    {
        Unprepared,
        Preparing,
        Prepared,
        Taken
    }

    public enum EBrainrotState : int
    {
        Steal,
        Stealing,
        Stolen
    }

    /** Balance */
    [Serializable]
    public struct ItemFoodBalanceData
    {
        public float ProgressRate;
    }

    [Serializable]
    public struct ItemBalanceData
    {
        public GameObject Prefab;
        public double PriceToBuy;
        public double InitialRevenuePerSec;

        public ItemFoodBalanceData Food;
    }

    /** Item */
    [Serializable]
    public struct ItemFoodStageData
    {
        public Transform StageTransform;
        public float Threshold;
    }

    [Serializable]
    public struct ItemFoodData
    {
        [NonSerialized] public EBrainrotState BrainrotState;
        public float RespawnTimer;
        [NonSerialized] public float RespawnTimeLeft;
        public float SpeedMultiplier;
        public double RevenuePerSecBase;
        public Vector2 UpgradeRevenueRange;
        public Vector2 UpgradeLevelRange;

        public TextMeshPro TimeLeftText;
        public TextMeshPro LevelText;
        public TextMeshPro RarityText;
        public TextMeshPro NameText;
        public TextMeshPro RevenueText;

        public int Level;

        public EFoodType Type;

        public float Progress;
        public float ProgressRate;

        public int Stage;
        public List<ItemFoodStageData> Stages;

        public float WeightKg;
    }

    [Serializable]
    public struct ItemChefData
    {
        public float SpeedMultiplier;
    }

    public class Item : MonoBehaviour
    {
        public const string NoneId = "-";

        [SerializeField] private string m_Id = NoneId;
        public string Id => m_Id;

        [SerializeField] private EItemType m_Type = EItemType.Food;
        public EItemType Type => m_Type;

        [SerializeField] private bool m_Stackable = false;
        public bool Stackable => m_Stackable;

        /** Owner is always set, even when there're no inventory where item at */
        public Controller Owner;
        public CookPlatform Platform;

        [SerializeField] private double m_PriceToBuy = 0f;
        public double PriceToBuy => m_PriceToBuy;

        [SerializeField] private EShopElementRarity m_Rarity = EShopElementRarity.Common;
        public EShopElementRarity Rarity => m_Rarity;

        [SerializeField] private Sprite m_Icon;
        public Sprite Icon => m_Icon;

        private float m_ValueToSell;
        public float ValueToSell => m_ValueToSell;

        private Outline m_Outline;
        public Outline Outline => m_Outline;

        [SerializeField] private ItemFoodData m_Food = new ItemFoodData();
        public ref ItemFoodData Food => ref m_Food;

        [SerializeField] private ItemChefData m_Chef = new ItemChefData();
        public ref ItemChefData Chef => ref m_Chef;

        public void UpdateBrainrotText()
        {
            if (this == null)
            {
                return;
            }

            if (Type != EItemType.Food)
            {
                return;
            }

            if (Food.BrainrotState == EBrainrotState.Stolen)
            {
                Food.LevelText.text = $"{LocalizationManager.Instance.GetTranslation("level_short")} {Food.Level}";
                Food.LevelText.enabled = true;
                Food.TimeLeftText.enabled = false;
            }
            else
            {
                Food.TimeLeftText.text = $"{Food.RespawnTimeLeft:F0}";
                Food.TimeLeftText.enabled = true;
                Food.LevelText.enabled = false;
            }

            Food.RarityText.text = $"{UIShopElement.RarityToText(Rarity)}";
            Food.RarityText.color = UIShopElement.RarityToColor(Rarity);

            Food.NameText.text = $"{LocalizationManager.Instance.GetTranslation(Id)}";
            Food.RevenueText.text = $"${GetBrainrotRevenuePerSec():F0}/{LocalizationManager.Instance.GetTranslation("ui_seconds_short")}";
        }

        public static double RoundBigNumber(double Value)
        {
            long Temp = (long)Value;
            int DigitsCount = 0;

            while (Temp > 0)
            {
                ++DigitsCount;

                Temp /= 10;
            }

            const int DigitsThreshold = 2;
            int DigitsToProcess = DigitsCount - DigitsThreshold;

            if (DigitsToProcess <= 0)
            {
                return Value;
            }

            for (int i = 0; i < DigitsToProcess; ++i)
            {
                Value *= 0.1f;
            }

            Value = (double)(long)Value;

            for (int i = 0; i < DigitsToProcess; ++i)
            {
                Value *= 10f;
            }

            Value = (double)(long)Value;

            return Value;
        }

        public double GetBrainrotLevelUpgradePrice()
        {
            double UpgradeCost = Food.RevenuePerSecBase * 35f;

            for (int i = 0; i < Food.Level - 1; ++i)
            {
                UpgradeCost *= 1.5f;
            }

            return RoundBigNumber(UpgradeCost);
        }

        public double GetBrainrotRevenuePerSec()
        {
            double Revenue = Food.RevenuePerSecBase;

            for (int i = 0; i < Food.Level - 1; ++i)
            {
                float Multiplier = 1.5f;

                float Q = Food.UpgradeRevenueRange.y - Food.UpgradeRevenueRange.x;
                float W = Food.UpgradeLevelRange.x - Food.UpgradeLevelRange.y;
                float DiffRevPerLevel = Q / W;

                Multiplier -= DiffRevPerLevel * i;
                Multiplier = Mathf.Clamp(Multiplier, Food.UpgradeRevenueRange.x, Food.UpgradeRevenueRange.y);

                Revenue *= (double)Multiplier;
            }

            Revenue *= CookManager.Instance.RewardMoneyMultiplier;

            return RoundBigNumber(Revenue);
        }

        public void Save(ref ItemSaveData Data)
        {
            Data.Id = m_Id;
            Data.ValueToSell = ValueToSell;

            Data.Food.Type     = Food.Type.ToString();
            Data.Food.Progress = Food.Progress;
            Data.Food.WeightKg = Food.WeightKg;

            Data.Food.BrainrotState = Food.BrainrotState.ToString();
            Data.Food.Level = Food.Level;

            if (Platform)
            {
                Data.PlatformId                   = Platform.Id;
                Data.Food.LocalPositionOnPlatform = transform.localPosition;
            }
        }

        public void Load(in ItemSaveData Data)
        {
            // Get inventory
            var Inventory = GameManager.Instance.Player.Inventory;

            m_ValueToSell = Data.ValueToSell;

            Food.Progress = Data.Food.Progress;
            Food.WeightKg = Data.Food.WeightKg;
            Food.Level = Data.Food.Level;

            // Process food
            if (Type == EItemType.Food)
            {
                if (Enum.TryParse(Data.Food.Type, true, out EFoodType DataTypeEnum))
                {
                    if (Enum.TryParse(Data.Food.BrainrotState, true, out EBrainrotState BrainrotState))
                    {
                        Food.BrainrotState = BrainrotState;
                    }

                    int DataType = (int)DataTypeEnum;

                    if (!string.IsNullOrEmpty(Data.PlatformId))
                    {
                        CookPlatform P = CookManager.Instance.QueryPlatform(Data.PlatformId);
                        if (P != null)
                        {
                            Platform = P;
                            Platform.Brainrot = this;

                            transform.SetParent(Platform.transform, false);
                            transform.localPosition = Vector3.zero;
                            transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        }
                    }

                    // Add to inventory
                    if (Platform == null)
                    {
                        Inventory.Add(this);
                    }
                }
            }
        }

        // @NOTE: Updates material properies only if Color is not current OutlineColor
        public void EnableOutline(Color Color)
        {
            m_Outline.enabled = true;

            if (m_Outline.OutlineColor != Color)
            {
                m_Outline.OutlineColor = Color;
                m_Outline.UpdateMaterialProperties();
            }
        }

        public void DisableOutline()
        {
            m_Outline.enabled = false;
        }

        public void Use(RaycastHit[] HitResults)
        {
            switch (Type)
            {
                case EItemType.Food: UseFood(HitResults); break;
                case EItemType.Chef: UseChef(); break;
            }
        }

        public void UseAI(CookPlatform Platform)
        {
            switch (Type)
            {
                case EItemType.Food: UseFoodAI(Platform); break;
                case EItemType.Chef: UseChefAI(Platform); break;
            }
        }

        private void UseChef()
        {
            /*
            CookPlatform Platform = InputGame.Instance.OutlineCookPlatform;
            if (!Platform || Platform.Chef || Platform.Owner != Owner)
            {
                return;
            }

            var Inventory = GameManager.Instance.Player.Inventory;

            Item[] Items = Inventory.Remove(Inventory.EquippedSlot, 1);
            if (Items.Length > 0 && Items[0] is Item Item && Item)
            {
                Item.ToChefWorking(Platform);
            }
            */
        }

        public void UseChefAI(CookPlatform Platform)
        {
            Item[] Items = Owner.Inventory.Remove(this, 1);
            if (Items.Length > 0 && Items[0] is Item Item && Item)
            {
                Item.ToChefWorking(Platform);
            }
        }

        private void ToChefWorking(CookPlatform Platform, bool OnLoadData = false)
        {
            Assert.IsNotNull(Platform);

            Platform.SetChef(this);

            if (!OnLoadData && Owner.PlayerController)
            {
                CookManager.Instance.SaveGameFully();
            }
        }

        private void UseFood(RaycastHit[] HitResults)
        {
            if (Food.Type != EFoodType.Unprepared)
            {
                return;
            }

            for (int i = 0; i < HitResults.Length; ++i)
            {
                ref RaycastHit HitResult = ref HitResults[i]; 

                if (HitResult.collider && HitResult.collider.CompareTag("CookPlatform"))
                {
                    if (HitResult.transform.parent?.GetComponent<CookPlatform>() is CookPlatform Platform &&
                        Platform &&
                        Platform.Owner.PlayerController)
                    {
                        var Inventory = GameManager.Instance.Player.Inventory;

                        Item[] Items = Inventory.Remove(Inventory.EquippedSlot, 1);
                        if (Items.Length > 0 && Items[0] is Item Item && Item)
                        {
                            Vector3 Position = HitResult.point;
                            Position.y = Platform.PlaceableArea.bounds.center.y + Platform.PlaceableArea.bounds.extents.y;

                            Item.ToFoodPreparing(Position, Platform);
                            break;
                        }
                    }
                }
            }
        }

        public Item UseFoodAI(CookPlatform Platform)
        {
            if (Food.Type != EFoodType.Unprepared)
            {
                return null;
            }

            Assert.IsNotNull(Platform);

            var Inventory = Owner.Inventory;

            Item[] Items = Inventory.Remove(this, 1);
            if (Items.Length > 0 && Items[0] is Item Item && Item)
            {
                const float RandomModifier = 0.75f;

                Bounds Bounds = Platform.PlaceableArea.bounds;

                Vector3 Position = Bounds.center + new Vector3(
                    RandomModifier * UnityEngine.Random.Range(Bounds.extents.x, -Bounds.extents.x),
                    Bounds.extents.y,
                    RandomModifier * UnityEngine.Random.Range(Bounds.extents.z, -Bounds.extents.z)
                );

                Item.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                Item.ToFoodPreparing(Position, Platform);
                return Item;
            }

            return null;
        }

        public void UpdateFoodProgress(float DeltaProgress, bool InitialUpdateOnLoad = false)
        {
            Food.Progress += Food.ProgressRate * DeltaProgress * CookManager.Instance.ComputeGrowthSpeed();

            if (InitialUpdateOnLoad)
            {
                Food.Stages[Food.Stage].StageTransform.gameObject.SetActive(false);

                int Stage = -1;

                for (int i = Food.Stages.Count - 1; i >= 0; --i)
                {
                    if (Food.Progress >= Food.Stages[i].Threshold)
                    {
                        Stage = i;
                        break;
                    }
                }

                if (Stage == -1)
                {
                    Stage = Food.Stages.Count - 1;
                }

                Food.Stage = Stage;
                Food.Stages[Food.Stage].StageTransform.gameObject.SetActive(true);
            }
            else
            {
                int NextStage = Food.Stage + 1;
                if (Food.Stages.Count > 0 && NextStage < Food.Stages.Count && Food.Stages[NextStage].Threshold <= Food.Progress)
                {
                    Food.Stages[Food.Stage].StageTransform.gameObject.SetActive(false);
                    Food.Stages[NextStage].StageTransform.gameObject.SetActive(true);

                    ++Food.Stage;
                }
            }

            if (Food.Progress > 1f)
            {
                Food.Progress = 1f;
                // ToFoodPrepared();
            }
        }

        // @param Platform: can be null when OnLoadData == false
        private void ToFoodPreparing(Vector3 Point, CookPlatform CookPlatform, bool OnLoadData = false)
        {
            if (!OnLoadData)
            {
                Assert.IsNotNull(CookPlatform);
            }

            Food.Type = EFoodType.Preparing;

            // Get text right
            UpdateFoodProgress(0f);

            Platform = CookPlatform;

            transform.position = Point;
            if (Platform != null)
            {
                Platform.AddPreparingFood(this);
            }

            if (!OnLoadData && Owner.PlayerController)
            {
                CookManager.Instance.SaveGameFully();
                CookManager.Instance.OnFoodPlaced?.Invoke();

                SoundController.Instance.Play("ButtonClick");
            }
        }

        private void ToFoodPrepared(bool OnLoadData = false)
        {
            Food.Type = EFoodType.Prepared;
            m_Stackable = false;

            if (!OnLoadData && Owner.PlayerController)
            {
                CookManager.Instance.OnFoodPrepared?.Invoke();
            }
        }

        public void ToFoodTaken(bool OnLoadData = false)
        {
            Food.Type = EFoodType.Taken;

            if (Platform != null)
            {
                Platform.Food.Remove(this);
            }
            Platform = null;

            if (!OnLoadData)
            {
                double2 ValueVector = new double2(m_PriceToBuy * 1.3f, m_PriceToBuy * 2.5f);
                
                m_ValueToSell = UnityEngine.Random.Range((float)ValueVector.x, (float)ValueVector.y);

                double WeightMax = ValueVector.y * 0.005f;

                // ValueToSell / ValueMax = X / WeightMax
                // X = (ValueToSell * WeightMax) / ValueMax === Relationship WeightToValue * ValueToSell
                Food.WeightKg = (float)((m_ValueToSell * WeightMax) / ValueVector.y);

                if (Owner.PlayerController)
                {
                    // We don't add Item to inventory while Loading it so we don't need update ui
                    GameManager.Instance.Player.Inventory.UpdateLogicAndUI();

                    CookManager.Instance.SaveGameFully();
                }
            }

            if (!Owner.PlayerController)
            {
                return;
            }

            CookManager.Instance.UpdateSellFoodMoneyText();

            if (!OnLoadData)
            {
                CookManager.Instance.OnFoodTaken?.Invoke();

                SoundController.Instance.Play("Select");
            }
        }

        private void SetBalance(in ItemBalanceData Balance)
        {
            m_PriceToBuy = Balance.PriceToBuy;

            Food.ProgressRate = Balance.Food.ProgressRate;
        }

        private void Awake()
        {
            Assert.IsNotNull(m_Icon);

            // Components
            m_Outline = GetComponent<Outline>();
            Assert.IsNotNull(m_Outline);

            // Balance
            if (CookManager.Instance.QueryItem(m_Id, out ItemDBEntry Entry))
            {
                SetBalance(in Entry.Balance);
            }

            // Food preparation (I actually don't know if we really need it)
            if (Type == EItemType.Food)
            {
                Assert.IsTrue(Food.Stages.Count > 0);
                for (int i = 0; i < Food.Stages.Count; ++i)
                {
                    Assert.IsNotNull(Food.Stages[i].StageTransform);
                    Assert.IsTrue(i == Food.Stages.Count - 1 || Food.Stages[i + 1].Threshold > Food.Stages[i].Threshold);

                    if (i == Food.Stages.Count - 1)
                    {
                        Food.Stages[i].StageTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        Food.Stages[i].StageTransform.gameObject.SetActive(false);
                    }
                }
            }

            // Brainrot text
            UpdateBrainrotText();
            if (Type == EItemType.Food)
            {
                LocalizationManager.Instance.OnRefresh += UpdateBrainrotText;
            }
        }
    }
}

#endif