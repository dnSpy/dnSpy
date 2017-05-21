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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
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

		sealed class VariablesWindowValueNodesProviderImpl : VariablesWindowValueNodesProvider {
			public override DbgValueNodeInfo[] GetNodes(DbgLanguage language, DbgStackFrame frame, DbgEvaluationOptions options) {
				var returnValues = language.ReturnValueProvider.GetNodes(frame);
				var variables = language.AutosProvider.GetNodes(frame);

				var res = new DbgValueNodeInfo[returnValues.Length + variables.Length];
				int ri = 0;
				for (int i = 0; i < returnValues.Length; i++, ri++)
					res[ri] = new DbgValueNodeInfo(returnValues[i], GetNextReturnValueId());
				for (int i = 0; i < variables.Length; i++, ri++)
					res[ri] = new DbgValueNodeInfo(variables[i]);

				return res;
			}

			string GetNextReturnValueId() => returnValueIdBase + returnValueCounter++.ToString();
			uint returnValueCounter;
			readonly string returnValueIdBase = "rid-" + Guid.NewGuid().ToString() + "-";
		}

		VariablesWindowVMOptions CreateVariablesWindowVMOptions() {
			var options = new VariablesWindowVMOptions() {
				VariablesWindowValueNodesProvider = new VariablesWindowValueNodesProviderImpl(),
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
