using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

namespace Journalism.UI {
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class TimeWindowLoader : MonoBehaviour {
        public Image[] Clocks;
        public TMP_Text TimeLabel;

        private void Awake() {
            GetComponent<HeaderWindow>().LoadData = () => {
                int timeRemaining = (int) Player.Data.TimeRemaining;
                int hours = timeRemaining / Stats.TimeUnitsPerHour;
                int hourChunks = timeRemaining % Stats.TimeUnitsPerHour;

                int clocksUsed = (int) hours;
                if (hourChunks > 0) {
                    clocksUsed++;
                }

                Image clock;
                int diff;
                for(int i = 0; i < clocksUsed; i++) {
                    clock = Clocks[i];
                    diff = Math.Min(timeRemaining - (i * Stats.TimeUnitsPerHour), Stats.TimeUnitsPerHour);
                    clock.fillAmount = (float) diff / Stats.TimeUnitsPerHour;
                    clock.gameObject.SetActive(true);
                }

                for(int i = clocksUsed; i < Clocks.Length; i++) {
                    Clocks[i].gameObject.SetActive(false);
                }

                TimeLabel.SetText(GameText.FormatTime((uint) timeRemaining));
            };
        }
    }
}