using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Journalism
{
    public class StoryScrapToggle : Toggle, IPointerDownHandler
    {
        /// <summary>
        /// React to mouse down
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData) {
            base.OnPointerDown(eventData);

            // update value
            if (eventData.button != PointerEventData.InputButton.Left) {
                return;
            }

            isOn = !isOn;
        }

        /// <summary>
        /// Disable OnPointerClick responses
        /// </summary>
        public override void OnPointerClick(PointerEventData eventData) {

        }
    }
}