#if GAME_COOK


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cook
{
    public class AIState_Brain_Brainrot : AIState
    {
        public string State = "none";
        public Controller EnemyInZone;
        public float ChasingTime = 0f;

        public Vector2 SleepTimerRand = new Vector2(5f, 15f);
        public Vector2 WalkAwakeTimerRand = new Vector2(30f, 60f);
        public float SleepStateTimeLeft = 0f;

        public AIState_Brain_Brainrot()
        {
            m_Exclusive = true;

            SleepStateTimeLeft = UnityEngine.Random.Range(WalkAwakeTimerRand.x, WalkAwakeTimerRand.y);
        }

        public override void Update()
        {
            base.Update();

            SleepStateTimeLeft -= Time.deltaTime;

            if (State == "sleep")
            {
                if (SleepStateTimeLeft < 0f)
                {
                    SleepStateTimeLeft = UnityEngine.Random.Range(WalkAwakeTimerRand.x, WalkAwakeTimerRand.y);
                    State = "none";

                    Vector3 Rot = m_Owner.transform.localRotation.eulerAngles;
                    Rot.x = 0f;
                    m_Owner.transform.localRotation = Quaternion.Euler(Rot);
                    m_Owner.AI.SleepingText.enabled = false;

                    m_Owner.AI.NavAgent.enabled = true;
                }
                return;
            }
            else
            {
                if (SleepStateTimeLeft < 0f)
                {
                    FinishSubstates(EAIStateStatus.Success);

                    m_Owner.AI.NavAgent.enabled = false;

                    Vector3 Rot = m_Owner.transform.localRotation.eulerAngles;
                    Rot.x = -90f;
                    m_Owner.transform.localRotation = Quaternion.Euler(Rot);
                    m_Owner.AI.SleepingText.enabled = true;

                    SleepStateTimeLeft = UnityEngine.Random.Range(SleepTimerRand.x, SleepTimerRand.y);
                    State = "sleep";
                    return;
                }
            }

            if (State == "guard-zone")
            {
                ChasingTime += Time.deltaTime;
            }

            var Zone = CookManager.Instance.StealZone;
            var ClosestEnemy = FindClosestEnemyInZone();

            if (GetSubstates().Count > 0)
            {
                if (State == "fuck-around")
                {
                    if (ClosestEnemy != null)
                    {
                        FinishSubstates(EAIStateStatus.Success);
                        return;
                    }
                }
                else if (State == "guard-zone")
                {
                    if (ClosestEnemy == null || EnemyInZone != ClosestEnemy || ChasingTime > 0.2f)
                    {
                        EnemyInZone = null;
                        FinishSubstates(EAIStateStatus.Success);
                        return;
                    }
                }

                return;
            }

            // Sleep

            // Guard zone
            if (Zone.Controllers.Count > 0)
            {
                EnemyInZone = FindClosestEnemyInZone();

                if (EnemyInZone != null)
                {
                    var Goto2 = new AIState_Goto(EnemyInZone.transform.position);
                    AddState(Goto2);
                    State = "guard-zone";
                    ChasingTime = 0f;
                    return;
                }
            }

            // Fuck around when nothing else to do
            var Bounds = Zone.Collider.bounds;

            Vector3 Pos = new Vector3(
                UnityEngine.Random.Range(Bounds.min.x, Bounds.max.x),
                Bounds.min.y,
                UnityEngine.Random.Range(Bounds.min.z, Bounds.max.z)
            );

            var Goto = new AIState_Goto(Pos);
            AddState(Goto);

            State = "fuck-around";
        }

        public Controller FindClosestEnemyInZone()
        {
            Controller BestEnemy = null;
            float BestDist = float.MaxValue;

            Vector2 MyPos = m_Owner.transform.position;

            foreach (var C in CookManager.Instance.StealZone.Controllers)
            {
                if (C == null || (C.AIController && C.AI.Enemy))
                {
                    continue;
                }

                Vector2 EnemyPos = C.transform.position;
                float Dist = (EnemyPos - MyPos).sqrMagnitude;
                if (Dist < BestDist)
                {
                    BestEnemy = C;
                    BestDist = Dist;
                }
            }

            return BestEnemy;
        }
    }
}

#endif