using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;

namespace Journalism.UI {
    public sealed class ClockIncrements : MonoBehaviour {
        public Image[] Clocks;

        static public void Populate(ClockIncrements increments, int timeRemaining) {
            int hours = timeRemaining / Stats.TimeUnitsPerHour;
            int hourChunks = timeRemaining % Stats.TimeUnitsPerHour;
            int minutes = hourChunks * Stats.MinutesPerTimeUnit;

            int clocksUsed = (int) hours;
            if (hourChunks > 0) {
                clocksUsed++;
            }

            Image clock;
            int diff;
            for(int i = 0; i < clocksUsed; i++) {
                clock = increments.Clocks[i];
                diff = Math.Min(timeRemaining - (i * Stats.TimeUnitsPerHour), Stats.TimeUnitsPerHour);
                clock.fillAmount = (float) diff / Stats.TimeUnitsPerHour;
                clock.gameObject.SetActive(true);
            }

            for(int i = clocksUsed; i < increments.Clocks.Length; i++) {
                increments.Clocks[i].gameObject.SetActive(false);
            }
        }
    }
}