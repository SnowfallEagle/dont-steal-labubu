#if GAME_COOK


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cook
{
    public class AIState_SellFood : AIState
    {
        public AIState_SellFood()
        {
            m_Exclusive = true;
        }

        public override void Init()
        {
            base.Init();

            // Sell Food
            if (!m_Owner.Inventory.HasTakenFood())
            {
                Finish(EAIStateStatus.Abort);
                return;
            }

            // Teleport if lucky
            float MaxDistanceToWalk = CookManager.Instance.AI.MaxDistanceToWalk;
            Transform GuideSellButton = CookManager.Instance.Tutorial.GuideSell;

            if (MaxDistanceToWalk * MaxDistanceToWalk <
                (m_Owner.transform.position - GuideSellButton.position).sqrMagnitude &&
                CoreUtils.RandBool()
            )
            {
                m_Owner.transform.position = GuideSellButton.position;
            }

            var GotoState = AddState(new AIState_Goto(CookManager.Instance.AI.SellButtonTransform.position));
            if (GotoState == null)
            {
                Finish(EAIStateStatus.Abort);
                return;
            }

            GotoState.OnFinishedEvent += (Status) =>
            {
                m_Owner.SellPreparedFoodInInventory();

                var WaitState = AddState(new AIState_Wait());
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
        }
    }
}

#endif
