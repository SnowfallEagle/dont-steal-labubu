#if GAME_COOK


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cook
{
    public class AIState_Brain : AIState
    {
        public AIState_Brain()
        {
            m_Exclusive = true;
        }

        public override void Update()
        {
            base.Update();

            if (GetSubstates().Count > 0)
            {
                return;
            }

            /*
            if (CoreUtils.RandBool())
            {
                AddState<AIState_FuckAround>();
                return;
            }

            if (m_Owner.Inventory.HasPlaceableItems())
            {
                AddState<AIState_PlaceItems>();
                return;
            }

            if (m_Owner.PlayerPlace.HasPreparedFoodWithSpaceForOwnerInventory())
            {
                AddState<AIState_TakePreparedFood>();
                return;
            }

            if (m_Owner.Inventory.HasTakenFood())
            {
                AddState<AIState_SellFood>();
                return;
            }

            if (m_Owner.HasEnoughMoneyToBuyFood() &&
                (m_Owner.PlayerPlace.GetFoodCount() < m_Owner.AI.MaxFood ||
                 m_Owner.PlayerPlace.GetChefCount() < m_Owner.AI.MaxChefs))
            {
                AddState<AIState_GoShopping>();
                return;
            }
            */

            AddState<AIState_WalkAround>();
        }
    }
}

#endif