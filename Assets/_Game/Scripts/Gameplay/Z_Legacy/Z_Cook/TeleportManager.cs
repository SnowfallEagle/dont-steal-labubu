using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cook
{
    public class TeleportManager : Singleton<TeleportManager>
    {
        [SerializeField] private Transform m_Buy;
        [SerializeField] private Transform m_Home;
        [SerializeField] private Transform m_Sell;

        public void OnBuy()
        {
            Teleport(m_Buy);
        }

        public void OnSell()
        {
            Teleport(m_Sell);
        }

        public void OnHome()
        {
            Teleport(m_Home);
        }

        private void Teleport(Transform Point)
        {
            var Player = GameManager.Instance.Player;
            if (!Player || !Point)
            {
                Debug.LogWarning($"{nameof(Teleport)}: No Player or Transform Point");
                return;
            }

            Player.transform.position = Point.position;
            Player.transform.rotation = Point.rotation;
        }

        private void Start()
        {
            GameManager.Instance.Player.CharMoveController.OnStuck += OnHome;
        }
    }
}
