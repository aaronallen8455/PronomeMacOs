using System;
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
        #endregion

        public Repeat()
        {
        }
    }
}
