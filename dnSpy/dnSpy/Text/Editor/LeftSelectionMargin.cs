/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.Left)]
	[Name(PredefinedMarginNames.LeftSelection)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class LeftSelectionMarginProvider : IWpfTextViewMarginProvider {
		readonly IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		LeftSelectionMarginProvider(IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.wpfTextViewMarginProviderCollectionProvider = wpfTextViewMarginProviderCollectionProvider;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new LeftSelectionMargin(wpfTextViewMarginProviderCollectionProvider, wpfTextViewHost, editorOperationsFactoryService.GetEditorOperations(wpfTextViewHost.TextView));
	}

	sealed class LeftSelectionMargin : WpfTextViewContainerMargin {
		readonly IWpfTextViewHost wpfTextViewHost;
		readonly IEditorOperations editorOperations;

		public LeftSelectionMargin(IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, IWpfTextViewHost wpfTextViewHost, IEditorOperations editorOperations)
			: base(wpfTextViewMarginProviderCollectionProvider, wpfTextViewHost, PredefinedMarginNames.LeftSelection, false) {
			if (editorOperations == null)
				throw new ArgumentNullException(nameof(editorOperations));
			this.Cursor = Cursors.Arrow;//TODO: Use an arrow pointing to the right
			this.wpfTextViewHost = wpfTextViewHost;
			this.editorOperations = editorOperations;
			wpfTextViewHost.TextView.ZoomLevelChanged += TextView_ZoomLevelChanged;
			// Make sure that the user can click anywhere in this margin so we'll get mouse events
			this.Background = Brushes.Transparent;
		}

		void TextView_ZoomLevelChanged(object sender, ZoomLevelChangedEventArgs e) => LayoutTransform = e.ZoomTransform;
		protected override void DisposeInternal() => wpfTextViewHost.TextView.ZoomLevelChanged -= TextView_ZoomLevelChanged;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			if (CaptureMouse()) {
				var line = MouseLocation.Create(wpfTextViewHost.TextView, e, insertionPosition: false).TextViewLine;
				editorOperations.SelectLine(line, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
				mouseCaptured = true;
				e.Handled = true;
				return;
			}

			base.OnMouseLeftButtonDown(e);
		}
		bool mouseCaptured;

		protected override void OnMouseMove(MouseEventArgs e) {
			if (mouseCaptured) {
				var mouseLoc = MouseLocation.Create(wpfTextViewHost.TextView, e, insertionPosition: false);
				var line = mouseLoc.TextViewLine;
				editorOperations.SelectLine(line, true);
				// Needed or the scrolling will stop
				if (mouseLoc.Point.Y <= wpfTextViewHost.TextView.ViewportTop)
					editorOperations.ScrollUpAndMoveCaretIfNecessary();
				else if (mouseLoc.Point.Y >= wpfTextViewHost.TextView.ViewportBottom) {
					var lastVisLine = wpfTextViewHost.TextView.TextViewLines.LastVisibleLine;
					if (!lastVisLine.IsLastDocumentLine() || lastVisLine.VisibilityState != VisibilityState.FullyVisible)
						editorOperations.ScrollDownAndMoveCaretIfNecessary();
				}
				e.Handled = true;
				return;
			}

			base.OnMouseMove(e);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
			if (mouseCaptured) {
				mouseCaptured = false;
				ReleaseMouseCapture();
				e.Handled = true;
				return;
			}

			base.OnMouseLeftButtonUp(e);
		}
	}
}
