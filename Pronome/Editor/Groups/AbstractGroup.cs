using System;
using System.Collections.Generic;

namespace Pronome.Mac.Editor.Groups
{
    public abstract class AbstractGroup
    {
        #region Public Fields
        public double Position;

        /// <summary>
        /// The bpm length of a single cycle, not including the LTM
        /// </summary>
        public double Length;

        public Row Row;

        public LinkedList<Cell> Cells;
        #endregion

        public AbstractGroup()
        {
            Cells = new LinkedList<Cell>();
        }
    }
}
