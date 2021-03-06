﻿using System.Linq;
using System.Collections.Generic;
using AppKit;

namespace Pronome.Mac.Editor
{
	public interface IEditorAction
	{
		void Redo();
		void Undo();

		/// <summary>
		/// String describing the action
		/// </summary>
		string HeaderText { get; }

        bool CanPerform();
	}

	public abstract class AbstractAction
	{
		protected Row Row;

		/// <summary>
		/// Redraw the rows which reference the row being modified
		/// </summary>
        public void RedrawReferencers()
		{
			if (Row.ReferenceMap.ContainsKey(Row.Index))
			{
				if (!Row.BeatCodeIsCurrent)
				{
					Row.UpdateBeatCode();
				}
				foreach (int rowIndex in Row.ReferenceMap[Row.Index])
				{
                    if (rowIndex == Row.Index) continue;

                    Row r = EditorViewController.Instance.DView.Rows[rowIndex];

					// deselect if in row
                    if (EditorViewController.Instance.DView.SelectedCells.Root?.Cell.Row == r)
					{
                        EditorViewController.Instance.DView.DeselectCells();
					}

					r.Redraw();

                    EditorViewController.Instance.DView.QueueRowToDraw(r);
				}
			}
		}
	}

	public abstract class AbstractBeatCodeAction : AbstractAction, IEditorAction
	{
		/// <summary>
		/// The row's beat code before transformation
		/// </summary>
		protected string BeforeBeatCode;

		/// <summary>
		/// The row's beat code after transformation
		/// </summary>
		protected string AfterBeatCode;

		/// <summary>
		/// The row's offset before transformation, in beat code
		/// </summary>
		protected string BeforeOffset;

		/// <summary>
		/// The row's offset after transformation, in beat code
		/// </summary>
		protected string AfterOffset;

		/// <summary>
		/// Used to display action name in undo menu
		/// </summary>
		public virtual string HeaderText { get; set; }

        /// <summary>
        /// True if this action changes the width of the view. Will need to draw the expanded portion if needed.
        /// </summary>
        /// <value><c>true</c> if changes view width; otherwise, <c>false</c>.</value>
        public bool ChangesViewWidth { get; set; }

		/// <summary>
		/// Code to execute before generating the AfterBeatCode string. Should also dispose of unneeded resources.
		/// </summary>
		abstract protected void Transformation();

		protected int RightIndexBoundOfTransform;

		public AbstractBeatCodeAction(Row row, string headerText)
		{
			Row = row;

			if (!row.BeatCodeIsCurrent)
			{
				row.UpdateBeatCode();
			}
			BeforeBeatCode = row.BeatCode;
			BeforeOffset = row.OffsetValue;
			HeaderText = headerText;

            // Find the right index
			CellTree selectedCells = EditorViewController.Instance.DView.SelectedCells;

			// get current selection range if it's in this row
			//int selectionStart = -1;
			//int rowLengthBefore = Row.Cells.Count;

			// get selection indexes if there is a selection in the action's row
			if (selectedCells.Root != null && selectedCells.Root.Cell.Row == Row)
			{
				// find index of first selected cell
				//selectionStart = 0;
				//foreach (Cell c in Row.Cells)
				//{
				//	if (c.IsSelected) break;
				//	selectionStart++;
				//}

                RightIndexBoundOfTransform = selectedCells.Min.Cell.Index + selectedCells.Count - 1;
			}
		}

		public virtual void Redo()
		{
            CellTree selectedCells = EditorViewController.Instance.DView.SelectedCells;

			// get current selection range if it's in this row
			int selectionStart = -1;
			int selectionEnd = -1;
			int rowLengthBefore = Row.Cells.Count;

            // get selection indexes if there is a selection in the action's row
            if (selectedCells.Root != null && selectedCells.Root.Cell.Row == Row)
			{
                // find index of first selected cell

                selectionStart = selectedCells.Min.Cell.Index;

                selectionEnd = selectedCells.Max.Cell.Index;
			}

			if (string.IsNullOrEmpty(AfterBeatCode))
			{
				// perform the transform and get the new beat code
				Transformation();

				AfterBeatCode = Row.Stringify();
				AfterOffset = Row.OffsetValue;
			}

			bool selectFromBack = selectionEnd > RightIndexBoundOfTransform;

			if (selectFromBack)
			{
				// get index from back of list
				selectionStart = rowLengthBefore - selectionStart;
				selectionEnd = rowLengthBefore - selectionEnd;
			}

			Row.FillFromBeatCode(AfterBeatCode);
			if (BeforeOffset != AfterOffset)
			{
				Row.OffsetValue = AfterOffset;
				Row.Offset = BeatCell.Parse(AfterOffset);
			}

            if (ChangesViewWidth)
            {
                double maxDur = DrawingView.Instance.Rows.Max(x => x.Duration);

                // change the view's width
                //var curFrame = DrawingView.Instance.Frame;
                //curFrame.Width = (System.nfloat)(maxDur * DrawingView.ScalingFactor + 550);
                //DrawingView.Instance.Frame = curFrame;

                // need to draw the end portion of other rows
                if (maxDur == Row.Duration)
                {
					DrawingView.Instance.ResizeFrame(maxDur);
                }
                else
                {
                    ChangesViewWidth = false;
                }
            }

			DrawingView.Instance.QueueRowToDraw(Row);

            DrawingView.Instance.ChangesApplied = false;
			RedrawReferencers();

			if (selectionStart > -1)
			{
				if (selectFromBack)
				{
					// convert back to forward indexed
					selectionStart = Row.Cells.Count - selectionStart;
					selectionEnd = Row.Cells.Count - selectionEnd;
				}

                if (selectionStart < 0) selectionStart = 0;
                if (selectionEnd >= Row.Cells.Count) selectionEnd = Row.Cells.Count - 1;

                // make new selection

                CellTreeNode startNode = Row.Cells.LookupIndex(selectionStart);
                CellTreeNode endNode = Row.Cells.LookupIndex(selectionEnd);

                if (startNode != null && endNode != null)
                {
					EditorViewController.Instance.DView.SelectCell(startNode.Cell);
					if (startNode != endNode)
					{
						EditorViewController.Instance.DView.SelectCell(endNode.Cell, true);
					}
                }
			}
		}

		public virtual void Undo()
		{
			// if no change, don't do anything
			if (AfterBeatCode == BeforeBeatCode)
			{
				return;
			}

            CellTree selectedCells = EditorViewController.Instance.DView.SelectedCells;

			// get current selection range if it's in this row
			int selectionStart = -1;
			int selectionEnd = -1;

			// get selection indexes if there is a selection in the action's row
			if (selectedCells.Root != null && selectedCells.Root.Cell.Row == Row)
			{
                // find index of first selected cell
                selectionStart = selectedCells.Min.Cell.Index;

                selectionEnd = selectedCells.Max.Cell.Index;
			}

			bool selectFromBack = selectionEnd > RightIndexBoundOfTransform;

			if (selectFromBack)
			{
				// get index from back of list
				selectionStart = Row.Cells.Count - selectionStart;
				selectionEnd = Row.Cells.Count - selectionEnd;
			}

			Row.FillFromBeatCode(BeforeBeatCode);
			if (BeforeOffset != AfterOffset)
			{
				Row.OffsetValue = BeforeOffset;
				Row.Offset = BeatCell.Parse(BeforeOffset);
			}

			if (ChangesViewWidth)
			{
				double maxDur = DrawingView.Instance.Rows.Max(x => x.Duration);

                DrawingView.Instance.ResizeFrame(maxDur, false);
			}
            else
            {
				EditorViewController.Instance.DView.QueueRowToDraw(Row);
				
				RedrawReferencers();
            }

            EditorViewController.Instance.DView.ChangesApplied = false;

            // would be nice to only draw individual rows, but seems to be a problem
            //DrawingView.Instance.NeedsDisplay = true;


			if (selectionStart > -1)
			{
				if (selectFromBack)
				{
					// convert back to forward indexed
					selectionStart = Row.Cells.Count - selectionStart;
					selectionEnd = Row.Cells.Count - selectionEnd;
				}

				if (selectionStart < 0) selectionStart = 0;
				if (selectionEnd >= Row.Cells.Count) selectionEnd = Row.Cells.Count - 1;

                // make new selection
                CellTreeNode startNode = Row.Cells.LookupIndex(selectionStart);
                CellTreeNode endNode = Row.Cells.LookupIndex(selectionEnd);

                EditorViewController.Instance.DView.SelectCell(startNode.Cell);

                if (startNode != endNode)
                {
					EditorViewController.Instance.DView.SelectCell(endNode.Cell, true);
                }
			}
		}

        public abstract bool CanPerform();
	}
}
