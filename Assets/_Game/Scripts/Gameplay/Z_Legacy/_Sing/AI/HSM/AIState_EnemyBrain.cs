#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sing
{
    public class AIState_EnemyBrain : AIState
    {
        private float m_GotoElapsed = 0f;
        private Controller m_Target;

        public AIState_EnemyBrain()
        {
            m_Exclusive = true;
        }

        private bool IsControllerInZone(Controller Controller)
        {
            if (Controller == null)
            {
                return false;
            }

            bool InZone = m_Owner.Zone.Controllers.Contains(Controller);

            if (!InZone)
            {
                return false;
            }

            for (int i = 0; i < SingManager.Instance.Zones.Count; ++i)
            {
                Zone Zone = SingManager.Instance.Zones[i];

                if (Zone.ZoneId == m_Owner.Zone.ZoneId)
                {
                    continue;
                }

                if (Zone.Controllers.Contains(Controller))
                {
                    // We don't follow if guy is in quantum super position :)
                    InZone = false;
                    break;
                }
            }

            return InZone;
        }

        public override void Update()
        {
            base.Update();

            if (m_Owner.Zone.Controllers.Count <= 0)
            {
                FinishSubstates(EAIStateStatus.Success);
                return;
            }

            m_GotoElapsed += Time.deltaTime;

            bool TargetInZone = m_Target != null ? IsControllerInZone(m_Target) : false;

            if (GetSubstates().Count <= 0 || m_GotoElapsed > 1f || !TargetInZone)
            {
                FinishSubstates(EAIStateStatus.Success);

                Controller BestTarget = null;
                float BestTargetDist = float.MaxValue;

                for (int i = 0; i < m_Owner.Zone.Controllers.Count; ++i)
                {
                    Controller Controller = m_Owner.Zone.Controllers[i];
                    Assert.IsNotNull(Controller);

                    if ((Controller.AIController && Controller.AI.Enemy) || !IsControllerInZone(Controller))
                    {
                        continue;
                    }

                    float Dist = (Controller.transform.position - m_Owner.transform.position).sqrMagnitude;

                    if (Dist < BestTargetDist)
                    {
                        BestTarget = Controller;
                        BestTargetDist = Dist;
                    }
                }

                if (BestTarget != null)
                {
                    AddState(new AIState_Goto(BestTarget.transform.position));
                    m_Target = BestTarget;
                }

                m_GotoElapsed = 0f;
                return;
            }
        }
    }
}

#endif
