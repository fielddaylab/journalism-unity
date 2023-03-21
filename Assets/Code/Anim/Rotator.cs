using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Journalism
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Transform m_ToRotate;
        [SerializeField] private float m_RotationAmt;
        [SerializeField] private float m_RotationTime;

        private float m_StartRotation;

        private void Awake() {
            m_StartRotation = m_ToRotate.rotation.z;
        }

        private void Start() {
            Routine.Start(RotateRoutine());
        }

        private IEnumerator RotateRoutine() {
            float dest = m_StartRotation + m_RotationAmt;
            yield return m_ToRotate.RotateTo(dest, m_RotationTime, Axis.Z);

            while (m_ToRotate.gameObject.activeInHierarchy) {
                dest = m_StartRotation - m_RotationAmt;
                yield return m_ToRotate.RotateTo(dest, m_RotationTime * 2, Axis.Z);

                if (!m_ToRotate.gameObject.activeInHierarchy) { break; }
                yield return 0.3f;

                if (!m_ToRotate.gameObject.activeInHierarchy) { break; }
                dest = m_StartRotation + m_RotationAmt;
                yield return m_ToRotate.RotateTo(dest, m_RotationTime * 2, Axis.Z);
            }
        }
    }
}
