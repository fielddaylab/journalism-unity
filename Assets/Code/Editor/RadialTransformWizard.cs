using UnityEditor;
using UnityEngine;

namespace Journalism {
    public class RadialTransformWizard : ScriptableWizard {
        public Transform[] Transforms = new Transform[0];
        public float Radius = 50;
        public float AngleOffset = 0;

        [MenuItem("GameObject/Wizards/Distribute Transforms Radially")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<RadialTransformWizard>("Distribute Radially", "Distribute");
        }

        private void OnWizardCreate() {
            float angleOffset = Mathf.Deg2Rad * AngleOffset;
            float angleIncrement = Mathf.PI * 2 / Transforms.Length;
            float a;
            Transform t;
            Vector3 local;
            for(int i = 0; i < Transforms.Length; i++) {
                t = Transforms[i];
                a = angleOffset + angleIncrement * i;
                Undo.RecordObject(t, "Radial distribution");
                local = t.localPosition;
                local.x = Radius * Mathf.Cos(a);
                local.y = Radius * Mathf.Sin(a);
                t.localPosition = local;
            }
        }
    }
}