

namespace Pronome.Mac.Editor.Groups
{
    public class Repeat : AbstractGroup
    {
        #region Public fields
        /// <summary>
        /// Number of times to repeat.
        /// </summary>
        public int Times;

        public string LastTermModifier;

        /// <summary>
        /// Aggregated mult factors, used to effect the LTM
        /// </summary>
        public string MultFactor = "1";

        /// <summary>
        /// If this repeat group contains a break, this is the cell that is directly before the break occurs.
        /// </summary>
        public Cell BreakCell;

        /// <summary>
        /// The duration expanded to include rep times and break point. Does not include the LTM
        /// </summary>
        public double FullDuration;
        #endregion

        #region Protected Fields
        protected string MultedLtm;
        #endregion

        public Repeat()
        {
        }

        #region Public Methods
        /// <summary>
        /// Gets the ltm with mult factor.
        /// </summary>
        /// <returns>The ltm with mult factor.</returns>
        public string GetLtmWithMultFactor(bool ignoreSettings = false)
        {
            if (string.IsNullOrEmpty(LastTermModifier) || (!ignoreSettings && !UserSettings.GetSettings().DrawMultToScale))
            {
                return LastTermModifier;
            }

            if (string.IsNullOrEmpty(MultedLtm))
            {
                MultedLtm = BeatCell.MultiplyTerms(LastTermModifier, MultFactor);
            }

            return MultedLtm;
        }

        /// <summary>
        /// Resets the multed ltm, so that it will be recalculated against a new value.
        /// </summary>
        public void ResetMultedLtm()
        {
            MultedLtm = string.Empty;
        }

        public string GetValueDividedByMultFactor(string value, bool ignoreSetting = false)
        {
            if (string.IsNullOrEmpty(value) || (!ignoreSetting && !UserSettings.GetSettings().DrawMultToScale)) return value;

            return BeatCell.DivideTerms(value, MultFactor);
        }
        #endregion
    }
}
