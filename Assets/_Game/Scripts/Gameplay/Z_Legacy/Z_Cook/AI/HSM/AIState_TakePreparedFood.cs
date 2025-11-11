#if GAME_COOK


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cook
{
    public class AIState_TakePreparedFood : AIState
    {
        public AIState_TakePreparedFood()
        {
            m_Exclusive = true;
        }

        public override void Init()
        {
            base.Init();

            // Teleport if lucky
            float MaxDistanceToWalk = CookManager.Instance.AI.MaxDistanceToWalk;

            if (MaxDistanceToWalk * MaxDistanceToWalk <
                (m_Owner.transform.position - m_Owner.PlayerPlace.CenterPoint.position).sqrMagnitude &&
                CoreUtils.RandBool()
            )
            {
                m_Owner.transform.position = m_Owner.PlayerPlace.CenterPoint.position;
            }

            Item BestFood = null;
            float BestDistance = float.MaxValue;

            for (int PlatformIdx = 0; PlatformIdx < m_Owner.PlayerPlace.Platforms.Length; ++PlatformIdx)
            {
                var Platform = m_Owner.PlayerPlace.Platforms[PlatformIdx];

                for (int FoodIdx = 0; FoodIdx < Platform.Food.Count; ++FoodIdx)
                {
                    var Food = Platform.Food[FoodIdx];

                    if (Food.Food.Type == EFoodType.Prepared &&
                        m_Owner.Inventory.HasFreeSlotFor(Food) &&
                        (Food.transform.position - m_Owner.transform.position).sqrMagnitude < BestDistance)
                    {
                        BestFood = Food;
                    }
                }
            }

            if (BestFood == null)
            {
                Finish(EAIStateStatus.Success);
                return;
            }

            if (!m_Owner.Inventory.HasFreeSlotFor(BestFood))
            {
                Debug.LogError($"{m_Owner.gameObject.name} has no space in inventory for {BestFood.gameObject.name}");
                Finish(EAIStateStatus.Abort);
                return;
            }

            var GotoState = AddState(new AIState_Goto(BestFood.Platform.AIPointToTakeFood.position));
            if (GotoState == null)
            {
                Finish(EAIStateStatus.Abort);
                return;
            }

            GotoState.OnFinishedEvent += (Status) =>
            {
                var WaitState = AddState(new AIState_Wait(1f, 0.5f));
                if (WaitState == null)
                {
                    Finish(EAIStateStatus.Abort);
                    return;
                }

                WaitState.OnFinishedEvent += (Status) =>
                {
                    // Double check
                    Assert.IsNotNull(BestFood);
                    Assert.IsTrue(m_Owner.Inventory.HasFreeSlotFor(BestFood));

                    m_Owner.Inventory.Add(BestFood);
                    // @WORKAROUND: We better to put ToFoodTaken when Inventory.Add() happens like OnItemTaken
                    BestFood.ToFoodTaken();

                    WaitState = AddState(new AIState_Wait(1f, 0.5f));
                    if (WaitState == null)
                    {
                        Finish(EAIStateStatus.Abort);
                        return;
                    }

                    WaitState.OnFinishedEvent += (Status) =>
                    {
                        Finish(EAIStateStatus.Success);
                    };
                };
            };
        }
    }
}
#endif
