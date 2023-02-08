#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauUtil;
using BeauUtil.Services;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FieldDay;
using BeauUtil.Debugger;

namespace Journalism
{
    public partial class AnalyticsService : ServiceBehaviour, IDebuggable
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "JOURNALISM";
        [SerializeField, Required] private string m_AppVersion = " "; // "1.0"; // TODO: what should this val be?
        [SerializeField] private FirebaseConsts m_Firebase = default(FirebaseConsts);
        
        #endregion // Inspector

        #region Logging Variables

        private OGDLog m_Log;

        /* Examples
        [NonSerialized] private StringHash32 m_CurrentJobHash = null;
        [NonSerialized] private string m_CurrentJobName = NoActiveJobId;
        [NonSerialized] private string m_PreviousJobName = NoActiveJobId;
        */

        [NonSerialized] private bool m_Debug;


        #endregion // Logging Variables

        #region IService

        protected override void Initialize() // TODO: Where to actually initialize this?
        {
            /*
            Game.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob, this)
                .Register();
            */

            // SceneHelper.OnSceneLoaded += LogSceneChanged;

            // CrashHandler.OnCrash += OnCrash;

            // NetworkStats.OnError.Register(OnNetworkError);

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 3 // TODO: what should this val be?
            });
            m_Log.UseFirebase(m_Firebase);

            #if DEVELOPMENT
                m_Debug = true;
            #endif // DEVELOPMENT

            m_Log.SetDebug(m_Debug);
        }

        private void SetUserCode(string userCode)
        {
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 3,
                AppBranch = BuildInfo.Branch()
            });
            m_Log.SetUserId(userCode);
        }

        protected override void Shutdown()
        {
            Game.Events?.DeregisterAll(this);
        }
        #endregion // IService

        #region Log Events

        /*
        private void OnCrash(Exception exception, string error) {
            string text = exception != null ? exception.Message : error;
            using(var e = m_Log.NewEvent("game_error")) {
                e.Param("error_message", text);
                e.Param("scene", SceneHelper.ActiveScene().Name);
                e.Param("time_since_launch", Time.realtimeSinceStartup, 2);
                e.Param("job_name", m_CurrentJobName);
            }
            m_Log.Flush();
        }
        */

        /*
        private void LogSceneChanged(SceneBinding scene, object context)
        {
            string sceneName = scene.Name;

            if (sceneName != "Boot" && sceneName != "Title")
            {
                using(var e = m_Log.NewEvent("scene_changed")) {
                    e.Param("scene_name", sceneName);
                }
            }
        }
        */

        /*
        private void LogRoomChanged(string roomName)
        {
            using(var e = m_Log.NewEvent("room_changed")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("room_name", roomName);
            }
        }
        */

        private void OnNetworkError(string url) {
            if (url.Length > 480) {
                url = url.Substring(0, 477) + "...";
            }
            using(var e = m_Log.NewEvent("load_error")) {
                e.Param("url", url);
            }
        }

        #endregion // Log Events

        #if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus() {
            DMInfo menu = new DMInfo("Analytics", 1);
            menu.AddToggle("Enable Logging", () => {
                return m_Debug;
            }, (t) => {
                m_Debug = t;
                m_Log.SetDebug(t);
            });
            yield return menu;
        }

        #endif // DEVELOPMENT
    }
}
