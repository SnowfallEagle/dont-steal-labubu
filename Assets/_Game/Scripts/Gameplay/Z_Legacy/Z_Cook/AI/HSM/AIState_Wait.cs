using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cook
{
    public class AIState_Wait : AIState
    {
        private float m_WaitTime;

        public AIState_Wait(float Time = 1f, float RandomDeviation = 0.5f)
        {
            m_Exclusive = false;

            m_WaitTime = Time + (UnityEngine.Random.Range(0f, RandomDeviation) * CoreUtils.RandSign());
        }

        public override void Update()
        {
            base.Update();

            m_WaitTime -= Time.deltaTime;

            if (m_WaitTime <= 0f)
            {
                Finish(EAIStateStatus.Success);
            }
        }
    }
}