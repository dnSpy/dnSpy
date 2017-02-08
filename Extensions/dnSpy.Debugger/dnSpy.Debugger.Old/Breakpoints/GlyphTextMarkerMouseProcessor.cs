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
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.Breakpoints {
	[Export(typeof(IGlyphTextMarkerMouseProcessorProvider))]
	[Name(PredefinedDsGlyphTextMarkerMouseProcessorProviderNames.DebuggerBreakpoints)]
	[TextViewRole(PredefinedTextViewRoles.Debuggable)]
	sealed class GlyphTextMarkerMouseProcessorProvider : IGlyphTextMarkerMouseProcessorProvider {
		readonly Lazy<IBreakpointService> breakpointService;

		[ImportingConstructor]
		GlyphTextMarkerMouseProcessorProvider(Lazy<IBreakpointService> breakpointService) {
			this.breakpointService = breakpointService;
		}

		public IGlyphTextMarkerMouseProcessor GetAssociatedMouseProcessor(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin) =>
			new GlyphTextMarkerMouseProcessor(wpfTextViewHost, breakpointService);
	}

	sealed class GlyphTextMarkerMouseProcessor : GlyphTextMarkerMouseProcessorBase {
		readonly IWpfTextViewHost wpfTextViewHost;
		readonly Lazy<IBreakpointService> breakpointService;

		public GlyphTextMarkerMouseProcessor(IWpfTextViewHost wpfTextViewHost, Lazy<IBreakpointService> breakpointService) {
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (breakpointService == null)
				throw new ArgumentNullException(nameof(breakpointService));
			this.wpfTextViewHost = wpfTextViewHost;
			this.breakpointService = breakpointService;
			wpfTextViewHost.TextView.Closed += TextView_Closed;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		WeakReference leftButtonDownLineIdentityTagWeakReference;

		void ClearPressedLine() => leftButtonDownLineIdentityTagWeakReference = null;

		public override void OnMouseLeftButtonDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) =>
			leftButtonDownLineIdentityTagWeakReference = new WeakReference(context.Line.IdentityTag);

		public override void OnMouseLeftButtonUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) {
			bool sameLine = leftButtonDownLineIdentityTagWeakReference?.Target == context.Line.IdentityTag;
			leftButtonDownLineIdentityTagWeakReference = null;

			if (sameLine) {
				e.Handled = true;
				var documentViewer = wpfTextViewHost.TextView.TextBuffer.TryGetDocumentViewer();
				if (documentViewer != null)
					breakpointService.Value.Toggle(documentViewer, context.Line.Start.Position);
			}
		}

		public override void OnMouseEnter(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) => ClearPressedLine();
		public override void OnMouseLeave(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) => ClearPressedLine();
		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) => ClearPressedLine();

		void TextView_Closed(object sender, EventArgs e) {
			ClearPressedLine();
			wpfTextViewHost.TextView.Closed -= TextView_Closed;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}
	}
}
