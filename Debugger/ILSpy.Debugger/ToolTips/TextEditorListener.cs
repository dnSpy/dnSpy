// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.NRefactory.CSharp;
using ILSpy.Debugger.AvalonEdit;
using ILSpy.Debugger.Services;

namespace ILSpy.Debugger.ToolTips
{
	/// <summary>
	/// Description of TextEditorListener.
	/// </summary>
	public class TextEditorListener : IWeakEventListener 
	{
		private static readonly TextEditorListener instance;
		
		static TextEditorListener() 
		{
			instance = new TextEditorListener();
		}
		
		private TextEditorListener() { }
		
		Popup popup;
		ToolTip toolTip;
		TextEditor editor;
		
		public static TextEditorListener Instance {
			get { return instance; }
		}
		
		public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(ILSpy.Debugger.AvalonEdit.TextEditorWeakEventManager.MouseHover)) {
				editor = (TextEditor)sender;
				OnMouseHover((MouseEventArgs)e);
			}
			
			if (managerType == typeof(ILSpy.Debugger.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped)) {
				OnMouseHoverStopped((MouseEventArgs)e);
			}
			
			if (managerType == typeof(ILSpy.Debugger.AvalonEdit.TextEditorWeakEventManager.MouseDown)) {
				OnMouseDown((MouseEventArgs)e);
			}
			
			return true;
		}
		
		void OnMouseDown(MouseEventArgs mouseEventArgs0)
		{
			if (toolTip != null)
				toolTip.IsOpen = false;
			
			TryCloseExistingPopup(true);
			
			if (popup != null)
				popup.IsOpen = false;
		}
		
		void OnMouseHoverStopped(MouseEventArgs e)
		{			
			if (toolTip != null)
				toolTip.IsOpen = false;
		}
		
		void OnMouseHover(MouseEventArgs e)
		{
			ToolTipRequestEventArgs args = new ToolTipRequestEventArgs(editor);			
			var pos = editor.GetPositionFromPoint(e.GetPosition(editor));
			args.InDocument = pos.HasValue;
			
			if (pos.HasValue) {
				args.LogicalPosition = new AstLocation(pos.Value.Line, pos.Value.Column);
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
				} else {
					if (toolTip == null) {
						toolTip = new ToolTip();
						toolTip.Closed += delegate { toolTip = null; };
					}
					toolTip.PlacementTarget = editor; // required for property inheritance
					
					if(args.ContentToShow is string) {
						toolTip.Content = new TextBlock
						{
							Text = (args.ContentToShow as string),
							TextWrapping = TextWrapping.Wrap
						};
					}
					else
						toolTip.Content = args.ContentToShow;
					
					toolTip.IsOpen = true;
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
				positionInPixels =
					textView.PointToScreen(
						textView.GetVisualPosition(logicalPos.Value, VisualYPosition.LineBottom) - textView.ScrollOffset);
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
			popup.PlacementTarget = editor; // required for property inheritance
			popup.Placement = PlacementMode.Absolute;
			popup.StaysOpen = true;
			return popup;
		}
		
		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			return ReceiveWeakEvent(managerType, sender, e);
		}
	}
}
