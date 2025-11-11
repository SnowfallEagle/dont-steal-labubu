#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace Sing
{
    public class Zone : MonoBehaviour
    {
        public const int NoneId = -1;
        public const int ZeroId = 0;

        [Header("Id")]
        [SerializeField] private int m_ZoneId = NoneId;
        public int ZoneId => m_ZoneId;
        [SerializeField] private string m_ZoneNameId = "";

        [Header("Core")]
        public bool Open
        {
            get
            {
                return
                    (m_ZoneId == ZeroId ?
                        true :
                        (m_ZoneBorderTransform != null ?
                            !m_ZoneBorderTransform.gameObject.activeSelf :
                            true
                        )
                );
            }
        }

        [Header("Coins")]
        [SerializeField] private double m_Cost = 1000f;
        public double Cost => m_Cost;

        [SerializeField] private int m_CoinsCountInZone = 50;
        [SerializeField] private int m_CoinsAnimBucketsCount = 8;
        [SerializeField] private float m_CoinsYOffset = 1f;

        [SerializeField] private float m_CoinsAnimOffsetY = 0.5f;
        [SerializeField] private float m_CoinsAnimTime = 1f;

        private List<Coin> m_Coins = new();

        private List<Transform> m_CoinsAnimBuckets = new();

        [SerializeField] private BoxCollider m_CoinsArea;
        public BoxCollider CoinsArea => m_CoinsArea;

        public bool SpawnZone => m_ZoneId == 0;
        public float CoinsMultiplier => SpawnZone ? 1f : m_ZoneId * 2f;

        [Header("Controllers")]
        [SerializeField] private Controller m_EnemyController;

        private List<Controller> m_Controllers = new();
        public List<Controller> Controllers => m_Controllers;

        [Header("Optimization")]
        [SerializeField] private float m_HideDistance = 25000f;
        public float HideDistance => m_HideDistance;

        [SerializeField] private float m_CoinsVisibilityDistanceThreshold = 100f;
        private bool m_CoinsVisible = false;

        [Header("References")]
        [SerializeField] private Transform m_ZoneBorderTransform;
        public Transform ZoneBorderTransform => m_ZoneBorderTransform;
        [SerializeField] private TextMeshPro m_NameAndMultiplierText;
        [SerializeField] private TextMeshPro m_CostText;

        [SerializeField] private Transform m_ZoneContentWithoutCollidersTransform;

        [Header("Prefabs")]
        [SerializeField] private GameObject m_CoinPrefab;

        public Transform GetClosestCoinTransform(Vector3 Position)
        {
            Transform BestTransform = null;
            float BestDistance = float.MaxValue;

            for (int i = 0; i < m_Coins.Count; ++i)
            {
                if (!m_Coins[i].gameObject.activeSelf)
                {
                    continue;
                }

                Transform CoinTransform = m_Coins[i].transform;

                float DistanceSqr = (CoinTransform.position - Position).sqrMagnitude;
                
                if (DistanceSqr < BestDistance)
                {
                    BestTransform = CoinTransform;
                    BestDistance = DistanceSqr;
                }
            }

            return BestTransform;
        }

        private void AnimateCoins()
        {
            for (int i = 0; i < m_CoinsAnimBuckets.Count; ++i)
            {
                GameObject Bucket = m_CoinsAnimBuckets[i].gameObject;

                Vector3 LocalPos = Bucket.transform.localPosition;
                LocalPos.y = 0f;
                Bucket.transform.localPosition = LocalPos;

                LeanTween.moveLocalY(Bucket, m_CoinsAnimOffsetY, m_CoinsAnimTime)
                    .setEaseOutCubic()
                    .setLoopPingPong()
                    .setDelay(Random.Range(0f, m_CoinsAnimTime));
            }
        }

        private void DeanimateCoins()
        {
            for (int i = 0; i < m_CoinsAnimBuckets.Count; ++i)
            {
                GameObject Bucket = m_CoinsAnimBuckets[i].gameObject;

                LeanTween.cancel(Bucket);

                Vector3 LocalPos = Bucket.transform.localPosition;
                LocalPos.y = 0f;
                Bucket.transform.localPosition = LocalPos;
            }
        }

        public void OnCharacterEntered(Controller Controller)
        {
            Assert.IsNotNull(Controller);

            if (Controller.AIController && Controller.AI.Enemy)
            {
                return;
            }

            if (Controller.Zone != null &&
                System.Math.Abs(Controller.Zone.ZoneId - ZoneId) != 1)
            {
                Controller.Zone.OnCharacterLeft(Controller);
            }

            if (!HasController(Controller))
            {
                m_Controllers.Add(Controller);
            }
            Controller.Zone = this;
        }

        public void OnCharacterLeft(Controller Controller)
        {
            Assert.IsNotNull(Controller);

            if (Controller.AIController && Controller.AI.Enemy)
            {
                return;
            }

            m_Controllers.Remove(Controller);
            if (Controller.Zone == this)
            {
                Controller.Zone = null;
            }
        }

        public void OnTryToBuyBorder()
        {
            if (SpawnZone)
            {
                return;
            }

            double Res = GameManager.Instance.Player.WithdrawMoney(m_Cost);

            if (Res < 0f)
            {
                return;
            }

            DeactivateBorder();

            SingManager.Instance.PlayPlayerCoinsVFX();

            SingManager.Instance.SaveGameFully();
        }

        public void OnCollectedCoin(Coin Coin)
        {
            var Player = GameManager.Instance.Player;

            float Coins = 1f;
            Coins *= CoinsMultiplier;
            Coins *= Player.Owner.SkinCoinsMultiplier;
            Coins *= SingManager.Instance.RebirthMoneyMultiplier;
            Coins *= SingManager.Instance.RewardCoinsMultiplier;

            Player.AddMoney(Coins);

            Coin.gameObject.SetActive(false);
            Coin.LastCollectedTime = Time.time;
        }

        public Vector3 GetRandomPositionForCharacter()
        {
            Vector3 Center  = m_CoinsArea.bounds.center;
            Vector3 Extents = m_CoinsArea.bounds.extents;

            Vector3 Position = Center + new Vector3(
                Random.Range(-Extents.x, Extents.x),
                -Extents.y + m_CoinsYOffset,
                Random.Range(-Extents.z, Extents.z)
            ) + new Vector3(0f, 1f, 0f);

            return Position;
        }

        public Vector3 GetCenterFloorPoint()
        {
            Vector3 Center  = m_CoinsArea.bounds.center;
            Vector3 Extents = m_CoinsArea.bounds.extents;

            return Center + new Vector3(0f, -Extents.y, 0f);
        }

        public Vector3 GetRandomPositionInCoinsArea()
        {
            Vector3 Center  = m_CoinsArea.bounds.center;
            Vector3 Extents = m_CoinsArea.bounds.extents;

            Vector3 Position = Center + new Vector3(
                Random.Range(-Extents.x, Extents.x),
                -Extents.y + m_CoinsYOffset,
                Random.Range(-Extents.z, Extents.z)
            );

            return Position;
        }

        public bool IsActivatedZone() => m_ZoneBorderTransform != null && !m_ZoneBorderTransform.gameObject.activeSelf;

        public void ActivateBorder()
        {
            if (!SpawnZone)
            {
                m_ZoneBorderTransform.gameObject.SetActive(true);
            }
        }

        public void DeactivateBorder()
        {
            if (!SpawnZone)
            {
                m_ZoneBorderTransform.gameObject.SetActive(false);
            }
        }

        private bool HasController(Controller Controller)
        {
            return m_Controllers.Find((X) => X == Controller);
        }

        private void OnMoneyChanged()
        {
            if (m_CostText != null)
            {
                m_CostText.color = m_Cost <= GameManager.Instance.Player.Money ? Color.green : Color.red;
            }
        }

        public void Tick()
        {
            // Hide Zone content
            m_ZoneContentWithoutCollidersTransform.gameObject.SetActive((GameManager.Instance.Player.transform.position - transform.position).sqrMagnitude < m_HideDistance * m_HideDistance);

            // Restore some coins
            for (int i = 0; i < m_Coins.Count; ++i)
            {
                var Coin = m_Coins[i];

                if (Time.time - Coin.LastCollectedTime >= Coin.CoinDeactivationDelay)
                {
                    Coin.gameObject.SetActive(true);
                }
            }

            // Coins animation optimization
            bool CanSeeCoins =
                (GameManager.Instance.Player.transform.position - GetCenterFloorPoint()).sqrMagnitude <=
                m_CoinsVisibilityDistanceThreshold * m_CoinsVisibilityDistanceThreshold;

            if (!CanSeeCoins)
            {
                if (m_CoinsVisible)
                {
                    DeanimateCoins();
                    m_CoinsVisible = false;
                }
                return;
            }

            if (!m_CoinsVisible)
            {
                AnimateCoins();
                m_CoinsVisible = true;
            }
        }

        private void OnLocalizationRefresh()
        {
            if (!SpawnZone)
            {
                m_NameAndMultiplierText.text = $"{LocalizationManager.Instance.GetTranslation(m_ZoneNameId)}<br>x{CoinsMultiplier}";
            }
        }

        private void Awake()
        {
            Assert.IsTrue(m_ZoneId != NoneId);
            Assert.IsTrue(!string.IsNullOrEmpty(m_ZoneNameId));

            Assert.IsNotNull(m_CoinsArea);
            Assert.IsNotNull(m_CoinPrefab);

            Assert.IsNotNull(m_ZoneContentWithoutCollidersTransform);

            if (!SpawnZone)
            {
                Assert.IsNotNull(m_EnemyController);

                Assert.IsNotNull(m_ZoneBorderTransform);
                Assert.IsNotNull(m_NameAndMultiplierText);
                Assert.IsNotNull(m_CostText);

                m_CostText.text = $"{m_Cost:F0}";
                LocalizationManager.Instance.OnRefresh += OnLocalizationRefresh;

                m_EnemyController.Zone = this;

                ActivateBorder();
            }

            GameManager.Instance.Player.OnMoneyChanged += OnMoneyChanged;

            // Coin anim buckets
            m_CoinsAnimBuckets.Capacity = m_CoinsAnimBucketsCount;

            for (int i = 0; i < m_CoinsAnimBucketsCount; ++i)
            {
                var GO = new GameObject($"CoinAnimBucket ({i})");
                Assert.IsNotNull(GO);

                m_CoinsAnimBuckets.Add(GO.transform);

                m_CoinsAnimBuckets[i].SetParent(m_ZoneContentWithoutCollidersTransform, true);
                m_CoinsAnimBuckets[i].position = Vector3.zero;
                m_CoinsAnimBuckets[i].rotation = Quaternion.identity;
            }

            // Spawn some coins
            m_Coins.Capacity = m_CoinsCountInZone;

            for (int i = 0; i < m_CoinsCountInZone; ++i)
            {
                Vector3 Position = GetRandomPositionInCoinsArea();

                GameObject CoinGO = Instantiate(m_CoinPrefab, Position, Quaternion.identity, m_CoinsAnimBuckets[i % m_CoinsAnimBucketsCount]);
                Assert.IsNotNull(CoinGO);

                Coin Coin = CoinGO.GetComponent<Coin>();
                m_Coins.Add(Coin);
                Assert.IsNotNull(Coin);
                Coin.Zone = this;
            }
        }
    }
}

#endif
