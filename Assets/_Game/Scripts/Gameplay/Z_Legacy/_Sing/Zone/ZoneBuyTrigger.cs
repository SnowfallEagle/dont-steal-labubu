#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sing
{
    public class ZoneBuyTrigger : MonoBehaviour
    {
        [SerializeField] private Zone m_Zone;

        private void Awake()
        {
            Assert.IsNotNull(m_Zone);
        }

        private void OnTriggerEnter(Collider Other)
        {
            if (Other.gameObject != GameManager.Instance.Player.gameObject)
            {
                return;
            }

            m_Zone.OnTryToBuyBorder();
        }
    }
}

#endif
