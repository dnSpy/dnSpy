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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.CallStack {
	//[Export(typeof(ILoadBeforeDebug))]
	sealed class CallStackMarker : ILoadBeforeDebug {
		readonly IStackFrameService stackFrameService;
		readonly IGlyphTextMarkerService glyphTextMarkerService;
		readonly IClassificationType classificationTypeCurrentStatement;
		readonly IClassificationType classificationTypeCallReturn;
		readonly Lazy<ActiveStatementService> activeStatementService;
		IGlyphTextMethodMarker currentStatementMarker;
		IGlyphTextMethodMarker callReturnMarker;

		[ImportingConstructor]
		CallStackMarker(IStackFrameService stackFrameService, IGlyphTextMarkerService glyphTextMarkerService, IClassificationTypeRegistryService classificationTypeRegistryService, Lazy<ActiveStatementService> activeStatementService) {
			this.stackFrameService = stackFrameService;
			this.glyphTextMarkerService = glyphTextMarkerService;
			classificationTypeCurrentStatement = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.CurrentStatement);
			classificationTypeCallReturn = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.CallReturn);
			this.activeStatementService = activeStatementService;
			stackFrameService.NewFrames += StackFrameService_NewFrames;
		}

		void StackFrameService_NewFrames(object sender, NewFramesEventArgs e) {
			switch (e.Kind) {
			case NewFramesKind.NewFrames:
				ClearMarkers();
				AddMarkers(updateActiveStatements: true);
				break;

			case NewFramesKind.NewFrameNumber:
				ClearMarkers();
				AddMarkers(updateActiveStatements: false);
				break;

			case NewFramesKind.Cleared:
				ClearMarkers();
				activeStatementService.Value.OnCleared();
				break;

			default:
				Debug.Fail("Unknown value: " + e.Kind);
				break;
			}
		}

		void ClearMarkers() {
			if (currentStatementMarker != null || callReturnMarker != null) {
				var list = new List<IGlyphTextMarker>(2);
				if (currentStatementMarker != null)
					list.Add(currentStatementMarker);
				if (callReturnMarker != null)
					list.Add(callReturnMarker);
				currentStatementMarker = null;
				callReturnMarker = null;
				glyphTextMarkerService.Remove(list);
			}
		}

		KeyValuePair<ModuleTokenId, uint>? GetModuleTokenId(CorFrame frame) {
			if (!frame.IsILFrame)
				return null;
			var ip = frame.ILFrameIP;
			if (!ip.IsExact && !ip.IsApproximate && !ip.IsProlog && !ip.IsEpilog)
				return null;
			uint token = frame.Token;
			if (token == 0)
				return null;
			var mod = frame.DnModuleId;
			if (mod == null)
				return null;
			return new KeyValuePair<ModuleTokenId, uint>(new ModuleTokenId(mod.Value.ToModuleId(), frame.Token), ip.Offset);
		}

		static readonly Func<ITextView, bool> textViewFilter = textView => textView.Roles.Contains(PredefinedTextViewRoles.Debuggable);
		void AddMarkers(bool updateActiveStatements) {
			Debug.Assert(currentStatementMarker == null);
			Debug.Assert(callReturnMarker == null);
			bool tooManyFrames;
			var frames = stackFrameService.GetFrames(out tooManyFrames);

			if (frames.Count == 0)
				return;

			var methodOffset = GetModuleTokenId(frames[0]);
			if (methodOffset != null) {
				currentStatementMarker = glyphTextMarkerService.AddMarker(
					methodOffset.Value.Key,
					methodOffset.Value.Value,
					DsImages.CurrentInstructionPointer,
					ThemeClassificationTypeNameKeys.CurrentStatementMarker,
					null,
					classificationTypeCurrentStatement,
					GlyphTextMarkerServiceZIndexes.CurrentStatement,
					null,
					null,
					textViewFilter);
			}

			int selectedFrameNumber = stackFrameService.SelectedFrameNumber;
			methodOffset = selectedFrameNumber != 0 && selectedFrameNumber < frames.Count ? GetModuleTokenId(frames[selectedFrameNumber]) : null;
			if (methodOffset != null) {
				callReturnMarker = glyphTextMarkerService.AddMarker(
					methodOffset.Value.Key,
					methodOffset.Value.Value,
					DsImages.CallReturnInstructionPointer,
					ThemeClassificationTypeNameKeys.CallReturnMarker,
					null,
					classificationTypeCallReturn,
					GlyphTextMarkerServiceZIndexes.ReturnStatement,
					null,
					null,
					textViewFilter);
			}

			if (updateActiveStatements)
				activeStatementService.Value.OnNewActiveStatements(frames);
		}
	}
}
