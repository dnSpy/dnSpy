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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	[ExportCommandTargetFilterProvider(CommandConstants.CMDTARGETFILTER_ORDER_DOCUMENTVIEWER - 1)]
	sealed class DocumentViewerCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider;

		[ImportingConstructor]
		DocumentViewerCommandTargetFilterProvider(DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider) {
			this.documentViewerToolTipServiceProvider = documentViewerToolTipServiceProvider;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(PredefinedDnSpyTextViewRoles.DocumentViewer) != true)
				return null;

			return new DocumentViewerCommandTargetFilter(documentViewerToolTipServiceProvider, textView);
		}
	}

	sealed class DocumentViewerCommandTargetFilter : ICommandTargetFilter {
		readonly DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider;
		readonly ITextView textView;

		public DocumentViewerCommandTargetFilter(DocumentViewerToolTipServiceProvider documentViewerToolTipServiceProvider, ITextView textView) {
			this.documentViewerToolTipServiceProvider = documentViewerToolTipServiceProvider;
			this.textView = textView;
		}

		DocumentViewerToolTipService TryGetInstance() {
			if (__documentViewerToolTipService == null) {
				var docViewer = textView.TextBuffer.TryGetDocumentViewer();
				if (docViewer != null)
					__documentViewerToolTipService = documentViewerToolTipServiceProvider.GetService(docViewer);
			}
			return __documentViewerToolTipService;
		}
		DocumentViewerToolTipService __documentViewerToolTipService;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			var service = TryGetInstance();
			if (service == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.CANCEL:
					if (service.IsToolTipOpen)
						return CommandTargetStatus.Handled;
					else
						return nextCommandTarget.CanExecute(group, cmdId);
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			var service = TryGetInstance();
			if (service == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.CANCEL:
					if (service.IsToolTipOpen) {
						service.CloseToolTip();
						return CommandTargetStatus.Handled;
					}
					else
						return nextCommandTarget.Execute(group, cmdId, args, ref result);
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		ICommandTarget nextCommandTarget;
		public void SetNextCommandTarget(ICommandTarget commandTarget) => nextCommandTarget = commandTarget;
		public void Dispose() { }
	}
}
