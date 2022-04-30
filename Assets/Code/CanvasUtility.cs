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

        static public void SetAnchorX(RectTransform rect, float x) {
            Vector2 anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax;
            anchorMin.x = x;
            anchorMax.x = x;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }

        static public void SetAnchorsX(RectTransform rect, float left, float right) {
            Vector2 anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax;
            anchorMin.x = left;
            anchorMax.x = right;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }

        #region RectTransform

        static public RectTransform RectTransform(this Component component) {
            return (RectTransform) component.transform;
        }

        static public RectTransform RectTransform(this GameObject go) {
            return (RectTransform) go.transform;
        }

        #endregion // RectTransform

        #region CanvasGroup

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

        #endregion // Canvas Group

        #region ColorGroup

        static public IEnumerator Show(this ColorGroup group, float duration, bool? raycasts = true) {
            if (!group.gameObject.activeSelf) {
                group.SetAlpha(0);
                group.gameObject.SetActive(true);
            }
            if (raycasts.HasValue)
                group.BlocksRaycasts = false;
            yield return Tween.Float(group.GetAlpha(), 1, group.SetAlpha, duration);
            if (raycasts.HasValue)
                group.BlocksRaycasts = raycasts.Value;
        }

        static public void Show(this ColorGroup group, bool? raycasts = true) {
            group.SetAlpha(1);
            if (raycasts.HasValue) {
                group.BlocksRaycasts = raycasts.Value;
            }
            group.gameObject.SetActive(true);
        }

        static public IEnumerator Hide(this ColorGroup group, float duration, bool? raycasts = false) {
            if (group.gameObject.activeSelf) {
                if (raycasts.HasValue)
                    group.BlocksRaycasts = raycasts.Value;
                yield return Tween.Float(group.GetAlpha(), 0, group.SetAlpha, duration);
                group.gameObject.SetActive(false);
            }
        }

        static public void Hide(this ColorGroup group, bool? raycasts = false) {
            group.gameObject.SetActive(false);
            group.SetAlpha(0);
            if (raycasts.HasValue) {
                group.BlocksRaycasts = raycasts.Value;
            }
        }

        #endregion // ColorGroup

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

        static public bool IsVisible(this Graphic graphic) {
            return graphic.isActiveAndEnabled && graphic.GetAlpha() > 0;
        }
    }
}