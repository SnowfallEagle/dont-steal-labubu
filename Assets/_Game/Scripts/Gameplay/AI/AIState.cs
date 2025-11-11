using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public enum EAIStateStatus : int
{
    None,
    PreInitValidation,
    InProgress,

    /** Finish >>> */
    Success,
    Abort,
    Cleanup
    /** Finish <<< */
};

public class AIState
{
    public event Action<EAIStateStatus> OnFinishedEvent; // Not called on Cleanup
    public event Action OnCleanupEvent; // Called only on Cleanup

    protected bool m_Exclusive = false;

    protected 
#if GAME_COOK
    Cook.
#elif GAME_SING
    Sing.
#endif
    Controller m_Owner;

    private EAIStateStatus m_Status = EAIStateStatus.None;

    private AIState m_Superstate;
    private List<AIState> m_Substates = new();

    // Works only on new substates
    private bool m_CanAddSubstates = true;

    public static bool IsFinishStatus(EAIStateStatus Status)
    {
        return Status == EAIStateStatus.Success || Status == EAIStateStatus.Abort || Status == EAIStateStatus.Cleanup;
    }

    // Called by AIState before Init() and Update() on PreInitValidation and InProgress AIStateStatus.
    // Also can be called by ANYONE with ANY STATE
    // @sa GetStatus()
    public virtual bool Validate()
    {
        return true;
    }

    public virtual void Init()
    {
        m_Status = EAIStateStatus.InProgress;
    }

    public virtual void Update()
    {
    }

    // @NOTE: For internal use
    public void Finish(EAIStateStatus FinishStatus)
    {
        Assert.IsTrue(IsFinishStatus(FinishStatus));

        // Return if already finished
        if (IsFinishStatus(m_Status))
        {
            return;
        }

        // Finish
        m_Status = FinishStatus;

        FinishSubstates(FinishStatus);
        OnFinished(FinishStatus);

        if (m_Status != EAIStateStatus.Cleanup)
        {
            OnFinishedEvent?.Invoke(FinishStatus);
        }
        else
        {
            OnCleanupEvent?.Invoke();
        }
    }

    protected void FinishSubstates(EAIStateStatus FinishStatus)
    {
        Assert.IsTrue(IsFinishStatus(FinishStatus));

        for (int i = 0; i < m_Substates.Count; ++i)
        {
            if (m_Substates[i] != null)
            {
                m_Substates[i].Finish(FinishStatus);
            }
        }
    }

    public virtual void OnFinished(EAIStateStatus Status)
    {
    }

    // @returns: Given state or Null
    public AIState AddState(AIState State)
    {
        if (GetSubstates().Count >= 1000)
        {
            Debug.LogError($"{m_Owner.gameObject.name}: {GetType().Name} trying to add State {State.GetType().Name} but already has {GetSubstates().Count} Substates!");
        }

        if (State == null || !m_CanAddSubstates)
        {
            return null;
        }

        if (IsFinishStatus(m_Status))
        {
            if (m_Status != EAIStateStatus.Cleanup)
            {
                Debug.LogError($"{m_Owner.gameObject.name} tried to AddState() on {GetType().Name} when Status is {m_Status}");
            }

            return null;
        }

        var RootState = GetRootState();

        if (State.m_Exclusive)
        {
            RootState.FinishStatesOfClass(State.GetType());
        }

#if false
        for (const auto pClass : pState->m_IncompatibleStates)
        {
            if (!pClass || !pRootState->FindStateOfClass(*pClass)) continue;

            pRootState->FinishAllStatesOfClass(*pClass);

            GELogf(hLogAIState, Error,
                "AIState::AddState(): %s [%s] State [%s]: Incompatible state [%s] detected!",

                GetOwner()->Class().m_ClassName.c_str(),
                GetOwner()->m_entityName.c_str(),
                pState->Class().m_ClassName.c_str(),
                pClass->m_ClassName.c_str()
            );
#endif

        // Just to be safe put it in substates before validation
        m_Substates.Add(State);
        State.PreInit(this);

        if (!State.Validate())
        {
            m_Substates.Remove(State);
            return null;
        }

        State.Init();
        return State;
    }

    // @returns: Given state
    public T AddState<T>() where T : AIState, new()
    {
        return (T)AddState(new T());
    }

    public AIState FindStateOfClass(Type T)
    {
        if (GetType() == T || GetType().IsSubclassOf(T))
        {
            return this;
        }

        foreach (var Substate in m_Substates)
        {
            if (Substate.IsFinished())
            {
                continue;
            }

            AIState StateOfClass = Substate.FindStateOfClass(T);

            if (StateOfClass != null)
            {
                return StateOfClass;
            }
        }

        return null;
    }

    public T FindStateOfClass<T>() where T : AIState
    {
        return (T)FindStateOfClass(typeof(T));
    }

    public bool HasAnyGivenState(List<Type> classes)
    {
        foreach (var Class in classes)
        {
            if (GetType().IsSubclassOf(Class))
            {
                return true;
            }
        }

        foreach (var Substate in m_Substates)
        {
            if (Substate.IsFinished())
            {
                continue;
            }

            if (Substate.HasAnyGivenState(classes))
            {
                return true;
            }
        }

        return false;
    }

    public
#if GAME_COOK
    Cook.Controller
#elif GAME_SING
    Sing.Controller
#endif
    GetOwner()
    {
        return m_Owner;
    }

    public EAIStateStatus GetStatus()
    {
        return m_Status;
    }

    public bool IsFinished()
    {
        return IsFinishStatus(GetStatus());
    }

    public AIRootState GetRootState()
    {
        AIState State = this;

        while (State.GetSuperstate() != null)
        {
            State = State.GetSuperstate();
        }

        return (AIRootState)State;
    }

    public AIState GetSuperstate()
    {
        return m_Superstate;
    }

    public List<AIState> GetSubstates()
    {
        return m_Substates;
    }

    protected void SetCanAddSubstates(bool bCanAddSubstates)
    {
        m_CanAddSubstates = bCanAddSubstates;
    }

    protected void PreInit(AIState Superstate)
    {
        m_Superstate = Superstate;
        if (m_Superstate != null)
        {
            m_Owner = m_Superstate.GetOwner();
        }
        m_Status = EAIStateStatus.PreInitValidation;
    }

    protected void UpdateSubstates()
    {
        for (int i = 0; i < m_Substates.Count; ++i)
        {
            if (m_Substates[i].IsFinished())
            {
                continue;
            }

            if (!m_Substates[i].Validate())
            {
                m_Substates[i].Finish(EAIStateStatus.Abort);
                continue;
            }

            m_Substates[i].Update();
            m_Substates[i].UpdateSubstates();
        }

        CleanFinishedSubstates();
    }

    protected void CleanFinishedSubstates()
    {
        for (int i = m_Substates.Count - 1; i >= 0; --i)
        {
            if (!m_Substates[i].IsFinished())
            {
                continue;
            }

            m_Substates[i].CleanFinishedSubstates();
            m_Substates.RemoveAtSwapBack(i);
        }
    }
}