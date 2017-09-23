// This file has been autogenerated from a class added in the UI designer.

using System;
using CoreGraphics;
using Foundation;
using AppKit;
using Pronome.Mac.Editor;
using System.Collections.Generic;
using Pronome.Mac.Editor.Action;
using System.Linq;

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
            action.Redo();
            UndoStack.Push(action);
            RedoStack.Clear();
        }
        #endregion

        #region Actions
        /// <summary>
        /// Applies changes that were made to the beat.
        /// </summary>
        /// <param name="sender">Sender.</param>
        partial void ApplyChangesAction(NSObject sender)
        {
            DView.ChangesApplied = true;
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

            switch(actionName)
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
                    break;
                case "editRepGroup:":
                    break;
                case "removeRepGroup:":
                    break;
                case "createMultGroup:":
                    break;
                case "multGroupDrawToScale:":
                    // assign state based on user setting
                    item.State = UserSettings.GetSettings().DrawMultToScale 
                        ? NSCellStateValue.On 
                        : NSCellStateValue.Off;
                    break;
                case "editMultGroup:":
                    break;
                case "removeMultGroup:":
                    break;
                case "createRef:":
                    break;
                case "editRef:":
                    break;
                case "removeRef:":
                    break;
                case "moveCellsLeft:":
                    break;
                case "moveCellsRight:":
                    break;
                case "removeCells:":
                    return DView.SelectedCells.Root != null;
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

		}

		[Action("editRepGroup:")]
		void EditRepGroup(NSObject sender)
		{

		}

		[Action("removeRepGroup:")]
		void RemoveRepGroup(NSObject sender)
		{

		}

        [Action("createMultGroup:")]
        void CreateMultGroup(NSObject sender)
        {
            
        }

		[Action("editMultGroup:")]
		void EditMultGroup(NSObject sender)
		{

		}

		[Action("removeMultGroup:")]
		void RemoveMultGroup(NSObject sender)
		{

		}

        [Action("multGroupDrawToScale:")]
        void MultGroupDrawToScaleToggle(NSMenuItem sender)
        {
            // toggle the setting
            UserSettings.GetSettings().DrawMultToScale = !UserSettings.GetSettings().DrawMultToScale;

			// redraw rows that have mults
			foreach (Row row in DView.Rows.Where(x => x.MultGroups.Any()))
			{
				DView.QueueRowToDraw(row);
			}
        }

		[Action("createRef:")]
		void CreateRef(NSObject sender)
		{

		}

		[Action("editRef:")]
		void EditRef(NSObject sender)
		{

		}

		[Action("removeRef:")]
		void RemoveRef(NSObject sender)
		{

		}

		[Action("moveCellsLeft:")]
		void MoveCellsLeft(NSObject sender)
		{

		}

		[Action("moveCellsRight:")]
		void MoveCellsRight(NSObject sender)
		{

		}

		[Action("removeCells:")]
		void RemoveCells(NSObject sender)
		{
            var action = new RemoveCells(DView.SelectedCells);

            InitNewAction(action);
		}
        #endregion
    }
}
