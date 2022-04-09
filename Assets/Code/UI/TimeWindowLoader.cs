using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;

namespace Journalism.UI {
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class TimeWindowLoader : MonoBehaviour {
        public ClockIncrements Clocks;
        public TMP_Text Hour0;
        public TMP_Text Hour1;
        public TMP_Text Min0;
        public TMP_Text Min1;

        private void Awake() {
            GetComponent<HeaderWindow>().LoadData = () => {
                int timeRemaining = (int) Player.Data.TimeRemaining;
                ClockIncrements.Populate(Clocks, timeRemaining);

                int hours = timeRemaining / Stats.TimeUnitsPerHour;
                int minutes = (timeRemaining % Stats.TimeUnitsPerHour) * Stats.MinutesPerTimeUnit;

                Hour0.SetText((hours / 10).ToStringLookup());
                Hour1.SetText((hours % 10).ToStringLookup());

                Min0.SetText((minutes / 10).ToStringLookup());
                Min1.SetText((minutes % 10).ToStringLookup());
            };
        }
    }
}