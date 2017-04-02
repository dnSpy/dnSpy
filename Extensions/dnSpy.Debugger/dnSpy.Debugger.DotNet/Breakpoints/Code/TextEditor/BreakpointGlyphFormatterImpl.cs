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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code.TextEditor {
	[ExportBreakpointGlyphFormatter]
	sealed class BreakpointGlyphFormatterImpl : BreakpointGlyphFormatter {
		readonly IDecompilerService decompilerService;

		[ImportingConstructor]
		BreakpointGlyphFormatterImpl(IDecompilerService decompilerService) => this.decompilerService = decompilerService;

		public override bool WriteLocation(IDebugOutputWriter output, DbgCodeBreakpoint breakpoint, ITextView textView, SnapshotSpan span) {
			if (breakpoint.EngineBreakpoint is DbgDotNetEngineCodeBreakpointImpl location)
				return WriteLocation(output, textView, span, location);

			return false;
		}

		bool WriteLocation(IDebugOutputWriter output, ITextView textView, SnapshotSpan span, DbgDotNetEngineCodeBreakpointImpl location) {
			var line = span.Start.GetContainingLine();
			output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_DotNet_Resources.GlyphToolTip_line_0_character_1,
				(line.LineNumber + 1).ToString(CultureInfo.CurrentUICulture),
				(span.Start - line.Start + 1).ToString(CultureInfo.CurrentUICulture)));
			output.Write(BoxedTextColor.Text, " ");
			output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_DotNet_Resources.GlyphToolTip_IL_offset_0, location.Offset.ToString("X4")));

			var documentViewer = textView.TextBuffer.TryGetDocumentViewer();
			Debug.Assert(documentViewer != null);
			var statement = documentViewer?.GetMethodDebugService().FindByCodeOffset(new ModuleTokenId(location.Module, location.Token), location.Offset);
			Debug.Assert((documentViewer != null) == (statement != null));
			if (statement != null) {
				output.Write(BoxedTextColor.Text, " ('");
				var decompiler = (documentViewer?.DocumentTab.Content as IDecompilerTabContent)?.Decompiler ?? decompilerService.Decompiler;
				decompiler.Write(new TextColorWriterImpl(output), statement.Value.Method, SimplePrinterFlags.Default);
				output.Write(BoxedTextColor.Text, "')");
			}

			return true;
		}

		sealed class TextColorWriterImpl : ITextColorWriter {
			readonly IDebugOutputWriter output;
			public TextColorWriterImpl(IDebugOutputWriter output) => this.output = output;
			public void Write(object color, string text) => output.Write(color, text);
			public void Write(TextColor color, string text) => output.Write(color.Box(), text);
		}
	}
}
