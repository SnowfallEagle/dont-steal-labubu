#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Sing
{
    public class AIState_GotoStageAndSing : AIState
    {
        private Transform m_SingTransform;

        public AIState_GotoStageAndSing()
        {
            m_Exclusive = true;
        }
        
        public override void Init()
        {
            base.Init();

            m_SingTransform = SingManager.Instance.TakeFreeSingTransform();
            if (m_SingTransform == null)
            {
                Finish(EAIStateStatus.Success);
                return;
            }

            var GotoState = new AIState_Goto(m_SingTransform.position);

            GotoState.OnFinishedEvent += (Status) =>
            {
                m_Owner.transform.position = m_SingTransform.position;
                m_Owner.transform.rotation = m_SingTransform.rotation;

                var WaitState = new AIState_Wait(30f, 10f);

                if (WaitState == null)
                {
                    Finish(EAIStateStatus.Abort);
                }

                WaitState.OnFinishedEvent += (Status) =>
                {
                    Finish(EAIStateStatus.Success);
                };
            };

            GotoState = (AIState_Goto)AddState(GotoState);

            if (GotoState == null)
            {
                Finish(EAIStateStatus.Abort);
            }
        }

        public override void OnFinished(EAIStateStatus Status)
        {
            base.OnFinished(Status);

            if (Status != EAIStateStatus.Cleanup)
            {
                SingManager.Instance.ReturnSingTransform(m_SingTransform);
            }
        }
    }
}

#endif
