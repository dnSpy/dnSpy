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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.Dialogs;
using dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Debugger.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	abstract class BreakpointGlyphTextMarkerHandler : IGlyphTextMarkerHandler {
		public abstract IGlyphTextMarkerHandlerMouseProcessor MouseProcessor { get; }
		public abstract IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint);
		public abstract GlyphTextMarkerToolTip GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker);
		public abstract FrameworkElement GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker);
	}

	[Export(typeof(BreakpointGlyphTextMarkerHandler))]
	sealed class BreakpointGlyphTextMarkerHandlerImpl : BreakpointGlyphTextMarkerHandler {
		public override IGlyphTextMarkerHandlerMouseProcessor MouseProcessor => null;

		readonly Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService;
		readonly BreakpointConditionsFormatter breakpointConditionsFormatter;
		readonly DbgCodeBreakpointHitCountService2 dbgCodeBreakpointHitCountService;
		readonly IEnumerable<Lazy<DbgBreakpointGlyphFormatter, IDbgBreakpointGlyphFormatterMetadata>> dbgBreakpointGlyphFormatters;

		[ImportingConstructor]
		BreakpointGlyphTextMarkerHandlerImpl(Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService, BreakpointConditionsFormatter breakpointConditionsFormatter, DbgCodeBreakpointHitCountService2 dbgCodeBreakpointHitCountService, [ImportMany] IEnumerable<Lazy<DbgBreakpointGlyphFormatter, IDbgBreakpointGlyphFormatterMetadata>> dbgBreakpointGlyphFormatters) {
			this.showCodeBreakpointSettingsService = showCodeBreakpointSettingsService;
			this.breakpointConditionsFormatter = breakpointConditionsFormatter;
			this.dbgCodeBreakpointHitCountService = dbgCodeBreakpointHitCountService;
			this.dbgBreakpointGlyphFormatters = dbgBreakpointGlyphFormatters.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public override FrameworkElement GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) {
			var vm = new BreakpointGlyphPopupVM(showCodeBreakpointSettingsService.Value, (DbgCodeBreakpoint)marker.Tag);
			return new BreakpointGlyphPopupControl(vm, context.Margin.VisualElement);
		}

		public override GlyphTextMarkerToolTip GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) {
			var bp = (DbgCodeBreakpoint)marker.Tag;
			return new GlyphTextMarkerToolTip(GetToolTipContent(bp, context.TextView, context.SpanProvider.GetSpan(marker)));
		}

		string GetToolTipContent(DbgCodeBreakpoint breakpoint, ITextView textView, SnapshotSpan span) {
			var output = new DbgStringBuilderTextWriter();

			var msg = breakpoint.BoundBreakpointsMessage;
			if (msg.Severity != DbgBoundCodeBreakpointSeverity.None) {
				output.Write(DbgTextColor.Error, msg.Message);
				output.WriteLine();
				output.WriteLine();
			}

			output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.GlyphToolTip_Location);
			output.Write(DbgTextColor.Text, ": ");
			WriteLocation(output, breakpoint, textView, span);

			const string INDENTATION = "    ";
			if (breakpoint.Condition != null || breakpoint.HitCount != null || breakpoint.Filter != null) {
				output.WriteLine();
				output.WriteLine();
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.GlyphToolTip_Conditions);

				if (breakpoint.Condition != null) {
					output.WriteLine();
					output.Write(DbgTextColor.Text, INDENTATION);
					breakpointConditionsFormatter.WriteToolTip(output, breakpoint.Condition.Value);
				}

				if (breakpoint.HitCount != null) {
					output.WriteLine();
					output.Write(DbgTextColor.Text, INDENTATION);
					breakpointConditionsFormatter.WriteToolTip(output, breakpoint.HitCount.Value, dbgCodeBreakpointHitCountService.GetHitCount(breakpoint));
				}

				if (breakpoint.Filter != null) {
					output.WriteLine();
					output.Write(DbgTextColor.Text, INDENTATION);
					breakpointConditionsFormatter.WriteToolTip(output, breakpoint.Filter.Value);
				}
			}

			if (breakpoint.Trace != null) {
				output.WriteLine();
				output.WriteLine();
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.GlyphToolTip_Actions);

				if (!breakpoint.Trace.Value.Continue) {
					output.WriteLine();
					output.Write(DbgTextColor.Text, INDENTATION);
					output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.Breakpoints_GlyphMargin_BreakWhenBreakpointHit);
				}

				output.WriteLine();
				output.Write(DbgTextColor.Text, INDENTATION);
				breakpointConditionsFormatter.WriteToolTip(output, breakpoint.Trace.Value);
			}

			return output.ToString();
		}

		void WriteLocation(IDbgTextWriter output, DbgCodeBreakpoint breakpoint, ITextView textView, SnapshotSpan span) {
			foreach (var lz in dbgBreakpointGlyphFormatters) {
				if (lz.Value.WriteLocation(output, breakpoint, textView, span))
					return;
			}
			Debug.Fail("Missing BP location writer");
			output.Write(DbgTextColor.Error, "???");
		}

		public override IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint) {
			var bp = (DbgCodeBreakpoint)marker.Tag;
			yield return new GuidObject(MenuConstants.GUIDOBJ_BREAKPOINT_GUID, bp);
		}
	}
}
