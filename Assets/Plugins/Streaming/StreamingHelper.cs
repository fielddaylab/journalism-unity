#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauUtil;
using System;
using UnityEngine;

namespace StreamingAssets {

    /// <summary>
    /// Helper and utility methods for streaming.
    /// </summary>
    static internal class StreamingHelper {
        #region Resources

        static internal void DestroyResource<T>(ref T resource) where T : UnityEngine.Object {
            if (resource != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEngine.Object.DestroyImmediate(resource);
                } else {
                    UnityEngine.Object.Destroy(resource);
                }
                #else
                UnityEngine.Object.Destroy(resource);
                #endif // UNITY_EDITOR

                resource = null;
            }
        }

        static internal void DestroyResource(UnityEngine.Object resource) {
            if (resource != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEngine.Object.DestroyImmediate(resource);
                } else {
                    UnityEngine.Object.Destroy(resource);
                }
                #else
                UnityEngine.Object.Destroy(resource);
                #endif // UNITY_EDITOR
            }
        }

        static internal long CalculateMemoryUsage(UnityEngine.Object resource) {
            Texture2D tex = resource as Texture2D;
            if (tex != null) {
                return UnityHelper.CalculateMemoryUsage(tex);
            }

            return 0;
        }
    
        #endregion // Resources

        #region Streamed Textures

        /// <summary>
        /// Calculates the size of a window into a given texture region.
        /// </summary>
        static internal Vector2 GetTextureRegionSize(Texture2D texture, Rect uvRect) {
            Vector2 size;
            size.x = texture.width * Math.Abs(uvRect.width);
            size.y = texture.height * Math.Abs(uvRect.height);
            return size;
        }

        /// <summary>
        /// Retrieves the parent size for the given Transform.
        /// </summary>
        static internal Vector2? GetParentSize(Transform transform) {
            RectTransform parent = transform.parent as RectTransform;
            if (parent) {
                return parent.rect.size;
            }

            RectTransform selfRect = transform as RectTransform;
            if (selfRect) {
                return selfRect.rect.size;
            }

            return null;
        }

        static internal bool IsAutoSizeHorizontal(AutoSizeMode sizeMode) {
            switch(sizeMode) {
                case AutoSizeMode.StretchX:
                case AutoSizeMode.Fit:
                case AutoSizeMode.Fill:
                    return true;

                default:
                    return false;
            }
        }

        static internal bool IsAutoSizeVertical(AutoSizeMode sizeMode) {
            switch(sizeMode) {
                case AutoSizeMode.StretchY:
                    return true;

                default:
                    return false;
            }
        }

        static internal bool AutoSize(AutoSizeMode sizeMode, Texture2D texture, Rect uvRect, ref Vector2 size, Vector2? parentSize) {
            if (sizeMode == AutoSizeMode.Disabled || !texture) {
                return false;
            }

            Vector2 textureSize = GetTextureRegionSize(texture, uvRect);

            if (textureSize.x == 0 || textureSize.y == 0) {
                return false;
            }

            Vector2 originalSize = size;
            Vector2 parentSizeVector = parentSize.GetValueOrDefault();

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    size.x = size.y * (textureSize.x / textureSize.y);
                    break;
                }
                case AutoSizeMode.StretchY: {
                    size.y = size.x * (textureSize.y / textureSize.x);
                    break;
                }
                case AutoSizeMode.Fit: {
                    if (!parentSize.HasValue) {
                        break;
                    }

                    float aspect = textureSize.x / textureSize.y;
                    size.x = parentSizeVector.y * aspect;
                    size.y = parentSizeVector.y;

                    if (size.x > parentSizeVector.x) {
                        size.x = parentSizeVector.x;
                        size.y = size.x / aspect;
                    } else if (size.y > parentSizeVector.y) {
                        size.y = parentSizeVector.y;
                        size.x = size.y * aspect;
                    }
                    break;
                }
                case AutoSizeMode.Fill: {
                    if (!parentSize.HasValue) {
                        break;
                    }

                    float aspect = textureSize.x / textureSize.y;
                    size.x = parentSizeVector.y * aspect;
                    size.y = parentSizeVector.y;

                    if (size.x < parentSizeVector.x) {
                        size.x = parentSizeVector.x;
                        size.y = size.x / aspect;
                    } else if (size.y < parentSizeVector.y) {
                        size.y = parentSizeVector.y;
                        size.x = size.y * aspect;
                    }
                    break;
                }
            }

            return size != originalSize;
        }

        #endregion // Streamed Textures
    }
}