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
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.AsmEditor.Compiler {
	[ExportCommandTargetFilterProvider(CommandConstants.CMDTARGETFILTER_ORDER_EDITCODE)]
	sealed class ReplCommandTargetFilterProvider : ICommandTargetFilterProvider {
		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(PredefinedDnSpyTextViewRoles.CodeEditor) != true)
				return null;

			return new EditCodeCommandTargetFilter(textView);
		}
	}

	sealed class EditCodeCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public EditCodeCommandTargetFilter(ITextView textView) {
			this.textView = textView;
		}

		EditCodeVM TryGetInstance() =>
			__editCodeVM ?? (__editCodeVM = EditCodeVM.TryGet(textView));
		EditCodeVM __editCodeVM;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			var vm = TryGetInstance();
			if (vm == null)
				return CommandTargetStatus.NotHandled;

			if (group == EditCodeCommandConstants.EditCodeGroup) {
				switch ((EditCodeIds)cmdId) {
				case EditCodeIds.Compile:
					return vm.CanCompile ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandled;

				default:
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
			var vm = TryGetInstance();
			if (vm == null)
				return CommandTargetStatus.NotHandled;

			if (group == EditCodeCommandConstants.EditCodeGroup) {
				switch ((EditCodeIds)cmdId) {
				case EditCodeIds.Compile:
					vm.CompileCode();
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
