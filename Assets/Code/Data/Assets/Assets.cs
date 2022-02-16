using System;
using BeauUtil;

namespace Journalism {
    static public class Assets {

        static private TextStyles s_TextStyles;
        static private LevelDef[] s_AllLevels;

        static private LevelDef s_CurrentLevel;

        static internal void DeclareStyles(TextStyles styles) {
            s_TextStyles = styles;
        }

        static internal void DeclareLevelList(LevelDef[] levels) {
            s_AllLevels = levels;;
        }

        static internal void DeclareLevel(LevelDef level) {
            s_CurrentLevel = level;
        }

        #region Styles

        static public TextStyles.StyleData DefaultStyle {
            get { return s_TextStyles.Default(); }
        }

        static public TextStyles.StyleData Style(StringHash32 id) {
            return s_TextStyles.Style(id);
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
        /// Scrap data for the currently loaded level.
        /// </summary>
        static public StoryScrapData Scrap(StringHash32 id) {
            return s_CurrentLevel.StoryScraps.Scrap(id);
        }
    }
}