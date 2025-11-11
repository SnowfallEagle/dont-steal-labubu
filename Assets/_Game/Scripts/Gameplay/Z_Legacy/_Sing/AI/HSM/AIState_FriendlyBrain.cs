#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sing
{

    public class AIState_FriendlyBrain : AIState
    {
        public AIState_FriendlyBrain()
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

            if (m_Owner.Zone != null && m_Owner.Zone.ZoneId == Zone.ZeroId)
            {
                if (Random.value <= SingManager.Instance.AI.ZoneZeroSingProbability)
                {
                    AddState<AIState_GotoStageAndSing>();
                    return;
                }

                if (Random.value <= SingManager.Instance.AI.ZoneZeroChillProbability)
                {
                    if (Random.value <= SingManager.Instance.AI.ZoneZeroLongChillProbability)
                    {
                        AddState(new AIState_Wait(SingManager.Instance.AI.ZoneZeroLongChillBaseTime, SingManager.Instance.AI.ZoneZeroLongChillDeviationTime));
                        return;
                    }
                    AddState(new AIState_Wait());
                    return;
                }
            }

            int NextZone = m_Owner.Zone != null ? m_Owner.Zone.ZoneId : Zone.ZeroId;
            bool ForwardZoneIsUnlocked = false;

            if (m_Owner.Zone != null)
            {
                if (SingManager.Instance.GetZoneById(m_Owner.Zone.ZoneId + 1) is var Zone && Zone != null)
                {
                    ForwardZoneIsUnlocked = true;
                }
            }

            if ((ForwardZoneIsUnlocked && Random.value <= SingManager.Instance.AI.AdvanceNextZoneWhenUnlockedProbability) || (!ForwardZoneIsUnlocked && Random.value <= SingManager.Instance.AI.AdvanceNextZoneProbability))
            {
                bool Next = Random.value < SingManager.Instance.AI.AdvanceNextZoneForwardProbability;

                if (m_Owner.Zone)
                {
                    NextZone = m_Owner.Zone.ZoneId + (Next ? 1 : -1);
                    NextZone = System.Math.Clamp(NextZone, 0, SingManager.Instance.Zones.Count - 1);
                }
                else
                {
                    NextZone = m_Owner.UnlockedZones[Random.Range(0, m_Owner.UnlockedZones.Count - 1)].ZoneId;
                }
            }

            Zone ZoneToGo = SingManager.Instance.GetZoneById(NextZone);
            Assert.IsNotNull(ZoneToGo, $"Failed to get Zone from NextZoneId: {NextZone}");

            AddState(new AIState_Goto(ZoneToGo.GetRandomPositionForCharacter()));
        }
    }
}

#endif