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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.Autos {
	[Export(typeof(AutosContent))]
	sealed class AutosContent : VariablesWindowContentBase {
		public static readonly Guid VariablesWindowGuid = new Guid("F183274A-8EC3-4DE7-A291-388C6BB73362");

		[ImportingConstructor]
		AutosContent(IWpfCommandService wpfCommandService, VariablesWindowVMFactory variablesWindowVMFactory) =>
			Initialize(wpfCommandService, variablesWindowVMFactory, CreateVariablesWindowVMOptions());

		VariablesWindowVMOptions CreateVariablesWindowVMOptions() {
			var options = new VariablesWindowVMOptions() {
				VariablesWindowValueNodesProvider = new AutosVariablesWindowValueNodesProvider(),
				WindowContentType = ContentTypes.AutosWindow,
				NameColumnName = PredefinedTextClassifierTags.AutosWindowName,
				ValueColumnName = PredefinedTextClassifierTags.AutosWindowValue,
				TypeColumnName = PredefinedTextClassifierTags.AutosWindowType,
				VariablesWindowKind = VariablesWindowKind.Autos,
				VariablesWindowGuid = VariablesWindowGuid,
			};
			return options;
		}
	}
}
