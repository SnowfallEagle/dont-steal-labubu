#if GAME_SING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace Sing
{
    public enum EToggleAudio
    {
        All,
        Player,
        Others
    }

    public class UIToggleAudioButton : MonoBehaviour
    {
        [SerializeField] private EToggleAudio m_Type = EToggleAudio.All;
        private bool m_AudioActivated = true;

        [SerializeField] private Image m_MuteIcon;

        public void OnToggle()
        {
            m_AudioActivated = !m_AudioActivated;
            m_MuteIcon.gameObject.SetActive(!m_AudioActivated);

            switch (m_Type)
            {
                case EToggleAudio.All:
                    if (m_AudioActivated)
                    {
                        SoundController.Instance.UnmuteGame("toggle_all_button");
                    }
                    else
                    {
                        SoundController.Instance.MuteGame("toggle_all_button");
                    }
                    break;

                case EToggleAudio.Player:
                    SoundController.Instance.TogglePlayerMusic(m_AudioActivated);
                    break;

                case EToggleAudio.Others:
                    SoundController.Instance.ToggleOthersMusic(m_AudioActivated);
                    break;

                default:
                    break;
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(m_MuteIcon);
        }
    }
}

#endif
