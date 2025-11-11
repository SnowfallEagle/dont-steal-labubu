#if GAME_COOK
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cook
{
    public class AIState_GoShopping : AIState
    {
        private bool m_StateBuyChef = true;

        public AIState_GoShopping()
        {
            m_Exclusive = true;
        }

        public override void Init()
        {
            base.Init();

            // Teleport if lucky
            float MaxDistanceToWalk = CookManager.Instance.AI.MaxDistanceToWalk;
            Transform GotoTransform = CookManager.Instance.Tutorial.GuideBuy;

            if (MaxDistanceToWalk * MaxDistanceToWalk <
                (m_Owner.transform.position - GotoTransform.position).sqrMagnitude &&
                CoreUtils.RandBool()
            )
            {
                m_Owner.transform.position = GotoTransform.position;
            }

            // Can we even buy chef?
            m_StateBuyChef = m_Owner.HasEnoughMoneyToBuyChef() && m_Owner.PlayerPlace.HasPlatformsWithoutChef();
        }

        public override void Update()
        {
            base.Update();

            if (GetSubstates().Count > 0)
            {
                return;
            }

            Transform GotoTransform = m_StateBuyChef ? CookManager.Instance.AI.ChefButtonTransform : CookManager.Instance.AI.FoodButtonTransform;

            var GotoState = AddState(new AIState_Goto(GotoTransform.position));
            if (GotoState == null)
            {
                Finish(EAIStateStatus.Abort);
                return;
            }

            GotoState.OnFinishedEvent += (Status) =>
            {
                BuyItems();

                var WaitState = AddState(new AIState_Wait());
                if (WaitState == null)
                {
                    Finish(EAIStateStatus.Abort);
                    return;
                }

                WaitState.OnFinishedEvent += (Status) =>
                {
                    if (m_StateBuyChef)
                    {
                        // Go buy food now
                        m_StateBuyChef = false;
                    }
                    else
                    {
                        // We bought everything we could
                        Finish(EAIStateStatus.Success);
                    }
                };
            };
        }

        private void BuyItems()
        {
            List<ItemBalanceData> BalanceList;
            float PriceMultiplier;

            int BuyCount;

            if (m_StateBuyChef)
            {
                BalanceList = CookManager.Instance.ShopMenuChef.BalanceList;
                PriceMultiplier = 2f;
                BuyCount = Math.Min(m_Owner.AI.MaxChefs - m_Owner.PlayerPlace.GetChefCount(), m_Owner.PlayerPlace.GetCountPlatformsWithoutChef());
            }
            else
            {
                BalanceList = CookManager.Instance.ShopMenuFood.BalanceList;
                PriceMultiplier = 1f;
                BuyCount = m_Owner.AI.MaxFood - m_Owner.PlayerPlace.GetFoodCount();
            }

            Assert.IsTrue(BuyCount >= 0);

            for (int i = BalanceList.Count - 1; i >= 0 && BuyCount > 0; --i)
            {
                while (m_Owner.Money >= BalanceList[i].PriceToBuy * PriceMultiplier &&
                       m_Owner.Inventory.HasFreeSlotFor(BalanceList[i].Prefab) &&
                       BuyCount > 0
                )
                {
                    m_Owner.Money -= BalanceList[i].PriceToBuy;
                    m_Owner.Inventory.SpawnAndAdd(BalanceList[i].Prefab);
                    --BuyCount;
                }
            }
        }
    }
}
#endif
