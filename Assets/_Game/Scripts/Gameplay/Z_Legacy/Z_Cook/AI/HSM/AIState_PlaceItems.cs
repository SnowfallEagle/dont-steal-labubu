using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Cook;

#if GAME_COOK

public class AIState_PlaceItems : AIState
{
    private List<Item> m_Chefs = new();
    private List<Item> m_Food = new();

    private Item m_CurrentItem;

    public AIState_PlaceItems()
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

        // Sort items
        for (int i = 0; i < Inventory.MaxSlots; ++i)
        {
            if (!m_Owner.Inventory.Slots[i].Filled)
            {
                continue;
            }

            var Item = m_Owner.Inventory.Slots[i].Item;

            if (Item.Type == EItemType.Chef)
            {
                m_Chefs.Add(Item);
                continue;
            }

            if (Item.Type == EItemType.Food && Item.Food.Type == EFoodType.Unprepared)
            {
                m_Food.Add(Item);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        if (m_CurrentItem != null)
        {
            return;
        }

        if (m_Chefs.Count > 0)
        {
            m_CurrentItem = m_Chefs[0];
        }
        else if (m_Food.Count > 0)
        {
            m_CurrentItem = m_Food[0];
        }
        else
        {
            Finish(EAIStateStatus.Success);
            return;
        }

        Assert.IsNotNull(m_CurrentItem);

        CookPlatform Platform = m_CurrentItem.Type == EItemType.Chef ?
            m_Owner.PlayerPlace.FindAvailablePlatformForChefs() :
            m_Owner.PlayerPlace.FindAvailablePlatformForFood();

        Assert.IsNotNull(Platform);

        var GotoState = AddState(new AIState_Goto(Platform.AIPointToTakeFood.position));
        if (GotoState == null)
        {
            Finish(EAIStateStatus.Abort);
            return;
        }

        GotoState.OnFinishedEvent += (Status) =>
        {
            var WaitState = AddState(new AIState_Wait());
            if (WaitState == null)
            {
                Finish(EAIStateStatus.Abort);
                return;
            }

            WaitState.OnFinishedEvent += (Status) =>
            {
                m_CurrentItem.UseAI(Platform);
                m_Chefs.Remove(m_CurrentItem);
                m_Food.Remove(m_CurrentItem);

                WaitState = AddState(new AIState_Wait());
                if (WaitState == null)
                {
                    Finish(EAIStateStatus.Abort);
                    return;
                }

                WaitState.OnFinishedEvent += (Status) =>
                {
                    m_CurrentItem = null;
                };
            };
        };
    }
}

#endif
