using UnityEngine;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Audio/Audio Bundle")]
    public sealed class AudioBundle : ScriptableObject {
        public AudioEvent[] Events;

        #if UNITY_EDITOR

        private void FindAllAudioEvents()
        {
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string myDirectory = Path.GetDirectoryName(myPath);
            Events = FindAllAssets<AudioEvent>(myDirectory);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        static private T[] FindAllAssets<T>(params string[] inDirectories) where T : UnityEngine.Object
        {
            if (inDirectories.Length == 0)
                inDirectories = null;
            
            string[] assetGuids = AssetDatabase.FindAssets("t:" + typeof(T).FullName, inDirectories);
            if (assetGuids == null)
                return null;
            
            HashSet<T> assets = new HashSet<T>();
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    T asset = obj as T;
                    if (asset)
                        assets.Add(asset);
                }
            }

            T[] arr = new T[assets.Count];
            assets.CopyTo(arr);
            return arr;
        }

        [UnityEditor.CustomEditor(typeof(AudioBundle)), UnityEditor.CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Load All In Directory"))
                {
                    foreach(AudioBundle bundle in targets)
                    {
                        bundle.FindAllAudioEvents();
                    }
                }
            }
        }
        
        #endif // UNITY_EDITOR
    }
}