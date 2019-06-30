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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.Watch {
	sealed class WatchContent : VariablesWindowContentBase {
		public static Guid GetVariablesWindowGuid(int index) =>
			new Guid("912F5C0B-173A-4525-969D-DD97" + (0x15CA417C + index).ToString("X8"));

		readonly int windowIndex;
		readonly WatchVariablesWindowValueNodesProvider watchVariablesWindowValueNodesProvider;

		public WatchContent(int windowIndex, WatchVariablesWindowValueNodesProvider watchVariablesWindowValueNodesProvider, IWpfCommandService wpfCommandService, VariablesWindowVMFactory variablesWindowVMFactory) {
			this.windowIndex = windowIndex;
			this.watchVariablesWindowValueNodesProvider = watchVariablesWindowValueNodesProvider;
			Initialize(wpfCommandService, variablesWindowVMFactory, CreateVariablesWindowVMOptions());
		}

		VariablesWindowVMOptions CreateVariablesWindowVMOptions() {
			var options = new VariablesWindowVMOptions() {
				VariablesWindowValueNodesProvider = watchVariablesWindowValueNodesProvider,
				WindowContentType = ContentTypes.WatchWindow,
				NameColumnName = PredefinedTextClassifierTags.WatchWindowName,
				ValueColumnName = PredefinedTextClassifierTags.WatchWindowValue,
				TypeColumnName = PredefinedTextClassifierTags.WatchWindowType,
				VariablesWindowKind = VariablesWindowKind.Watch,
				VariablesWindowGuid = GetVariablesWindowGuid(windowIndex),
			};
			return options;
		}
	}
}
