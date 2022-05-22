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

            stats.CanPublish = stats.ScrapCount > 0;

            if (attributeCount > 0 && targetCount > 0) {
                //0.33 - 0.4 = 0.07
                //0.33 - 0.3 = 0.03
                //0.33 - 0.3 = 0.03 = 0.13: good score
                //0.33 - 0.6 = 0.27 
                //0.33 - 0.2 = 0.13
                //0.33 - 0.2 = 0.13 = 0.54: bad score
                float factRatioDiff = (float) stats.FactCount / attributeCount - (float) config.FactWeight / targetCount;
                float colorRatioDiff = (float) stats.ColorCount / attributeCount - (float) config.ColorWeight / targetCount;
                float usefulRatioDiff = (float) stats.UsefulCount / attributeCount - (float) config.UsefulWeight / targetCount;

                float totalDiff = Math.Abs(factRatioDiff) + Math.Abs(colorRatioDiff) + Math.Abs(usefulRatioDiff);
                stats.Alignment = 0.8f - totalDiff;
                //~0.3 bad
                //~0.6 good
            }

            int missing = config.Slots.Length - stats.ScrapCount;
            if (stats.ScrapCount < config.Slots.Length) {
                stats.Alignment *= 1 - (missing * 0.15f);
            }

            if (stats.TotalQuality >= 3) {
                stats.Score = StoryScore.Good;
            } else if (stats.TotalQuality >= 1) {
                stats.Score = StoryScore.Medium;
            } else {
                stats.Score = StoryScore.Bad;
            }

            return stats;
        }
    }

    public enum StoryScore {
        Bad,
        Medium,
        Good
    }
}