using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using TMPro;

#if GAME_COOK

namespace Cook
{
    [Serializable]
    public class ControllerAIData
    {
        [Header("State Machine")]
        [NonSerialized] public AIRootState HSM;

        [Header("Enemy")]
        public bool Enemy = false;
        public TextMeshPro SleepingText;

        [Header("Money")]
        public float MaxMoney = 500000f;

        [Header("Food & Chef")]
        public int MaxChefs = 3;
        public int MaxFood = 5;

        [Header("Clothes")]
        public GameObject[] SpecialCharacterHats;
        public GameObject[] SpecialCharacterBodies;  

        public GameObject DefaultCharacterBody;
        public GameObject DefaultCharacterShirt;
        public GameObject[] DefaultCharacterPants;
        public Material[] DefaultColorMaterials;

        [Header("References")]
        public TextMeshPro NicknameText;
        public NavMeshAgent NavAgent;
        public Animator Animator;
        public Rigidbody Rigidbody;
    }

    [Serializable]
    public class ControllerPlayerData
    {
        public GameObject SpecialCharacterHat;
        public GameObject SpecialCharacterBody;
    }

    public class Controller : MonoBehaviour
    {
        private string m_Nickname;
        public string Nickname => m_Nickname;

        private bool m_Initialized = false;

        [SerializeField] private bool m_AIController = true;
        public bool AIController => m_AIController;
        public bool PlayerController => !AIController;

        /** Available by Start() */
        [NonSerialized] public PlayerPlace PlayerPlace;

        // @WORKAROUND: Only for AI for now
        [SerializeField] private Inventory m_Inventory;
        public Inventory Inventory => m_Inventory;

        [NonSerialized] public double Money;

        [SerializeField] private ControllerAIData m_AI = new();
        public ControllerAIData AI => m_AI;
        [SerializeField] private ControllerPlayerData m_Player = new();

        private void OnTriggerEnter(Collider Other)
        {
            if (!AIController || !AI.Enemy)
            {
                return;
            }

            if (Other.CompareTag("alarm"))
            {
                if (AI.HSM != null && AI.HSM.FindStateOfClass<AIState_Brain_Brainrot>() is var State && State != null && State.State == "sleep")
                {
                    State.SleepStateTimeLeft = 0f;
                }

                return;
            }

            var OtherC = Other.GetComponent<Controller>();
            if (OtherC == null)
            {
                return;
            }

            if (OtherC.PlayerController)
            {
                CookManager.Instance.OnPlayerDied();
            }
            else if (OtherC.PlayerPlace != null)
            {
                OtherC.transform.position = OtherC.PlayerPlace.CenterPoint.position;
            }
        }

        public void SellPreparedFoodInInventory()
        {
            var Inventory = PlayerController ? GameManager.Instance.Player.Inventory : m_Inventory;
            List<Item> FoodToSell = new();

            for (int i = 0; i < Inventory.MaxSlots; ++i)
            {
                var Item = Inventory.Slots[i].Item;

                if (Item && Item.Type == EItemType.Food && Item.Food.BrainrotState == EBrainrotState.Stolen)
                {
                    FoodToSell.Add(Item);
                }
            }

            if (FoodToSell.Count <= 0)
            {
                return;
            }

            double SellTotal = 0f;

            for (int i = 0; i < FoodToSell.Count; ++i)
            {
                SellTotal += FoodToSell[i].ValueToSell;

                var Item = FoodToSell[i];

                SellTotal += Item.GetBrainrotRevenuePerSec();

                Item[] ItemsToDestroy = Inventory.RemoveFully(Item);
                for (int DestroyIdx = 0; DestroyIdx < ItemsToDestroy.Length; ++DestroyIdx)
                {
                    Destroy(ItemsToDestroy[DestroyIdx].gameObject);
                }
            }

            // AI Controller
            if (AIController)
            {
                Money += SellTotal;
                if (Money > AI.MaxMoney)
                {
                    Money = AI.MaxMoney;
                }
                return;
            }

            // Player Controller
            GameManager.Instance.Player.AddMoney(SellTotal);
            CookManager.Instance.UpdateSellFoodMoneyText();

            CookManager.Instance.SaveGameFully();

            CookManager.Instance.OnFoodSold?.Invoke();

            if (SellTotal > 0f)
            {
                CookManager.Instance.PlaySellCoinsVFX();
            }
        }

        public bool HasEnoughMoneyToBuyChef()
        {
            return Money > CookManager.Instance.ShopMenuChef.BalanceList[0].PriceToBuy;
        }

        public bool HasEnoughMoneyToBuyFood()
        {
            return Money > CookManager.Instance.ShopMenuFood.BalanceList[0].PriceToBuy;
        }

        public void PreInit()
        {
            if (!m_AIController)
            {
                Assert.IsNotNull(m_Player.SpecialCharacterHat);
                Assert.IsNotNull(m_Player.SpecialCharacterBody);

                return;
            }

            Assert.IsNotNull(m_AI.NavAgent);
            Assert.IsNotNull(m_AI.Rigidbody);

            m_AI.HSM = new();
            m_AI.HSM.InitHSM(this);

            if (!AI.Enemy)
            {
                Assert.IsNotNull(m_AI.NicknameText);
                Assert.IsNotNull(m_AI.Animator);

                Assert.IsNotNull(m_AI.DefaultCharacterBody);
                Assert.IsNotNull(m_AI.DefaultCharacterShirt);
                Assert.IsTrue(m_AI.SpecialCharacterHats.Length > 0);
                Assert.IsTrue(m_AI.SpecialCharacterBodies.Length > 0);
                Assert.IsTrue(m_AI.DefaultCharacterPants.Length > 0);
                Assert.IsTrue(m_AI.DefaultColorMaterials.Length > 0);

                m_Inventory.Init(this);
            }
        }

        public void Init()
        {
            if (!m_AIController)
            {
                PlayerPlace.UpdateSignText();

                m_Player.SpecialCharacterHat.SetActive(true);
                m_Player.SpecialCharacterBody.SetActive(true);

                GameManager.Instance.Player.Outline.Custom_OnMeshChanged();

                m_Initialized = true;

                return;
            }

            if (!AI.Enemy)
            {
                // Nickname
                m_Nickname = $"{LocalizationManager.Instance.GetTranslation("bot_nickname_player")} {UnityEngine.Random.Range(0, 999)}";

                m_AI.NicknameText.gameObject.SetActive(true);
                m_AI.NicknameText.text = m_Nickname;

                // --- AI Appearance Logic ---
                bool useDefault = UnityEngine.Random.value < 0.5f;
                if (useDefault)
                {
                    // Enable default body/shirt/pants
                    if (m_AI.DefaultCharacterBody) m_AI.DefaultCharacterBody.SetActive(true);
                    if (m_AI.DefaultCharacterShirt) m_AI.DefaultCharacterShirt.SetActive(true);

                    if (m_AI.DefaultCharacterPants != null)
                    {
                        foreach (var pants in m_AI.DefaultCharacterPants)
                            if (pants) pants.SetActive(true);
                    }

                    // Disable all special hats/bodies
                    if (m_AI.SpecialCharacterHats != null)
                    {
                        foreach (var hat in m_AI.SpecialCharacterHats)
                            if (hat) hat.SetActive(false);
                    }

                    if (m_AI.SpecialCharacterBodies != null)
                    {
                        foreach (var body in m_AI.SpecialCharacterBodies)
                            if (body) body.SetActive(false);
                    }

                    // Randomly color default body/shirt/pants
                    if (m_AI.DefaultColorMaterials != null && m_AI.DefaultColorMaterials.Length > 0)
                    {
                        var mat = m_AI.DefaultColorMaterials[UnityEngine.Random.Range(0, m_AI.DefaultColorMaterials.Length)];
                        if (m_AI.DefaultCharacterBody)
                        {
                            var renderer = m_AI.DefaultCharacterBody.GetComponent<Renderer>();
                            if (renderer) renderer.material = mat;
                        }
                        if (m_AI.DefaultCharacterShirt)
                        {
                            var renderer = m_AI.DefaultCharacterShirt.GetComponent<Renderer>();
                            if (renderer) renderer.material = mat;
                        }
                        if (m_AI.DefaultCharacterPants != null)
                        {
                            foreach (var pants in m_AI.DefaultCharacterPants)
                            {
                                if (pants)
                                {
                                    var renderer = pants.GetComponent<Renderer>();
                                    if (renderer) renderer.material = mat;
                                }
                            }
                        }
                    }
                }
                else // use special
                {
                    // Disable default body/shirt/pants
                    if (m_AI.DefaultCharacterBody) m_AI.DefaultCharacterBody.SetActive(false);
                    if (m_AI.DefaultCharacterShirt) m_AI.DefaultCharacterShirt.SetActive(false);
                    if (m_AI.DefaultCharacterPants != null)
                    {
                        foreach (var pants in m_AI.DefaultCharacterPants)
                            if (pants) pants.SetActive(false);
                    }

                    // Enable one random special hat/body, disable others
                    int hatIdx = m_AI.SpecialCharacterHats != null && m_AI.SpecialCharacterHats.Length > 0 ? UnityEngine.Random.Range(0, m_AI.SpecialCharacterHats.Length) : -1;
                    int bodyIdx = m_AI.SpecialCharacterBodies != null && m_AI.SpecialCharacterBodies.Length > 0 ? UnityEngine.Random.Range(0, m_AI.SpecialCharacterBodies.Length) : -1;
                    if (m_AI.SpecialCharacterHats != null)
                    {
                        for (int i = 0; i < m_AI.SpecialCharacterHats.Length; ++i)
                        {
                            if (m_AI.SpecialCharacterHats[i])
                                m_AI.SpecialCharacterHats[i].SetActive(i == hatIdx);
                        }
                    }

                    if (m_AI.SpecialCharacterBodies != null)
                    {
                        for (int i = 0; i < m_AI.SpecialCharacterBodies.Length; ++i)
                        {
                            if (m_AI.SpecialCharacterBodies[i])
                                m_AI.SpecialCharacterBodies[i].SetActive(i == bodyIdx);
                        }
                    }
                }
                // --- End AI Appearance Logic ---

                PlayerPlace.UpdateSignText();

                // Teleport bot on random point
                var PointTransform = CookManager.Instance.GetRandomPointAI(this, ERandomPointAI.Anywhere ^ ERandomPointAI.OthersPlace);
                transform.position = PointTransform.position;

                // Start AI
                m_AI.HSM.AddState<AIState_Brain>();

                // Spawn stuff
                int BrainrotsCount = UnityEngine.Random.Range(1, 4);

                for (int i = 0; i < BrainrotsCount; ++i)
                {
                    List<CookPlatform> AvailablePlatforms = new();

                    for (int p = 0; p < PlayerPlace.Platforms.Length; ++p)
                    {
                        var pIt = PlayerPlace.Platforms[p];

                        if (pIt.Brainrot == null)
                        {
                            AvailablePlatforms.Add(pIt);
                        }
                    }

                    if (AvailablePlatforms.Count <= 0)
                    {
                        continue;
                    }

                    CookPlatform Platform = AvailablePlatforms[UnityEngine.Random.Range(0, AvailablePlatforms.Count)];

                    Vector3 Pos = Platform.transform.position;

                    List<GameObject> PrefabList = UnityEngine.Random.value < CookManager.Instance.BrainrotSpecialProbability ?
                        CookManager.Instance.BrainrotPrefabsSpecial :
                        CookManager.Instance.BrainrotPrefabsUsual;

                    GameObject Prefab = PrefabList[UnityEngine.Random.Range(0, PrefabList.Count)];
                    GameObject GO = Instantiate(Prefab, Pos, Quaternion.Euler(0f, Platform.transform.rotation.eulerAngles.y + 180f, 0f));

                    var Item = GO.GetComponent<Item>();
                    if (Item != null)
                    {
                        var Stage = Item.Food.Stages[Item.Food.Stages.Count - 1];
                        Item.UpdateFoodProgress(Stage.Threshold / Item.Food.ProgressRate + 1f, true);

                        Item.Food.BrainrotState = EBrainrotState.Stolen;
                        Item.UpdateBrainrotText();
                        Item.Owner = this;
                        Item.Platform = Platform;
                        Platform.Brainrot = Item;
                    }
                    else
                    {
                        Destroy(GO);
                    }
                }
            }
            else
            {
                m_AI.HSM.AddState<AIState_Brain_Brainrot>();
            }

                /*
                var FoodBalanceList = CookManager.Instance.ShopMenuFood.BalanceList;
                var ChefBalanceList = CookManager.Instance.ShopMenuChef.BalanceList;

                // Randomize money
                Vector2 RangeModifier = new Vector2(1.5f, 2f);

                Money = UnityEngine.Random.Range(
                    (float)(RangeModifier.x * FoodBalanceList[0].PriceToBuy),
                    (float)(RangeModifier.y * FoodBalanceList[FoodBalanceList.Count - 1].PriceToBuy)
                );

                // Buy chefs
                int PotentialChefCount = UnityEngine.Random.Range(0, m_AI.MaxChefs + 1);
                Assert.IsTrue(PotentialChefCount <= m_AI.MaxChefs);

                while (PotentialChefCount > 0)
                {
                    for (int i = ChefBalanceList.Count - 1; i >= 0; --i)
                    {
                        if (Money > ChefBalanceList[i].PriceToBuy * 2f)
                        {
                            m_Inventory.SpawnAndAdd(ChefBalanceList[i].Prefab);
                            Money -= ChefBalanceList[i].PriceToBuy;
                            break;
                        }
                    }

                    --PotentialChefCount;
                }

                // Place chefs
                while (m_Inventory.Slots[0].Filled)
                {
                    ref var Slot = ref m_Inventory.Slots[0];

                    CookPlatform Platform = PlayerPlace.FindAvailablePlatformForChefs();
                    Assert.IsNotNull(Platform);

                    Slot.Item.UseChefAI(Platform);
                }

                // Buy food
                int PotentialFoodCount = UnityEngine.Random.Range(1, m_AI.MaxFood + 1);
                Assert.IsTrue(PotentialFoodCount > 0 && PotentialFoodCount <= m_AI.MaxFood);

                while (PotentialFoodCount > 0)
                {
                    for (int i = FoodBalanceList.Count - 1; i >= 0; --i)
                    {
                        if (Money - FoodBalanceList[i].PriceToBuy >= 0f &&
                            m_Inventory.HasFreeSlotFor(FoodBalanceList[i].Prefab))
                        {
                            m_Inventory.SpawnAndAdd(FoodBalanceList[i].Prefab);
                            Money -= FoodBalanceList[i].PriceToBuy;
                            break;
                        }
                    }

                    --PotentialFoodCount;
                }

                // Place food
                while (m_Inventory.Slots[0].Filled)
                {
                    ref var Slot = ref m_Inventory.Slots[0];

                    CookPlatform Platform = PlayerPlace.FindAvailablePlatformForFood();
                    Assert.IsNotNull(Platform);

                    var Item = Slot.Item.UseFoodAI(Platform);
                    if (Item != null)
                    {
                        Item.UpdateFoodProgress(UnityEngine.Random.Range(0f, 1f) / Item.Food.ProgressRate, true);
                        // Debug.Log(Item.Food.Progress);
                    }
                }
                */

                m_Initialized = true;
        }

        private void Update()
        {
            if (!m_Initialized)
            {
                return;
            }

            if (!m_AIController)
            {
                return;
            }

            if (!AI.Enemy)
            {
                // Nickname
                float DistanceToCameraSqr = (transform.position - Camera.main.transform.position).sqrMagnitude;
                const float DistanceThreshold = 35f;
                float DistanceThresholdSqr = DistanceThreshold * DistanceThreshold;

                m_AI.NicknameText.gameObject.SetActive(DistanceToCameraSqr <= DistanceThresholdSqr);

                // Animator
                m_AI.Animator.SetFloat("Movement", m_AI.NavAgent.velocity.sqrMagnitude);
            }

            // HSM
            m_AI.HSM.Update();
        }

        private void OnDestroy()
        {
            if (m_AIController && m_AI.HSM != null)
            {
                m_AI.HSM.Clean();
            }
        }
    }
}

#endif