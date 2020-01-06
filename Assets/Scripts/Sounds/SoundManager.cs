using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Audio;

namespace Sounds
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup backgroundMusicGroup;
        [SerializeField] private AudioSource backgroundAudioSource;

        private GameState _state;

        public GameState State
        {
            get => _state;
            set
            {
                if (value == _state)
                    return;

                _state = value;
                _SwitchState();
            }
        }

        private void _SwitchState()
        {
            switch (_state)
            {
                case GameState.Paused:
                    _GainDownBackground();
                    break;
                case GameState.Menu:
                    StopAllCoroutines();
                    StartCoroutine(_PitchUp());
                    _GainDownBackground();
                    break;
                case GameState.Dead:
                    StopAllCoroutines();
                    StartCoroutine(_PitchDown());
                    break;
                case GameState.Play:
                    _GainUpBackground();
                    StopAllCoroutines();
                    StartCoroutine(_PitchUp());
                    break;
            }
        }

        private IEnumerator _PitchDown()
        {
            while (backgroundAudioSource.pitch > 0f)
            {
                backgroundAudioSource.pitch -= Time.deltaTime;
                yield return new WaitForSeconds(0.01f);
            }

            backgroundAudioSource.pitch = 0f;
        }

        private IEnumerator _PitchUp()
        {
            while (backgroundAudioSource.pitch < 1f)
            {
                backgroundAudioSource.pitch += Time.deltaTime;
                yield return new WaitForSeconds(0.01f);
            }

            backgroundAudioSource.pitch = 1f;
        }

        private void _GainDownBackground()
        {
            backgroundMusicGroup.audioMixer.SetFloat("FrequencyGain", 0.5f);
            backgroundMusicGroup.audioMixer.SetFloat("LowpassFreq", 300f);
        }

        private void _GainUpBackground()
        {
            backgroundMusicGroup.audioMixer.SetFloat("FrequencyGain", 1f);
            backgroundMusicGroup.audioMixer.SetFloat("LowpassFreq", 22000f);
        }
    }
}