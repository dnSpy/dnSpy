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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Shared.Intellisense.QuickInfo {
	[ExportCommandTargetFilterProvider(CommandConstants.CMDTARGETFILTER_ORDER_ROSLYN_QUICKINFO)]
	sealed class DefaultTextViewCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<IQuickInfoTriggerServiceProvider> quickInfoTriggerServiceProvider;

		[ImportingConstructor]
		DefaultTextViewCommandTargetFilterProvider(Lazy<IQuickInfoTriggerServiceProvider> quickInfoTriggerServiceProvider) {
			this.quickInfoTriggerServiceProvider = quickInfoTriggerServiceProvider;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView != null && textView.Roles.ContainsAll(roles))
				return new CommandTargetFilter(textView, quickInfoTriggerServiceProvider.Value.Create(textView));
			return null;
		}
		static readonly string[] roles = new string[] {
			PredefinedDsTextViewRoles.RoslynCodeEditor,
			PredefinedTextViewRoles.Editable,
		};
	}

	sealed class CommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;
		readonly IQuickInfoTriggerService quickInfoTriggerService;

		public CommandTargetFilter(ITextView textView, IQuickInfoTriggerService quickInfoTriggerService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (quickInfoTriggerService == null)
				throw new ArgumentNullException(nameof(quickInfoTriggerService));
			this.textView = textView;
			this.quickInfoTriggerService = quickInfoTriggerService;
		}

		bool IsSupportedContentType => textView.TextDataModel.ContentType.IsOfType(ContentTypes.RoslynCode);

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (!IsSupportedContentType)
				return CommandTargetStatus.NotHandled;
			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.QUICKINFO:
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			if (!IsSupportedContentType)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.QUICKINFO:
					if (TryTriggerQuickInfo(false))
						return CommandTargetStatus.Handled;
					break;
				}
			}

			return CommandTargetStatus.NotHandled;
		}

		bool TryTriggerQuickInfo(bool trackMouse) {
			if (textView.IsClosed)
				return false;
			var caretPos = textView.Caret.Position;
			if (caretPos.VirtualSpaces > 0)
				return false;
			return quickInfoTriggerService.TryTrigger(caretPos.BufferPosition, trackMouse);
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
