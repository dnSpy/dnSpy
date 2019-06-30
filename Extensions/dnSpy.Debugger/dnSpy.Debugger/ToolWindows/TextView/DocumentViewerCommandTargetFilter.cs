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
using System.Diagnostics;
using dnSpy.Contracts.Command;

namespace dnSpy.Debugger.ToolWindows.TextView {
	sealed class DocumentViewerCommandTargetFilter : ICommandTargetFilter {
		readonly Lazy<ToolWindowsOperations> toolWindowsOperations;

		public DocumentViewerCommandTargetFilter(Lazy<ToolWindowsOperations> toolWindowsOperations) => this.toolWindowsOperations = toolWindowsOperations;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == DebuggerCommandConstants.DebuggerToolWindowGroup) {
				switch ((DebuggerToolWindowIds)cmdId) {
				case DebuggerToolWindowIds.ShowAutos:
					return toolWindowsOperations.Value.CanShowAutos ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandled;

				case DebuggerToolWindowIds.ShowWatch1:
				case DebuggerToolWindowIds.ShowWatch2:
				case DebuggerToolWindowIds.ShowWatch3:
				case DebuggerToolWindowIds.ShowWatch4:
					return toolWindowsOperations.Value.CanShowWatch(cmdId - (int)DebuggerToolWindowIds.ShowWatch1) ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandled;

				default:
					Debug.Fail($"Unknown {nameof(DebuggerToolWindowIds)} id: {(DebuggerToolWindowIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
			if (group == DebuggerCommandConstants.DebuggerToolWindowGroup) {
				switch ((DebuggerToolWindowIds)cmdId) {
				case DebuggerToolWindowIds.ShowAutos:
					toolWindowsOperations.Value.ShowAutos();
					return CommandTargetStatus.Handled;

				case DebuggerToolWindowIds.ShowWatch1:
				case DebuggerToolWindowIds.ShowWatch2:
				case DebuggerToolWindowIds.ShowWatch3:
				case DebuggerToolWindowIds.ShowWatch4:
					toolWindowsOperations.Value.ShowWatch(cmdId - (int)DebuggerToolWindowIds.ShowWatch1);
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(DebuggerToolWindowIds)} id: {(DebuggerToolWindowIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
