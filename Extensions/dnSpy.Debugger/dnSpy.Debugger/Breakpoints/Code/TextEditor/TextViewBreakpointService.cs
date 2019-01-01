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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Code.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Debugger.Code.TextEditor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	abstract class TextViewBreakpointService {
		public abstract void ToggleCreateBreakpoint(ITextView textView, VirtualSnapshotPoint position);
		public abstract bool CanToggleCreateBreakpoint { get; }
		public abstract void ToggleCreateBreakpoint();
		public abstract ToggleCreateBreakpointKind GetToggleCreateBreakpointKind();
		public abstract bool CanToggleEnableBreakpoint { get; }
		public abstract void ToggleEnableBreakpoint();
		public abstract ToggleEnableBreakpointKind GetToggleEnableBreakpointKind();
	}

	enum ToggleCreateBreakpointKind {
		None,
		Add,
		Delete,
		Enable,
	}

	enum ToggleEnableBreakpointKind {
		None,
		Enable,
		Disable,
	}

	[Export(typeof(TextViewBreakpointService))]
	sealed class TextViewBreakpointServiceImpl : TextViewBreakpointService {
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<IBreakpointMarker> breakpointMarker;
		readonly Lazy<DbgTextViewCodeLocationService> dbgTextViewCodeLocationService;

		[ImportingConstructor]
		TextViewBreakpointServiceImpl(Lazy<DbgManager> dbgManager, Lazy<IDocumentTabService> documentTabService, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<IBreakpointMarker> breakpointMarker, Lazy<DbgTextViewCodeLocationService> dbgTextViewCodeLocationService) {
			this.dbgManager = dbgManager;
			this.documentTabService = documentTabService;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.breakpointMarker = breakpointMarker;
			this.dbgTextViewCodeLocationService = dbgTextViewCodeLocationService;
		}

		ITextView GetTextView() => GetTextView(documentTabService.Value.ActiveTab);
		ITextView GetTextView(IDocumentTab tab) => (tab?.UIContext as IDocumentViewer)?.TextView;

		readonly struct LocationsResult : IDisposable {
			public readonly DbgTextViewBreakpointLocationResult? locRes;
			readonly Lazy<DbgManager> dbgManager;
			readonly List<DbgCodeLocation> allLocations;

			public LocationsResult(Lazy<DbgManager> dbgManager, DbgTextViewBreakpointLocationResult? locRes, List<DbgCodeLocation> allLocations) {
				this.dbgManager = dbgManager;
				this.locRes = locRes;
				this.allLocations = allLocations;
			}

			public void Dispose() {
				if (allLocations.Count > 0)
					dbgManager.Value.Close(allLocations);
			}
		}

		LocationsResult GetLocations(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var allLocations = new List<DbgCodeLocation>();
			var textView = GetTextView(tab);
			if (textView == null)
				return new LocationsResult(dbgManager, null, allLocations);
			var pos = position ?? textView.Caret.Position.VirtualBufferPosition;
			if (pos.Position.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();
			DbgTextViewBreakpointLocationResult? res = null;
			foreach (var loc in dbgTextViewCodeLocationService.Value.CreateLocation(tab, textView, pos))
				UpdateResult(allLocations, textView, ref res, loc, useIfSameSpan: false);
			SnapshotSpan span;
			if (res != null) {
				var resSpan = res.Value.Span.SnapshotSpan;
				var newStart = Min(pos.Position, resSpan.Start);
				var newEnd = Max(pos.Position, resSpan.End);
				span = new SnapshotSpan(newStart, newEnd);
			}
			else
				span = new SnapshotSpan(pos.Position, new SnapshotPoint(pos.Position.Snapshot, pos.Position.Snapshot.Length));
			// This one has higher priority since it already exists (eg. could be a stack frame BP location)
			UpdateResult(allLocations, textView, ref res, breakpointMarker.Value.GetLocations(textView, span), useIfSameSpan: true);
			return new LocationsResult(dbgManager, res, allLocations);
		}

		static SnapshotPoint Min(SnapshotPoint a, SnapshotPoint b) => a <= b ? a : b;
		static SnapshotPoint Max(SnapshotPoint a, SnapshotPoint b) => a >= b ? a : b;

		static void UpdateResult(List<DbgCodeLocation> allLocations, ITextView textView, ref DbgTextViewBreakpointLocationResult? res, DbgTextViewBreakpointLocationResult? result, bool useIfSameSpan) {
			if (result?.Locations == null)
				return;
			allLocations.AddRange(result.Value.Locations);
			if (result.Value.Span.Snapshot != textView.TextSnapshot)
				return;
			if (res == null)
				res = result;
			else if (useIfSameSpan) {
				if (result.Value.Span.Start == res.Value.Span.Start)
					res = result;
			}
			else if (result.Value.Span.Start < res.Value.Span.Start)
				res = result;
		}

		DbgCodeBreakpoint[] GetBreakpoints(DbgTextViewBreakpointLocationResult locations) {
			var list = new List<DbgCodeBreakpoint>();
			foreach (var loc in locations.Locations) {
				var bp = dbgCodeBreakpointsService.Value.TryGetBreakpoint(loc);
				if (bp?.IsHidden == false)
					list.Add(bp);
			}
			return list.ToArray();
		}

		DbgCodeBreakpoint[] GetBreakpoints() {
			using (var info = GetLocations(documentTabService.Value.ActiveTab, null)) {
				if (info.locRes is DbgTextViewBreakpointLocationResult locRes)
					return GetBreakpoints(locRes);
				return Array.Empty<DbgCodeBreakpoint>();
			}
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
			ToggleCreateBreakpoint(GetToggleCreateBreakpointInfo(GetTab(textView), position));

		public override ToggleCreateBreakpointKind GetToggleCreateBreakpointKind() {
			using (var info = GetToggleCreateBreakpointInfo(documentTabService.Value.ActiveTab, null))
				return info.kind;
		}

		public override bool CanToggleCreateBreakpoint => GetToggleCreateBreakpointKind() != ToggleCreateBreakpointKind.None;
		public override void ToggleCreateBreakpoint() => ToggleCreateBreakpoint(GetToggleCreateBreakpointInfo(documentTabService.Value.ActiveTab, null));

		readonly struct ToggleCreateBreakpointInfoResult : IDisposable {
			readonly Lazy<DbgManager> dbgManager;
			public readonly ToggleCreateBreakpointKind kind;
			public readonly DbgCodeBreakpoint[] breakpoints;
			public readonly DbgCodeLocation[] locations;
			public ToggleCreateBreakpointInfoResult(Lazy<DbgManager> dbgManager, ToggleCreateBreakpointKind kind, DbgCodeBreakpoint[] breakpoints, DbgCodeLocation[] locations) {
				this.dbgManager = dbgManager;
				this.kind = kind;
				this.breakpoints = breakpoints;
				this.locations = locations;
			}

			public void Dispose() {
				if (locations != null && locations.Length > 0)
					dbgManager.Value.Close(locations);
			}
		}

		ToggleCreateBreakpointInfoResult GetToggleCreateBreakpointInfo(IDocumentTab tab, VirtualSnapshotPoint? position) {
			using (var info = GetLocations(tab, position)) {
				var locRes = info.locRes;
				var bps = locRes == null ? Array.Empty<DbgCodeBreakpoint>() : GetBreakpoints(locRes.Value);
				if (bps.Length != 0) {
					if (bps.All(a => a.IsEnabled))
						return new ToggleCreateBreakpointInfoResult(dbgManager, ToggleCreateBreakpointKind.Delete, bps, Array.Empty<DbgCodeLocation>());
					return new ToggleCreateBreakpointInfoResult(dbgManager, ToggleCreateBreakpointKind.Enable, bps, Array.Empty<DbgCodeLocation>());
				}
				else {
					if (locRes == null || locRes.Value.Locations.Length == 0)
						return new ToggleCreateBreakpointInfoResult(dbgManager, ToggleCreateBreakpointKind.None, Array.Empty<DbgCodeBreakpoint>(), Array.Empty<DbgCodeLocation>());
					return new ToggleCreateBreakpointInfoResult(dbgManager, ToggleCreateBreakpointKind.Add, Array.Empty<DbgCodeBreakpoint>(), locRes.Value.Locations.Select(a => a.Clone()).ToArray());
				}
			}
		}

		void ToggleCreateBreakpoint(ToggleCreateBreakpointInfoResult info) {
			using (info) {
				switch (info.kind) {
				case ToggleCreateBreakpointKind.Add:
					dbgCodeBreakpointsService.Value.Add(info.locations.Select(a => new DbgCodeBreakpointInfo(a.Clone(), new DbgCodeBreakpointSettings() { IsEnabled = true })).ToArray());
					break;

				case ToggleCreateBreakpointKind.Delete:
					dbgCodeBreakpointsService.Value.Remove(info.breakpoints);
					break;

				case ToggleCreateBreakpointKind.Enable:
					dbgCodeBreakpointsService.Value.Modify(info.breakpoints.Select(a => {
						var newSettings = a.Settings;
						newSettings.IsEnabled = true;
						return new DbgCodeBreakpointAndSettings(a, newSettings);
					}).ToArray());
					break;

				case ToggleCreateBreakpointKind.None:
				default:
					return;
				}
			}
		}

		public override bool CanToggleEnableBreakpoint => GetToggleEnableBreakpointInfo().kind != ToggleEnableBreakpointKind.None;
		public override void ToggleEnableBreakpoint() {
			var info = GetToggleEnableBreakpointInfo();
			bool newIsEnabled;
			switch (info.kind) {
			case ToggleEnableBreakpointKind.Enable:
				newIsEnabled = true;
				break;

			case ToggleEnableBreakpointKind.Disable:
				newIsEnabled = false;
				break;

			case ToggleEnableBreakpointKind.None:
			default:
				return;
			}
			dbgCodeBreakpointsService.Value.Modify(info.breakpoints.Select(a => {
				var newSettings = a.Settings;
				newSettings.IsEnabled = newIsEnabled;
				return new DbgCodeBreakpointAndSettings(a, newSettings);
			}).ToArray());
		}

		public override ToggleEnableBreakpointKind GetToggleEnableBreakpointKind() => GetToggleEnableBreakpointInfo().kind;

		(ToggleEnableBreakpointKind kind, DbgCodeBreakpoint[] breakpoints) GetToggleEnableBreakpointInfo() {
			var bps = GetBreakpoints();
			if (bps.Length == 0)
				return (ToggleEnableBreakpointKind.None, Array.Empty<DbgCodeBreakpoint>());
			bool newIsEnabled = !bps.All(a => a.IsEnabled);
			var kind = newIsEnabled ? ToggleEnableBreakpointKind.Enable : ToggleEnableBreakpointKind.Disable;
			return (kind, bps);
		}
	}
}
