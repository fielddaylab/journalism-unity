#if !UNITY_EDITOR && UNITY_WEBGL
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using EasyAssetStreaming;
using Journalism.UI;
using Journalism;

namespace Aqua {
    public class FastBootController : MonoBehaviour {
        private enum ReadyPhase {
            Loading,
            AudioClick,
            Ready
        }

        [Header("Ready")]
        public CanvasGroup ClickAnywhere;
        public StreamingUGUITexture HeadlineTex;
        public CanvasGroup HeadlineGroup;
        public TMP_Text PromptText;

        [Header("Run")]
        public AudioSource BootAudio;
        public TitleWindow TitleWindow;
        

        [NonSerialized] private ReadyPhase m_ReadyPhase = 0;

        private void Start() {
            Routine.Start(OnStart());
        }

        private IEnumerator OnStart() {
            m_ReadyPhase = ReadyPhase.Loading;
            ClickAnywhere.gameObject.SetActive(true);

            HeadlineGroup.alpha = 0;
            PromptText.alpha = 0;

            while (HeadlineTex.IsLoading()) {
                yield return null;
            }

            yield return 0.5f;

            yield return HeadlineGroup.FadeTo(1, 1f);

            m_ReadyPhase = ReadyPhase.AudioClick;
            Routine.Start(this, SwapToPrompt());

            while (m_ReadyPhase < ReadyPhase.Ready) {
                yield return null;
            }

            if (BootAudio != null) {
                yield return BootAudio.WaitToComplete();
            }

            yield return 0.5f;

            yield return Routine.Combine(
                PromptText.FadeTo(0, 0.2f)
            );
        }

        private void Update() {
            if (Input.GetMouseButton(0)) {
                Routine.Start(OnMouseDown());
            }
        }
        
        private IEnumerator SwapToPrompt() {
            PromptText.gameObject.SetActive(true);
            PromptText.alpha = 0;
            yield return Routine.Combine(
                PromptText.FadeTo(1, 0.2f)
            );
        }

        private IEnumerator OnMouseDown() {
            if (m_ReadyPhase != ReadyPhase.AudioClick) {
                yield break; 
            }

            if (BootAudio != null) {
                BootAudio.Play();
            }

            m_ReadyPhase = ReadyPhase.Ready;

            TitleWindow.Show();

            yield return Routine.Combine(
                ClickAnywhere.FadeTo(0, 0.6f)
            );

            ClickAnywhere.gameObject.SetActive(false);
        }
    }
}