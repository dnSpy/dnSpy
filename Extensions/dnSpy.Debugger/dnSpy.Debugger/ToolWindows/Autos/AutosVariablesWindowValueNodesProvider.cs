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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.Autos {
	sealed class AutosVariablesWindowValueNodesProvider : VariablesWindowValueNodesProvider {
		public override ValueNodesProviderResult GetNodes(DbgEvaluationInfo evalInfo, DbgLanguage language, DbgEvaluationOptions evalOptions, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions) {
			var returnValues = language.ReturnValuesProvider.GetNodes(evalInfo, nodeEvalOptions);
			var variables = language.AutosProvider.GetNodes(evalInfo, nodeEvalOptions);

			var res = new DbgValueNodeInfo[returnValues.Length + variables.Length];
			int ri = 0;
			for (int i = 0; i < returnValues.Length; i++, ri++)
				res[ri] = new DbgValueNodeInfo(returnValues[i], GetNextReturnValueId(), causesSideEffects: false);
			for (int i = 0; i < variables.Length; i++, ri++)
				res[ri] = new DbgValueNodeInfo(variables[i], causesSideEffects: false);

			const bool recreateAllNodes = false;
			return new ValueNodesProviderResult(res, recreateAllNodes);
		}

		string GetNextReturnValueId() => returnValueIdBase + returnValueCounter++.ToString();
		uint returnValueCounter;
		readonly string returnValueIdBase = "rid-" + Guid.NewGuid().ToString() + "-";
	}
}
