// This file has been autogenerated from a class added in the UI designer.

using System;
using CoreGraphics;
using Foundation;
using AppKit;
using Pronome.Mac.Editor;
using System.Collections.Generic;
using Pronome.Mac.Editor.Action;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac
{
    public partial class EditorViewController : NSViewController
    {
        #region static fields
        /// <summary>
        /// The instance. Behaves like a singleton
        /// </summary>
        public static EditorViewController Instance;

        /// <summary>
        /// The undo stack.
        /// </summary>
        public static Stack<IEditorAction> UndoStack = new Stack<IEditorAction>(50);

        /// <summary>
        /// The redo stack.
        /// </summary>
        public static Stack<IEditorAction> RedoStack = new Stack<IEditorAction>(50);
        #endregion

        #region Public fields
        /// <summary>
        /// True if a dialog sheet is currently open and all other interactions should be disabled.
        /// </summary>
        public bool SheetIsOpen;
        #endregion

        #region private fields
        private Repeat RepGroupToEdit;
        private Multiply MultGroupToEdit;
        private Cell RefToEdit;

        /// <summary>
        /// Prevents the view from being redrawn when beatcode changes due to change apply
        /// </summary>
        bool JustAppliedChanges;
        #endregion

        #region Computed Properties
        [Export("DView")]
        public DrawingView DView
        {
            get => DrawingView;
        }
        #endregion

        #region Constructor
        public EditorViewController(IntPtr handle) : base(handle)
        {
            Instance = this;
        }
        #endregion

        #region Public static methods
        public static void InitNewAction(IEditorAction action)
        {
            if (action.CanPerform())
            {
                action.Redo();
                UndoStack.Push(action);
                RedoStack.Clear();
            }
        }
        #endregion

        #region Actions
        /// <summary>
        /// Applies changes that were made to the beat.
        /// </summary>
        /// <param name="sender">Sender.</param>
        partial void ApplyChangesAction(NSObject sender)
        {
            JustAppliedChanges = true;
            // apply the changes
            foreach (Row row in DView.Rows)
            {
                if (!row.BeatCodeIsCurrent) row.UpdateBeatCode(); // probably not used
                Metronome.Instance.Layers[row.Index].SetBeatCode(row.BeatCode);
                // need to apply color to the beat code.
                Metronome.Instance.Layers[row.Index].Controller.HighlightBeatCodeSyntax();
            }

            DView.ChangesApplied = true;
        }

        partial void QuarterPresetAction(NSObject sender)
        {
            DView.GridSpacingString = "1";
        }

        partial void QuarterTripletPresetAction(NSObject sender)
        {
            DView.GridSpacingString = "2/3";
        }

        partial void EighthPresetAction(NSObject sender)
        {
            DView.GridSpacingString = ".5";
        }

        partial void EighthTripletPresetAction(NSObject sender)
        {
            DView.GridSpacingString = "1/3";
        }

        partial void SixteenthPresetAction(NSObject sender)
        {
            DView.GridSpacingString = ".25";
        }

        partial void SixteenthTripletPresetAction(NSObject sender)
        {
            DView.GridSpacingString = "1/6";
        }

        /// <summary>
        /// Enable or disable the various menu items
        /// </summary>
        /// <returns><c>true</c>, if menu action was validated, <c>false</c> otherwise.</returns>
        /// <param name="item">Item.</param>
		[Action("validateMenuItem:")]
        public bool ValidateMenuAction(NSMenuItem item)
        {
            string actionName = item.Action.Name;

            CellTree selectedCells = DView.SelectedCells;
            bool cellsSelected = DView.SelectedCells.Root != null;

            switch (actionName)
            {
                case "undoBeat:":
                    if (UndoStack.Count > 0)
                    {
                        var action = UndoStack.Peek();

                        item.Title = "Undo " + action.HeaderText;

                        return true;
                    }

                    item.Title = "Undo";
                    return false;
                case "redoBeat:":
                    if (RedoStack.Count > 0)
                    {
                        var action = RedoStack.Peek();

                        item.Title = "Redo " + action.HeaderText;

                        return true;
                    }
                    item.Title = "Redo";
                    return false;
                case "createRepGroup:":
                    if (cellsSelected)
                    {
                        var firstGroup = selectedCells.Min.Cell.RepeatGroups.LastOrDefault();
                        var lastGroup = selectedCells.Max.Cell.RepeatGroups.LastOrDefault();

                        if (firstGroup == null && lastGroup == null)
                        {
                            return true;
                        }

                        if (firstGroup == lastGroup)
                        {
                            // don't create overlapping groups
                            return firstGroup.Cells.First() != selectedCells.Min.Cell || lastGroup.Cells.Last() != selectedCells.Max.Cell;
                        }
                    }

                    return false;

                case "editRepGroup:":
                    if (cellsSelected)
                    {
						Cell first = selectedCells.Min.Cell;
						Cell last = selectedCells.Max.Cell;
						foreach ((bool b, AbstractGroup g) in first.GroupActions.Where(x => x.Item2.GetType() == typeof(Repeat)))
						{
							if (last.GroupActions.Contains((false, g)))
							{
                                // just get the group to edit here so we don't have to find it again later.
                                RepGroupToEdit = g as Repeat;
								return true;
							}
						}
                        
                    }

                    return false;
                case "removeRepGroup:":
                    if (cellsSelected)
                    {
                        var firstGroups = selectedCells.Min.Cell.RepeatGroups.Where(x => x.Cells.First() == selectedCells.Min.Cell);

                        var lastGroups = selectedCells.Max.Cell.RepeatGroups.Where(x => x.Cells.Last() == selectedCells.Max.Cell);

                        return firstGroups.Intersect(lastGroups).Any();
                    }
                    return false;
                case "createMultGroup:":
					if (cellsSelected)
					{
                        var firstGroup = selectedCells.Min.Cell.MultGroups.LastOrDefault();
                        var lastGroup = selectedCells.Max.Cell.MultGroups.LastOrDefault();

						if (firstGroup == null && lastGroup == null)
						{
							return true;
						}

						if (firstGroup == lastGroup)
						{
							// don't create overlapping groups
							return firstGroup.Cells.First() != selectedCells.Min.Cell || lastGroup.Cells.Last() != selectedCells.Max.Cell;
						}
					}

					return false;

                case "multGroupDrawToScale:":
                    // assign state based on user setting
                    item.State = UserSettings.GetSettings().DrawMultToScale
                        ? NSCellStateValue.On
                        : NSCellStateValue.Off;
                    break;
                case "editMultGroup:":
					if (cellsSelected)
					{
						Cell first = selectedCells.Min.Cell;
						Cell last = selectedCells.Max.Cell;
                        foreach ((bool b, AbstractGroup g) in first.GroupActions.Where(x => x.Item2.GetType() == typeof(Multiply)))
						{
							if (last.GroupActions.Contains((false, g)))
							{
								// just get the group to edit here so we don't have to find it again later.
                                MultGroupToEdit = g as Multiply;
								return true;
							}
						}

					}

					return false;
                case "removeMultGroup:":
                    if (cellsSelected)
                    {
                        var firstGroups = selectedCells.Min.Cell.MultGroups.Where(x => x.Cells.First() == selectedCells.Min.Cell);

                        var lastGroups = selectedCells.Max.Cell.MultGroups.Where(x => x.Cells.Last() == selectedCells.Max.Cell);

                        return firstGroups.Intersect(lastGroups).Any();
                    }
                    return false;
                case "createRef:":
                    return cellsSelected && selectedCells.Count == 1;
                case "editRef:":
                    if (cellsSelected && selectedCells.Count == 1 && !string.IsNullOrEmpty(selectedCells.Root.Cell.Reference))
                    {
                        RefToEdit = selectedCells.Root.Cell;
                        return true;
                    }
                    return false;
                case "removeRef:":
                    return cellsSelected && selectedCells.Count == 1 && !string.IsNullOrEmpty(selectedCells.Root.Cell.Reference);
                case "moveCellsLeft:":
                    return cellsSelected;
                case "moveCellsRight:":
                    return cellsSelected;
                case "removeCells:":
                    return cellsSelected;
            }

            return true;
        }

        [Action("undoBeat:")]
        void UndoBeat(NSObject sender)
        {
            var action = UndoStack.Pop();

            action.Undo();

            RedoStack.Push(action);
        }

        [Action("redoBeat:")]
        void RedoBeat(NSObject sender)
        {
            var action = RedoStack.Pop();

            action.Redo();

            UndoStack.Push(action);
        }

        [Action("createRepGroup:")]
        void CreateRepGroup(NSObject sender)
        {
            RepGroupToEdit = null;
            PerformSegue("RepeatGroupSegue", this);
        }

        [Action("editRepGroup:")]
        void EditRepGroup(NSObject sender)
        {
            PerformSegue("RepeatGroupSegue", this);
        }

        [Action("removeRepGroup:")]
        void RemoveRepGroup(NSObject sender)
        {
            var action = new RemoveRepeatGroup(DView.SelectedCells);

            InitNewAction(action);
        }

        [Action("createMultGroup:")]
        void CreateMultGroup(NSObject sender)
        {
            MultGroupToEdit = null;
            PerformSegue("MultGroupSegue", this);
        }

        [Action("editMultGroup:")]
        void EditMultGroup(NSObject sender)
        {
			PerformSegue("MultGroupSegue", this);
		}

        [Action("removeMultGroup:")]
        void RemoveMultGroup(NSObject sender)
        {
            var action = new RemoveMultGroup(DView.SelectedCells);

            InitNewAction(action);
        }

        /// <summary>
        /// Activate or deactivate mult group scaling
        /// </summary>
        /// <param name="sender">Sender.</param>
        [Action("multGroupDrawToScale:")]
        void MultGroupDrawToScaleToggle(NSMenuItem sender)
        {
            // toggle the setting
            UserSettings.GetSettings().DrawMultToScale = !UserSettings.GetSettings().DrawMultToScale;

            // see if view needs to be resized
            double beforeLength = 0;
            double afterLength = 0;
            // redraw rows that have mults
            foreach (Row row in DView.Rows)
            {
                if (row.Duration > beforeLength) beforeLength = row.Duration;

                if (row.MultGroups.Any())
                {
                    row.Redraw();

                    DView.QueueRowToDraw(row);
                }

                if (row.Duration > afterLength) afterLength = row.Duration;
            }
            // resize the canvas if necessary
            if (afterLength > beforeLength)
            {
                DView.ResizeFrame(afterLength);
            }
        }

        [Action("createRef:")]
        void CreateRef(NSObject sender)
        {
            RefToEdit = null;
            PerformSegue("ReferenceSegue", this);
        }

        [Action("editRef:")]
        void EditRef(NSObject sender)
        {
            PerformSegue("ReferenceSegue", this);
        }

        [Action("removeRef:")]
        void RemoveRef(NSObject sender)
        {
            var action = new RemoveReference(DView.SelectedCells.Root.Cell);

            InitNewAction(action);
        }

        [Action("moveCellsLeft:")]
        void MoveCellsLeft(NSObject sender)
        {
            var action = new MoveCells(DView.SelectedCells.ToArray(), DView.GridSpacingString, -1);

            InitNewAction(action);
        }

        [Action("moveCellsRight:")]
        void MoveCellsRight(NSObject sender)
        {
            var action = new MoveCells(DView.SelectedCells.ToArray(), DView.GridSpacingString, 1);

            InitNewAction(action);
        }

        [Action("removeCells:")]
        void RemoveCells(NSObject sender)
        {
            var action = new RemoveCells(DView.SelectedCells);

            InitNewAction(action);
        }
        #endregion

        #region Overrides
        public override void PrepareForSegue(NSStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);

            switch (segue.Identifier)
            {
                case "RepeatGroupSegue":
                    SheetIsOpen = true;

                    var sheet = segue.DestinationController as RepeatGroupDialog;
                    sheet.Presentor = this;

                    //int oldTimes = 2;
                    //string oldLtm = "";
                    if (RepGroupToEdit != null)
                    {
                        sheet.Ltm = RepGroupToEdit.LastTermModifier;
                        sheet.Times = RepGroupToEdit.Times;
                    }
                    else
                    {
                        // pass in the group that will be configured
                        sheet.Group = new Repeat() { Times = 2, LastTermModifier = "" };
                    }

                    sheet.Accepted += (s, e) => {
						AbstractBeatCodeAction action;

                        if (RepGroupToEdit != null)
                        {
                            action = new EditRepeatGroup(RepGroupToEdit, sheet.Ltm, (int)sheet.Times);
                        }
                        else
                        {
							// adding the new group
							action = new AddRepeatGroup(sheet.Group, DView.SelectedCells);
                        }

						InitNewAction(action);

                        SheetIsOpen = false;
                    };

                    sheet.Canceled += (s, e) => {
                        SheetIsOpen = false;
                    };

                    sheet.Dispose();
                    break;
                case "MultGroupSegue":
                    SheetIsOpen = true;

                    var multSheet = segue.DestinationController as MultGroupDialog;
                    multSheet.Presentor = this;

                    if (MultGroupToEdit != null)
                    {
                        multSheet.Factor = MultGroupToEdit.FactorValue;
                    }
                    else
                    {
                        multSheet.Group = new Multiply() { FactorValue = "1" };
                    }

                    multSheet.Accepted += (s, e) => {
                        AbstractBeatCodeAction action;

                        if (MultGroupToEdit != null)
                        {
                            // edit
                            action = new EditMultGroup(MultGroupToEdit, multSheet.Factor);
                        }
                        else
                        {
                            // create new
                            action = new AddMultGroup(multSheet.Group, DView.SelectedCells);
                        }

                        InitNewAction(action);

                        SheetIsOpen = false;
                    };

                    multSheet.Canceled += (s, e) => {
                        SheetIsOpen = false;
                    };
                    break;
                case "ReferenceSegue":
                    SheetIsOpen = true;

                    var refSheet = segue.DestinationController as ReferenceDialog;
                    refSheet.Presentor = this;

                    if (RefToEdit != null)
                    {
                        refSheet.Index = RefToEdit.Reference;
                    }

                    refSheet.Accepted += (s, e) => {
                        AbstractBeatCodeAction action;

                        if (RefToEdit != null)
                        {
                            action = new AddEditReference(RefToEdit, refSheet.Index);
                        }
                        else
                        {
                            action = new AddEditReference(DView.SelectedCells.Root.Cell, refSheet.Index);
                        }

                        InitNewAction(action);

                        SheetIsOpen = false;
                    };

                    refSheet.Canceled += (s, e) => {
                        SheetIsOpen = false;
                    };

                    break;
            }
        }

        public override void AwakeFromNib()
        {
            SourceSelector.DataSource = new SourceSelectorDataSource(SourceSelector);
			SourceSelector.VisibleItems = 10;

			// autoselect the first source
			SourceSelector.StringValue =
				(NSString)SourceSelector.DataSource.ObjectValueForItem(SourceSelector, 0);

			// Epand the selector to show full items, not truncated
			var cell = SourceSelector.Cell;
			var frame = SourceSelector.Frame;
			bool open = false;
			SourceSelector.WillPopUp += (sender, e) => {
				if (!open)
				{
					SourceSelector.SetFrameSize(new CoreGraphics.CGSize(220, 23));
					// force it to close then reopen to apply frame size to list
					cell.AccessibilityExpanded = false;
					cell.AccessibilityExpanded = true;
					open = true;
				}
			};

			SourceSelector.WillDismiss += (sender, e) => {
				SourceSelector.Frame = frame;
				open = false;
			};
        }

        public override void ViewWillDisappear()
        {
            base.ViewWillDisappear();

            Metronome.Instance.BeatChanged -= Instance_BeatChanged;
        }
		
        public override void ViewWillAppear()
        {
            base.ViewWillAppear();

			Metronome.Instance.BeatChanged -= Instance_BeatChanged;
			Metronome.Instance.BeatChanged += Instance_BeatChanged;
        }
        #endregion

        void Instance_BeatChanged(object sender, EventArgs e)
        {
            if (!JustAppliedChanges)
            {
				DView.DeselectCells();
				DView.InitRows();
				UndoStack.Clear();
            }
            else
                JustAppliedChanges = false;
        }
    }
}
