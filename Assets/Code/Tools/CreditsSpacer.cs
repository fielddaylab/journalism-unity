using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Journalism {
    public class CreditsSpacer : MonoBehaviour
    {
        [SerializeField] private CreditsChunk[] m_ToSpace;
        [SerializeField] private Vector2 m_StartPos;
        [SerializeField] private float m_SpacingPerChunk = 75;
        [SerializeField] private float m_SpacingPerGroupLine = 15;
        [SerializeField] private float m_SpacingPerNamesLine = 5;


        [ContextMenu("ApplySpacing")]
        public void ApplySpacing() {
            float currY = m_StartPos.y;

            for (int i = 0; i < m_ToSpace.Length; i++) {
                m_ToSpace[i].transform.position = new Vector2(m_StartPos.x, currY);

                int numGroupLines = m_ToSpace[i].GroupText.textInfo.lineCount;
                int numNamesLines = m_ToSpace[i].NamesText.textInfo.lineCount;

                currY -= (m_SpacingPerChunk + (m_ToSpace[i].GroupText.fontSize + m_SpacingPerGroupLine) * numGroupLines + (m_ToSpace[i].NamesText.fontSize + m_SpacingPerNamesLine) * numNamesLines);
            }
        }
    }

}