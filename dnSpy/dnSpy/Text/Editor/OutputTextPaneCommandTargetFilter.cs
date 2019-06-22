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
using dnSpy.Contracts.Output;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class OutputTextPaneCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public OutputTextPaneCommandTargetFilter(ITextView textView) => this.textView = textView;

		IOutputTextPane TryGetInstance() => __outputTextPane ??= OutputTextPaneUtils.TryGetInstance(textView);
		IOutputTextPane? __outputTextPane;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (TryGetInstance() is null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.OutputTextPaneGroup) {
				switch ((OutputTextPaneIds)cmdId) {
				case OutputTextPaneIds.ClearAll:
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(OutputTextPaneIds)} value: {(OutputTextPaneIds)cmdId}");
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
			var textPane = TryGetInstance();
			if (textPane is null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.OutputTextPaneGroup) {
				switch ((OutputTextPaneIds)cmdId) {
				case OutputTextPaneIds.ClearAll:
					textPane.Clear();
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
