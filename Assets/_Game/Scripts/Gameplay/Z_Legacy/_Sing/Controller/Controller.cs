#if GAME_SING

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using TMPro;

namespace Sing
{
    [Serializable]
    public class SkinData
    {
        public string Id = "skin_unknown";
        public bool Unlocked = false;

        public GameObject Hat;
        public GameObject Body;

        public float MoneyMultiplier = 1f;
        public float Speed = 1f;

        public AudioClip Clip;
    }

    [Serializable]
    public class ControllerAIData
    {
        [Header("State Machine")]
        [NonSerialized] public AIRootState HSM;
        public bool Enemy = false;
        public float EnemySpeed = 5f;

        [Header("Money")]
        public float MaxMoney = 500000f;

        [Header("Skin")]
        public string SkinId = "skin_default";

        [Header("References")]
        public TextMeshPro NicknameText;
        public NavMeshAgent NavAgent;
        public Animator Animator;
        public Rigidbody Rigidbody;
    }

    [Serializable]
    public class ControllerPlayerData
    {
        public float SpeedMultiplier = 1f;
        public bool ForceDefaultSpeed = false;

        public bool EnableDebugSpeed = false;
        public float DebugSpeed = 10f;
    }

    public class Controller : MonoBehaviour
    {
        private string m_Nickname;
        public string Nickname => m_Nickname;

        private bool m_Initialized = false;

        [SerializeField] private bool m_AIController = true;
        public bool AIController => m_AIController;
        public bool PlayerController => !AIController;

        // @WORKAROUND: For now only for AI
        [NonSerialized] public float Money;

        public float SkinCoinsMultiplier
        {
            get
            {
                if (m_Skin == null)
                {
                    return 1f;
                }

                return m_Skin.MoneyMultiplier;
            }
        }

        [NonSerialized] public Zone Zone;
        [NonSerialized] public List<Zone> UnlockedZones = new();

        private SkinData m_Skin;
        public SkinData Skin => m_Skin;
        [SerializeField] private SkinData[] m_Skins;

        [SerializeField] private AudioSource m_AudioSource;
        public AudioSource AudioSource => m_AudioSource;

        [SerializeField] private ControllerAIData m_AI = new();
        public ControllerAIData AI => m_AI;
        [SerializeField] private ControllerPlayerData m_Player = new();

        public void PlayMusic()
        {
            if (PlayerController && !SingManager.Instance.MusicStarted)
            {
                return;
            }

            // Start music
            SkinData Skin = PlayerController ? m_Skin : GameManager.Instance.Player.Owner.GetSkin(m_AI.SkinId);

            if (m_AudioSource == null)
            {
                Debug.Log($"AudioSource is null for {gameObject.name}!");
                return;
            }

            if (Skin != null && Skin.Clip != null)
            {
                m_AudioSource.clip = Skin.Clip;
                m_AudioSource.Play();

                m_AudioSource.time = AIController ? 0f : SingManager.Instance.GetSongPosition();
            }
            else
            {
                m_AudioSource.Stop();
                m_AudioSource.clip = null;
            }
        }

        public void TogglePlayerDebugSpeed()
        {
            m_Player.EnableDebugSpeed = !m_Player.EnableDebugSpeed;
            UpdateSpeed();
        }

        public void SetPlayerDefaultSpeed(bool Toggle)
        {
            m_Player.ForceDefaultSpeed = Toggle;
            UpdateSpeed();
        }

        public void SetPlayerSpeedMultiplier(float Multiplier)
        {
            m_Player.SpeedMultiplier = Multiplier;
            UpdateSpeed();
        }

        public void ChangeSkin(string SkinName)
        {
            var SkinData = GetSkin(SkinName);
            Assert.IsNotNull(SkinData);

            if (m_Skin != null)
            {
                if (m_Skin.Hat != null)
                {
                    m_Skin.Hat.gameObject.SetActive(false);
                }

                if (m_Skin.Body != null)
                {
                    m_Skin.Body.gameObject.SetActive(false);
                }
            }

            m_Skin = SkinData;

            if (SkinData.Hat != null)
            {
                SkinData.Hat.gameObject.SetActive(true);
            }

            if (SkinData.Body != null)
            {
                SkinData.Body.gameObject.SetActive(true);
            }

            UpdateSpeed();

            if (PlayerController)
            {
                GameManager.Instance.Player.UpdateOutline();

                // Update sound
                PlayMusic();

                SoundController.Instance.MuteSpecificOtherMusic(m_Skin.Clip);
            }
        }

        public void UnlockSkin(string SkinName)
        {
            var SkinData = GetSkin(SkinName);

            if (SkinData != null)
            {
                SkinData.Unlocked = true;
            }

            SingManager.Instance.UINextSkinProgress.UpdateUI();
        }

        public void LockSkin(string SkinName)
        {
            var SkinData = GetSkin(SkinName);

            if (SkinData != null)
            {
                SkinData.Unlocked = false;
            }

            SingManager.Instance.UINextSkinProgress.UpdateUI();
        }

        public SkinData GetSkin(string SkinName)
        {
            Assert.IsTrue(!string.IsNullOrEmpty(SkinName));

            for (int i = 0; i < m_Skins.Length; i++)
            {
                if (m_Skins[i].Id == SkinName)
                {
                    return m_Skins[i];
                }
            }

            return null;
        }

        private void UpdateSpeed()
        {
            if (PlayerController)
            {
                if (m_Player.ForceDefaultSpeed)
                {
                    var DefaultSkin = GetSkin("skin_default");
                    GameManager.Instance.Player.CharMoveController.moveSpeed = DefaultSkin.Speed;
                    return;
                }

                if (m_Player.EnableDebugSpeed)
                {
                    GameManager.Instance.Player.CharMoveController.moveSpeed = m_Player.DebugSpeed;
                    return;
                }

                if (m_Skin != null)
                {
                    GameManager.Instance.Player.CharMoveController.moveSpeed = m_Skin.Speed * m_Player.SpeedMultiplier;
                }
                return;
            }

            if (AI.Enemy)
            {
                AI.NavAgent.speed = AI.EnemySpeed;
                return;
            }

            if (m_Skin != null)
            {
                AI.NavAgent.speed = m_Skin.Speed;
            }
        }

        public void PreInit()
        {
            if (!m_AI.Enemy || PlayerController)
            {
                Assert.IsNotNull(m_AudioSource);
            }

            if (!m_AIController)
            {
#if !UNITY_EDITOR
                m_Player.EnableDebugSpeed = false;
#endif
                return;
            }

            Assert.IsNotNull(m_AI.NavAgent);
            Assert.IsNotNull(m_AI.Rigidbody);

            if (!AI.Enemy)
            {
                Assert.IsNotNull(m_AI.Animator);
                Assert.IsNotNull(m_AI.NicknameText);

                // Unlock zones
                for (int i = 0; i < SingManager.Instance.Zones.Count; ++i)
                {
                    // Zone 0 is always unlocked, but next zones will have chance each time making it harder to get to the latest zone
                    if (i > 0 && UnityEngine.Random.value > SingManager.Instance.AI.UnlockZoneChance)
                    {
                        break;
                    }

                    Zone Zone = SingManager.Instance.GetZoneById(i);
                    Assert.IsNotNull(Zone);

                    UnlockedZones.Add(Zone);
                }

                // Spawn in random unlocked zone
                Zone SpawnZone = UnlockedZones[UnityEngine.Random.Range(0, UnlockedZones.Count)];
                Assert.IsNotNull(SpawnZone);

                transform.position = SpawnZone.GetRandomPositionForCharacter();
            }

            // Init HSM
            m_AI.HSM = new();
            m_AI.HSM.InitHSM(this);
        }

        public void Init()
        {
            if (!m_AIController)
            {
                m_Initialized = true;
                ChangeSkin("skin_default");
                return;
            }

            if (m_AI.Enemy)
            {
                UpdateSpeed();

                m_AI.HSM.AddState<AIState_EnemyBrain>();
            }
            else
            {
                m_Nickname = $"{LocalizationManager.Instance.GetTranslation("bot_nickname_player")} {UnityEngine.Random.Range(0, 999)}";

                m_AI.NicknameText.gameObject.SetActive(true);
                m_AI.NicknameText.text = m_Nickname;

                m_AI.HSM.AddState<AIState_FriendlyBrain>();
            }

            m_Initialized = true;
        }

        private void Update()
        {
            if (!m_Initialized)
            {
                return;
            }

            // Animator
            if (!AI.Enemy || (PlayerController && m_AI.Animator != null))
            {
                // Yeah, we use m_AI.Animator for AI and Player because we need to make changes fast and not brake everything

                if (AIController)
                {
                    m_AI.Animator.SetFloat("Movement", m_AI.NavAgent.velocity.sqrMagnitude);
                }
                else
                {
                    m_AI.Animator.SetFloat("Movement", GameManager.Instance.Player.CharMoveController.JoystickVector.sqrMagnitude);
                }
            }

            if (!m_AIController)
            {
                return;
            }

            // Nickname
            if (!m_AI.Enemy)
            {
                float DistanceToCameraSqr = (transform.position - Camera.main.transform.position).sqrMagnitude;
                const float DistanceThreshold = 35f;
                float DistanceThresholdSqr = DistanceThreshold * DistanceThreshold;

                m_AI.NicknameText.gameObject.SetActive(DistanceToCameraSqr <= DistanceThresholdSqr);
            }

            // HSM
            m_AI.HSM.Update();
        }

        private void OnTriggerEnter(Collider Other)
        {
            if (!m_AI.Enemy)
            {
                return;
            }

            var OtherController = Other.GetComponent<Controller>();
            if (OtherController == null)
            {
                return;
            }

            if (OtherController.PlayerController || !OtherController.AI.Enemy)
            {
                SingManager.Instance.KillCharacter(OtherController);
            }
        }

        private void OnDestroy()
        {
            if (m_AIController && m_AI.HSM != null)
            {
                m_AI.HSM.Clean();
            }
        }
    }
}

#endif