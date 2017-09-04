using System.Collections.Generic;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor
{
    public class CellTreeNode
    {
        #region Public fields
        public CellTreeNode Left;

        public CellTreeNode Right;

        public CellTreeNode Parent;

        public Cell Cell;

        public bool IsRed;

        public Stack<Repeat> RepeatGroups;
        public Stack<Multiply> MultGroups;
        #endregion

        #region Constructor
        public CellTreeNode(Cell cell)
        {
            Cell = cell;
        }
        #endregion

        #region Public Methods
        public CellTreeNode GetGrandParent()
        {
            return Parent?.Parent;
        }

        public CellTreeNode GetUncle()
        {
            var gp = GetGrandParent();

            if (gp != null)
            {
                if (gp.Left == Parent) return gp.Right;
                return gp.Left;
            }

            return null;
        }

        public CellTreeNode GetSibling()
        {
            if (Parent != null)
            {
                return Parent.Left == this ? Parent.Right : Parent.Left;
            }

            return null;
        }

        public bool IsLeftChild()
        {
            if (Parent != null)
            {
                return Parent.Left == this;
            }

            return true;
        }

        public bool HasSiblingWithInnerRedChild()
        {
            var sibling = GetSibling();

            if (sibling != null)
            {
                if (IsLeftChild())
                {
                    return sibling.Left != null && sibling.Left.IsRed && (sibling.Right == null || !sibling.Right.IsRed);
                }
                return sibling.Right != null && sibling.Right.IsRed && (sibling.Left == null || !sibling.Left.IsRed);
            }

            return false;
        }

        public bool HasSiblingWithOuterRedChild()
        {
            var sibling = GetSibling();

            if (sibling != null)
            {
                if (IsLeftChild())
                {
                    return sibling.Right != null && sibling.Right.IsRed;
                }
                return sibling.Left != null && sibling.Left.IsRed;
            }

            return false;
        }

        /// <summary>
        /// Get the next node in order
        /// </summary>
        /// <returns>The next.</returns>
        public CellTreeNode Next()
        {
            CellTreeNode node;

            if (Right != null)
            {
                node = Right;

                while (node.Left != null)
                {
                    node = node.Left;
                }
            }
            else
            {
				node = Parent;

                HashSet<CellTreeNode> touched = new HashSet<CellTreeNode>();

                while (node != null && (node.Right == null || node.Right == this || touched.Contains(node.Right)))
                {
                    touched.Add(node);

                    node = node.Parent;
                }
            }

            return node;
        }

        public CellTreeNode Prev()
        {
            CellTreeNode node;

            if (Left != null) 
            {
                node = Left;

                while (node.Right != null)
                {
                    node = node.Right;
                }
            }
            else
            {
				node = Parent;

                HashSet<CellTreeNode> touched = new HashSet<CellTreeNode>();

                while (node != null && (node.Left == null || node.Left == this || touched.Contains(node.Left)))
                {
                    touched.Add(node);
                    node = node.Parent;
                }
            }

            return node;
        }

        public void CopyTo(CellTreeNode node)
        {
            node.Cell = Cell;
        }
        #endregion
    }
}
