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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class WpfHexViewImpl : WpfHexView {
		public override ICommandTargetCollection CommandTarget => RegisteredCommandElement.CommandTarget;
		IRegisteredCommandElement RegisteredCommandElement { get; }
		public override Brush Background { get; set; }//TODO:
		public override FrameworkElement VisualElement { get; }//TODO:
		public override double ZoomLevel { get; set; }//TODO:
		public override event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;//TODO:
		public override event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;//TODO:
		public override HexCaret Caret { get; }//TODO:
		public override HexSelection Selection { get; }//TODO:
		public override HexBufferSpan? ProvisionalTextHighlight { get; set; }//TODO:
		public override bool HasAggregateFocus { get; }//TODO:
		public override bool IsMouseOverViewOrAdornments { get; }//TODO:
		public override double LineHeight { get; }//TODO:
		public override bool InLayout { get; }//TODO:
		public override bool IsClosed => isClosed;
		public override IEditorOptions Options { get; }
		public override ITextViewRoleSet Roles { get; }
		public override HexBuffer HexBuffer { get; }
		public override double ViewportTop { get; }//TODO:
		public override double ViewportBottom { get; }//TODO:
		public override double ViewportLeft { get; set; }//TODO:
		public override double ViewportRight { get; }//TODO:
		public override double ViewportWidth { get; }//TODO:
		public override double ViewportHeight { get; }//TODO:
		public override HexViewScroller ViewScroller { get; }//TODO:
		public override HexFormattedLineSource FormattedLineSource { get; }//TODO:
		public override HexLineTransformSource LineTransformSource { get; }//TODO:
		public override HexViewLineCollection HexViewLines => WpfHexViewLines;
		public override WpfHexViewLineCollection WpfHexViewLines { get; }//TODO:
		public override event EventHandler Closed;//TODO:
		public override event EventHandler GotAggregateFocus;//TODO:
		public override event EventHandler LostAggregateFocus;//TODO:
		public override event EventHandler<HexViewLayoutChangedEventArgs> LayoutChanged;//TODO:
		public override event EventHandler ViewportHeightChanged;//TODO:
		public override event EventHandler ViewportWidthChanged;//TODO:
		public override event EventHandler ViewportLeftChanged;//TODO:
		public override event EventHandler<HexMouseHoverEventArgs> MouseHover;//TODO:

		readonly FormattedHexSourceFactoryService formattedHexSourceFactoryService;

		public WpfHexViewImpl(HexBuffer hexBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions, HexEditorOptionsFactoryService hexEditorOptionsFactoryService, ICommandService commandService, FormattedHexSourceFactoryService formattedHexSourceFactoryService) {
			if (hexBuffer == null)
				throw new ArgumentNullException(nameof(hexBuffer));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			if (hexEditorOptionsFactoryService == null)
				throw new ArgumentNullException(nameof(hexEditorOptionsFactoryService));
			if (commandService == null)
				throw new ArgumentNullException(nameof(commandService));
			if (formattedHexSourceFactoryService == null)
				throw new ArgumentNullException(nameof(formattedHexSourceFactoryService));
			this.formattedHexSourceFactoryService = formattedHexSourceFactoryService;
			HexBuffer = hexBuffer;
			Roles = roles;
			Options = hexEditorOptionsFactoryService.GetOptions(this);
			Options.Parent = parentOptions;

			if (Roles.Contains(PredefinedHexViewRoles.Interactive))
				RegisteredCommandElement = commandService.Register(VisualElement, this);
			else
				RegisteredCommandElement = Text.Editor.NullRegisteredCommandElement.Instance;
		}

		bool isClosed;
		public override void Close() {
			if (isClosed)
				throw new InvalidOperationException();
			isClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
		}

		public override HexAdornmentLayer GetAdornmentLayer(string name) {
			throw new NotSupportedException();//TODO:
		}

		public override HexSpaceReservationManager GetSpaceReservationManager(string name) {
			throw new NotSupportedException();//TODO:
		}

		public override void DisplayHexLineContainingBufferPosition(HexBufferPoint bufferPosition, double verticalDistance, HexViewRelativePosition relativeTo) =>
			DisplayHexLineContainingBufferPosition(bufferPosition, verticalDistance, relativeTo, null, null);

		public override void DisplayHexLineContainingBufferPosition(HexBufferPoint bufferPosition, double verticalDistance, HexViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) {
			throw new NotSupportedException();//TODO:
		}

		public override HexViewLine GetHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition) =>
			GetWpfHexViewLineContainingBufferPosition(bufferPosition);

		public override WpfHexViewLine GetWpfHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition) {
			throw new NotSupportedException();//TODO:
		}

		public override void QueueSpaceReservationStackRefresh() {
			//TODO:
		}
	}
}
