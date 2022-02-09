using UnityEngine;
using Leaf.Defaults;
using Leaf;
using Leaf.Runtime;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using BeauRoutine;

namespace Journalism {
    public sealed class ScriptSystem : MonoBehaviour {

        #region Inspector

        [SerializeField] private TextDisplaySystem m_TextDisplay = null;
        [SerializeField] private ScriptVisualsSystem m_Visuals = null;

        [Header("-- DEBUG --")]
        [SerializeField] private LeafAsset m_TestScript = null;

        #endregion // Inspector

        private LeafIntegration m_Integration;
        private CustomVariantResolver m_Resolver;

        private void Awake() {
            m_Resolver = new CustomVariantResolver();

            BeauRoutine.Routine.Settings.ForceSingleThreaded = true;

            m_Integration = new LeafIntegration(this, m_Resolver);
            m_Integration.ConfigureDisplay(m_TextDisplay, m_TextDisplay);

            CustomTagParserConfig parserConfig = new CustomTagParserConfig();
            TagStringEventHandler eventHandler = new TagStringEventHandler();
            GameText.InitializeEvents(parserConfig, eventHandler, m_Integration, m_Visuals, m_TextDisplay);
            m_Integration.ConfigureTagStringHandling(parserConfig, eventHandler);

            m_Integration.HandleNodeEnter = HandleNodeEnter;

            m_TextDisplay.LookupNextChoice = m_Integration.PredictChoice;
            m_TextDisplay.LookupNextLine = m_Integration.PredictNextLine;
            m_TextDisplay.LookupLine = m_Integration.LookupLine;

            // TODO: TESTING LOGIC - MOVE TO ITS OWN SCRIPT

            DeclareData(new PlayerData());

            m_Integration.LoadScript(m_TestScript).OnComplete((s) => m_Integration.StartFromBeginning());
        }

        private IEnumerator HandleNodeEnter(ScriptNode node, LeafThreadState thread) {
            yield return m_TextDisplay.HandleNodeStart(node, thread);
        }

        public void DeclareData(PlayerData data) {
            m_Resolver.Clear();
            m_Resolver.SetDefaultTable(data.GlobalTable);
            m_Resolver.SetTable(data.UITable);
            Player.DeclareData(data, m_Resolver);
            Game.Events.DispatchAsync(Events.SaveDeclared, data);
        }
    }
}