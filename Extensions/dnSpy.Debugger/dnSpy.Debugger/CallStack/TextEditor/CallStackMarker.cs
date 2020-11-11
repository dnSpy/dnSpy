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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.CallStack.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.CallStack.TextEditor {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class CallStackMarker : IDbgManagerStartListener {
		readonly UIDispatcher uiDispatcher;
		readonly DbgCallStackService dbgCallStackService;
		readonly Lazy<ActiveStatementService> activeStatementService;
		readonly Lazy<CallStackGlyphTextMarkerHandler> callStackGlyphTextMarkerHandler;
		readonly Lazy<IGlyphTextMarkerService> glyphTextMarkerService;
		readonly Lazy<DbgStackFrameGlyphTextMarkerLocationInfoProvider>[] dbgStackFrameGlyphTextMarkerLocationInfoProviders;
		IClassificationType? classificationTypeCurrentStatement;
		IClassificationType? classificationTypeCallReturn;
		IGlyphTextMarker? currentStatementMarker;
		IGlyphTextMarker? callReturnMarker;
		DbgProcess? currentProcess;

		[ImportingConstructor]
		CallStackMarker(UIDispatcher uiDispatcher, DbgCallStackService dbgCallStackService, Lazy<ActiveStatementService> activeStatementService, Lazy<CallStackGlyphTextMarkerHandler> callStackGlyphTextMarkerHandler, Lazy<IGlyphTextMarkerService> glyphTextMarkerService, Lazy<IClassificationTypeRegistryService> classificationTypeRegistryService, [ImportMany] IEnumerable<Lazy<DbgStackFrameGlyphTextMarkerLocationInfoProvider>> dbgStackFrameGlyphTextMarkerLocationInfoProviders) {
			this.uiDispatcher = uiDispatcher;
			this.dbgCallStackService = dbgCallStackService;
			this.activeStatementService = activeStatementService;
			this.callStackGlyphTextMarkerHandler = callStackGlyphTextMarkerHandler;
			this.glyphTextMarkerService = glyphTextMarkerService;
			this.dbgStackFrameGlyphTextMarkerLocationInfoProviders = dbgStackFrameGlyphTextMarkerLocationInfoProviders.ToArray();
			UI(() => Initialize_UI(classificationTypeRegistryService));
			dbgCallStackService.FramesChanged += DbgCallStackService_FramesChanged;
		}

		void Initialize_UI(Lazy<IClassificationTypeRegistryService> classificationTypeRegistryService) {
			classificationTypeCurrentStatement = classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.CurrentStatement);
			classificationTypeCallReturn = classificationTypeRegistryService.Value.GetClassificationType(ThemeClassificationTypeNames.CallReturn);
		}

		// random thread
		void UI(Action callback) => uiDispatcher.UI(callback);

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) { }

		void DbgCallStackService_FramesChanged(object? sender, FramesChangedEventArgs e) => UI(() => UpdateMarkers(updateActiveStatements: e.FramesChanged));

		void UpdateMarkers(bool updateActiveStatements) {
			SetCurrentProcess(dbgCallStackService.Thread?.Process);
			ClearMarkers();
			AddMarkers(updateActiveStatements);
		}

		void SetCurrentProcess(DbgProcess? process) {
			if (currentProcess == process)
				return;
			if (currentProcess is not null)
				currentProcess.IsRunningChanged -= DbgProcess_IsRunningChanged;
			currentProcess = process;
			if (process is not null)
				process.IsRunningChanged += DbgProcess_IsRunningChanged;
		}

		void DbgProcess_IsRunningChanged(object? sender, EventArgs e) {
			if (currentProcess != sender)
				return;
			if (currentProcess!.IsRunning) {
				UI(() => {
					ClearMarkers();
					activeStatementService.Value.OnNewActiveStatements(emptyStackFrames);
				});
			}
		}
		static readonly ReadOnlyCollection<DbgStackFrame> emptyStackFrames = new ReadOnlyCollection<DbgStackFrame>(Array.Empty<DbgStackFrame>());

		void ClearMarkers() {
			if (currentStatementMarker is not null || callReturnMarker is not null) {
				var list = new List<IGlyphTextMarker>(2);
				if (currentStatementMarker is not null)
					list.Add(currentStatementMarker);
				if (callReturnMarker is not null)
					list.Add(callReturnMarker);
				currentStatementMarker = null;
				callReturnMarker = null;
				glyphTextMarkerService.Value.Remove(list);
			}
		}

		GlyphTextMarkerLocationInfo? GetTextMarkerLocationInfo(DbgStackFrame frame) {
			foreach (var provider in dbgStackFrameGlyphTextMarkerLocationInfoProviders) {
				var info = provider.Value.Create(frame);
				if (info is not null)
					return info;
			}
			return null;
		}

		void AddMarkers(bool updateActiveStatements) {
			Debug2.Assert(currentStatementMarker is null);
			Debug2.Assert(callReturnMarker is null);
			var frames = dbgCallStackService.Frames.Frames;

			if (frames.Count != 0) {
				var markerLocationInfo = GetTextMarkerLocationInfo(frames[0]);
				if (markerLocationInfo is not null) {
					currentStatementMarker = glyphTextMarkerService.Value.AddMarker(
						markerLocationInfo,
						DsImages.CurrentInstructionPointer,
						ThemeClassificationTypeNameKeys.CurrentStatementMarker,
						null,
						classificationTypeCurrentStatement,
						GlyphTextMarkerServiceZIndexes.CurrentStatement,
						CallStackFrameKind.CurrentStatement,
						callStackGlyphTextMarkerHandler.Value,
						textViewFilter);
				}

				int activeFrameIndex = dbgCallStackService.ActiveFrameIndex;
				markerLocationInfo = activeFrameIndex != 0 && (uint)activeFrameIndex < (uint)frames.Count ? GetTextMarkerLocationInfo(frames[activeFrameIndex]) : null;
				if (markerLocationInfo is not null) {
					callReturnMarker = glyphTextMarkerService.Value.AddMarker(
						markerLocationInfo,
						DsImages.CallReturnInstructionPointer,
						ThemeClassificationTypeNameKeys.CallReturnMarker,
						null,
						classificationTypeCallReturn,
						GlyphTextMarkerServiceZIndexes.ReturnStatement,
						CallStackFrameKind.ReturnStatement,
						callStackGlyphTextMarkerHandler.Value,
						textViewFilter);
				}
			}

			if (updateActiveStatements)
				activeStatementService.Value.OnNewActiveStatements(frames);
		}
		static readonly Func<ITextView, bool> textViewFilter = textView => textView.Roles.Contains(PredefinedTextViewRoles.Debuggable);
	}
}
