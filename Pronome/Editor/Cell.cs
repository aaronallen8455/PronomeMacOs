﻿using System;
using System.Collections.Generic;
using CoreGraphics;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor
{
	public class Cell
	{
		public Row Row;

		/// <summary>
		/// Currently selected cells
		/// </summary>
		public static Selection SelectedCells = new Selection();

		protected double _duration;
		/// <summary>
		/// The cell's value in BPM
		/// </summary>
		public double Duration
		{
			get => _duration;
			set
			{
				//HashSet<RepeatGroup> touchedRepGroups = new HashSet<RepeatGroup>();
				//HashSet<MultGroup> touchedMultGroups = new HashSet<MultGroup>();
				HashSet<Group> touchedGroups = new HashSet<Group>();
				double diff = value - _duration;
				_duration = value;
				// resize groups of which this cell is a part
				foreach (RepeatGroup rg in RepeatGroups)
				{
					touchedGroups.Add(rg);
					rg.Duration += diff;
				}
				foreach (MultGroup mg in MultGroups)
				{
					touchedGroups.Add(mg);
					mg.Duration += diff;
				}
				// reposition all subsequent cells and groups and references
				foreach (Cell cell in Row.Cells.SkipWhile(x => x != this).Skip(1))
				{
					cell.Position += diff;
					// reposition reference rect
					if (cell.ReferenceRectangle != null)
					{
						double cur = Canvas.GetLeft(cell.ReferenceRectangle);
						Canvas.SetLeft(cell.ReferenceRectangle, cur + diff);
					}
				}
				// reposition groups
				foreach (RepeatGroup rg in Row.RepeatGroups.Where(x => !touchedGroups.Contains(x) && x.Position > Position))
				{
					rg.Position += diff;
				}
				foreach (MultGroup mg in Row.MultGroups.Where(x => !touchedGroups.Contains(x) && x.Position > Position))
				{
					mg.Position += diff;
				}
				// resize sizer
				Row.ChangeSizerWidthByAmount(diff);
			}
		}

		protected double _actualDuration = -1;
		/// <summary>
		/// Get the duration of the cell with multiplication groups applied
		/// </summary>
		public double ActualDuration
		{
			get => Duration;
		}

		protected string _value;
		/// <summary>
		/// The string representation of the duration. ie 1+2/3
		/// </summary>
		public string Value
		{
			get => _value;
			set
			{
				// update the duration UI input if this is the only selected cell
				if (IsSelected && SelectedCells.Cells.Count == 1)
				{
					//EditorWindow.Instance.durationInput.Text = value;
				}

				_value = value;
			}
		}

		protected double _position;
		/// <summary>
		/// The horizontal position of the cell in BPM. Changes actual position when set.
		/// </summary>
		public double Position
		{
			get => _position;
			set
			{
				_position = value;
				//Canvas.SetLeft(Rectangle, value * EditorWindow.Scale * EditorWindow.BaseFactor);

			}
		}
		public bool IsSelected = false;

		/// <summary>
		/// Whether cell is a break point for a loop - |
		/// </summary>
		public bool IsBreak = false;

		/// <summary>
		/// The index of the layer that this cell is a reference for. Null if it's a regular cell.
		/// </summary>
		public string Reference;
		
		/// <summary>
		/// The audio source for this cell.
		/// </summary>
        public StreamInfoProvider Source = null;

		/// <summary>
		/// Multiplication groups that this cell is a part of
		/// </summary>
        public LinkedList<Multiply> MultGroups = new LinkedList<Multiply>();

		/// <summary>
		/// Repeat groups that this cell is part of
		/// </summary>
		public LinkedList<Repeat> RepeatGroups = new LinkedList<Repeat>();

		/// <summary>
		/// Is this cell part of a reference. Should not be manipulable if so
		/// </summary>
		public bool IsReference = false;

		/// <summary>
		/// Drawn in place of cell rect if this is a reference. Denotes a referenced block.
		/// </summary>
		//public CGLayer ReferenceRectangle;

		public Cell(Row row)
		{
			Row = row;
			//Rectangle = EditorWindow.Instance.Resources["cellRectangle"] as Rectangle;
			//Rectangle.Height = (double)EditorWindow.Instance.Resources["cellHeight"];

			// the ref rect
			//ReferenceRectangle = EditorWindow.Instance.Resources["referenceRectangle"] as Rectangle;
			//// set Canvas.Top
			//double top = (double)EditorWindow.Instance.Resources["rowHeight"] / 2 - (double)EditorWindow.Instance.Resources["cellHeight"] / 2;
			//Canvas.SetTop(Rectangle, top);
			//Canvas.SetTop(ReferenceRectangle, top);
			//
			//Panel.SetZIndex(Rectangle, 10);
			//Rectangle.MouseLeftButtonDown += Rectangle_MouseDown;
			//ReferenceRectangle.MouseLeftButtonDown += Rectangle_MouseDown;
		}

		///// <summary>
		///// Select the cell
		///// </summary>
		///// <param name="sender"></param>
		///// <param name="e"></param>
		//private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
		//{
		//	if (!IsReference) // ref cells should not be manipulable
		//	{
		//		ToggleSelect();
		//
		//		EditorWindow.Instance.UpdateUiForSelectedCell();
		//	}
		//
		//	e.Handled = true;
		//}

		public void ToggleSelect(bool Clicked = true)
		{

			IsSelected = !IsSelected;
			// set selection color
			//Rectangle.Stroke = IsSelected ? System.Windows.Media.Brushes.DeepPink : System.Windows.Media.Brushes.Black;
			// select the reference rect if this cell is a ref
			if (!string.IsNullOrEmpty(Reference))
			{
				//ReferenceRectangle.Opacity += .4 * (IsSelected ? 1 : -1);
			}
			// if not a multi select, deselect currently selected cell(s)
			if (IsSelected)
			{
				if (Clicked)
				{
					// multiSelect
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
					{
						int start = -1;
						int end = -1;
						for (int i = 0; i < Row.Cells.Count; i++)
						{
							if (start == -1 && Row.Cells[i].IsSelected)
							{
								start = i;
								end = i;
							}
							else if (Row.Cells[i].IsSelected || Row.Cells[i] == this)
							{
								end = i;
							}
						}
						// have to set this otherwise it will get flipped by SelectRange.
						IsSelected = false;

						SelectedCells.SelectRange(start, end, Row, false);

						return;
					}
					else
					{ // single select : deselect others
						foreach (Cell c in SelectedCells.Cells.ToArray())
						{
							c.ToggleSelect(false);
						}
					}
				}

				SelectedCells.Cells.Add(this);
				EditorWindow.Instance.SetCellSelected(true);
			}
			else if (!IsSelected)
			{ // deselect if not a cell in a multi-select being clicked
				if (SelectedCells.Cells.Count > 1 && Clicked)
				{
					//ToggleSelect(false);
					foreach (Cell c in SelectedCells.Cells.ToArray())
					{
						c.ToggleSelect(false);
					}
				}

				SelectedCells.Cells.Remove(this);
				if (!SelectedCells.Cells.Any())
				{
					// no cells selected
					EditorWindow.Instance.SetCellSelected(false);
				}
			}
		}

		/// <summary>
		/// Assign a new duration with altering the UI
		/// </summary>
		/// <param name="duration"></param>
		public void SetDurationDirectly(double duration)
		{
			_duration = duration;
			//_actualDuration = -1; // reevaluate actual duration
		}

		//public int CompareTo(object obj)
		//{
		//	if (obj is Cell cell)
		//	{
		//		return Position > cell.Position ? 1 : -1;
		//	}
		//	return 0;
		//}

		public class Selection
		{
			/// <summary>
			/// Cells currently contained by the selection
			/// </summary>
			public List<Cell> Cells = new List<Cell>();

			/// <summary>
			/// First cell in the selection. Set when grid lines are drawn
			/// </summary>
			public Cell FirstCell;

			/// <summary>
			/// Last cell in the selection. Set when grid lines are drawn
			/// </summary>
			public Cell LastCell;

			/// <summary>
			/// Remove all cells from the selection
			/// </summary>
			public void Clear()
			{
				Cells.Clear();
				FirstCell = null;
				LastCell = null;
			}

			/// <summary>
			/// Deselect all curently selected cells
			/// </summary>
			public void DeselectAll(bool updateUi = true)
			{
				foreach (Cell c in Cells.ToArray())
				{
					c.ToggleSelect(false);
				}

				Clear();

				if (updateUi)
				{
					//EditorWindow.Instance.UpdateUiForSelectedCell();
				}
			}

			/// <summary>
			/// Select all cells from start to end inclusive
			/// </summary>
			/// <param name="start"></param>
			/// <param name="end"></param>
			/// <param name="row"></param>
			public void SelectRange(int start, int end, Row row, bool updateUi = true)
			{
				DeselectAll(false);

				if (row.Cells.Count > start && row.Cells.Count > end && start <= end)
				{
					for (int i = start; i <= end; i++)
					{
						row.Cells[i].ToggleSelect(false);
					}
				}

				if (updateUi)
				{
					EditorWindow.Instance.UpdateUiForSelectedCell();
				}
			}
		}
	}
}