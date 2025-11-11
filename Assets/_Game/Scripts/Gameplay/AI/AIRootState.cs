using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class AIRootState : AIState
{
    public AIRootState()
    {
        m_Exclusive = true;
    }

    public void InitHSM(
#if GAME_COOK
        Cook.Controller
#elif GAME_SING
        Sing.Controller
#endif
        Owner
    )
    {
        Assert.IsNotNull(Owner);

        PreInit(null);
        m_Owner = Owner;
        Init();
    }

    public override void Update()
    {
        base.Update();

        UpdateSubstates();
    }


    public void Clean(EAIStateStatus FinishStatus = EAIStateStatus.Cleanup)
    {
        Finish(FinishStatus);
        CleanFinishedSubstates();
    }

    public void FinishStatesOfClass<T>() where T : AIState
    {
        FinishStatesOfClass(typeof(T));
    }

    public void FinishStatesOfClass(Type T)
    {
        void FinishStatesOfClassRecursive(AIState State)
        {
            foreach (var Substate in State.GetSubstates())
            {
                if (Substate.GetType().IsSubclassOf(T))
                {
                    Substate.Finish(EAIStateStatus.Abort);
                    continue;
                }

                FinishStatesOfClassRecursive(Substate);
            }
        }

        FinishStatesOfClassRecursive(this);
    }
}
