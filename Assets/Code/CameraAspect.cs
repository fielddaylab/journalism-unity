using UnityEngine;
using BeauUtil;
using System;

namespace Journalism {
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, ExecuteAlways]
    public class CameraAspect : MonoBehaviour {
        public int DesiredWidth = 1024;
        public int DesiredHeight = 660;
        public Color LetterboxColor = Color.black;

        [NonSerialized] private Camera m_Camera;

        private void Awake() {

        }

        private void OnDestroy() {
            
        }

        private void OnPreRender() {
            if (DesiredWidth <= 0 || DesiredHeight <= 0 || !isActiveAndEnabled) {
                return;
            }

            Rect cameraRect = GetCameraRenderRegion((float) DesiredWidth / DesiredHeight);
            this.CacheComponent(ref m_Camera).rect = cameraRect;
        }

        private void OnPostRender() {
            RenderLetterboxing(m_Camera.rect, LetterboxColor);
        }

        static private Rect GetCameraRenderRegion(float aspect) {
            float scrW = Screen.width, scrH = Screen.height;
            float w = scrH * aspect, h = scrH;

            if (w > scrW) {
                w = scrW;
                h = w / aspect;
            } else if (h > scrH) {
                h = scrH;
                w = h * aspect;
            }

            float diffX = 1 - w / scrW,
                diffY = 1 - h / scrH;

            Rect r = default;
            r.x = diffX / 2;
            r.y = diffY / 2;
            r.width = 1 - diffX;
            r.height = 1 - diffY;

            return r;
        }
    
        static private void RenderLetterboxing(Rect inner, Color color) {
            float left = inner.x, bottom = inner.y;
            if (left != 0 || bottom != 0) {
                float scrW = Screen.width, scrH = Screen.height;
                // woo boy we're getting into some low-level graphics
                GL.PushMatrix();
                GL.LoadOrtho();
                Rect r = default;
                if (left != 0) {
                    r.x = 0;
                    r.y = 0;
                    r.width = left * scrW;
                    r.height = scrH;
                    GL.Viewport(r);
                    GL.Clear(false, true, color);
                    r.x = scrW - r.width;
                    GL.Viewport(r);
                    GL.Clear(false, true, color);
                } else {
                    r.x = 0;
                    r.y = 0;
                    r.width = scrW;
                    r.height = bottom * scrH;
                    GL.Viewport(r);
                    GL.Clear(false, true, color);
                    r.y = scrH - r.height;
                    GL.Viewport(r);
                    GL.Clear(false, true, color);
                }
                GL.PopMatrix();
            }
        }
    }
}