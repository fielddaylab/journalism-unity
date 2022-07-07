using UnityEngine;
using BeauUtil.Debugger;
using BeauData;
using EasyBugReporter;

namespace Journalism {
    public sealed class SaveSystem : MonoBehaviour, IDumpSource {
        private PlayerData m_CurrentData;

        // TODO: Actual saving system
        private string m_CheckpointData;

        private void Start() {
            NewSaveData();
        }

        public void NewSaveData() {
            PlayerData data = new PlayerData();
            data.SetDefaults();
            
            m_CheckpointData = null;
            DeclareSave(data);
        }

        public void LoadLastCheckpoint() {
            m_CurrentData = Serializer.Read<PlayerData>(m_CheckpointData);
            DeclareSave(m_CurrentData);
            Log.Msg("[SaveSystem] Loaded checkpoint from node '{0}'", m_CurrentData.CheckpointId);
        }

        public void SaveCheckpoint(bool dispatchEvent = true) {
            m_CheckpointData = Serializer.Write(m_CurrentData, OutputOptions.None, Serializer.Format.Binary);
            Log.Msg("[SaveSystem] Saved checkpoint at node '{0}'", m_CurrentData.CheckpointId);
            if (dispatchEvent) {
                Game.Events.Queue(GameEvents.LevelCheckpoint);
            }
        }

        private void DeclareSave(PlayerData data) {
            m_CurrentData = data;
            Game.Scripting.DeclareData(data);
            Game.Events.Queue(GameEvents.SaveDeclared, data);
        }

        bool IDumpSource.Dump(IDumpWriter dump) {
            dump.Header("Stats");
            for(int stat = 0; stat < Stats.Count; stat++) {
                StatId id = (StatId) stat;
                dump.KeyValue(Stats.Info(id).Id.ToString(), Player.Stat(id));
            }

            dump.Header("Story Inventory");
            foreach(var scrap in Player.Data.StoryScrapInventory) {
                dump.Text(scrap.ToDebugString());
            }

            dump.Header("Time");
            dump.KeyValue("Time Remaining", Player.TimeRemaining());
            dump.Header("Location");
            dump.KeyValue("Location Id", Player.Location().ToDebugString());

            dump.Header("Custom Vars (Global)");
            foreach(var value in Player.Data.GlobalTable) {
                dump.Text(value.ToDebugString());
            }

            dump.Header("Custom Vars (UI)");
            foreach(var value in Player.Data.UITable) {
                dump.Text(value.ToDebugString());
            }

            dump.Header("Visited Nodes");
            foreach(var node in Player.Data.VisitedNodeIds) {
                dump.Text(node.ToDebugString());
            }
            return true;
        }
    }
}