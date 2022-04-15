using System;

namespace Journalism {
    public struct StoryStats {
        public int QualityAdd;
        public int QualitySubtract;
        public int TotalQuality;
        public StoryScore Score;

        public int ScrapCount;
        public bool CanPublish;

        public int FactCount;
        public int ColorCount;
        public int UsefulCount;
        public float Alignment;

        static public StoryStats FromPlayerData(PlayerData data, StoryConfig config) {
            StoryStats stats = default;

            int attributeCount = 0;
            int targetCount = config.FactWeight + config.ColorWeight + config.UsefulWeight;
            foreach(var scrapId in data.AllocatedScraps) {
                if (!scrapId.IsEmpty) {
                    stats.ScrapCount++;
                    StoryScrapData scrapData = Assets.Scrap(scrapId);
                    switch(scrapData.Quality) {
                        case StoryScrapQuality.Bad: {
                            stats.QualitySubtract++;
                            stats.TotalQuality--;
                            break;
                        }
                        case StoryScrapQuality.Great: {
                            stats.QualityAdd++;
                            stats.TotalQuality++;
                            break;
                        }
                    }

                    if ((scrapData.Attributes & StoryScrapAttribute.Facts) != 0) {
                        stats.FactCount++;
                        attributeCount++;
                    }
                    if ((scrapData.Attributes & StoryScrapAttribute.Color) != 0) {
                        stats.ColorCount++;
                        attributeCount++;
                    }
                    if ((scrapData.Attributes & StoryScrapAttribute.Useful) != 0) {
                        stats.UsefulCount++;
                        attributeCount++;
                    }
                }
            }

            if (attributeCount > 0 && targetCount > 0) {
                float factRatioDiff = (float) stats.FactCount / attributeCount - (float) config.FactWeight / targetCount;
                float colorRatioDiff = (float) stats.ColorCount / attributeCount - (float) config.ColorWeight / targetCount;
                float usefulRatioDiff = (float) stats.UsefulCount / attributeCount - (float) config.UsefulWeight / targetCount;

                float totalDiff = Math.Abs(factRatioDiff) + Math.Abs(colorRatioDiff) + Math.Abs(usefulRatioDiff);
                stats.Alignment = 0.8f - totalDiff;
            }

            if (stats.TotalQuality >= 3) {
                stats.Score = StoryScore.Good;
            } else if (stats.TotalQuality >= 1) {
                stats.Score = StoryScore.Medium;
            } else {
                stats.Score = StoryScore.Bad;
            }

            stats.CanPublish = stats.ScrapCount == config.Slots.Length;

            return stats;
        }
    }

    public enum StoryScore {
        Bad,
        Medium,
        Good
    }
}