using UnityEngine;
using TMPro;

namespace Journalism.UI {
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class StatWindowLoader : MonoBehaviour {
        public StatLine[] StatLines;

        private void Awake() {
            GetComponent<HeaderWindow>().LoadData = () => {
                foreach(var stat in StatLines) {
                    ushort statVal = (ushort) Player.Stat(stat.Stat);
                    stat.Name.SetText(Stats.Name(stat.Stat));
                    stat.Rank.SetText(string.Format("{0} ({1})", Stats.RankLabel(stat.Stat, statVal), statVal));
                    stat.Rank.color = Stats.RankColor(statVal);
                }
            };
        }
    }
}