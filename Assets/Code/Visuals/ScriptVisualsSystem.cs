using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using Leaf.Runtime;
using EasyAssetStreaming;
using UnityEngine;

namespace Journalism {
    public sealed class ScriptVisualsSystem : MonoBehaviour {

        #region Inspector
        
        [Header("Background")]
        [SerializeField] private Camera m_CameraBackground = null;
        [SerializeField] private StreamingQuadTexture m_BackgroundTextureA = null;
        [SerializeField] private StreamingQuadTexture m_BackgroundTextureB = null;
        [SerializeField] private Material m_BackgroundOpaqueMaterial = null;
        [SerializeField] private Material m_BackgroundTransparencyMaterial = null;

        [Header("Transitions")]
        [SerializeField] private float m_DefaultCrossfadeDuration = 0.5f;
        [SerializeField] private ColorGroup m_FullScreenSolid = null;

        #endregion // Inspector

        [NonSerialized] private StreamingQuadTexture m_CurrentBackgroundTexture = null;
        [NonSerialized] private StreamingQuadTexture m_QueuedBackgroundTexture = null;
        private Routine m_CurrentBackgroundTransition;
        private Routine m_FullScreenTransition;

        private void Awake() {
            m_CameraBackground.backgroundColor = Color.black;

            Game.Events.Register(GameEvents.StoryEvalBegin, OnEvalBegin, this)
                .Register(GameEvents.StoryEvalEnd, OnEvalEnd, this);
        }

        private StreamingQuadTexture GetNextBackground() {
            if (m_CurrentBackgroundTexture == null) {
                return m_BackgroundTextureA;
            }

            if (m_CurrentBackgroundTexture.Alpha == 0 || m_CurrentBackgroundTexture.Color == Color.black) {
                return m_CurrentBackgroundTexture;
            }

            if (m_CurrentBackgroundTexture == m_BackgroundTextureA) {
                return m_BackgroundTextureB;
            } else {
                return m_BackgroundTextureA;
            }
        }

        #region Handlers

        public void ConfigureHandlers(CustomTagParserConfig config, TagStringEventHandler handlers) {
            handlers.Register(GameText.Events.Background, HandleBackgroundTransition)
                .Register(GameText.Events.BackgroundFadeOut, HandleBackgroundFadeOut)
                .Register(GameText.Events.BackgroundFadeIn, HandleBackgroundFadeIn);
        }

        private IEnumerator HandleBackgroundTransition(TagEventData eventData, object context) {
            var args = eventData.ExtractStringArgs();
            LeafEvalContext eval = LeafEvalContext.FromObject(context);
            string path = args[0].ToString();

            return FadeInBackground(path);
        }

        private IEnumerator HandleBackgroundFadeOut(TagEventData eventData, object context) {
            if (m_CurrentBackgroundTexture != null) {
                yield return FadeOut(m_CurrentBackgroundTexture, Color.black, 0.3f);
            }
        }

        private IEnumerator HandleBackgroundFadeIn(TagEventData eventData, object context) {
            if (m_CurrentBackgroundTexture != null) {
                yield return FadeIn(m_CurrentBackgroundTexture, 0.3f);
            }
        }

        #endregion // Handlers

        public void ClearBackgrounds() {
            m_CurrentBackgroundTransition.Stop();
            m_CurrentBackgroundTexture = null;
            m_QueuedBackgroundTexture = null;
            m_CameraBackground.backgroundColor = Color.black;
            m_BackgroundTextureA.Unload();
            m_BackgroundTextureB.Unload();
            m_BackgroundTextureA.Alpha = 0;
            m_BackgroundTextureB.Alpha = 0;
            m_FullScreenSolid.Hide();
            m_FullScreenTransition.Stop();
        }

        public IEnumerator FadeInBackground(string path) {
            if (m_CurrentBackgroundTexture != null && m_CurrentBackgroundTexture.Path == path && m_CurrentBackgroundTexture.Alpha > 0) {
                yield break;
            }

            yield return PrepareNextBackground(path);
            yield return CrossFade(m_CurrentBackgroundTexture, m_QueuedBackgroundTexture, 0.3f);
            CompletedSwap();
        }

        public IEnumerator FadeOutBackgrounds() {
            return HandleBackgroundFadeOut(default, null);
        }

        #region Loading

        private IEnumerator PrepareNextBackground(string path) {
            m_QueuedBackgroundTexture = GetNextBackground();
            m_QueuedBackgroundTexture.Path = path;
            m_QueuedBackgroundTexture.Preload();
            while(m_QueuedBackgroundTexture.IsLoading())
                yield return null;
        }

        private void CompletedSwap() {
            m_CurrentBackgroundTexture = m_QueuedBackgroundTexture;
            m_QueuedBackgroundTexture = null;
            Streaming.UnloadUnusedAsync();
        }

        #endregion // Loading

        #region Transitions

        private IEnumerator CrossFade(StreamingQuadTexture a, StreamingQuadTexture b, float duration) {
            if (a == null || a == b) {
                yield return FadeIn(b, duration);
                yield break;
            }

            if (!b.gameObject.activeSelf) {
                b.gameObject.SetActive(true);
                b.Alpha = 0;
            }
            a.SharedMaterial = m_BackgroundTransparencyMaterial;
            b.SharedMaterial = m_BackgroundTransparencyMaterial;
            a.SortingOrder = 1;
            b.SortingOrder = 0;
            yield return Tween.ZeroToOne((f) => {
                a.Alpha = (1 - f);
                b.Alpha = f;
            }, duration).ForceOnCancel();
            a.SharedMaterial = m_BackgroundOpaqueMaterial;
            b.SharedMaterial = m_BackgroundOpaqueMaterial;
            a.gameObject.SetActive(false);
        }

        private IEnumerator FadeOut(StreamingQuadTexture background, Color color, float duration) {
            if (background.Alpha == 0) {
                yield return m_CameraBackground.BackgroundColorTo(color, duration);
            } else {
                background.SharedMaterial = m_BackgroundTransparencyMaterial;
                IEnumerator fadeBackgroundOut = Tween.Float(background.Alpha, 0, (f) => background.Alpha = f, duration);
                if (background.Color.a == 1) {
                    m_CameraBackground.backgroundColor = color;
                    yield return fadeBackgroundOut;
                } else {
                    yield return Routine.Inline(Routine.Combine(
                        fadeBackgroundOut,
                        m_CameraBackground.BackgroundColorTo(color, duration)
                    ));
                }
            }
        }

        private IEnumerator FadeIn(StreamingQuadTexture background, float duration) {
            if (!background.gameObject.activeSelf || background.Alpha == 0) {
                background.gameObject.SetActive(true);
                background.SharedMaterial = m_BackgroundTransparencyMaterial;
                background.Color = Color.white.WithAlpha(0);
                yield return Tween.Float(0, 1, (f) => background.Alpha = f, duration);
                background.SharedMaterial = m_BackgroundOpaqueMaterial;
            } else if (background.Alpha == 1) {
                background.SharedMaterial = m_BackgroundOpaqueMaterial;
                yield return Tween.Color(background.Color, Color.white, (f) => background.Color = f, duration);
            } else {
                yield return Tween.Color(background.Color, Color.white, (f) => background.Color = f, duration);
                background.SharedMaterial = m_BackgroundOpaqueMaterial;
            }
        }

        private void OnEvalBegin() {
            m_FullScreenTransition.Replace(this, m_FullScreenSolid.Show(0.2f));
        }

        private void OnEvalEnd() {
            m_FullScreenTransition.Replace(this, m_FullScreenSolid.Hide(0.2f));
        }
    
        #endregion // Transitions
    }
}