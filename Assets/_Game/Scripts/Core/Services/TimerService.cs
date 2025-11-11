using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerService : Service<TimerService>
{
    public class Handle
    {
        public Timer Timer;

        public UnityEngine.Object Owner;
        public bool Owned;

        public bool Valid => (!Owned || Owner) && Timer != null;

        public void Invalidate()
        {
            TimerService.Instance.RemoveTimer(this);
        }
    }

    public class Timer
    {
        // Null if timer removed
        public Handle Handle;

        public float TimeRate;
        public float TimeLeftToFire;

        public float FirstDelay;
        public bool NeedDelay;
        public bool Loop;

        public Action Callback;
    }

    private List<Timer> m_Timers = new List<Timer>();

    private void Update()
    {
        // Clean timers
        m_Timers.RemoveAll((Timer) => Timer.Handle == null);

        // Update timers
        for (int i = m_Timers.Count - 1; i >= 0; --i)
        {
            Timer Timer = m_Timers[i];

            // Skip removed timers
            if (Timer.Handle == null)
            {
                continue;
            }

            // Process delay
            if (Timer.NeedDelay)
            {
                Timer.FirstDelay -= Time.deltaTime;
                if (Timer.FirstDelay <= 0f)
                {
                    Timer.TimeLeftToFire -= Mathf.Abs(Timer.FirstDelay);
                    Timer.NeedDelay = false;
                }

                continue;
            }

            // Process time left to fire
            Timer.TimeLeftToFire -= Time.deltaTime;

            if (Timer.TimeLeftToFire <= 0f)
            {
                if (Timer.Loop)
                {
                    float AbsTimeLeftToFire = Mathf.Abs(Timer.TimeLeftToFire);

                    float NextFireTimeRemainder = AbsTimeLeftToFire % Timer.TimeRate;
                    Timer.TimeLeftToFire = Timer.TimeRate - NextFireTimeRemainder;

                    int FireCount = 1 + (int)(AbsTimeLeftToFire / Timer.TimeRate);
                    for (int FireIdx = 0; FireIdx < FireCount; ++FireIdx)
                    {
                        Timer.Callback?.Invoke();
                    }

                    continue;
                }

                /* @NOTE:
                    Timer callback could use its handle again to create new timer, so
                    call callback after removing old timer
                */
                var Callback = Timer.Callback;
                RemoveTimer(Timer.Handle);
                Callback?.Invoke();
            }
        }
    }

    /** Add timer that fire at time rate.
        Attach Timer to Handle with ownership if Handle and Owner != null.
        Set bLoop = true to loop timer.
        If FirstDelay < 0f and bLoop = true then first time timer fires immediately.
        With delay timer can fire only on second Update() after adding.
    */
    public void AddTimer(Handle Handle, UnityEngine.Object Owner, Action Callback, float TimeRate, bool Loop = false, float FirstDelay = -1f)
    {
        // Check given handle
        if (Handle != null)
        {
            RemoveTimer(Handle);
        }
        else
        {
            Handle = new Handle();
        }

        // Add new timer
        m_Timers.Add(new Timer
        {
            Handle = Handle,

            TimeRate = TimeRate,
            TimeLeftToFire = Loop && FirstDelay < 0f ? 0f : TimeRate,

            FirstDelay = FirstDelay,
            NeedDelay = FirstDelay >= 0f,
            Loop = Loop,

            Callback = Callback,
        });

        // Set up timer handle
        Timer Timer = m_Timers[m_Timers.Count - 1];

        Handle.Timer = Timer;
        Handle.Owner = Owner;
        Handle.Owned = Owner ? true : false;
    }

    /** Add timer without ownership.
        Dangerous! Timer can call Action with this == null.
    */
    public void AddTimer(Action Callback, float TimeRate, bool bLoop = false, float FirstDelay = -1f)
    {
        AddTimer(null, null, Callback, TimeRate, bLoop, FirstDelay);
    }

    public void RemoveTimer(Handle Handle)
    {
        if (Handle.Valid)
        {
            // Mark timer as removed
            Handle.Timer.Handle = null;

            // Reset handle
            Handle.Timer = null;
            Handle.Owner = null;
            Handle.Owned = false;
        }
    }

    public void RemoveOwnerTimers(UnityEngine.Object Owner)
    {
        for (int i = m_Timers.Count - 1; i >= 0; --i)
        {
            if (m_Timers[i].Handle != null && m_Timers[i].Handle.Owner == Owner)
            {
                RemoveTimer(m_Timers[i].Handle);
            }
        }
    }
}
