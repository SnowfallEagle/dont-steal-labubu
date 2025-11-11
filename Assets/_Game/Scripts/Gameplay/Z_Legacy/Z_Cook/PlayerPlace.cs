using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

#if GAME_COOK

namespace Cook
{
    public class PlayerPlace : MonoBehaviour
    {
        [SerializeField] private Controller m_Owner;
        public Controller Owner => m_Owner;
        [SerializeField] private CookPlatform[] m_Platforms;
        public CookPlatform[] Platforms => m_Platforms;

        [SerializeField] private Transform[] m_RandomPoints;
        public Transform[] RandomPoints => m_RandomPoints;

        [SerializeField] private Transform m_CenterPoint;
        public Transform CenterPoint => m_CenterPoint;

        [SerializeField] private TextMeshPro m_SignText;
        public TextMeshPro SignText => m_SignText;

        public bool IsOwnerAI => m_Owner.AIController;

        private int m_Id;
        public int Id => m_Id;

        public void Init(int Id)
        {
            Assert.IsNotNull(m_Owner);
            Assert.IsTrue(m_Platforms.Length > 0);
            Assert.IsTrue(m_RandomPoints.Length > 0);
            Assert.IsNotNull(m_CenterPoint);
            Assert.IsNotNull(m_SignText);

            m_Id = Id;
            m_Owner.PlayerPlace = this;

            for (int i = 0; i < m_Platforms.Length; ++i)
            {
                m_Platforms[i].AttachToPlayerPlace(this, i);
            }
        }

        public void UpdateSignText()
        {
            m_SignText.text = m_Owner.AIController ? m_Owner.Nickname : LocalizationManager.Instance.GetTranslation("grill");
        }

        public int GetFoodCount()
        {
            int Result = 0;

            for (int PlatformIdx = 0; PlatformIdx < m_Platforms.Length; ++PlatformIdx)
            {
                var Platform = m_Platforms[PlatformIdx];

                Result += Platform.Food.Count;
            }

            return Result;
        }

        public int GetChefCount()
        {
            int Result = 0;

            for (int PlatformIdx = 0; PlatformIdx < m_Platforms.Length; ++PlatformIdx)
            {
                var Platform = m_Platforms[PlatformIdx];

                if (Platform.Chef != null)
                {
                    ++Result;
                }
            }

            return Result;
        }

        // @returns: Can be null, can be best preparing food
        public Item GetMostPreparedFood()
        {
            List<Item> PreparingFood = new();

            for (int PlatformIdx = 0; PlatformIdx < m_Platforms.Length; ++PlatformIdx)
            {
                var Platform = m_Platforms[PlatformIdx];

                for (int FoodIdx = 0; FoodIdx < Platform.Food.Count; ++FoodIdx)
                {
                    var Food = Platform.Food[FoodIdx];

                    if (Food.Food.Type == EFoodType.Prepared)
                    {
                        return Food;
                    }

                    if (Food.Food.Type == EFoodType.Preparing)
                    {
                        PreparingFood.Add(Food);
                    }
                }
            }

            Item BestPreparingFood = null;
            float BestPreparingFoodTimeLeft = float.MaxValue;

            for (int i = 0; i < PreparingFood.Count; ++i)
            {
                var Food = PreparingFood[i];

                float ProgressLeft = 1f - Mathf.Clamp01(Food.Food.Progress);
                float TimeLeft = ProgressLeft / MathF.Max(0.0000001f, Food.Food.ProgressRate);

                if (TimeLeft < BestPreparingFoodTimeLeft)
                {
                    BestPreparingFood = Food;
                    BestPreparingFoodTimeLeft = TimeLeft;
                }
            }

            return BestPreparingFood;
        }

        public bool HasPreparedFoodWithSpaceForOwnerInventory()
        {
            for (int PlatformIdx = 0; PlatformIdx < m_Platforms.Length; ++PlatformIdx)
            {
                var Platform = m_Platforms[PlatformIdx];

                for (int FoodIdx = 0; FoodIdx < Platform.Food.Count; ++FoodIdx)
                {
                    var Food = Platform.Food[FoodIdx];

                    if (Food.Food.Type == EFoodType.Prepared && m_Owner.Inventory.HasFreeSlotFor(Food))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int GetCountPlatformsWithoutChef()
        {
            int Result = 0;

            for (int PlatformIdx = 0; PlatformIdx < m_Platforms.Length; ++PlatformIdx)
            {
                var Platform = m_Platforms[PlatformIdx];

                if (Platform.Chef == null)
                {
                    ++Result;                   
                }
            }

            return Result;
        }

        public bool HasPlatformsWithoutChef() => GetCountPlatformsWithoutChef() > 0;

        public CookPlatform FindAvailablePlatformForChefs()
        {
            List<int> LookupRandomIndices = new();
            for (int i = 0; i < Platforms.Length; ++i)
            {
                LookupRandomIndices.Add(i);
            }

            while (LookupRandomIndices.Count > 0)
            {
                int LookupRandomIdx = UnityEngine.Random.Range(0, LookupRandomIndices.Count);
                int PlatformIdx = LookupRandomIndices[LookupRandomIdx];
                LookupRandomIndices.RemoveAt(LookupRandomIdx);
                
                if (Platforms[PlatformIdx].Chef == null)
                {
                    return Platforms[PlatformIdx];
                }
            }

            return null;
        }

        public CookPlatform FindAvailablePlatformForFood()
        {
            List<int> RandomizedIndices = CoreUtils.RandIndices(Platforms.Length);

            for (int i = 0; i < RandomizedIndices.Count; ++i)
            {
                var Platform = Platforms[RandomizedIndices[i]];

                if (Platform.Chef != null)
                {
                    return Platform;
                }
            }

            const int FoodStackCount = 4;

            CookPlatform Best = null;
            int BestCount = int.MinValue;

            for (int i = 0; i < Platforms.Length; ++i)
            {
                int Count = Platforms[i].Food.Count;

                // Check when StepsToStack is Negative
                int StepsToStack = FoodStackCount - Count;

                if (
                       (StepsToStack > 0 &&
                           (
                               // Both are positive, we look for less steps to stack
                               (BestCount > 0 && StepsToStack < BestCount) ||
                               // Positive steps to stack always beat negative (negative is going backwards)
                               (BestCount <= 0)
                           )
                       ) ||
                       (StepsToStack <= 0 &&
                           (
                               // Positive best count always beat negative (negative is going backwards)
                               !(BestCount > 0) ||
                               // Both are negative, we look for less steps to "go backwards" (5..StackCount..3..2)
                               (BestCount <= 0 && StepsToStack > BestCount)
                           )
                       )
                )
                {
                    Best = Platforms[i];
                    BestCount = Best.Food.Count;
                }
            }

            return Best;
        }
    }
}

#endif
