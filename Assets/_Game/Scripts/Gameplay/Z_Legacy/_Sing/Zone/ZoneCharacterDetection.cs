#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sing
{
    public class ZonePlayerDetection : MonoBehaviour
    {
        [SerializeField] private Zone m_Zone;

        private void OnTriggerEnter(Collider Other)
        {
            var Controller = Other.GetComponent<Controller>();

            if (Controller == null)
            {
                return;
            }

            m_Zone.OnCharacterEntered(Controller);
        }

        private void OnTriggerExit(Collider Other)
        {
            var Controller = Other.GetComponent<Controller>();

            if (Controller == null)
            {
                return;
            }

            m_Zone.OnCharacterLeft(Controller);
        }

        private void Awake()
        {
            Assert.IsNotNull(m_Zone);
        }
    }
}

#endif
