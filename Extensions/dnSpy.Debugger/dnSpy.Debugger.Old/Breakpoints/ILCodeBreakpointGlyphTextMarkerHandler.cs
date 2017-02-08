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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Debugger.Old.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints {
	//[Export(typeof(ILCodeBreakpointGlyphTextMarkerHandler))]
	sealed class ILCodeBreakpointGlyphTextMarkerHandler : IGlyphTextMarkerHandler {
		readonly IDecompilerService decompilerService;

		[ImportingConstructor]
		ILCodeBreakpointGlyphTextMarkerHandler(IDecompilerService decompilerService) {
			this.decompilerService = decompilerService;
		}

		IGlyphTextMarkerHandlerMouseProcessor IGlyphTextMarkerHandler.MouseProcessor => null;

		FrameworkElement IGlyphTextMarkerHandler.GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) {
			return null;//TODO: Return debugger settings toolbar
		}

		GlyphTextMarkerToolTip IGlyphTextMarkerHandler.GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) {
			var ilbp = GlyphTextMarkerHelper.ToILCodeBreakpoint(marker);
			return new GlyphTextMarkerToolTip(GetToolTipContent(ilbp, context.TextView));
		}

		string GetToolTipContent(ILCodeBreakpoint ilbp, ITextView textView) {
			var documentViewer = textView.TextBuffer.TryGetDocumentViewer();
			Debug.Assert(documentViewer != null);
			var statement = documentViewer?.GetMethodDebugService().FindByCodeOffset(ilbp.MethodToken, ilbp.ILOffset);
			Debug.Assert((documentViewer != null) == (statement != null));
			ITextSnapshotLine snapshotLine = null;
			if (statement != null && statement.Value.Statement.TextSpan.End <= textView.TextSnapshot.Length)
				snapshotLine = textView.TextSnapshot.GetLineFromPosition(statement.Value.Statement.TextSpan.Start);

			var sb = new StringBuilder();
			sb.Append(dnSpy_Debugger_Resources.GlyphToolTip_Location);
			sb.Append(": ");
			if (snapshotLine != null) {
				sb.Append(string.Format(dnSpy_Debugger_Resources.GlyphToolTip_line_0_character_1,
					(snapshotLine.LineNumber + 1).ToString(CultureInfo.CurrentUICulture),
					(statement.Value.Statement.TextSpan.Start - snapshotLine.Start + 1).ToString(CultureInfo.CurrentUICulture)));
				sb.Append(" ");
			}
			sb.Append(string.Format(dnSpy_Debugger_Resources.GlyphToolTip_IL_offset_0, ilbp.ILOffset.ToString("X4")));

			if (statement != null) {
				sb.Append(" ('");
				var decompiler = (documentViewer?.DocumentTab.Content as IDecompilerTabContent)?.Decompiler ?? decompilerService.Decompiler;
				decompiler.Write(new StringBuilderTextColorOutput(sb), statement.Value.Method, SimplePrinterFlags.Default);
				sb.Append("')");
			}

			return sb.ToString();
		}

		IEnumerable<GuidObject> IGlyphTextMarkerHandler.GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint) {
			var ilbp = GlyphTextMarkerHelper.ToILCodeBreakpoint(marker);
			yield return new GuidObject(MenuConstants.GUIDOBJ_BREAKPOINT_GUID, ilbp);
		}
	}
}
