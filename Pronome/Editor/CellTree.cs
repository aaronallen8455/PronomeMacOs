using System;
namespace Pronome.Mac.Editor
{
    public class CellTree
    {
        const double Thickness = 1;

        #region Public Fields
        public CellTreeNode Root;
        #endregion

        public CellTree()
        {
        }

        #region Public Methods
        public CellTree Insert(CellTreeNode node)
        {
            node.IsRed = true;

            CellTreeNode parent = Root;

            if (Root == null) Root = node;
            else
            {
                while (true)
                {
                    if (parent.Value > node.Value)
                    {
                        if (parent.Left == null)
                        {
                            parent.Left = node;
                            break;
                        }

                        parent = parent.Left;
                    }
                    else
                    {
                        if (parent.Right == null)
                        {
                            parent.Right = node;
                            break;
                        }

                        parent = parent.Right;
                    }
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
                if (uncle.IsRed)
                {
                    node.Parent.IsRed = uncle.IsRed = false;
                    gp.IsRed = true;
                    node = gp;
                    continue; // recurse with GP because it's now red
                }

                // step 4
                if (gp.Left.Right == node)
                {
                    // rotate
                    RotateLeft(parent);

                    // parent is new node, will be target of step 5
                    node = parent;
                    parent = node.Parent;
                }
                else if (gp.Right.Left == node)
                {
                    // mirror of above
                    RotateRight(parent);

                    node = parent;
                    parent = node.Parent;
                }

                // step 5
                // parent becomes GP
                if (gp.Left.Left == node)
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

            return this;
        }

        public CellTree Remove(CellTreeNode node)
        {
            //while (node != null)
            //{
            // if no children, we can simply delete

            CellTreeNode nodeToDeleteAfterwards = null;

            if (node.Left == null && node.Right == null)
            {
                if (Root == node)
                {
                    Root = null;
                    return this;
                }
                else if (node.IsRed)
                {
                    Replace(node, null);
                    return this;
                }
                else
                {
                    // black node with no children, not root
                    //Replace(node, null);
                    // do stuff...
                    //node = node.Parent;

                    // we will delete the node afterwards. it's a 'phantom' leaf.
                    nodeToDeleteAfterwards = node;
                    // do we need to track the actual node, as it may be changed by a rule?
                }

                //break;
            }
            else if (node.Left != null && node.Right != null)
            {
                // we find the left-most child of right side and copy it's value to the node being deleted
                // The node that we copied from (left-most) can then by deleted because it
                // has at most 1 non-leaf child.

                var succesor = node.Right;
                while (succesor.Left != null)
                {
                    succesor = succesor.Left;
                }

                node.Value = succesor.Value;
                node.Cell = succesor.Cell;

                node = succesor;
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
                Replace(nodeToDeleteAfterwards,null);
            }

            return this;
        }

        public CellTreeNode LookUp(double value)
        {
            CellTreeNode node = Root;

            while (node != null)
            {
                if (node.Value + Thickness >= value && value >= node.Value)
                {
                    return node;
                }

                if (node.Value > value) node = node.Left;
                else node = node.Right;
            }

            return null;
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

			if (node.Parent.Left == node)
            {
				node.Parent.Left = node.Right;
            }
            else
            {
                node.Parent.Right = node.Right;
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
        protected void RotateRight (CellTreeNode node)
        {
            var leftRight = node.Left.Right;

			if (node.Parent.Left == node)
			{
                node.Parent.Left = node.Left;
			}
			else
			{
                node.Parent.Right = node.Left;
			}

            node.Left.Parent = node.Parent;
            node.Left.Right = node;
            node.Parent = node.Left;
            node.Right = leftRight;

            if (leftRight != null)
			{
                leftRight.Parent = node;
			}
        }

        protected void Replace (CellTreeNode node, CellTreeNode replacement)
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

    public class CellTreeNode
    {
        #region Public fields
        public BeatCell Cell;

        public CellTreeNode Left;

        public CellTreeNode Right;

        public CellTreeNode Parent;

        public bool IsRed;

        /// <summary>
        /// Positional value
        /// </summary>
        public double Value;
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
                    return sibling.Right != null && sibling.Right.IsRed && sibling.Left == null;
                }
                return sibling.Left != null && sibling.Left.IsRed && sibling.Right == null;
            }

            return false;
        }
        #endregion
    }
}
