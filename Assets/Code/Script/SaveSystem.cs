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
            m_CheckpointData = null;
            DeclareSave(data);
        }

        public void LoadLastCheckpoint() {
            m_CurrentData = Serializer.Read<PlayerData>(m_CheckpointData);
            DeclareSave(m_CurrentData);
        }

        public void SaveCheckpoint() {
            m_CheckpointData = Serializer.Write(m_CurrentData, OutputOptions.None, Serializer.Format.Binary);
        }

        private void DeclareSave(PlayerData data) {
            m_CurrentData = data;
            Game.Scripting.DeclareData(data);
            Game.Events.DispatchAsync(Events.SaveDeclared, data);
        }
    }
}