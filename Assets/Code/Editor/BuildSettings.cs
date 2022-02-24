using System.Collections;
using BeauUtil;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Collections.Generic;
using BeauUtil.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using BeauUtil.Debugger;
using System;

namespace Journalism.Editor
{
    static public class BuildSettings
    {
        [InitializeOnLoadMethod]
        static private void Setup()
        {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;

            // TODO: More sophisticated mechanism for controlling development, defines, other flags...
            string branch = BuildUtils.GetSourceControlBranchName();
            bool bDesiredDevBuild = false;
            
            if (branch != null)
            {
                if (branch.StartsWith("feature/") || branch.StartsWith("fix/") || branch.StartsWith("improvement/")
                    || branch.Contains("dev") || branch.Contains("proto") || branch.Contains("main"))
                {
                    bDesiredDevBuild = true;
                }
            }

            bool bApply = bDesiredDevBuild != EditorUserBuildSettings.development || UnityEditorInternal.InternalEditorUtility.inBatchMode;

            if (bApply)
            {
                EditorUserBuildSettings.development = bDesiredDevBuild;
                Debug.LogFormat("[BuildSettings] Source control branch is '{0}', switched development build to {1}", branch, bDesiredDevBuild);

                if (bDesiredDevBuild)
                {
                    BuildUtils.WriteDefines("DEVELOPMENT");
                }
                else
                {
                    BuildUtils.WriteDefines(null);
                }
            }

            PlayerSettings.SetManagedStrippingLevel(EditorUserBuildSettings.selectedBuildTargetGroup, bDesiredDevBuild ? ManagedStrippingLevel.Medium : ManagedStrippingLevel.High);
        }

        private class BuildPreprocess : IPreprocessBuildWithReport
        {
            public int callbackOrder { get { return 99; } }

            public void OnPreprocessBuild(BuildReport report)
            {
                string branch = BuildUtils.GetSourceControlBranchName();
                bool bBatch = UnityEditorInternal.InternalEditorUtility.inBatchMode;
                if (bBatch)
                {
                    PlayerSettings.SplashScreen.show = false;
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                }
                Debug.LogFormat("[BuildSettings] Building branch '{0}', development mode {1}", branch, EditorUserBuildSettings.development);
            }
        }
    }
}