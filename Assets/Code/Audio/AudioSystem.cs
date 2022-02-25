using UnityEngine;
using Leaf.Runtime;
using BeauUtil;
using BeauUtil.Debugger;
using System.Collections.Generic;
using EasyAssetStreaming;
using System.Collections;

namespace Journalism {
    public sealed class AudioSystem : MonoBehaviour {
        #region Inspector

        [SerializeField] private AudioBundle m_DefaultBundle = null;
        [SerializeField, Required] private DownloadStreamingAudioSource m_AmbienceAudio = null;
        [SerializeField, Required] private DownloadStreamingAudioSource m_MusicAudio = null;

        #endregion // Inspector

        private readonly Dictionary<StringHash32, AudioEvent> m_Events = new Dictionary<StringHash32, AudioEvent>(64);
        private readonly HashSet<AudioBundle> m_LoadedBundles = new HashSet<AudioBundle>();

        private void Awake() {
            if (m_DefaultBundle != null) {
                LoadBundle(m_DefaultBundle);
            }
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

        public void PlayOneShot(StringHash32 id) {
            if (id.IsEmpty) {
                return;
            }
            
            AudioEvent evt;
            if (!m_Events.TryGetValue(id, out evt)) {
                Log.Warn("[AudioSystem] Event with id '{0}' is not loaded", id);
                return;
            }

            PlayOneShot(evt);
        }

        public void PlayOneShot(AudioEvent evt) {
            if (evt == null) {
                return;
            }

            if (evt.SampleRandomizer == null) {
                evt.SampleRandomizer = new RandomDeck<AudioClip>(evt.Samples);
            }

            AudioClip clip = evt.SampleRandomizer.Next();
            AudioSource.PlayClipAtPoint(clip, default, evt.Volume.Generate());
        }

        public void SetAmbience(string url, float volume) {
            m_AmbienceAudio.Path = url;
            if (!string.IsNullOrEmpty(url)) {
                m_AmbienceAudio.Play();
            } else {
                m_AmbienceAudio.Stop();
            }
        }

        public void SetMusic(string url, float volume) {
            m_MusicAudio.Path = url;
            m_MusicAudio.Volume = volume;
            if (!string.IsNullOrEmpty(url)) {
                m_MusicAudio.Play();
            } else {
                m_MusicAudio.Stop();
            }
        }

        #endregion // Playing

        #region Leaf

        [LeafMember("SFX"), UnityEngine.Scripting.Preserve]
        static private void LeafPlayOneShot(StringHash32 id) {
            Game.Audio.PlayOneShot(id);
        }

        [LeafMember("Music"), UnityEngine.Scripting.Preserve]
        static private void LeafSetMusic(string url, float volume = 1) {
            Game.Audio.SetMusic(url, volume);
        }

        [LeafMember("StopMusic"), UnityEngine.Scripting.Preserve]
        static private void LeafStopMusic() {
            Game.Audio.SetMusic(null, 1);
        }

        [LeafMember("Ambience"), UnityEngine.Scripting.Preserve]
        static private void LeafSetAmbience(string url, float volume = 1) {
            Game.Audio.SetAmbience(url, volume);
        }

        [LeafMember("StopAmbience"), UnityEngine.Scripting.Preserve]
        static private void LeafStopAmbience() {
            Game.Audio.SetAmbience(null, 1);
        }

        #endregion // Leaf
    }
}