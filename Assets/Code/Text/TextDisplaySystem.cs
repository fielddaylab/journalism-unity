using UnityEngine;
using Leaf.Defaults;
using Leaf;
using Leaf.Runtime;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Debugger;

namespace Journalism {
    public sealed class TextDisplaySystem : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {

        public delegate TagString NextLineDelegate();
        public delegate bool NextChoicesDelegate();

        #region Inspector

        [SerializeField] private TextPools m_Pools = null;
        [SerializeField] private TextLineScroll m_TextDisplay = null;
        [SerializeField] private TextChoiceGroup m_ChoiceDisplay = null;
        [SerializeField] private TextStyles m_Styles = null;

        #endregion // Inspector

        private readonly TagStringEventHandler m_EventHandler = new TagStringEventHandler();
        private TextLine m_QueuedLine;

        public NextLineDelegate LookupNextLine;
        public NextChoicesDelegate LookupNextChoice;

        #region Unity

        private void Awake() {
            m_EventHandler.Register(GameText.Events.Character, HandleCharacter);

            GameText.InitializeScroll(m_TextDisplay);
            GameText.InitializeChoices(m_ChoiceDisplay);
        }

        #endregion // Unity

        #region Events

        private void HandleCharacter(TagEventData evtData, object context) {
            StringHash32 characterId = evtData.GetStringHash();
            // TODO: Some indirection? Character -> Style as opposed to Character == Style?
            SetStyle(characterId);
        }

        private void SetStyle(StringHash32 styleId) {
            var style = m_Styles.Style(styleId);
            Assert.NotNull(m_QueuedLine);
            GameText.SetTextLineStyle(m_QueuedLine, style);
        }

        public IEnumerator HandleNodeStart(ScriptNode node, LeafThreadState thread) {
            bool needsClear = node.HasFlags(ScriptNodeFlags.ClearText);
            if (needsClear) {
                yield return ClearLines();
                yield return 0.1f;
            }
        }

        #endregion // Events

        public IEnumerator ClearLines() {
            yield return GameText.AnimateVanish(m_TextDisplay);
            GameText.ClearLines(m_TextDisplay);
        }

        #region ITextDisplayer

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            m_EventHandler.Base = inBaseHandler;
            if (inString.RichText.Length > 0) {
                m_QueuedLine = GameText.AllocLine(m_TextDisplay, m_Pools);
                StringHash32 characterId = GameText.FindCharacter(inString);
                GameText.PopulateTextLine(m_QueuedLine, inString.RichText, null, default, m_Styles.Style(characterId));
                GameText.AdjustComputedLocations(m_TextDisplay, 1);
            }

            return m_EventHandler;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            GameText.ClearOverflowLines(m_TextDisplay);
            yield return 0.5f;
        }

        public IEnumerator CompleteLine() {
            bool hasChoices = LookupNextChoice();
            if (hasChoices) {
                yield return 0.5f;
                yield break;
            }

            var nextLineText = LookupNextLine();
            if (nextLineText != null && GameText.FindCharacter(nextLineText) == "me") {
                yield return GameText.WaitForPlayerNext(m_ChoiceDisplay, nextLineText.RichText, m_Styles.Style("me"));
                yield break;
            }

            yield return GameText.WaitForDefaultNext(m_ChoiceDisplay, m_Styles.Style("action"));
        }

        #endregion // ITextDisplayer

        #region IChoiceDisplayer

        public IEnumerator ShowChoice(LeafChoice inChoice, LeafThreadState inThread, ILeafPlugin inPlugin) {
            throw new System.NotImplementedException();
        }

        #endregion // IChoiceDisplayer
    }
}