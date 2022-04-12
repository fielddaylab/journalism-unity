using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauUtil.Debugger;
using BeauRoutine;
using System;
using System.Collections;

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

        static public void SetPivot(RectTransform rect, TextAnchor anchor) {
            rect.pivot = s_Anchors[(int) anchor];
        }

        static public RectTransform RectTransform(this Component component) {
            return (RectTransform) component.transform;
        }

        static public RectTransform RectTransform(this GameObject go) {
            return (RectTransform) go.transform;
        }

        static public IEnumerator Show(this CanvasGroup group, float duration, bool? raycasts = true) {
            if (!group.gameObject.activeSelf) {
                group.alpha = 0;
                group.gameObject.SetActive(true);
            }
            if (raycasts.HasValue)
                group.blocksRaycasts = false;
            yield return group.FadeTo(1, duration);
            if (raycasts.HasValue)
                group.blocksRaycasts = raycasts.Value;
        }

        static public void Show(this CanvasGroup group, bool? raycasts = true) {
            group.alpha = 1;
            if (raycasts.HasValue) {
                group.blocksRaycasts = raycasts.Value;
            }
            group.gameObject.SetActive(true);
        }

        static public IEnumerator Hide(this CanvasGroup group, float duration, bool? raycasts = false) {
            if (group.gameObject.activeSelf) {
                if (raycasts.HasValue)
                    group.blocksRaycasts = raycasts.Value;
                yield return group.FadeTo(0, duration);
                group.gameObject.SetActive(false);
            }
        }

        static public void Hide(this CanvasGroup group, bool? raycasts = false) {
            group.gameObject.SetActive(false);
            group.alpha = 0;
            if (raycasts.HasValue) {
                group.blocksRaycasts = raycasts.Value;
            }
        }

        static public void PropagateSizeUpwards(RectTransform start, RectTransform parent) {
            if (start == parent) {
                return;
            }

            RectTransform t = (RectTransform) start.parent;
            Vector2 size = start.sizeDelta;
            while(t) {
                t.sizeDelta = size;
                if (t == parent) {
                    break;
                }
                t = (RectTransform) t.parent;
            }
        }
    }
}