#if GAME_SING

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sing
{
    public class Coin : MonoBehaviour
    {
        [NonSerialized] public Zone Zone;
        [NonSerialized] public float LastCollectedTime = 0;

        [SerializeField] private float m_CoinDeactivationDelay = 2f;
        public float CoinDeactivationDelay => m_CoinDeactivationDelay;

        private void OnTriggerEnter(Collider Other)
        {
            var Player = GameManager.Instance.Player;

            if (Other.gameObject != Player.gameObject)
            {
                return;
            }

            Zone.OnCollectedCoin(this);
        }
    }
}

#endif