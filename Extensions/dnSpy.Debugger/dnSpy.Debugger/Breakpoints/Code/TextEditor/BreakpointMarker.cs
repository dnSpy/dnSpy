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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	[ExportDocumentViewerListener]
	sealed class BreakpointMarkerDocumentViewerListener : IDocumentViewerListener {
		[ImportingConstructor]
		BreakpointMarkerDocumentViewerListener(DbgCodeBreakpointsService dbgCodeBreakpointsService) {
			// Nothing, we just need to make sure that DbgCodeBreakpointsService gets imported and constructed
		}

		void IDocumentViewerListener.OnEvent(DocumentViewerEventArgs e) {
			// Nothing, the ctor does all the work
		}
	}

	[Export(typeof(IDbgCodeBreakpointsServiceListener))]
	sealed class BreakpointMarker : IDbgCodeBreakpointsServiceListener {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IGlyphTextMarkerService> glyphTextMarkerService;
		readonly Lazy<IClassificationTypeRegistryService> classificationTypeRegistryService;
		readonly BreakpointGlyphTextMarkerLocationProviderService breakpointGlyphTextMarkerLocationProviderService;
		readonly BreakpointGlyphTextMarkerHandler breakpointGlyphTextMarkerHandler;
		IClassificationType classificationTypeEnabledBreakpoint;
		BreakpointInfo[] breakpointInfos;

		[ImportingConstructor]
		BreakpointMarker(UIDispatcher uiDispatcher, Lazy<IGlyphTextMarkerService> glyphTextMarkerService, Lazy<IClassificationTypeRegistryService> classificationTypeRegistryService, BreakpointGlyphTextMarkerLocationProviderService breakpointGlyphTextMarkerLocationProviderService, BreakpointGlyphTextMarkerHandler breakpointGlyphTextMarkerHandler) {
			this.uiDispatcher = uiDispatcher;
			this.glyphTextMarkerService = glyphTextMarkerService;
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.breakpointGlyphTextMarkerLocationProviderService = breakpointGlyphTextMarkerLocationProviderService;
			this.breakpointGlyphTextMarkerHandler = breakpointGlyphTextMarkerHandler;
			UI(() => Initialize_UI());
		}

		void UI(Action action) => uiDispatcher.UI(action);

		void Initialize_UI() {
			uiDispatcher.VerifyAccess();
			classificationTypeEnabledBreakpoint = classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.BreakpointStatement);
			breakpointInfos = new BreakpointInfo[(int)BreakpointKind.Last] {
				new BreakpointInfo(BreakpointKind.BreakpointDisabled,			ThemeClassificationTypeNameKeys.DisabledBreakpointStatementMarker,			null,																				null,																																GlyphTextMarkerServiceZIndexes.DisabledBreakpoint),
				new BreakpointInfo(BreakpointKind.BreakpointEnabled,			ThemeClassificationTypeNameKeys.BreakpointStatementMarker,					ThemeClassificationTypeNameKeys.SelectedBreakpointStatementMarker,					classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.BreakpointStatement),					GlyphTextMarkerServiceZIndexes.EnabledBreakpoint),
				new BreakpointInfo(BreakpointKind.AdvancedBreakpointDisabled,	ThemeClassificationTypeNameKeys.DisabledAdvancedBreakpointStatementMarker,	ThemeClassificationTypeNameKeys.SelectedDisabledAdvancedBreakpointStatementMarker,	classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.DisabledAdvancedBreakpointStatement),	GlyphTextMarkerServiceZIndexes.DisabledAdvancedBreakpoint),
				new BreakpointInfo(BreakpointKind.AdvancedBreakpointEnabled,	ThemeClassificationTypeNameKeys.AdvancedBreakpointStatementMarker,			ThemeClassificationTypeNameKeys.SelectedAdvancedBreakpointStatementMarker,			classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.AdvancedBreakpointStatement),			GlyphTextMarkerServiceZIndexes.EnabledAdvancedBreakpoint),
				new BreakpointInfo(BreakpointKind.BreakpointWarning,			ThemeClassificationTypeNameKeys.BreakpointWarningStatementMarker,			ThemeClassificationTypeNameKeys.SelectedBreakpointWarningStatementMarker,			classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.BreakpointWarningStatement),				GlyphTextMarkerServiceZIndexes.BreakpointWarning),
				new BreakpointInfo(BreakpointKind.BreakpointError,				ThemeClassificationTypeNameKeys.BreakpointErrorStatementMarker,				ThemeClassificationTypeNameKeys.SelectedBreakpointErrorStatementMarker,				classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.BreakpointErrorStatement),				GlyphTextMarkerServiceZIndexes.BreakpointError),
				new BreakpointInfo(BreakpointKind.AdvancedBreakpointWarning,	ThemeClassificationTypeNameKeys.AdvancedBreakpointWarningStatementMarker,	ThemeClassificationTypeNameKeys.SelectedAdvancedBreakpointWarningStatementMarker,	classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.AdvancedBreakpointWarningStatement),		GlyphTextMarkerServiceZIndexes.AdvancedBreakpointWarning),
				new BreakpointInfo(BreakpointKind.AdvancedBreakpointError,		ThemeClassificationTypeNameKeys.AdvancedBreakpointErrorStatementMarker,		ThemeClassificationTypeNameKeys.SelectedAdvancedBreakpointErrorStatementMarker,		classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.AdvancedBreakpointErrorStatement),		GlyphTextMarkerServiceZIndexes.AdvancedBreakpointError),
				new BreakpointInfo(BreakpointKind.TracepointDisabled,			ThemeClassificationTypeNameKeys.DisabledTracepointStatementMarker,			ThemeClassificationTypeNameKeys.SelectedDisabledTracepointStatementMarker,			classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.DisabledTracepointStatement),			GlyphTextMarkerServiceZIndexes.DisabledTracepoint),
				new BreakpointInfo(BreakpointKind.TracepointEnabled,			ThemeClassificationTypeNameKeys.TracepointStatementMarker,					ThemeClassificationTypeNameKeys.SelectedTracepointStatementMarker,					classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.TracepointStatement),					GlyphTextMarkerServiceZIndexes.EnabledTracepoint),
				new BreakpointInfo(BreakpointKind.AdvancedTracepointDisabled,	ThemeClassificationTypeNameKeys.DisabledAdvancedTracepointStatementMarker,	ThemeClassificationTypeNameKeys.SelectedDisabledAdvancedTracepointStatementMarker,	classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.DisabledAdvancedTracepointStatement),	GlyphTextMarkerServiceZIndexes.DisabledAdvancedTracepoint),
				new BreakpointInfo(BreakpointKind.AdvancedTracepointEnabled,	ThemeClassificationTypeNameKeys.AdvancedTracepointStatementMarker,			ThemeClassificationTypeNameKeys.SelectedAdvancedTracepointStatementMarker,			classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.AdvancedTracepointStatement),			GlyphTextMarkerServiceZIndexes.EnabledAdvancedTracepoint),
				new BreakpointInfo(BreakpointKind.TracepointWarning,			ThemeClassificationTypeNameKeys.TracepointWarningStatementMarker,			ThemeClassificationTypeNameKeys.SelectedTracepointWarningStatementMarker,			classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.TracepointWarningStatement),				GlyphTextMarkerServiceZIndexes.TracepointWarning),
				new BreakpointInfo(BreakpointKind.TracepointError,				ThemeClassificationTypeNameKeys.TracepointErrorStatementMarker,				ThemeClassificationTypeNameKeys.SelectedTracepointErrorStatementMarker,				classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.TracepointErrorStatement),				GlyphTextMarkerServiceZIndexes.TracepointError),
				new BreakpointInfo(BreakpointKind.AdvancedTracepointWarning,	ThemeClassificationTypeNameKeys.AdvancedTracepointWarningStatementMarker,	ThemeClassificationTypeNameKeys.SelectedAdvancedTracepointWarningStatementMarker,	classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.AdvancedTracepointWarningStatement),		GlyphTextMarkerServiceZIndexes.AdvancedTracepointWarning),
				new BreakpointInfo(BreakpointKind.AdvancedTracepointError,		ThemeClassificationTypeNameKeys.AdvancedTracepointErrorStatementMarker,		ThemeClassificationTypeNameKeys.SelectedAdvancedTracepointErrorStatementMarker,		classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.AdvancedTracepointErrorStatement),		GlyphTextMarkerServiceZIndexes.AdvancedTracepointError),
			};
		}

		void IDbgCodeBreakpointsServiceListener.Initialize(DbgCodeBreakpointsService dbgCodeBreakpointsService) {
			dbgCodeBreakpointsService.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
			dbgCodeBreakpointsService.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
			dbgCodeBreakpointsService.BoundBreakpointsMessageChanged += DbgCodeBreakpointsService_BoundBreakpointsMessageChanged;
		}

		void DbgCodeBreakpointsService_BoundBreakpointsMessageChanged(object sender, BoundBreakpointsMessageChangedEventArgs e) =>
			UI(() => OnBreakpointsModified_UI(e.Breakpoints));

		sealed class BreakpointData {
			public GlyphTextMarkerLocationInfo Location { get; }
			public IGlyphTextMarker Marker { get; set; }
			public BreakpointInfo Info { get; set; }
			public BreakpointData(GlyphTextMarkerLocationInfo location) => Location = location ?? throw new ArgumentNullException(nameof(location));
		}

		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
			if (e.Added)
				UI(() => OnBreakpointsAdded_UI(e));
			else {
				var list = new List<(DbgCodeBreakpoint breakpoint, BreakpointData data)>(e.Objects.Count);
				foreach (var bp in e.Objects) {
					if (!bp.TryGetData(out BreakpointData data))
						continue;
					list.Add((bp, data));
				}
				if (list.Count > 0)
					UI(() => OnBreakpointsRemoved_UI(list));
			}
		}

		void OnBreakpointsAdded_UI(DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
			uiDispatcher.VerifyAccess();
			if (!e.Added)
				throw new InvalidOperationException();
			foreach (var bp in e.Objects) {
				var location = breakpointGlyphTextMarkerLocationProviderService.GetLocation(bp);
				if (location != null) {
					bp.GetOrCreateData(() => new BreakpointData(location));
					UpdateMarker(bp);
					continue;
				}
			}
		}

		void OnBreakpointsRemoved_UI(List<(DbgCodeBreakpoint breakpoint, BreakpointData data)> list) {
			uiDispatcher.VerifyAccess();
			glyphTextMarkerService.Value.Remove(list.Select(a => a.data.Marker).Where(a => a != null));
		}

		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) =>
			UI(() => OnBreakpointsModified_UI(e.Breakpoints.Select(a => a.Breakpoint).ToArray()));

		void OnBreakpointsModified_UI(IList<DbgCodeBreakpoint> breakpoints) {
			uiDispatcher.VerifyAccess();
			var bps = new List<DbgCodeBreakpoint>(breakpoints.Count);
			var removedMarkers = new List<IGlyphTextMarker>(breakpoints.Count);
			for (int i = 0; i < breakpoints.Count; i++) {
				var bp = breakpoints[i];
				if (!bp.TryGetData(out BreakpointData data))
					continue;
				bps.Add(bp);
				if (data.Marker == null)
					continue;
				if (data.Info == breakpointInfos[(int)BreakpointImageUtilities.GetBreakpointKind(bp)])
					continue;
				removedMarkers.Add(data.Marker);
				data.Marker = null;
			}
			glyphTextMarkerService.Value.Remove(removedMarkers);
			foreach (var bp in bps)
				UpdateMarker(bp);
		}

		void UpdateMarker(DbgCodeBreakpoint bp) {
			if (!bp.TryGetData(out BreakpointData data))
				return;

			var info = breakpointInfos[(int)BreakpointImageUtilities.GetBreakpointKind(bp)];
			if (data.Info == info && data.Marker != null)
				return;
			data.Info = info;
			if (data.Marker != null)
				glyphTextMarkerService.Value.Remove(data.Marker);

			data.Marker = glyphTextMarkerService.Value.AddMarker(data.Location, info.ImageReference, info.MarkerTypeName, info.SelectedMarkerTypeName, info.ClassificationType, info.ZIndex, bp, breakpointGlyphTextMarkerHandler, textViewFilter);
		}
		static readonly Func<ITextView, bool> textViewFilter = textView => textView.Roles.Contains(PredefinedTextViewRoles.Debuggable);
	}
}
