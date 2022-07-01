using UnityEngine;
using TMPro;
using BeauRoutine.Extensions;
using System;
using UnityEngine.UI;
using System.Collections;
using BeauRoutine;
using EasyAssetStreaming;
using BeauUtil;
using Leaf.Defaults;
using Leaf;
using Leaf.Runtime;
using BeauUtil.Tags;

namespace Journalism.UI {
    public sealed class GameOverWindow : BasePanel, ITextDisplayer {
        #region Inspector

        [Header("Game Over")]
        [SerializeField] private AnimatedElement m_Icon = null;
        [SerializeField] private AnimatedElement m_Title = null;
        [SerializeField] private AnimatedElement m_Description = null;
        [SerializeField] private AnimatedElement m_Prompt = null;
        [SerializeField] private TextChoiceGroup m_Choices = null;

        #endregion // Inspector

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            GameText.InitializeChoices(m_Choices);
        }

        protected override void OnEnable() {
            base.OnEnable();
        }

        protected override void OnDisable() {
            base.OnDisable();
        }

        #endregion // Unity Events

        #region Transitions

        protected override IEnumerator TransitionToShow() {
            m_RootGroup.alpha = 0;
            m_RootTransform.gameObject.SetActive(true);
            while(Streaming.IsLoading()) {
                yield return null;
            }
            yield return m_RootGroup.Show(0.3f);

            yield return AnimatedElement.Show(m_Icon, 0.2f);
            yield return 0.2f;
            yield return AnimatedElement.Show(m_Title, 0.2f);
        }

        protected override IEnumerator TransitionToHide() {
            yield return m_RootGroup.Hide(0.3f);
        }

        protected override void InstantTransitionToShow() {
            m_RootGroup.Show();
        }

        protected override void InstantTransitionToHide() {
            m_RootGroup.Hide();
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            AnimatedElement.Hide(m_Icon);
            AnimatedElement.Hide(m_Title);
            AnimatedElement.Hide(m_Description);
            AnimatedElement.Hide(m_Prompt);
            GameText.ClearChoices(m_Choices);
            m_Description.Text.SetText(string.Empty);
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHide(inbInstant);

            GameText.ClearChoices(m_Choices);
        }

        #endregion // Transitions

        public Future<bool> DisplayChoices() {
            return Future.CreateLinked<bool>(DisplayChoices_Routine, this);
        }

        private IEnumerator DisplayChoices_Routine(Future<bool> future) {
            if (DebugService.AutoTesting) {
                future.Complete(true);
                yield break;
            }
            
            yield return AnimatedElement.Show(m_Prompt, 0.2f);
            yield return 0.2f;
            yield return GameText.WaitForYesNoChoice(m_Choices, future, "Continue", "Quit", Assets.Style(GameText.Characters.Action));
        }

        #region Leaf

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.VisibleText.Length > 0) {
                m_Description.Text.SetText(GameText.StripQuotes(inString.VisibleText).ToString());
                m_Description.Text.maxVisibleCharacters = 0;
            }

            return null;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            AnimatedElement.Show(m_Description);
            yield return Tween.Int((int) inType.VisibleCharacterOffset, (int) (inType.VisibleCharacterOffset + inType.VisibleCharacterCount), (i) => m_Description.Text.maxVisibleCharacters = i, 0.5f);
        }

        public IEnumerator CompleteLine() {
            yield return 2;
        }

        #endregion // Leaf
    }
}