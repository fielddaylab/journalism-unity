using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;

namespace Journalism.Editor
{
    public class SceneProcessor : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (EditorApplication.isPlaying)
                return;
            
            RemoveDebug(scene);
        }


        static private void RemoveDebug(Scene scene)
        {
            if (EditorUserBuildSettings.development)
                return;
            
            DebugService debug = GameObject.FindObjectOfType<DebugService>();
            if (debug)
            {
                Debug.LogFormat("[SceneProcessor] Removing debug service from scene '{0}'...", scene.name);
                GameObject.DestroyImmediate(debug.gameObject);
            }
        }
    }
}