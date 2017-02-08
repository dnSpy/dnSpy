/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Operations;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.Left)]
	[VSUTIL.Name(PredefinedHexMarginNames.LeftSelection)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	sealed class LeftSelectionMarginProvider : WpfHexViewMarginProvider {
		readonly WpfHexViewMarginProviderCollectionProvider wpfHexViewMarginProviderCollectionProvider;
		readonly HexEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		LeftSelectionMarginProvider(WpfHexViewMarginProviderCollectionProvider wpfHexViewMarginProviderCollectionProvider, HexEditorOperationsFactoryService editorOperationsFactoryService) {
			this.wpfHexViewMarginProviderCollectionProvider = wpfHexViewMarginProviderCollectionProvider;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new LeftSelectionMargin(wpfHexViewMarginProviderCollectionProvider, wpfHexViewHost, editorOperationsFactoryService.GetEditorOperations(wpfHexViewHost.HexView));
	}

	sealed class LeftSelectionMargin : WpfHexViewContainerMargin {
		readonly WpfHexViewHost wpfHexViewHost;
		readonly HexEditorOperations editorOperations;

		public LeftSelectionMargin(WpfHexViewMarginProviderCollectionProvider wpfHexViewMarginProviderCollectionProvider, WpfHexViewHost wpfHexViewHost, HexEditorOperations editorOperations)
			: base(wpfHexViewMarginProviderCollectionProvider, wpfHexViewHost, PredefinedHexMarginNames.LeftSelection, false) {
			if (editorOperations == null)
				throw new ArgumentNullException(nameof(editorOperations));
			VisualElement.Cursor = Cursors.Arrow;//TODO: Use an arrow pointing to the right
			this.wpfHexViewHost = wpfHexViewHost;
			this.editorOperations = editorOperations;
			wpfHexViewHost.HexView.ZoomLevelChanged += HexView_ZoomLevelChanged;
			// Make sure that the user can click anywhere in this margin so we'll get mouse events
			Grid.Background = Brushes.Transparent;
			VisualElement.MouseLeftButtonDown += VisualElement_MouseLeftButtonDown;
			VisualElement.MouseLeftButtonUp += VisualElement_MouseLeftButtonUp;
			VisualElement.MouseMove += VisualElement_MouseMove;
		}

		void HexView_ZoomLevelChanged(object sender, VSTE.ZoomLevelChangedEventArgs e) => VisualElement.LayoutTransform = e.ZoomTransform;

		protected override void DisposeCore() {
			wpfHexViewHost.HexView.ZoomLevelChanged -= HexView_ZoomLevelChanged;
			VisualElement.MouseLeftButtonDown -= VisualElement_MouseLeftButtonDown;
			VisualElement.MouseLeftButtonUp -= VisualElement_MouseLeftButtonUp;
			VisualElement.MouseMove -= VisualElement_MouseMove;
			base.DisposeCore();
		}

		void VisualElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (VisualElement.CaptureMouse()) {
				var line = HexMouseLocation.Create(wpfHexViewHost.HexView, e, insertionPosition: false).HexViewLine;
				editorOperations.SelectLine(line, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
				mouseCaptured = true;
				e.Handled = true;
				return;
			}
		}
		bool mouseCaptured;

		void VisualElement_MouseMove(object sender, MouseEventArgs e) {
			if (mouseCaptured) {
				var mouseLoc = HexMouseLocation.Create(wpfHexViewHost.HexView, e, insertionPosition: false);
				var line = mouseLoc.HexViewLine;
				editorOperations.SelectLine(line, true);
				// Needed or the scrolling will stop
				if (mouseLoc.Point.Y <= wpfHexViewHost.HexView.ViewportTop)
					editorOperations.ScrollUpAndMoveCaretIfNecessary();
				else if (mouseLoc.Point.Y >= wpfHexViewHost.HexView.ViewportBottom) {
					var lastVisLine = wpfHexViewHost.HexView.HexViewLines.LastVisibleLine;
					if (!lastVisLine.IsLastDocumentLine() || lastVisLine.VisibilityState != VSTF.VisibilityState.FullyVisible)
						editorOperations.ScrollDownAndMoveCaretIfNecessary();
				}
				e.Handled = true;
				return;
			}
		}

		void VisualElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (mouseCaptured) {
				mouseCaptured = false;
				VisualElement.ReleaseMouseCapture();
				e.Handled = true;
				return;
			}
		}
	}
}
