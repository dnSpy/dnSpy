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
using dnSpy.Contracts.Debugger.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor {
	/// <summary>
	/// Writes breakpoints info used by breakpoint glyph margin code, eg. in tooltips.
	/// Use <see cref="ExportDbgBreakpointGlyphFormatterAttribute"/> to export an instance.
	/// </summary>
	public abstract class DbgBreakpointGlyphFormatter {
		/// <summary>
		/// Writes the text shown after "Location: " in tooltips when hovering over the breakpoint icon in the glyph margin.
		/// Returns true if something was written, and false if nothing was written.
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="textView">Text view</param>
		/// <param name="span">Span of breakpoint marker in the document</param>
		public abstract bool WriteLocation(IDbgTextWriter output, DbgCodeBreakpoint breakpoint, ITextView textView, SnapshotSpan span);
	}

	/// <summary>Metadata</summary>
	public interface IDbgBreakpointGlyphFormatterMetadata {
		/// <summary>See <see cref="ExportDbgBreakpointGlyphFormatterAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgBreakpointGlyphFormatter"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgBreakpointGlyphFormatterAttribute : ExportAttribute, IDbgBreakpointGlyphFormatterMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgBreakpointGlyphFormatterAttribute(double order = double.MaxValue)
			: base(typeof(DbgBreakpointGlyphFormatter)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
