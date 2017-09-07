// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;
using Pronome.Mac.Editor;
using System.Collections.Generic;
using CoreGraphics;
using Pronome.Mac.Editor.Groups;
using System.Linq;
using CoreAnimation;

namespace Pronome.Mac
{
    public partial class DrawingView : NSView
    {
        #region Static fields
        static public DrawingView Instance;

		/// <summary>
		/// Used to convert BPM to pixels
		/// </summary>
		static public double ScalingFactor = 20;
        #endregion

        #region Constants
        const int RowHeight = 50;
        const int RowSpacing = 20;
        public const int CellWidth = 4; // pixel width of cell elements
        public const int PaddingLeft = 5; // distance from left of frame to the start of rows

        /// <summary>
        /// Space between the bottom of top of cells and the row bounds.
        /// </summary>
        const int CellHeightPad = 10;
        #endregion

        #region Colors
        static CGColor CellColor = NSColor.Black.CGColor;
        static CGColor SelectedCellColor = NSColor.Purple.CGColor;
        static CGColor RowBackgroundColor = NSColor.LightGray.CGColor;
        static CGColor SelectBoxColor = NSColor.DarkGray.CGColor;
        static CGColor GridLineColor = NSColor.Red.CGColor;
        static CGColor MeasureLineColor = NSColor.Blue.CGColor;
        #endregion

        #region Computed Properties
        private string _cursorPostion;
        /// <summary>
        /// Used to display the mouse location in BPM
        /// </summary>
        /// <value>The cursor position.</value>
        [Export("CursorPosition")]
        public string CursorPosition
        {
            get => _cursorPostion;
            set
            {
                WillChangeValue("CursorPosition");
                _cursorPostion = value;
                DidChangeValue("CursorPosition");
            }
        }


        private string _gridSpacingString = "1";
        [Export("GridSpacingString")]
        public string GridSpacingString
        {
            get => _gridSpacingString;
            set
            {
                WillChangeValue("GridSpacingString");
                if (BeatCell.TryParse(value, out double bpm) && bpm > 0)
                {
                    GridSpacing = bpm;
					_gridSpacingString = value;

                    // draw selected row if one exists
                    if (SelectedCells.Root != null)
                    {
                        QueueRowToDraw(SelectedCells.Root.Cell.Row);
                    }
                }
                DidChangeValue("GridSpacingString");
            }
        }

        private string _measureSizeString = "4";
        [Export("MeasureSizeString")]
        public string MeasureSizeString
        {
            get => _measureSizeString;
            set
            {
                WillChangeValue("MeasureSizeString");
                if (BeatCell.TryParse(value, out double bpm) && bpm > 0)
                {
                    MeasureSize = bpm;
                    _measureSizeString = value;
                    // need to redraw the whole view
                    NeedsDisplay = true;
                }
                DidChangeValue("MeasureSizeString");
            }
        }
        #endregion

        #region Public fields
        public Row[] Rows;

        public CellTree SelectedCells = new CellTree();
        #endregion

        #region Protected fields
        protected Queue<Row> RowsToDraw = new Queue<Row>();

        /// <summary>
        /// Used to determine which direction to collapse a selection (using shift key)
        /// </summary>
        protected CellTreeNode SelectionAnchor;

        private CGPoint SelectBoxOrigin = CGPoint.Empty;
        private CAShapeLayer SelectBox;
        private int SelectBoxUpperBound;
        private int SelectBoxLowerBound;
        private int _selectRowIndex;

        /// <summary>
        /// The spacing of the grid lines in BPM
        /// </summary>
        protected double GridSpacing = 1;

        /// <summary>
        /// Spacing between measure lines in BPM
        /// </summary>
        protected double MeasureSize = 4;
        #endregion

        public DrawingView(IntPtr handle) : base(handle)
        {
            Instance = this;

            // instantiate the rows
            Rows = Metronome.Instance.Layers.Select(x => new Row(x)).ToArray();
        }

        #region Overrides

        /// <summary>
        /// Handles all mouse1 operations: selecting cells, creating new cells
        /// </summary>
        /// <param name="theEvent">The event.</param>
        public override void MouseDown(NSEvent theEvent)
        {
            base.MouseDown(theEvent);

            ClickHandler(theEvent);
        }

        public override void RightMouseDown(NSEvent theEvent)
        {
            base.RightMouseDown(theEvent);

            ClickHandler(theEvent, true);
        }

        public override void MouseDragged(NSEvent theEvent)
        {
            base.MouseDragged(theEvent);

            DragHandler(theEvent);
        }

        public override void RightMouseDragged(NSEvent theEvent)
        {
            base.RightMouseDragged(theEvent);

            DragHandler(theEvent);
        }

        public override void MouseUp(NSEvent theEvent)
        {
            base.MouseUp(theEvent);

            MouseUpHandler(theEvent);
        }

        public override void RightMouseUp(NSEvent theEvent)
        {
            base.RightMouseUp(theEvent);

            MouseUpHandler(theEvent);
        }

        public override void MouseMoved(NSEvent theEvent)
        {
            base.MouseMoved(theEvent);
            // display the current position in BPM
            double pos = (ConvertPointFromView(theEvent.LocationInWindow, null).X - PaddingLeft) / ScalingFactor;

            if (pos >= 0)
            {
                CursorPosition = pos.ToString("F2");
            }
            else
            {
                CursorPosition = string.Empty;
            }
        }

        public override void MouseEntered(NSEvent theEvent)
        {
            base.MouseEntered(theEvent);

            Window.AcceptsMouseMovedEvents = true;
            Window.MakeFirstResponder(this);
        }

        public override void MouseExited(NSEvent theEvent)
        {
            base.MouseExited(theEvent);

            Window.AcceptsMouseMovedEvents = false;
            CursorPosition = string.Empty;
        }

        /// <summary>
        /// Render the rows in the queue.
        /// </summary>
        /// <param name="dirtyRect">Dirty rect.</param>
        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

			using (CGContext ctx = NSGraphicsContext.CurrentContext.CGContext)
			{
                // draw measure lines
                ctx.SetStrokeColor(MeasureLineColor);
                ctx.SetLineWidth(1);
                double xPos = PaddingLeft;
                ctx.MoveTo((int)xPos, 0);
                double spacing = MeasureSize * ScalingFactor;
                while (xPos <= Frame.Width)
                {
                    int x = (int)xPos;
                    ctx.MoveTo(x, 0);

                    ctx.AddLineToPoint(x, Frame.Height);

                    xPos += spacing;
                }
                ctx.StrokePath();
                
                if (!RowsToDraw.Any())
                {
                    // draw all rows if initializing or changing window size.
                    foreach (Row row in Rows)
                    {
                        DrawRow(row, ctx);
                    }
                }
                else
                {
                    // draw just the rows in the queue
					while (RowsToDraw.TryDequeue(out Row row))
					{
						DrawRow(row, ctx);
					}
                }

				// draw grid lines if there's a selection
				if (SelectedCells.Root != null)
                {
                    DrawGridLines(ctx);
                }
            }
        }

        public override void ViewDidMoveToWindow()
        {
            base.ViewDidMoveToWindow();

            // add the tracking area, for mouse move events
            AddTrackingRect(Frame, this, IntPtr.Zero, false);
        }

		#endregion

		#region Protected Methods

        /// <summary>
        /// Check what elements are under the select box and perform necessary actions.
        /// </summary>
        /// <param name="theEvent">The event.</param>
		private void MouseUpHandler(NSEvent theEvent)
		{
			if (!SelectBoxOrigin.IsEmpty)
			{
				// handle selection
				var loc = ConvertPointFromView(theEvent.LocationInWindow, null);
				double start = ConvertPosition(Math.Min(SelectBoxOrigin.X, loc.X), Rows[_selectRowIndex]);
				double end = ConvertPosition(Math.Max(SelectBoxOrigin.X, loc.X), Rows[_selectRowIndex]);

				// check if extending current selection
				bool shift = theEvent.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask);

				var startNode = Rows[_selectRowIndex].Cells.FindAboveOrEqualTo(start, true);
				var endNode = Rows[_selectRowIndex].Cells.FindBelowOrEqualTo(end);

				if (startNode != null && startNode.Cell.Position <= endNode.Cell.Position)
				{
					// perform selection
					SelectCell(startNode.Cell, shift);

					if (startNode != endNode)
					{
						SelectCell(endNode.Cell, true);
					}

					QueueRowToDraw(Rows[_selectRowIndex]);
				}
				else if (!shift)
				{
					DeselectCells();

					QueueRowToDraw(Rows[_selectRowIndex]);
				}


				SelectBox.RemoveFromSuperLayer();
				SelectBox.Dispose();
				SelectBoxOrigin = CGPoint.Empty;
			}
		}

        /// <summary>
        /// Resize the select box
        /// </summary>
        /// <param name="theEvent">The event.</param>
		private void DragHandler(NSEvent theEvent)
		{
			if (!SelectBoxOrigin.IsEmpty)
			{
				var loc = ConvertPointFromView(theEvent.LocationInWindow, null);

				using (CGPath path = new CGPath())
				{
					nfloat y = loc.Y;
					if (y > SelectBoxUpperBound)
					{
						y = SelectBoxUpperBound;
					}
					else if (y < SelectBoxLowerBound)
					{
						y = SelectBoxLowerBound;
					}

					loc.Y = y;

					path.MoveToPoint(SelectBoxOrigin);
					path.AddLineToPoint(SelectBoxOrigin.X, y);
					path.AddLineToPoint(loc);
					path.AddLineToPoint(loc.X, SelectBoxOrigin.Y);
					path.CloseSubpath();

					SelectBox.Path = path;
				}
			}
		}

        /// <summary>
        /// Check if an element was clicked or select box drawn and do selection action
        /// </summary>
        /// <param name="theEvent">The event.</param>
        /// <param name="isRightButton">If set to <c>true</c> is right button.</param>
		private void ClickHandler(NSEvent theEvent, bool isRightButton = false)
		{
			// get coordinates of mouse
			var loc = ConvertPointFromView(theEvent.LocationInWindow, null);

			// determine which row was clicked in
			int offset = (int)(Frame.Height - loc.Y - RowSpacing);
			int rowIndex = offset / (RowSpacing + RowHeight);

			// see if it's inside the row (not in spacing)
			if (rowIndex < Rows.Length)
			{
				var ypos = GetYPositionOfRow(Rows[rowIndex]);

				// check if a selection is being drawn
				if (theEvent.ModifierFlags.HasFlag(NSEventModifierMask.ControlKeyMask) || isRightButton)
				{
					if (loc.Y >= ypos && loc.Y <= ypos + RowHeight)
					{
						_selectRowIndex = rowIndex;
						InitSelectBox(loc, ypos + RowHeight, ypos);
						return;
					}
				}

				// see if click is in y range
				if (!isRightButton && loc.Y >= ypos && loc.Y <= ypos + RowHeight)
				{
                    // cell range
                    bool cellSelected = false;
                    if (loc.Y >= ypos + CellHeightPad && loc.Y <= ypos + RowHeight - CellHeightPad)
                    {
                        if (Rows[rowIndex].Cells.TryFind(ConvertPosition(loc.X, Rows[rowIndex]), out Cell cell))
                        {
                            // perform selection actions
                            SelectCell(cell, theEvent.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask));
                            cellSelected = true;
                            QueueRowToDraw(Rows[rowIndex]);
                        }
                    }

					// check if a cell was clicked
					if (!cellSelected && SelectedCells.Root != null)
					{
						// try to create new cell
						// see if clicked on a grid line (within a pad amount)
						double start = (SelectedCells.GetMin().Cell.Position + Rows[rowIndex].Offset) * ScalingFactor + PaddingLeft;

						if (start < loc.X)
						{
							double x = loc.X - start;
							double spacing = GridSpacing * ScalingFactor;
							double mod = x % spacing;
                            double pad = Math.Min(CellWidth / 2, spacing * .125);
							// see if it registers as a hit
							if (mod <= pad || mod >= spacing - pad)
							{

							}
						}
						else
						{
							double end = (SelectedCells.GetMax().Cell.Position + Rows[rowIndex].Offset) * ScalingFactor + PaddingLeft;

							if (end > loc.X)
							{

							}
						}
					}
				}
			}
		}

        protected double ConvertPosition(double value, Row row)
        {
            return (value - PaddingLeft) / ScalingFactor - row.Offset;
        }

        /// <summary>
        /// Queues all rows to draw.
        /// </summary>
        protected void QueueAllRowsToDraw()
        {
            foreach(Row row in Rows)
            {
                QueueRowToDraw(row);
            }
        }

        /// <summary>
        /// Queues the row to draw.
        /// </summary>
        /// <param name="row">Row.</param>
        protected void QueueRowToDraw(Row row)
        {
            //RowsToDraw.Clear();

            // see if there are any layers referencing this one.

            RowsToDraw.Enqueue(row);

            foreach (Row r in RowsToDraw)
            {
                int ind = r.Index;
                int y = GetYPositionOfRow(row);
                CGRect dirty = new CGRect(0, y, Frame.Width, RowHeight);
                SetNeedsDisplayInRect(dirty);
            }
        }

        /// <summary>
        /// Draws the specific row.
        /// </summary>
        /// <param name="row">Row.</param>
        /// <param name="baseCtx">Base context.</param>
        protected void DrawRow(Row row, CGContext baseCtx)
        {
			// get rect for the CGLayer to use
			int y = GetYPositionOfRow(row);
            int x = (int)(row.Offset * ScalingFactor);
            int width = (int)(row.Duration * ScalingFactor);

            using (CGLayer layer = CGLayer.Create(baseCtx, new CGSize(width, RowHeight)))
            {
                CGContext layerCtx = layer.Context;

                // draw the background
                layerCtx.SetFillColor(RowBackgroundColor);
                layerCtx.FillRect(new CGRect(0, 0, width, RowHeight));

                // these stacks facilitate the drawing of repeat groups.
                Stack<Repeat> ActiveRepeats = new Stack<Repeat>();
                Stack<CGLayer> RepeatLayers = new Stack<CGLayer>();

                Stack<Multiply> ActiveMults = new Stack<Multiply>();

                foreach (Cell cell in row.Cells)
                {
                    CGContext ctx = HandleRepeatGroups(layerCtx, ActiveRepeats, RepeatLayers, cell);

                    HandleMultGroups(ActiveMults, cell, ctx);

                    if (!string.IsNullOrEmpty(cell.Reference))
                    {
                        DrawReferenceRect(cell, ctx);

                        continue;
                    }

                    int xPos = (int)(cell.Position * ScalingFactor);
                    ctx.MoveTo(xPos, CellHeightPad);

                    if (cell.IsSelected) ctx.SetFillColor(SelectedCellColor);
                    else ctx.SetFillColor(CellColor);

                    ctx.FillRect(new CGRect(xPos, CellHeightPad, CellWidth, RowHeight - 2 * CellHeightPad));
				}

                int pos = (int)(row.Offset * ScalingFactor) + PaddingLeft;
                int length = (int)(row.Duration * ScalingFactor);
                //int ypos = GetYPositionOfRow(row);

                // draw actual elements
                baseCtx.DrawLayer(layer, new CGPoint(pos, y));
                pos += length;

                // draw ghosts
                baseCtx.SetAlpha(.5f);
                while (pos < Frame.Width)
                {
                    baseCtx.DrawLayer(layer, new CGPoint(pos, y));
                    pos += length;
                }
                baseCtx.SetAlpha(1);
            }
        }

        protected CGContext HandleRepeatGroups(CGContext layerCtx, Stack<Repeat> ActiveRepeats, Stack<CGLayer> RepeatLayers, Cell cell)
        {
            if (!ActiveRepeats.Any()) return layerCtx;
            // check if repeat groups are ended
            Repeat repGroup = ActiveRepeats.Peek();

            while (repGroup != null && !cell.RepeatGroups.Contains(repGroup))
            {
                ActiveRepeats.Pop();
                var replyer = RepeatLayers.Pop();
                // get the context to draw on
                var c = RepeatLayers.Peek()?.Context ?? layerCtx;
                // draw originals
                int dur = (int)(repGroup.Length * ScalingFactor);
                int xp = (int)(repGroup.Position * ScalingFactor);
                c.DrawLayer(replyer, new CGPoint(xp, 0));
                // draw copies
                c.SetAlpha(.7f); // repeats are faded
                for (int i = 1; i < repGroup.Times; i++)
                {
                    c.DrawLayer(replyer, new CGPoint(xp + dur * i, 0));
                }
                c.SetAlpha(1f);

                replyer.Dispose();

                repGroup = ActiveRepeats.Peek();
            }

            // update to current context
            CGContext ctx = RepeatLayers.Peek()?.Context ?? layerCtx;

            // check if repeat groups are opened
            if (ActiveRepeats.Peek() != cell.RepeatGroups.Last?.Value)
            {
                LinkedListNode<Repeat> cellGroup = cell.RepeatGroups.Last;
                // find the group that is currently open
                while (cellGroup?.Value != ActiveRepeats.Peek())
                {
                    cellGroup = cellGroup.Previous;
                }
                // add all new groups
                while (cellGroup != null)
                {
                    var gp = cellGroup.Value;
                    ActiveRepeats.Push(gp);
                    int w = (int)(gp.Length * ScalingFactor);
                    RepeatLayers.Push(CGLayer.Create(ctx, new CGSize(w, RowHeight)));

                    // draw the element
                    DrawGroupElement(ctx, NSColor.Green.CGColor, gp);

                    ctx = RepeatLayers.Peek().Context;

                    cellGroup = cellGroup.Next;
                }
            }

            return ctx;
        }

        /// <summary>
        /// Add / remove groups from the stack, draw any new groups.
        /// </summary>
        /// <param name="ActiveMults">Active mults.</param>
        /// <param name="cell">Cell.</param>
        /// <param name="ctx">Context.</param>
        protected void HandleMultGroups(Stack<Multiply> ActiveMults, Cell cell, CGContext ctx)
        {
            if (!ActiveMults.Any()) return;

            Multiply multGroup = ActiveMults.Peek();
            // check if a mult groups are closed
            while (multGroup != null && !cell.MultGroups.Contains(multGroup))
            {
                ActiveMults.Pop();
                multGroup = ActiveMults.Peek();
            }

            // check if mult groups are opened
            LinkedListNode<Multiply> cellMGrp = cell.MultGroups.Last;
            if (ActiveMults.Peek() != cellMGrp?.Value)
            {
                // descend to currently active group
                while (ActiveMults.Peek() != cellMGrp?.Value)
                {
                    cellMGrp = cellMGrp.Previous;
                }

                while (cellMGrp != null)
                {
                    var mg = cellMGrp.Value;
                    ActiveMults.Push(mg);

                    // draw the element
                    DrawGroupElement(ctx, NSColor.Orange.CGColor, mg);

                    cellMGrp = cellMGrp.Next;
                }
            }
        }

        /// <summary>
        /// Draws the rectangle that signifies a layer reference.
        /// </summary>
        /// <param name="cell">Cell.</param>
        /// <param name="ctx">Context.</param>
        protected void DrawReferenceRect(Cell cell, CGContext ctx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Draws the grid lines.
        /// </summary>
        /// <param name="ctx">Context.</param>
		protected void DrawGridLines(CGContext ctx)
		{
			Row row = SelectedCells.Root.Cell.Row;

			double start = (SelectedCells.GetMin().Cell.Position + row.Offset) * ScalingFactor + PaddingLeft;
			double end = (SelectedCells.GetMax().Cell.Position + row.Offset) * ScalingFactor + PaddingLeft;
			int yPos = GetYPositionOfRow(row);

			double spacing = GridSpacing * ScalingFactor;

			start -= spacing;
			end += spacing;

			ctx.SetStrokeColor(GridLineColor);
			ctx.SetLineWidth(1);

			while (start >= 0)
			{
				int x = (int)start;
				ctx.MoveTo(x, yPos);
				ctx.AddLineToPoint(x, yPos + RowHeight);
				start -= spacing;
			}

			while (end <= Frame.Width)
			{
				int x = (int)end;
				ctx.MoveTo(x, yPos);
				ctx.AddLineToPoint(x, yPos + RowHeight);
				end += spacing;
			}

			ctx.StrokePath();
		}

        /// <summary>
        /// Draws a group box with the specified attributes.
        /// </summary>
        /// <param name="ctx">Context.</param>
        /// <param name="color">Color.</param>
        /// <param name="group">Group.</param>
        /// <param name="pad">Pad.</param>
        protected void DrawGroupElement(CGContext ctx, CGColor color, AbstractGroup group, int pad = 0)
        {
            var clear = new CGColor(color, 0);

			var gradient = new CGGradient(
				CGColorSpace.CreateDeviceRGB(),
				new CGColor[] { color, clear, clear, color },
				new nfloat[] { 0, .1f, .9f, 1 }
			);

            var start = new CGPoint(group.Position * ScalingFactor, pad);
            var end = new CGPoint(group.Position * ScalingFactor + group.Length * ScalingFactor, pad);

            ctx.SaveState();

            var rect = new CGRect(start, new CGSize(group.Length * ScalingFactor, RowHeight - pad / 2));
            ctx.AddRect(rect);

            ctx.Clip();

			ctx.DrawLinearGradient(
				gradient,
				start,
				end,
				CGGradientDrawingOptions.None
			);

            ctx.SetStrokeColor(color);
            ctx.AddRect(rect);
            ctx.StrokePath();

            ctx.RestoreState();
        }

        protected void InitSelectBox(CGPoint origin, int upperBound, int lowerBound)
        {
			SelectBoxOrigin = origin;
			SelectBox = new CAShapeLayer();
			SelectBox.LineWidth = 1;
            SelectBox.StrokeColor = SelectBoxColor;
            var fillColor = new CGColor(SelectBoxColor, .2f);
			SelectBox.FillColor = fillColor;
            SelectBoxUpperBound = upperBound;
            SelectBoxLowerBound = lowerBound;

			Layer?.AddSublayer(SelectBox);
        }

        /// <summary>
        /// Performs actions on cell selection based on a targeted cell.
        /// </summary>
        /// <param name="cell">Cell.</param>
        /// <param name="extendSelection">If set to <c>true</c> extend selection.</param>
        protected void SelectCell(Cell cell, bool extendSelection = false)
        {
            // is there an existing selection?
            if (SelectedCells.Root != null)
            {
                // is current selection in same row?
                if (extendSelection && SelectedCells.Root.Cell.Row == cell.Row)
                {
                    // extend or collapse the selection
                    CellTreeNode min = SelectedCells.GetMin();
                    CellTreeNode max = SelectedCells.GetMax();

                    if (min.Cell == cell)
                    {
						// clicked on a boundary cell
                        if (SelectedCells.Count == 1)
                        {
                            DeselectCells();
                        }
                        else
                        {
                            SelectedCells.Remove(min);
                            cell.IsSelected = false;

                            if (SelectionAnchor == min)
                            {
                                SelectionAnchor = max;
                            }
                        }
                    }
                    else if (max.Cell == cell)
                    {
                        if (SelectedCells.Count == 1)
                        {
                            DeselectCells();
                        }
                        else
                        {
                            SelectedCells.Remove(max);
                            cell.IsSelected = false;

                            if (SelectionAnchor == max)
                            {
                                SelectionAnchor = min;
                            }
                        }
                    }
                    else if (cell.Position < min.Cell.Position)
                    {
                        SelectionAnchor = max;

                        CellTreeNode node = cell.Row.Cells.Lookup(min.Cell.Position, false).Prev();
                        // select cells down to the target
						while (node != null && node.Cell != cell)
                        {
                            node.Cell.IsSelected = true;
                            SelectedCells.Insert(node.Cell);

							node = node.Prev();
                        }

                        if (node != null)
                        {
                            node.Cell.IsSelected = true;
                            SelectedCells.Insert(node.Cell);
                        }
                    }
                    else if (cell.Position > max.Cell.Position)
                    {
                        SelectionAnchor = min;

                        CellTreeNode node = cell.Row.Cells.Lookup(max.Cell.Position, false);
                        // select cells up to the target
                        while (node.Cell != cell)
                        {
                            node = node.Next();
                            node.Cell.IsSelected = true;
                            SelectedCells.Insert(node.Cell);
                        }
                    }
                    else
                    {
                        // collapsing selection
                        if (SelectionAnchor == min)
                        {
                            CellTreeNode node = cell.Row.Cells.Lookup(max.Cell.Position, false);

                            while (node.Cell != cell)
                            {
                                node.Cell.IsSelected = false;
                                SelectedCells.Remove(node.Cell);
                                node = node.Prev();
                            }
                        }
                        else
                        {
                            CellTreeNode node = cell.Row.Cells.Lookup(min.Cell.Position, false);

                            while (node.Cell != cell)
                            {
                                node.Cell.IsSelected = false;
                                SelectedCells.Remove(node.Cell);
                                node = node.Next();
                            }
                        }
                    }
                }
                else
                {
                    // clear and select target cell only
                    if (SelectedCells.Root.Cell.Row != cell.Row)
                    {
                        QueueRowToDraw(SelectedCells.Root.Cell.Row);
                    }

                    DeselectCells();
                    SelectCell(cell);
                }
            }
            else
            {
                // select a single cell
                cell.IsSelected = true;
                SelectionAnchor = new CellTreeNode(cell);
                SelectedCells.Insert(SelectionAnchor);
            }
        }

        /// <summary>
        /// Clears the current selection. Does not redraw rows.
        /// </summary>
        protected void DeselectCells()
        {
            foreach (Cell c in SelectedCells)
            {
                c.IsSelected = false;
            }

            SelectedCells.Clear();
            SelectionAnchor = null;
        }

        /// <summary>
        /// Gets the Y position of a row based on it's index.
        /// </summary>
        /// <returns>The YP osition of row.</returns>
        /// <param name="row">Row.</param>
        protected int GetYPositionOfRow(Row row)
        {
            return (int)(Frame.Height - RowHeight - RowSpacing - (RowHeight + RowSpacing) * row.Index);
		}
        #endregion
    }
}
