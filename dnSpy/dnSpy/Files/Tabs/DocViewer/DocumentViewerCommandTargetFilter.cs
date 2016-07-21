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
using System.Diagnostics;
using dnSpy.Contracts.Command;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class DocumentViewerCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public DocumentViewerCommandTargetFilter(ITextView textView) {
			this.textView = textView;
		}

		DocumentViewerControl TryGetInstance() =>
			__documentViewerControl ?? (__documentViewerControl = DocumentViewerControl.TryGetInstance(textView));
		DocumentViewerControl __documentViewerControl;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (TryGetInstance() == null)
				return CommandTargetStatus.NotHandled;

			if (group == DocumentViewerCommandConstants.DocumentViewerGroup) {
				switch ((DocumentViewerIds)cmdId) {
				case DocumentViewerIds.MoveToNextReference:
				case DocumentViewerIds.MoveToPreviousReference:
				case DocumentViewerIds.MoveToNextDefinition:
				case DocumentViewerIds.MoveToPreviousDefinition:
				case DocumentViewerIds.FollowReference:
				case DocumentViewerIds.FollowReferenceNewTab:
				case DocumentViewerIds.ClearMarkedReferencesAndToolTip:
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(DocumentViewerIds)} id: {(DocumentViewerIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			var textCtrl = TryGetInstance();
			if (textCtrl == null)
				return CommandTargetStatus.NotHandled;

			if (group == DocumentViewerCommandConstants.DocumentViewerGroup) {
				switch ((DocumentViewerIds)cmdId) {
				case DocumentViewerIds.MoveToNextReference:
					textCtrl.MoveReference(true);
					return CommandTargetStatus.Handled;

				case DocumentViewerIds.MoveToPreviousReference:
					textCtrl.MoveReference(false);
					return CommandTargetStatus.Handled;

				case DocumentViewerIds.MoveToNextDefinition:
					textCtrl.MoveToNextDefinition(true);
					return CommandTargetStatus.Handled;

				case DocumentViewerIds.MoveToPreviousDefinition:
					textCtrl.MoveToNextDefinition(false);
					return CommandTargetStatus.Handled;

				case DocumentViewerIds.FollowReference:
					textCtrl.FollowReference();
					return CommandTargetStatus.Handled;

				case DocumentViewerIds.FollowReferenceNewTab:
					textCtrl.FollowReferenceNewTab();
					return CommandTargetStatus.Handled;

				case DocumentViewerIds.ClearMarkedReferencesAndToolTip:
					//TODO:textCtrl.ClearMarkedReferencesAndToolTip();
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(DocumentViewerIds)} id: {(DocumentViewerIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
