#if GAME_COOK


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cook
{
    public class AIState_WalkAround : AIState
    {
        public AIState_WalkAround()
        {
            m_Exclusive = true;
        }

        public override void Init()
        {
            base.Init();

            Transform Point = CookManager.Instance.GetRandomPointAI(m_Owner, ERandomPointAI.Anywhere);

            var GotoState = AddState(new AIState_Goto(Point.position));
            if (GotoState == null)
            {
                Finish(EAIStateStatus.Abort);
                return;
            }

            GotoState.OnFinishedEvent += (Status) =>
            {
                var WaitState = AddState(new AIState_Wait(2.5f, 2.5f));
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