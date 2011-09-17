// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Debugger.Services;

namespace ICSharpCode.ILSpy.Debugger.Tooltips
{
	[Export(typeof(ITextEditorListener))]
	public class TextEditorListener : IWeakEventListener, ITextEditorListener
	{
		public TextEditorListener()
		{
		}

		Popup popup;
		TextEditor editor;

		public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHover)) {
				editor = (TextEditor)sender;
				OnMouseHover((MouseEventArgs)e);
			}

			if (managerType == typeof(ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped)) {
				OnMouseHoverStopped((MouseEventArgs)e);
			}

			if (managerType == typeof(ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseDown)) {
				OnMouseDown((MouseEventArgs)e);
			}

			return true;
		}

		void OnMouseDown(MouseEventArgs e)
		{
			ClosePopup();
		}

		void OnMouseHoverStopped(MouseEventArgs e)
		{

		}

		void OnMouseHover(MouseEventArgs e)
		{
			ToolTipRequestEventArgs args = new ToolTipRequestEventArgs(editor);
			var pos = editor.GetPositionFromPoint(e.GetPosition(editor));
			args.InDocument = pos.HasValue;

			if (pos.HasValue) {
				args.LogicalPosition = new TextLocation(pos.Value.Line, pos.Value.Column);
			} else {
				return;
			}

			DebuggerService.HandleToolTipRequest(args);

			if (args.ContentToShow != null) {
				var contentToShowITooltip = args.ContentToShow as ITooltip;

				if (contentToShowITooltip != null && contentToShowITooltip.ShowAsPopup) {
					if (!(args.ContentToShow is UIElement)) {
						throw new NotSupportedException("Content to show in Popup must be UIElement: " + args.ContentToShow);
					}
					if (popup == null) {
						popup = CreatePopup();
					}
					if (TryCloseExistingPopup(false)) {
						// when popup content decides to close, close the popup
						contentToShowITooltip.Closed += delegate { popup.IsOpen = false; };
						popup.Child = (UIElement)args.ContentToShow;
						//ICSharpCode.SharpDevelop.Debugging.DebuggerService.CurrentDebugger.IsProcessRunningChanged
						SetPopupPosition(popup, e);
						popup.IsOpen = true;
					}
					e.Handled = true;
				}
			} else {
				// close popup if mouse hovered over empty area
				if (popup != null) {
					e.Handled = true;
				}
				TryCloseExistingPopup(false);
			}
		}

		bool TryCloseExistingPopup(bool mouseClick)
		{
			bool canClose = true;
			if (popup != null) {
				var popupContentITooltip = popup.Child as ITooltip;
				if (popupContentITooltip != null) {
					canClose = popupContentITooltip.Close(mouseClick);
				}
				if (canClose) {
					popup.IsOpen = false;
				}
			}
			return canClose;
		}

		void SetPopupPosition(Popup popup, MouseEventArgs mouseArgs)
		{
			var popupPosition = GetPopupPosition(mouseArgs);
			popup.HorizontalOffset = popupPosition.X;
			popup.VerticalOffset = popupPosition.Y;
		}

		/// <summary> Returns Popup position based on mouse position, in device independent units </summary>
		Point GetPopupPosition(MouseEventArgs mouseArgs)
		{
			Point mousePos = mouseArgs.GetPosition(editor);
			Point positionInPixels;
			// align Popup with line bottom
			TextViewPosition? logicalPos = editor.GetPositionFromPoint(mousePos);
			if (logicalPos.HasValue) {
				var textView = editor.TextArea.TextView;
				positionInPixels = textView.PointToScreen(textView.GetVisualPosition(logicalPos.Value, VisualYPosition.LineBottom) - textView.ScrollOffset);
				positionInPixels.X -= 4;
			} else {
				positionInPixels = editor.PointToScreen(mousePos + new Vector(-4, 6));
			}
			// use device independent units, because Popup Left/Top are in independent units
			return positionInPixels.TransformFromDevice(editor);
		}

		Popup CreatePopup()
		{
			popup = new Popup();
			popup.Closed += delegate { popup = null; };
			popup.AllowsTransparency = true;
			popup.PlacementTarget = editor;
			// required for property inheritance
			popup.Placement = PlacementMode.Absolute;
			popup.StaysOpen = true;
			return popup;
		}

		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			return ReceiveWeakEvent(managerType, sender, e);
		}

		public void ClosePopup()
		{
			TryCloseExistingPopup(true);

			if (popup != null)
				popup.IsOpen = false;
		}
	}
}
