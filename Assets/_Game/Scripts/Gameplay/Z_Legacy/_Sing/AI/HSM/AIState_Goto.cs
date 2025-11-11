#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sing
{
    public class AIState_Goto : AIState
    {
        private Vector3 m_Destination;

        public AIState_Goto(Vector3 Destination)
        {
            m_Destination = Destination;

            m_Exclusive = true;
        }
        
        public override void Init()
        {
            base.Init();

            m_Owner.AI.NavAgent.destination = m_Destination;

            if (m_Owner.AI.NavAgent.isActiveAndEnabled && m_Owner.AI.NavAgent.isOnNavMesh)
            {
                m_Owner.AI.NavAgent.isStopped = false;
            }
        }

        public override void Update()
        {
            if (m_Owner.AI.NavAgent.pathPending)
            {
                return;
            }

            if (!m_Owner.AI.NavAgent.hasPath && m_Owner.AI.NavAgent.remainingDistance <= m_Owner.AI.NavAgent.stoppingDistance)
            {
                Finish(EAIStateStatus.Success);
                return;
            }
        }

        public override void OnFinished(EAIStateStatus Status)
        {
            base.OnFinished(Status);

            if (m_Owner.AI.NavAgent.isActiveAndEnabled && m_Owner.AI.NavAgent.isOnNavMesh)
            {
                m_Owner.AI.NavAgent.isStopped = true;
            }
        }
    }
}

#endif