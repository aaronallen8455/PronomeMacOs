
using System;
using System.Collections;
using System.Collections.Generic;

namespace Pronome.Mac.Editor
{
    public class CellTree : IEnumerable
	{
        /// <summary>
        /// Thichness of cell ticks. Clicking within this range selects the cell.
        /// </summary>

		#region Public Fields
		public CellTreeNode Root;

        public CellTreeNode Max;

        public CellTreeNode Min;

        public int Count;

		#endregion

		public CellTree()
		{
		}


        #region Public Methods

        /// <summary>
        /// Insert the specified cell. Returns false if position is taken.
        /// </summary>
        /// <returns>The insert.</returns>
        /// <param name="cell">Cell.</param>
        public bool Insert (Cell cell)
        {
            return Insert(new CellTreeNode(cell));
        }

		public bool Insert(CellTreeNode node)
		{
			node.IsRed = true;

			CellTreeNode parent = Root;

			if (Root == null) 
            {
                Root = node;
                Min = node;
                Max = node;
            }
			else
			{
                bool isMin = true;
                bool isMax = true;
				while (true)
				{
                    if (parent.Cell.Position > node.Cell.Position)
					{
                        isMax = false;

						if (parent.Left == null)
						{
							parent.Left = node;
							break;
						}

						parent = parent.Left;
					}
					else
					{
                        isMin = false;

                        if (parent.Cell.Position == node.Cell.Position)
                        {
                            // no overlapping
                            return false;
                        }

						if (parent.Right == null)
						{
							parent.Right = node;
							break;
						}

						parent = parent.Right;
					}
				}

                if (isMin)
                {
                    Min = node;
                }
                else if (isMax)
                {
                    Max = node;
                }

				node.Parent = parent;
			}

			// operate on tree to assert red-black properties
			while (node != null)
			{
				//var parent = node.Parent;

				// step 1
				if (parent == null)
				{
					node.IsRed = false;
					break;
				}

				// step 2
				if (!parent.IsRed)
				{
					break;
				}

				var uncle = node.GetUncle();
				var gp = node.GetGrandParent();

				// step 3
				// check if both parent and uncle are red
				if (uncle != null && uncle.IsRed)
				{
					node.Parent.IsRed = uncle.IsRed = false;
					gp.IsRed = true;
					node = gp;
					parent = gp.Parent;
					continue; // recurse with GP because it's now red
				}

				// step 4
                if (gp.Left != null && gp.Left.Right == node)
				{
					// rotate
					RotateLeft(parent);

					// parent is new node, will be target of step 5
					node = parent;
					parent = node.Parent;
				}
				else if (gp.Right != null && gp.Right.Left != null && gp.Right.Left == node)
				{
					// mirror of above
					RotateRight(parent);

					node = parent;
					parent = node.Parent;
				}

				// step 5
				// parent becomes GP
				if (gp.Left != null && gp.Left.Left != null && gp.Left.Left == node)
				{
					RotateRight(gp);

					parent.IsRed = false;
					gp.IsRed = true;
				}
				else
				{
					// mirror of above
					RotateLeft(gp);

					parent.IsRed = false;
					gp.IsRed = true;
				}

				break;
			}

            Count++;

			return true;
		}

		public CellTree Remove(double value)
		{
			CellTreeNode node = Lookup(value, false);

			return Remove(node);
		}

        public CellTree Remove(Cell cell)
        {
            var node = Lookup(cell.Position, false);

            return Remove(node);
        }

		public CellTree Remove(CellTreeNode node)
		{
			CellTreeNode nodeToDeleteAfterwards = null;

			if (node == Min) Min = node.Next();
			else if (node == Max) Max = node.Prev();

			if (node.Left == null && node.Right == null)
			{
				if (Root == node)
				{
					Root = null;
                    Min = null;
                    Max = null;
					return this;
				}

				if (node.IsRed)
				{
					Replace(node, null);
					return this;
				}

				// black node with no children, not root

				// we will delete the node afterwards. it's a 'phantom' leaf.
				nodeToDeleteAfterwards = node;
				// do we need to track the actual node, as it may be changed by a rule?
			}
			else if (node.Left != null && node.Right != null)
			{
				// we find the left-most child of right side and copy it's value to the node being deleted
				// The node that we copied from (left-most) can then by deleted because it
				// has at most 1 non-leaf child.

				var succesor = node.Left;
				while (succesor.Right != null)
				{
					succesor = succesor.Right;
				}

				// copy data from succesor node
                succesor.CopyTo(node);

				//Swap(node, succesor);

				return Remove(succesor);
			}

			if (node.Left != null || node.Right != null)
			{
				// one child is a non-leaf

				var child = node.Left ?? node.Right;


				if (!node.IsRed && child.IsRed)
				{
					// black node, red child
					Replace(node, child);

					child.IsRed = false;
					return this;
				}

				if (node.IsRed && !child.IsRed)
				{
					// red node, black child
					Replace(node, child);
					return this;
				}


				// black node with black child
				Replace(node, child);
				node = child;
			}

			while (true)
			{
				// node is now a "double black" node

				if (node == Root)
				{
					break; // case 1. terminal case
				}

				var sibling = node.GetSibling();

				if (sibling.IsRed)
				{
					// case 2
					// node is black, has black parent, and a red sibling
					// make sibling take the place of the parent.
					if (node.Parent.Left == node)
					{
						RotateLeft(node.Parent);
					}
					else
					{
						RotateRight(node.Parent);
					}

					node.Parent.IsRed = true;
					sibling.IsRed = false;
					sibling = node.GetSibling();
				}

				else if (!node.Parent.IsRed && !sibling.IsRed && (sibling.Left == null || !sibling.Left.IsRed) && (sibling.Right == null || !sibling.Right.IsRed))
				{
					// case 3
					// node is black, black parent, black sibling with black children

					sibling.IsRed = true;

					node = node.Parent;
					// go to start with parent as new node. (we pushed the double black status up to the parent)
					continue;
				}

				else if (node.Parent.IsRed && (sibling.Left == null || !sibling.Left.IsRed) && (sibling.Right == null || !sibling.Right.IsRed))
				{
					// case 4, terminal case
					node.Parent.IsRed = false;
					sibling.IsRed = true;
					break;
				}

				else if (!sibling.IsRed && node.HasSiblingWithInnerRedChild())
				{
					// case 5
					// black node, black parent, black sibling with inner red child
					if (node.Parent.Left == node)
					{
						RotateRight(sibling);
						sibling.Left.IsRed = false;
					}
					else
					{
						RotateLeft(sibling);
						sibling.Right.IsRed = false;
					}

					sibling.IsRed = true;
					sibling = node.GetSibling();
				}

				if (!sibling.IsRed && node.HasSiblingWithOuterRedChild())
				{
					// case 6, terminal case
					// black node, don't care parent's color, black sibling with outer red child and inner child with either color.
					if (node.Parent.Left == node)
					{
						RotateLeft(node.Parent);
						sibling.Right.IsRed = false;
					}
					else
					{
						RotateRight(node.Parent);
						sibling.Left.IsRed = false;
					}

					sibling.IsRed = node.Parent.IsRed;
					node.Parent.IsRed = false;

					// node is no longer a bouble black.
					break;
				}
			}

			if (nodeToDeleteAfterwards != null)
			{
				Replace(nodeToDeleteAfterwards, null);
			}

            Count--;

			return this;
		}

        public bool TryFind(double value, out Cell cell, bool useThickness = true)
        {
            CellTreeNode node = Lookup(value, useThickness);

            cell = node?.Cell;

            return cell != null;
        }

        /// <summary>
        /// Look up the cell at the specified BPM position.
        /// </summary>
        /// <returns>The up.</returns>
        /// <param name="value">Value.</param>
        /// <param name="useThickness">If set to <c>true</c> use thickness.</param>
        public CellTreeNode Lookup(double value, bool useThickness = true, double thickness = DrawingView.CellWidth)
		{
			CellTreeNode node = Root;

            thickness /= DrawingView.ScalingFactor;

			while (node != null)
			{
                if (node.Cell.Position + (useThickness ? thickness : 0) >= value && value >= node.Cell.Position)
				{
					return node;
				}

                if (node.Cell.Position < value) node = node.Right;
                else node = node.Left;
			}

			return null;
		}

        /// <summary>
        /// Finds a node with the given cell index.
        /// </summary>
        /// <returns>The index.</returns>
        /// <param name="index">Index.</param>
        public CellTreeNode LookupIndex(int index)
        {
            CellTreeNode node = Root;

            while (node != null)
            {
                if (node.Cell.Index == index) return node;

                if (node.Cell.Index < index) node = node.Right;
                else node = node.Left;
            }

            return null;
        }

        /// <summary>
        /// Find the first cell above or equal to the given BPM position.
        /// </summary>
        /// <returns>The above or equal to.</returns>
        /// <param name="value">Value.</param>
        public CellTreeNode FindAboveOrEqualTo (double value, bool useThickness = false)
        {
            CellTreeNode node = Root;
            CellTreeNode result = null;

            if (node == null) return null;

            double thickness = useThickness ? DrawingView.CellWidth / DrawingView.ScalingFactor : 0;

            while (node != null)
            {
				if (node.Cell.Position + thickness >= value)
				{
					result = node;

                    if (node.Cell.Position + thickness == value) break;
				}

                if (node.Cell.Position + thickness < value)
                {
                    node = node.Right;
                }
                else
                {
                    node = node.Left;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the node at bpm position below or equal to value.
        /// </summary>
        /// <returns>The below or equal to.</returns>
        /// <param name="value">Value.</param>
        public CellTreeNode FindBelowOrEqualTo (double value)
        {
            CellTreeNode node = Root;
            CellTreeNode result = null;

            if (node == null) return null;

            while (node != null)
            {
                if (node.Cell.Position <= value)
                {
                    result = node;

                    if (node.Cell.Position == value) break;
                }

                if (node.Cell.Position > value)
                {
                    node = node.Left;
                }
                else
                {
                    node = node.Right;
                }
            }

            return result;
        }

        /// <summary>
        /// Find all cells inside the given range.
        /// </summary>
        /// <returns>The range.</returns>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        public CellTreeNode[] GetRange(double start, double end)
        {
            List<CellTreeNode> cells = new List<CellTreeNode>();

            CellTreeNode cell = FindAboveOrEqualTo(start);

            while (cell != null && cell.Cell.Position <= end)
            {
                cells.Add(cell);

                cell = cell.Next();
            }

            return cells.ToArray();
        }

        public IEnumerator GetEnumerator()
        {
            CellTreeNode node = Min;

            if (node == null) yield break;

            while (node != null)
            {
                yield return node.Cell;

                node = node.Next();
            }

            yield break;
        }

        //public CellTreeNode GetMin()
        //{
        //    CellTreeNode node = Root;
		//
        //    if (node == null) return null;
		//
        //    while (node.Left != null)
        //    {
        //        node = node.Left;
        //    }
		//
        //    return node;
        //}

        //public CellTreeNode GetMax()
        //{
        //    CellTreeNode node = Root;
		//
        //    if (node == null) return null;
		//
        //    while (node.Right != null)
        //    {
        //        node = node.Right;
        //    }
		//
        //    return node;
        //}

        /// <summary>
        /// Convert to an array of cells
        /// </summary>
        /// <returns>The array.</returns>
        public Cell[] ToArray()
        {
            Cell[] array = new Cell[Count];

            int i = 0;
            foreach (Cell cell in this)
            {
                array[i++] = cell;
            }

            return array;
        }

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public void Clear()
        {
            Root = null;
            Min = null;
            Max = null;
            Count = 0;
        }
		#endregion

		#region Protected Methods
		/// <summary>
		/// Rotate such that if node was the root, node's right child will now be root
		/// </summary>
		/// <param name="node">Node.</param>
		protected void RotateLeft(CellTreeNode node)
		{
			var rightLeft = node.Right.Left;

			if (Root != node)
			{
				if (node.Parent.Left == node)
				{
					node.Parent.Left = node.Right;
				}
				else
				{
					node.Parent.Right = node.Right;
				}
			}
			else
			{
				Root = node.Right;
			}

			node.Right.Parent = node.Parent;
			node.Right.Left = node;
			node.Parent = node.Right;
			node.Right = rightLeft;

			if (rightLeft != null)
			{
				rightLeft.Parent = node;
			}
		}

		/// <summary>
		/// Rotate such that if node was root, node's left child will now be root
		/// </summary>
		/// <param name="node">Node.</param>
		protected void RotateRight(CellTreeNode node)
		{
			var leftRight = node.Left.Right;

			if (Root != node)
			{
				if (node.Parent.Left == node)
				{
					node.Parent.Left = node.Left;
				}
				else
				{
					node.Parent.Right = node.Left;
				}
			}
			else
			{
				Root = node.Left;
			}

			node.Left.Parent = node.Parent;
			node.Left.Right = node;
			node.Parent = node.Left;
			node.Left = leftRight;

			if (leftRight != null)
			{
				leftRight.Parent = node;
			}
		}

		/// <summary>
		/// Replace the specified node with the given replacement and remove original node from the tree.
		/// </summary>
		/// <returns>The replace.</returns>
		/// <param name="node">Node.</param>
		/// <param name="replacement">Replacement.</param>
		protected void Replace(CellTreeNode node, CellTreeNode replacement)
		{
			if (node.Parent == null)
			{
				Root = replacement;
				if (replacement != null)
					replacement.Parent = null;
			}
			else
			{
				if (node.Parent.Left == node)
				{
					node.Parent.Left = replacement;
				}
				else
				{
					node.Parent.Right = replacement;
				}

				if (replacement != null)
					replacement.Parent = node.Parent;
			}
		}
		#endregion
	}
}
