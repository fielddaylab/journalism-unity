using UnityEngine;
using TMPro;
using UnityEngine.UI.Extensions;
using BeauUtil;
using BeauRoutine;

namespace Journalism.UI {
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class StatWindowLoader : MonoBehaviour {
        public StatLine[] StatLines;
        public StatBead[] StatBeads;
        public StatId[] StatOrder;
        public float MinRadius;
        public float MaxRadius;
        public UIPolygon DistributionFill;
        public UIPolygonGradient DistributionLine;
        public float AngleOffset = -30;

        private void Awake() {
            GetComponent<HeaderWindow>().LoadData = () => {
                for(int i = 0; i < StatOrder.Length; i++) {
                    StatLine line = StatLines[i];
                    StatBead bead = StatBeads[i];
                    StatId id = StatOrder[i];

                    ushort val = (ushort) Player.Stat(id);
                    Color32 color = Stats.RankColor(val);

                    line.Name.SetText(Stats.Name(id));
                    line.Rank.SetText(Stats.RankLabel(id, val));
                    line.Rank.color = color;

                    float angle = Mathf.Deg2Rad * (AngleOffset + 360f / StatOrder.Length * i);
                    float dist = Mathf.Lerp(MinRadius, MaxRadius, (float) val / Stats.MaxValue);
                    float normalizedDist = (dist + DistributionLine.thickness) / MaxRadius;

                    bead.Color.color = color;
                    bead.Value.SetText(((int) val).ToStringLookup());
                    bead.RectTransform().anchoredPosition = new Vector2(dist * Mathf.Cos(angle), dist * Mathf.Sin(angle));

                    DistributionFill.VerticesDistances[i] = normalizedDist;
                    DistributionLine.VerticesDistances[i] = normalizedDist;
                    DistributionLine.VerticesColors[i] = color;
                }
            };

            GetComponent<HeaderWindow>().BuildLayout = () => {
                DistributionFill.SetAllDirty();
                DistributionLine.SetAllDirty();
            };
        }
    }
}