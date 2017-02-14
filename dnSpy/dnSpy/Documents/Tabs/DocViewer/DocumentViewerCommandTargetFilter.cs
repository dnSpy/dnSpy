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
using System.Diagnostics;
using dnSpy.Contracts.Command;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class DocumentViewerCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public DocumentViewerCommandTargetFilter(ITextView textView) => this.textView = textView;

		DocumentViewer TryGetInstance() =>
			__documentViewer ?? (__documentViewer = DocumentViewer.TryGetInstance(textView));
		DocumentViewer __documentViewer;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (TryGetInstance() == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.CANCEL:
					return CommandTargetStatus.Handled;
				}
			}
			else if (group == CommandConstants.TextReferenceGroup) {
				switch ((TextReferenceIds)cmdId) {
				case TextReferenceIds.MoveToNextReference:
				case TextReferenceIds.MoveToPreviousReference:
				case TextReferenceIds.MoveToNextDefinition:
				case TextReferenceIds.MoveToPreviousDefinition:
				case TextReferenceIds.FollowReference:
				case TextReferenceIds.FollowReferenceNewTab:
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(TextReferenceIds)} id: {(TextReferenceIds)cmdId}");
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
			var documentViewer = TryGetInstance();
			if (documentViewer == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.CANCEL:
					DocumentViewerHighlightReferencesTagger.ClearMarkedReferences(documentViewer.TextView);
					documentViewer.TextView.Selection.Clear();
					return CommandTargetStatus.Handled;
				}
			}
			else if (group == CommandConstants.TextReferenceGroup) {
				switch ((TextReferenceIds)cmdId) {
				case TextReferenceIds.MoveToNextReference:
					documentViewer.MoveReference(true);
					return CommandTargetStatus.Handled;

				case TextReferenceIds.MoveToPreviousReference:
					documentViewer.MoveReference(false);
					return CommandTargetStatus.Handled;

				case TextReferenceIds.MoveToNextDefinition:
					documentViewer.MoveToNextDefinition(true);
					return CommandTargetStatus.Handled;

				case TextReferenceIds.MoveToPreviousDefinition:
					documentViewer.MoveToNextDefinition(false);
					return CommandTargetStatus.Handled;

				case TextReferenceIds.FollowReference:
					documentViewer.FollowReference();
					return CommandTargetStatus.Handled;

				case TextReferenceIds.FollowReferenceNewTab:
					documentViewer.FollowReferenceNewTab();
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(TextReferenceIds)} id: {(TextReferenceIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
