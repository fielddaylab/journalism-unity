using UnityEngine;
using BeauUtil.Debugger;
using BeauData;
using EasyBugReporter;
using System;
using BeauRoutine;
using System.Collections;

namespace Journalism {
    public sealed class SaveSystem : MonoBehaviour, IDumpSource {

        [SerializeField] private string m_ServerAddress = null;

        private const string LastUserNameKey = "settings/last-known-profile";

        [SerializeField] private float m_SaveRetryDelay = 10f;

        private PlayerData m_CurrentData;
        [NonSerialized] private string m_UserCode;
        [NonSerialized] private string m_CheckpointData;

        [NonSerialized] private string m_LastKnownProfile;

        private Future<bool> m_SaveOperation;

        public string LastProfileName() { return m_LastKnownProfile; }

        private void Start() {
            m_CurrentData = new PlayerData();

            OGD.Core.Configure(m_ServerAddress, "JOURNALISM");

            m_LastKnownProfile = PlayerPrefs.GetString(LastUserNameKey, string.Empty);
        }

        public bool IsServerSave() {
            return !string.IsNullOrEmpty(m_UserCode);
        }

        public void NewLocalSave() {
            PlayerData data = new PlayerData();
            data.SetDefaults();
            
            m_CheckpointData = null;
            DeclareSave(data, null);
        }

        public void LoadLastCheckpoint() {
            m_CurrentData = Serializer.Read<PlayerData>(m_CheckpointData);
            DeclareSave(m_CurrentData, m_UserCode);
            Log.Msg("[SaveSystem] Loaded checkpoint from node '{0}'", m_CurrentData.CheckpointId);
        }

        public void SaveCheckpoint(bool dispatchEvent = true) {
            m_CurrentData.LastSavedTimestamp = DateTime.Now.ToFileTimeUtc();

            m_CheckpointData = Serializer.Write(m_CurrentData, OutputOptions.None, Serializer.Format.Binary);
            Log.Msg("[SaveSystem] Saved checkpoint at node '{0}'", m_CurrentData.CheckpointId);
            if (dispatchEvent) {
                Game.Events.Queue(GameEvents.LevelCheckpoint);
            }

            if (IsServerSave()) {
                WriteServerSave();
            }
        }

        private void DeclareSave(PlayerData data, string userCode) {
            m_CurrentData = data;
            m_UserCode = userCode;
            SetLastKnownSave(userCode);
            Game.Scripting.DeclareData(data);
            Game.Events.Queue(GameEvents.SaveDeclared, data);
        }

        #region Save

        public Future<bool> NewServerSave(string userCode) {
            if (string.IsNullOrEmpty(userCode)) {
                return Future.Failed<bool>();
            }

            return Future.CreateLinked<bool, string>(ServerDeclareRoutine, userCode, this);
        }

        private IEnumerator ServerDeclareRoutine(Future<bool> future, string userCode) {
            using(var localFuture = Future.Create())
            using(var request = OGD.Player.ClaimId(userCode, null, localFuture.Complete, (f) => localFuture.Fail(f))) {
                yield return localFuture;

                if (localFuture.IsComplete()) {
                    Log.Msg("[DataService] Profile name '{0}' declared to server", userCode);
                } else {
                    Log.Warn("[DataService] Failed to declare profile name to server: {0}", localFuture.GetFailure());
                    future.Fail(localFuture.GetFailure());
                    yield break;
                }
            }

            PlayerData newData = new PlayerData();
            newData.LastSavedTimestamp = DateTime.Now.ToFileTimeUtc();
            newData.SetDefaults();
            string serialized = Serializer.Write(newData, OutputOptions.None, Serializer.Format.Binary);

            using(var localFuture = Future.Create())
            using(var saveRequest = OGD.GameState.PushState(userCode, serialized, localFuture.Complete, (f) => localFuture.Fail(f))) {
                Log.Msg("[SaveSystem] Attempting declare starting save data for id '{0}'", userCode);
                yield return localFuture;

                if (localFuture.IsComplete()) {
                    Log.Msg("[SaveSystem] Saved to server!");
                    DeclareSave(newData, userCode);
                    future.Complete(true);
                } else { 
                    Log.Warn("[SaveSystem] Server save failed.");
                    future.Fail(localFuture.GetFailure());
                }
            }
        }

        public Future<bool> WriteServerSave() {
            if (string.IsNullOrEmpty(m_UserCode)) {
                return Future.Failed<bool>();
            }

            string binarySave = Serializer.Write(m_CurrentData, OutputOptions.None, Serializer.Format.Binary);

            m_SaveOperation?.Cancel();
            return m_SaveOperation = Future.CreateLinked<bool, string, string>(ServerWriteRoutine, m_UserCode, binarySave, this);
        }

        private IEnumerator ServerWriteRoutine(Future<bool> future, string userCode, string saveData) {
            bool saved = false;
            while(!saved) {
                using(var localFuture = Future.Create())
                using(var saveRequest = OGD.GameState.PushState(userCode, saveData, localFuture.Complete, (f) => localFuture.Fail(f))) {
                    Log.Msg("[SaveSystem] Attempting server save with user code '{0}'", userCode);
                    yield return localFuture;

                    if (localFuture.IsComplete()) {
                        Log.Msg("[SaveSystem] Saved to server!");
                        SetLastKnownSave(userCode);
                        saved = true;
                    } else { 
                        Log.Warn("[SaveSystem] Server save failed! Trying again in {0} seconds...", m_SaveRetryDelay);
                        yield return m_SaveRetryDelay;
                    }
                }
            }

            future.Complete(true);
            m_SaveOperation = null;
        }

        public Future<PlayerData> ReadServerSave(string userCode) {
            if (string.IsNullOrEmpty(userCode)) {
                return Future.Failed<PlayerData>();
            }

            return Future.CreateLinked<PlayerData, string>(ServerReadRoutine, userCode, this);
        }

        private IEnumerator ServerReadRoutine(Future<PlayerData> data, string userCode) {
            using(var localFuture = Future.Create<string>())
            using(var loadRequest = OGD.GameState.RequestLatestState(userCode, localFuture.Complete, (f) => localFuture.Fail(f))) {
                Log.Msg("[SaveSystem] Attempting server load with user code '{0}'", userCode);
                yield return localFuture;

                if (localFuture.IsComplete()) {
                    Log.Msg("[SaveSystem] Loaded save from server!");
                    PlayerData deserialized = Serializer.Read<PlayerData>(localFuture.Get());
                    DeclareSave(deserialized, userCode);
                    data.Complete(deserialized);
                } else {
                    Log.Warn("[SaveSystem] Failed to find profile on server: {0}", localFuture.GetFailure());
                    data.Fail(localFuture.GetFailure());
                }
            }
        }

        private void SetLastKnownSave(string userCode) {
            m_LastKnownProfile = userCode;
            Log.Msg("[DataService] Profile name {0} set as last known name", m_LastKnownProfile);
            PlayerPrefs.SetString(LastUserNameKey, m_LastKnownProfile ?? string.Empty);
            PlayerPrefs.Save();
        }

        #endregion // Save

        #region IDumpSource

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

        #endregion // IDumpSource
    }
}