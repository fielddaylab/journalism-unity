using System;
using BeauUtil;

namespace Journalism {
    static public class Assets {

        static private TextStyles s_TextStyles;
        static private TextChars s_TextChars;
        static private LevelDef[] s_AllLevels;

        static private LevelDef s_CurrentLevel;

        static internal void DeclareStyles(TextStyles styles) {
            s_TextStyles = styles;
        }

        static internal void DeclareChars(TextChars chars) {
            s_TextChars = chars;
        }

        static internal void DeclareLevelList(LevelDef[] levels) {
            s_AllLevels = levels;
            for(int i = 0; i < levels.Length; i++) {
                levels[i].LevelIndex = i;
            }
        }

        static internal void DeclareLevel(LevelDef level) {
            s_CurrentLevel = level;
        }

        #region Styles

        static public TextStyles.StyleData DefaultStyle {
            get { return s_TextStyles.Default(); }
        }

        static public TextStyles.StyleData DefaultCharStyle {
            get { return s_TextStyles.DefaultForChar(); }
        }

        static public TextStyles.StyleData Style(StringHash32 id) {
            return s_TextStyles.Style(id);
        }

        static public TextChars.CharData Char(StringHash32 id) {
            return s_TextChars.Char(id);
        }

        static public MapLocationDef.MapLocation Location(StringHash32 id) {
            return MapLocations.GetMapLocation(id);
        }

        #endregion // Styles

        #region Level List

        /// <summary>
        /// List of all levels.
        /// </summary>
        static public ListSlice<LevelDef> AllLevels {
            get { return s_AllLevels; }
        }

        /// <summary>
        /// Returns the index of the given level.
        /// </summary>
        static public int LevelIndex(LevelDef level) {
            return Array.IndexOf(s_AllLevels, level);
        }

        /// <summary>
        /// Returns the level with the given index.
        /// </summary>
        static public LevelDef Level(int index) {
            return s_AllLevels[index];
        }

        /// <summary>
        /// Number of levels total.
        /// </summary>
        static public int LevelCount {
            get { return s_AllLevels.Length; }
        }

        #endregion // Level List

        /// <summary>
        /// Currently loaded level.
        /// </summary>
        static public LevelDef CurrentLevel {
            get { return s_CurrentLevel; }
        }

        /// <summary>
        /// Returns the index of the current level.
        /// </summary>
        static public int LevelIndex() {
            return Array.IndexOf(s_AllLevels, s_CurrentLevel);
        }

        /// <summary>
        /// Layout of the story for the current 
        /// </summary>
        static public StoryScraps StoryLayout {
            get { return s_CurrentLevel.StoryScraps; }
        }

        /// <summary>
        /// Scrap data for the currently loaded level.
        /// </summary>
        static public StoryScrapData Scrap(StringHash32 id) {
            return s_CurrentLevel.StoryScraps.Scrap(id);
        }
    }
}