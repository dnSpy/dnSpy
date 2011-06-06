// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.Debugger.Models.TreeModel;

namespace ICSharpCode.ILSpy.Debugger.Tooltips
{
	/// <summary>
	/// Popup containing <see cref="DebuggerTooltipControl"></see>.
	/// </summary>
	internal class DebuggerPopup : Popup
	{
		internal DebuggerTooltipControl contentControl;

		public DebuggerPopup(DebuggerTooltipControl parentControl, AstLocation logicalPosition, bool showPins = true)
		{
			this.contentControl = new DebuggerTooltipControl(parentControl, logicalPosition, showPins);
			this.contentControl.containingPopup = this;
			this.Child = this.contentControl;
			this.IsLeaf = false;
			
			//this.KeyDown += new KeyEventHandler(DebuggerPopup_KeyDown);

			//this.contentControl.Focusable = true;
			//Keyboard.Focus(this.contentControl);
			//this.AllowsTransparency = true;
			//this.PopupAnimation = PopupAnimation.Slide;
		}

		// attempt to propagate shortcuts to main windows when Popup is focusable (needed for keyboard scrolling + editing)
		/*void DebuggerPopup_KeyDown(object sender, KeyEventArgs e)
		{
			LoggingService.Debug("Unhandled popup key down: " + e.Key);
			RaiseEventPair(WorkbenchSingleton.MainWindow, PreviewKeyDownEvent, KeyDownEvent,
			                           new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key));
		}
		
		// copied from CompletionWindowBase
		static bool RaiseEventPair(UIElement target, RoutedEvent previewEvent, RoutedEvent @event, RoutedEventArgs args)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			if (previewEvent == null)
				throw new ArgumentNullException("previewEvent");
			if (@event == null)
				throw new ArgumentNullException("event");
			if (args == null)
				throw new ArgumentNullException("args");
			args.RoutedEvent = previewEvent;
			target.RaiseEvent(args);
			args.RoutedEvent = @event;
			target.RaiseEvent(args);
			return args.Handled;
		}*/

		public IEnumerable<ITreeNode> ItemsSource
		{
			get { return this.contentControl.ItemsSource; }
			set { this.contentControl.SetItemsSource(value); }
		}

		private bool isLeaf;
		public bool IsLeaf
		{
			get { return isLeaf; }
			set
			{
				isLeaf = value;
				// leaf popup closes on lost focus
				this.StaysOpen = !isLeaf;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (isLeaf) {
				this.contentControl.CloseOnLostFocus();
			}
		}

		public void Open()
		{
			this.IsOpen = true;
		}

		public void CloseSelfAndChildren()
		{
			this.contentControl.CloseChildPopups();
			this.IsOpen = false;
		}
	}
}