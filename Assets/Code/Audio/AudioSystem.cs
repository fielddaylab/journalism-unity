using UnityEngine;
using Leaf.Runtime;
using BeauUtil;
using BeauUtil.Debugger;
using System.Collections.Generic;
using EasyAssetStreaming;
using System.Collections;
using BeauRoutine;

namespace Journalism {
    public sealed class AudioSystem : MonoBehaviour {
        #region Inspector

        [SerializeField] private AudioBundle m_DefaultBundle = null;
        [SerializeField, Required] private DownloadStreamingAudioSource m_AmbienceAudio = null;
        [SerializeField, Required] private DownloadStreamingAudioSource m_MusicAudio = null;
        [SerializeField, Required] private DownloadStreamingAudioSource m_RainAudio = null;
        [SerializeField] private float m_CrossFadeDuration = 0.5f;

        #endregion // Inspector

        private readonly Dictionary<StringHash32, AudioEvent> m_Events = new Dictionary<StringHash32, AudioEvent>(64);
        private readonly HashSet<AudioBundle> m_LoadedBundles = new HashSet<AudioBundle>();

        private Routine m_AmbienceFade;
        private Routine m_MusicFade;
        private Routine m_RainFade;

        private float m_MaxVolume;
        [SerializeField] private float m_DefaultMaxVolume;

        private void Awake() {
            if (m_DefaultBundle != null) {
                LoadBundle(m_DefaultBundle);
            }

            m_MaxVolume = m_DefaultMaxVolume;
        }

        public void SetMaxVolume(float value) {
            m_MaxVolume = value;

            m_MusicAudio.Volume = m_MaxVolume;
            m_AmbienceAudio.Volume = m_MaxVolume;
            m_RainAudio.Volume = m_MaxVolume;
        }

        public float GetMaxVolume() {
            return m_MaxVolume;
        }

        #region Loading

        public void LoadBundle(AudioBundle bundle) {
            if (!m_LoadedBundles.Add(bundle)) {
                return;
            }

            foreach(var evt in bundle.Events) {
                StringHash32 id = evt.name;
                if (m_Events.ContainsKey(id)) {
                    Log.Error("[AudioSystem] Multiple AudioEvents with id '{0}'", id);
                } else {
                    m_Events.Add(id, evt);
                }
            }
        }

        public void UnloadBundle(AudioBundle bundle) {
            if (!m_LoadedBundles.Remove(bundle)) {
                return;
            }

            foreach(var evt in bundle.Events) {
                m_Events.Remove(evt.name);
            }
        }

        #endregion // Loading

        #region Playing

        public float PlayOneShot(StringHash32 id) {
            if (id.IsEmpty) {
                return 0;
            }
            
            AudioEvent evt;
            if (!m_Events.TryGetValue(id, out evt)) {
                Log.Warn("[AudioSystem] Event with id '{0}' is not loaded", id);
                return 0;
            }

            return PlayOneShot(evt);
        }

        public float PlayOneShot(AudioEvent evt) {
            if (evt == null) {
                return 0;
            }

            if (evt.SampleRandomizer == null) {
                evt.SampleRandomizer = new RandomDeck<AudioClip>(evt.Samples);
            }

            AudioClip clip = evt.SampleRandomizer.Next();
            AudioSource.PlayClipAtPoint(clip, default, m_MaxVolume);
            return clip.length;
        }

        public void SetAmbience(string url, float volume) {
            m_AmbienceFade.Replace(this, Transition(m_AmbienceAudio, url, volume * m_MaxVolume, m_CrossFadeDuration));
        }

        public void SetMusic(string url, float volume) {
            m_MusicFade.Replace(this, Transition(m_MusicAudio, url, volume * m_MaxVolume, m_CrossFadeDuration));
        }

        public void SetRain(string url, float volume) {
            m_RainFade.Replace(this, Transition(m_RainAudio, url, volume * m_MaxVolume, m_CrossFadeDuration));
        }

        static private IEnumerator Transition(DownloadStreamingAudioSource source, string url, float volume, float duration) {
            if (source.Path == url) {
                if (source.Volume != volume) {
                    yield return Tween.Float(source.Volume, volume, (f) => source.Volume = f, duration);
                }
            } else {
                bool bFadeOut = source.IsPlaying;
                bool bFadeIn = !string.IsNullOrEmpty(url);
                float animSlice = duration;
                if (bFadeOut && bFadeIn) {
                    animSlice /= 2;
                }
                if (bFadeOut) {
                    yield return Tween.Float(source.Volume, 0, (f) => source.Volume = f, animSlice);
                    source.Unload();
                }
                if (bFadeIn) {
                    source.Path = url;
                    source.Stop();
                    source.Volume = 0;
                    while(source.IsLoading()) {
                        yield return null;
                    }
                    source.Play();
                    yield return Tween.Float(source.Volume, volume, (f) => source.Volume = f, animSlice);
                }
            }

            Streaming.UnloadUnusedAsync();
        }

        #endregion // Playing

        #region Leaf

        private enum WaitMode {
            Forget,
            Wait,
        }

        [LeafMember("SFX"), UnityEngine.Scripting.Preserve]
        static private IEnumerator LeafPlayOneShot(StringHash32 id, WaitMode wait = WaitMode.Forget) {
            if (DebugService.AutoTesting) {
                return null;
            }
            float duration = Game.Audio.PlayOneShot(id);
            if (wait == WaitMode.Wait) {
                return Routine.Yield(duration);
            } else {
                return null;
            }
        }

        [LeafMember("Music"), UnityEngine.Scripting.Preserve]
        static private void LeafSetMusic(string url, float volume = 1) {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.Audio.SetMusic(url, volume);
        }

        [LeafMember("StopMusic"), UnityEngine.Scripting.Preserve]
        static private void LeafStopMusic() {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.Audio.SetMusic(null, 1);
        }

        [LeafMember("Ambience"), UnityEngine.Scripting.Preserve]
        static private void LeafSetAmbience(string url, float volume = 1) {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.Audio.SetAmbience(url, volume);
        }

        [LeafMember("StopAmbience"), UnityEngine.Scripting.Preserve]
        static private void LeafStopAmbience() {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.Audio.SetAmbience(null, 1);
        }

        [LeafMember("Rain"), UnityEngine.Scripting.Preserve]
        static private void LeafSetRain(string url, float volume = 1) {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.Audio.SetRain(url, volume);
        }

        [LeafMember("StopRain"), UnityEngine.Scripting.Preserve]
        static private void LeafStopRain() {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.Audio.SetRain(null, 1);
        }

        #endregion // Leaf
    }
}