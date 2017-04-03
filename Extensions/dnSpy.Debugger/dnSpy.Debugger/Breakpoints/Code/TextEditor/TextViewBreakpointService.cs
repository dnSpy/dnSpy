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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	abstract class TextViewBreakpointService {
		public abstract void ToggleCreateBreakpoint(ITextView textView, VirtualSnapshotPoint position);
		public abstract bool CanToggleCreateBreakpoint { get; }
		public abstract void ToggleCreateBreakpoint();
		public abstract bool CanToggleEnableBreakpoint { get; }
		public abstract void ToggleEnableBreakpoint();
	}

	[Export(typeof(TextViewBreakpointService))]
	sealed class TextViewBreakpointServiceImpl : TextViewBreakpointService {
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgTextViewBreakpointLocationProvider>[] dbgTextViewBreakpointLocationProviders;

		[ImportingConstructor]
		TextViewBreakpointServiceImpl(Lazy<IDocumentTabService> documentTabService, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, [ImportMany] IEnumerable<Lazy<DbgTextViewBreakpointLocationProvider>> dbgTextViewBreakpointLocationProviders) {
			this.documentTabService = documentTabService;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgTextViewBreakpointLocationProviders = dbgTextViewBreakpointLocationProviders.ToArray();
		}

		ITextView GetTextView() => GetTextView(documentTabService.Value.ActiveTab);
		ITextView GetTextView(IDocumentTab tab) => (tab?.UIContext as IDocumentViewer)?.TextView;

		DbgTextViewBreakpointLocationResult? GetLocations(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var textView = GetTextView(tab);
			if (textView == null)
				return null;
			var pos = position ?? textView.Caret.Position.VirtualBufferPosition;
			if (pos.Position.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();
			DbgTextViewBreakpointLocationResult? res = null;
			foreach (var lz in dbgTextViewBreakpointLocationProviders) {
				var result = lz.Value.CreateLocation(tab, textView, pos);
				if (result?.Locations == null || result.Value.Span.Snapshot != textView.TextSnapshot)
					continue;
				if (res == null || result.Value.Span.Start < res.Value.Span.Start)
					res = result;
			}
			return res;
		}

		DbgCodeBreakpoint[] GetBreakpoints(DbgTextViewBreakpointLocationResult locations) {
			var list = new List<DbgCodeBreakpoint>();
			foreach (var bp in dbgCodeBreakpointsService.Value.Breakpoints) {
				foreach (var loc in locations.Locations) {
					if (bp.Location.Equals(loc)) {
						list.Add(bp);
						break;
					}
				}
			}
			return list.ToArray();
		}

		DbgCodeBreakpoint[] GetBreakpoints() {
			if (GetLocations(documentTabService.Value.ActiveTab, null) is DbgTextViewBreakpointLocationResult locRes)
				return GetBreakpoints(locRes);
			return Array.Empty<DbgCodeBreakpoint>();
		}

		IDocumentTab GetTab(ITextView textView) {
			foreach (var g in documentTabService.Value.TabGroupService.TabGroups) {
				foreach (var t in g.TabContents) {
					var tab = t as IDocumentTab;
					if (GetTextView(tab) == textView)
						return tab;
				}
			}
			return null;
		}

		public override void ToggleCreateBreakpoint(ITextView textView, VirtualSnapshotPoint position) =>
			ToggleCreateBreakpoint(GetTab(textView), position);

		public override bool CanToggleCreateBreakpoint => GetTextView() != null;
		public override void ToggleCreateBreakpoint() => ToggleCreateBreakpoint(documentTabService.Value.ActiveTab, null);

		void ToggleCreateBreakpoint(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var locations = GetLocations(tab, position);
			var bps = locations == null ? Array.Empty<DbgCodeBreakpoint>() : GetBreakpoints(locations.Value);
			if (bps.Length != 0) {
				if (bps.All(a => a.IsEnabled))
					dbgCodeBreakpointsService.Value.Remove(bps);
				else {
					dbgCodeBreakpointsService.Value.Modify(bps.Select(a => {
						var newSettings = a.Settings;
						newSettings.IsEnabled = true;
						return new DbgCodeBreakpointAndSettings(a, newSettings);
					}).ToArray());
				}
			}
			else {
				if (locations == null || locations.Value.Locations.Length == 0)
					return;
				dbgCodeBreakpointsService.Value.Add(locations.Value.Locations.Select(a => new DbgCodeBreakpointInfo(a, new DbgCodeBreakpointSettings() { IsEnabled = true })).ToArray());
			}
		}

		public override bool CanToggleEnableBreakpoint => GetTextView() != null;
		public override void ToggleEnableBreakpoint() {
			var bps = GetBreakpoints();
			if (bps.Length == 0)
				return;
			bool newIsEnabled = !bps.All(a => a.IsEnabled);
			dbgCodeBreakpointsService.Value.Modify(bps.Select(a => {
				var newSettings = a.Settings;
				newSettings.IsEnabled = newIsEnabled;
				return new DbgCodeBreakpointAndSettings(a, newSettings);
			}).ToArray());
		}
	}
}
