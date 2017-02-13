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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex.Editor;
using VST = Microsoft.VisualStudio.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;
using VSTP = Microsoft.VisualStudio.Text.Projection;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Classification {
	sealed class HexTextView : VSTE.IWpfTextView {
		readonly HexView hexView;

		public HexTextView(HexView hexView) {
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			hexView.Closed += HexView_Closed;
		}

		void HexView_Closed(object sender, EventArgs e) {
			hexView.Closed -= HexView_Closed;
			Closed?.Invoke(this, EventArgs.Empty);
			hexView.Properties.RemoveProperty(typeof(HexTextView));
		}

		public static HexTextView GetOrCreate(HexView hexView) =>
			hexView.Properties.GetOrCreateSingletonProperty(typeof(HexTextView), () => new HexTextView(hexView));

		bool VSTE.ITextView.IsClosed => hexView.IsClosed;
		VSTE.IEditorOptions VSTE.ITextView.Options => hexView.Options;
		VSUTIL.PropertyCollection VSUTIL.IPropertyOwner.Properties => hexView.Properties;
		public event EventHandler Closed;

		Brush VSTE.IWpfTextView.Background {
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		VSTP.IBufferGraph VSTE.ITextView.BufferGraph {
			get { throw new NotImplementedException(); }
		}

		VSTE.ITextCaret VSTE.ITextView.Caret {
			get { throw new NotImplementedException(); }
		}

		VSTF.IFormattedLineSource VSTE.IWpfTextView.FormattedLineSource {
			get { throw new NotImplementedException(); }
		}

		bool VSTE.ITextView.HasAggregateFocus {
			get { throw new NotImplementedException(); }
		}

		bool VSTE.ITextView.InLayout {
			get { throw new NotImplementedException(); }
		}

		bool VSTE.ITextView.IsMouseOverViewOrAdornments {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.LineHeight {
			get { throw new NotImplementedException(); }
		}

		VSTF.ILineTransformSource VSTE.IWpfTextView.LineTransformSource {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.MaxTextRightCoordinate {
			get { throw new NotImplementedException(); }
		}

		VST.ITrackingSpan VSTE.ITextView.ProvisionalTextHighlight {
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		VSTE.ITextViewRoleSet VSTE.ITextView.Roles {
			get { throw new NotImplementedException(); }
		}

		VSTE.ITextSelection VSTE.ITextView.Selection {
			get { throw new NotImplementedException(); }
		}

		VST.ITextBuffer VSTE.ITextView.TextBuffer {
			get { throw new NotImplementedException(); }
		}

		VST.ITextDataModel VSTE.ITextView.TextDataModel {
			get { throw new NotImplementedException(); }
		}

		VST.ITextSnapshot VSTE.ITextView.TextSnapshot {
			get { throw new NotImplementedException(); }
		}

		VSTE.ITextViewLineCollection VSTE.ITextView.TextViewLines {
			get { throw new NotImplementedException(); }
		}

		VSTE.IWpfTextViewLineCollection VSTE.IWpfTextView.TextViewLines {
			get { throw new NotImplementedException(); }
		}

		VSTE.ITextViewModel VSTE.ITextView.TextViewModel {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.ViewportBottom {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.ViewportHeight {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.ViewportLeft {
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.ViewportRight {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.ViewportTop {
			get { throw new NotImplementedException(); }
		}

		double VSTE.ITextView.ViewportWidth {
			get { throw new NotImplementedException(); }
		}

		VSTE.IViewScroller VSTE.ITextView.ViewScroller {
			get { throw new NotImplementedException(); }
		}

		FrameworkElement VSTE.IWpfTextView.VisualElement {
			get { throw new NotImplementedException(); }
		}

		VST.ITextSnapshot VSTE.ITextView.VisualSnapshot {
			get { throw new NotImplementedException(); }
		}

		double VSTE.IWpfTextView.ZoomLevel {
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		event EventHandler<VSTE.BackgroundBrushChangedEventArgs> VSTE.IWpfTextView.BackgroundBrushChanged {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler VSTE.ITextView.GotAggregateFocus {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler<VSTE.TextViewLayoutChangedEventArgs> VSTE.ITextView.LayoutChanged {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler VSTE.ITextView.LostAggregateFocus {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler<VSTE.MouseHoverEventArgs> VSTE.ITextView.MouseHover {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler VSTE.ITextView.ViewportHeightChanged {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler VSTE.ITextView.ViewportLeftChanged {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler VSTE.ITextView.ViewportWidthChanged {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		event EventHandler<VSTE.ZoomLevelChangedEventArgs> VSTE.IWpfTextView.ZoomLevelChanged {
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		void VSTE.ITextView.Close() {
			throw new NotImplementedException();
		}

		void VSTE.ITextView.DisplayTextLineContainingBufferPosition(VST.SnapshotPoint bufferPosition, double verticalDistance, VSTE.ViewRelativePosition relativeTo) {
			throw new NotImplementedException();
		}

		void VSTE.ITextView.DisplayTextLineContainingBufferPosition(VST.SnapshotPoint bufferPosition, double verticalDistance, VSTE.ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) {
			throw new NotImplementedException();
		}

		VSTE.IAdornmentLayer VSTE.IWpfTextView.GetAdornmentLayer(string name) {
			throw new NotImplementedException();
		}

		VSTE.ISpaceReservationManager VSTE.IWpfTextView.GetSpaceReservationManager(string name) {
			throw new NotImplementedException();
		}

		VST.SnapshotSpan VSTE.ITextView.GetTextElementSpan(VST.SnapshotPoint point) {
			throw new NotImplementedException();
		}

		VSTF.ITextViewLine VSTE.ITextView.GetTextViewLineContainingBufferPosition(VST.SnapshotPoint bufferPosition) {
			throw new NotImplementedException();
		}

		VSTF.IWpfTextViewLine VSTE.IWpfTextView.GetTextViewLineContainingBufferPosition(VST.SnapshotPoint bufferPosition) {
			throw new NotImplementedException();
		}

		void VSTE.ITextView.QueueSpaceReservationStackRefresh() {
			throw new NotImplementedException();
		}
	}
}
