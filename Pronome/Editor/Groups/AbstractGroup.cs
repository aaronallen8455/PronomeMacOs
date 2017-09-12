using System;
using System.Collections.Generic;

namespace Pronome.Mac.Editor.Groups
{
    public abstract class AbstractGroup
    {
        #region Public Fields
        public double Position;

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
