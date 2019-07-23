/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	[Export(typeof(IGlyphTextMarkerMouseProcessorProvider))]
	[Name(PredefinedDsGlyphTextMarkerMouseProcessorProviderNames.DebuggerBreakpoints)]
	[TextViewRole(PredefinedTextViewRoles.Debuggable)]
	sealed class GlyphTextMarkerMouseProcessorProvider : IGlyphTextMarkerMouseProcessorProvider {
		readonly Lazy<TextViewBreakpointService> textViewBreakpointService;

		[ImportingConstructor]
		GlyphTextMarkerMouseProcessorProvider(Lazy<TextViewBreakpointService> textViewBreakpointService) => this.textViewBreakpointService = textViewBreakpointService;

		public IGlyphTextMarkerMouseProcessor? GetAssociatedMouseProcessor(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin) =>
			new GlyphTextMarkerMouseProcessor(wpfTextViewHost, textViewBreakpointService);
	}

	sealed class GlyphTextMarkerMouseProcessor : GlyphTextMarkerMouseProcessorBase {
		readonly IWpfTextViewHost wpfTextViewHost;
		readonly Lazy<TextViewBreakpointService> textViewBreakpointService;

		public GlyphTextMarkerMouseProcessor(IWpfTextViewHost wpfTextViewHost, Lazy<TextViewBreakpointService> textViewBreakpointService) {
			this.wpfTextViewHost = wpfTextViewHost ?? throw new ArgumentNullException(nameof(wpfTextViewHost));
			this.textViewBreakpointService = textViewBreakpointService ?? throw new ArgumentNullException(nameof(textViewBreakpointService));
			wpfTextViewHost.TextView.Closed += TextView_Closed;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		WeakReference? leftButtonDownLineIdentityTagWeakReference;

		void ClearPressedLine() => leftButtonDownLineIdentityTagWeakReference = null;

		public override void OnMouseLeftButtonDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) =>
			leftButtonDownLineIdentityTagWeakReference = new WeakReference(context.Line.IdentityTag);

		public override void OnMouseLeftButtonUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) {
			bool sameLine = leftButtonDownLineIdentityTagWeakReference?.Target == context.Line.IdentityTag;
			leftButtonDownLineIdentityTagWeakReference = null;

			if (sameLine) {
				e.Handled = true;
				textViewBreakpointService.Value.ToggleCreateBreakpoint(wpfTextViewHost.TextView, new VirtualSnapshotPoint(context.Line.Start));
			}
		}

		public override void OnMouseEnter(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) => ClearPressedLine();
		public override void OnMouseLeave(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) => ClearPressedLine();
		void TextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) => ClearPressedLine();

		void TextView_Closed(object? sender, EventArgs e) {
			ClearPressedLine();
			wpfTextViewHost.TextView.Closed -= TextView_Closed;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}
	}
}
