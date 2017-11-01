

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
        public string GetLtmWithMultFactor()
        {
            if (!UserSettings.GetSettings().DrawMultToScale) return LastTermModifier;

            if (string.IsNullOrEmpty(MultedLtm))
            {
                MultedLtm = BeatCell.MultiplyTerms(LastTermModifier, MultFactor);
            }

            return MultedLtm;
        }

        public string GetValueDividedByMultFactor(string value)
        {
            if (!UserSettings.GetSettings().DrawMultToScale) return value;

            return BeatCell.DivideTerms(value, MultFactor);
        }
        #endregion
    }
}
