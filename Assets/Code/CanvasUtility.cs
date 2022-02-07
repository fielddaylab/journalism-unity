using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauUtil.Debugger;
using BeauRoutine;
using System;

namespace Journalism {
    static public class CanvasUtility {

        static private readonly Vector2[] s_Anchors = new Vector2[] {
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(1, 1),
            new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f),
            new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(1, 0)
        };

        static public void SetAnchor(RectTransform rect, TextAnchor anchor) {
            Vector2 anchorVec = s_Anchors[(int) anchor];
            rect.anchorMin = rect.anchorMax = anchorVec;
        }
    }
}