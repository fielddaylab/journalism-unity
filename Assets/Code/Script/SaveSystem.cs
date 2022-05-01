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
using System;
using BeauData;

namespace Journalism {
    public sealed class SaveSystem : MonoBehaviour {
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
    }
}