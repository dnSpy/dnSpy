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
using dnSpy.Scripting.Roslyn.Commands;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class RoslynReplCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public RoslynReplCommandTargetFilter(ITextView textView) {
			this.textView = textView;
		}

		ScriptControlVM TryGetInstance() =>
			__replEditor ?? (__replEditor = RoslynReplEditorUtils.TryGetInstance(textView));
		ScriptControlVM __replEditor;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			var vm = TryGetInstance();
			if (vm == null)
				return CommandTargetStatus.NotHandled;

			if (group == RoslynReplCommandConstants.RoslynReplGroup) {
				switch ((RoslynReplIds)cmdId) {
				case RoslynReplIds.Reset:
					return vm.CanReset ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandledDontCallNextHandler;

				case RoslynReplIds.SaveText:
					return vm.CanSaveText ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandledDontCallNextHandler;

				case RoslynReplIds.SaveCode:
					return vm.CanSaveCode ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandledDontCallNextHandler;

				default:
					Debug.Fail($"Unknown {nameof(RoslynReplIds)} value: {(RoslynReplIds)cmdId}");
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

			if (group == RoslynReplCommandConstants.RoslynReplGroup) {
				switch ((RoslynReplIds)cmdId) {
				case RoslynReplIds.Reset:
					vm.Reset();
					return CommandTargetStatus.Handled;

				case RoslynReplIds.SaveText:
					vm.SaveText();
					return CommandTargetStatus.Handled;

				case RoslynReplIds.SaveCode:
					vm.SaveCode();
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(RoslynReplIds)} value: {(RoslynReplIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
