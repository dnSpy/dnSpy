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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.Code;
using dnSpy.Debugger.DotNet.CorDebug.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.DotNet.CorDebug.Breakpoints.TextEditor {
	[ExportDbgBreakpointGlyphFormatter]
	sealed class DbgBreakpointGlyphFormatterImpl : DbgBreakpointGlyphFormatter {
		readonly IDecompilerService decompilerService;

		[ImportingConstructor]
		DbgBreakpointGlyphFormatterImpl(IDecompilerService decompilerService) => this.decompilerService = decompilerService;

		public override bool WriteLocation(IDbgTextWriter output, DbgCodeBreakpoint breakpoint, ITextView textView, SnapshotSpan span) {
			if (breakpoint.Location is DbgDotNetNativeCodeLocationImpl location)
				return WriteLocation(output, textView, span, location);

			return false;
		}

		bool WriteLocation(IDbgTextWriter output, ITextView textView, SnapshotSpan span, DbgDotNetNativeCodeLocationImpl location) {
			var line = span.Start.GetContainingLine();
			output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.GlyphToolTip_line_0_character_1,
				(line.LineNumber + 1).ToString(CultureInfo.CurrentUICulture),
				(span.Start - line.Start + 1).ToString(CultureInfo.CurrentUICulture)));
			output.Write(DbgTextColor.Text, " ");
			switch (location.ILOffsetMapping) {
			case DbgILOffsetMapping.Exact:
			case DbgILOffsetMapping.Approximate:
				var prefix = location.ILOffsetMapping == DbgILOffsetMapping.Approximate ? "~0x" : "0x";
				output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.GlyphToolTip_IL_offset_0, prefix + location.Offset.ToString("X4")));
				break;

			case DbgILOffsetMapping.Prolog:
				output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.GlyphToolTip_IL_offset_0, "(prolog)"));
				break;

			case DbgILOffsetMapping.Epilog:
				output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.GlyphToolTip_IL_offset_0, "(epilog)"));
				break;

			case DbgILOffsetMapping.Unknown:
			case DbgILOffsetMapping.NoInfo:
			case DbgILOffsetMapping.UnmappedAddress:
				output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.GlyphToolTip_IL_offset_0, "(???)"));
				break;

			default:
				Debug.Fail($"Unknown IL offset mapping: {location.ILOffsetMapping}");
				goto case DbgILOffsetMapping.Unknown;
			}
			output.Write(DbgTextColor.Text, " ");
			output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.GlyphToolTip_NativeAddress, "0x" + location.NativeAddress.IP.ToString("X8")));

			var documentViewer = textView.TextBuffer.TryGetDocumentViewer();
			Debug2.Assert(documentViewer is not null);
			var statement = documentViewer?.GetMethodDebugService().FindByCodeOffset(new ModuleTokenId(location.Module, location.Token), location.Offset);
			Debug2.Assert((documentViewer is not null) == (statement is not null));
			if (statement is not null) {
				output.Write(DbgTextColor.Text, " ('");
				var decompiler = (documentViewer?.DocumentTab?.Content as IDecompilerTabContent)?.Decompiler ?? decompilerService.Decompiler;
				decompiler.Write(new DbgTextColorWriter(output), statement.Value.Method, FormatterOptions.Default);
				output.Write(DbgTextColor.Text, "')");
			}

			return true;
		}
	}
}
