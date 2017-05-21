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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.Locals {
	[Export(typeof(LocalsContent))]
	sealed class LocalsContent : VariablesWindowContentBase {
		public static readonly Guid VariablesWindowGuid = new Guid("1A53B7B7-19AE-490F-9D67-F1992D849150");

		readonly DbgObjectIdService dbgObjectIdService;

		[ImportingConstructor]
		LocalsContent(IWpfCommandService wpfCommandService, VariablesWindowVMFactory variablesWindowVMFactory, DbgObjectIdService dbgObjectIdService) {
			this.dbgObjectIdService = dbgObjectIdService;
			Initialize(wpfCommandService, variablesWindowVMFactory, CreateVariablesWindowVMOptions());
		}

		sealed class DbgObjectIdComparer : IComparer<DbgObjectId> {
			public static readonly DbgObjectIdComparer Instance = new DbgObjectIdComparer();
			DbgObjectIdComparer() { }
			public int Compare(DbgObjectId x, DbgObjectId y) {
				if (x == y)
					return 0;
				if (x == null)
					return -1;
				if (y == null)
					return 1;
				return x.Id.CompareTo(y.Id);
			}
		}

		sealed class VariablesWindowValueNodesProviderImpl : VariablesWindowValueNodesProvider {
			public override event EventHandler NodesChanged;
			readonly DbgObjectIdService dbgObjectIdService;

			public VariablesWindowValueNodesProviderImpl(DbgObjectIdService dbgObjectIdService) =>
				this.dbgObjectIdService = dbgObjectIdService ?? throw new ArgumentNullException(nameof(dbgObjectIdService));

			public override void Initialize(bool enable) {
				if (enable)
					dbgObjectIdService.ObjectIdsChanged += DbgObjectIdService_ObjectIdsChanged;
				else
					dbgObjectIdService.ObjectIdsChanged -= DbgObjectIdService_ObjectIdsChanged;
			}

			void DbgObjectIdService_ObjectIdsChanged(object sender, EventArgs e) => NodesChanged?.Invoke(this, EventArgs.Empty);

			public override DbgValueNodeInfo[] GetNodes(DbgLanguage language, DbgStackFrame frame, DbgEvaluationOptions options) {
				var exceptions = language.ExceptionProvider.GetNodes(frame);
				var returnValues = language.ReturnValueProvider.GetNodes(frame);
				var variables = language.LocalsProvider.GetNodes(frame);

				var objectIds = dbgObjectIdService.GetObjectIds(frame.Runtime);
				Array.Sort(objectIds, DbgObjectIdComparer.Instance);

				var res = new DbgValueNodeInfo[exceptions.Length + returnValues.Length + objectIds.Length + variables.Length];
				int ri = 0;
				for (int i = 0; i < exceptions.Length; i++, ri++)
					res[ri] = new DbgValueNodeInfo(exceptions[i], GetNextExceptionId());
				for (int i = 0; i < returnValues.Length; i++, ri++)
					res[ri] = new DbgValueNodeInfo(returnValues[i], GetNextReturnValueId());

				var objectIdInfos = language.ValueNodeFactory.Create(objectIds);
				Debug.Assert(objectIdInfos.Length == objectIds.Length);
				for (int i = 0; i < objectIdInfos.Length; i++, ri++) {
					var id = GetObjectIdNodeId(objectIds[i]);
					var result = objectIdInfos[i];
					if (result.Error is string error)
						res[ri] = new DbgValueNodeInfo(id, result.Expression, error, causesSideEffects: false);
					else
						res[ri] = new DbgValueNodeInfo(result.ValueNode, id);
				}

				for (int i = 0; i < variables.Length; i++, ri++)
					res[ri] = new DbgValueNodeInfo(variables[i]);

				return res;
			}

			string GetNextExceptionId() => exceptionIdBase + exceptionCounter++.ToString();
			uint exceptionCounter;
			readonly string exceptionIdBase = "eid-" + Guid.NewGuid().ToString() + "-";

			string GetNextReturnValueId() => returnValueIdBase + returnValueCounter++.ToString();
			uint returnValueCounter;
			readonly string returnValueIdBase = "rid-" + Guid.NewGuid().ToString() + "-";

			string GetObjectIdNodeId(DbgObjectId objectId) => objectIdNodeIdBase + objectId.Id.ToString();
			readonly string objectIdNodeIdBase = "oid-" + Guid.NewGuid().ToString() + "-";
		}

		VariablesWindowVMOptions CreateVariablesWindowVMOptions() {
			var options = new VariablesWindowVMOptions() {
				VariablesWindowValueNodesProvider = new VariablesWindowValueNodesProviderImpl(dbgObjectIdService),
				WindowContentType = ContentTypes.LocalsWindow,
				NameColumnName = PredefinedTextClassifierTags.LocalsWindowName,
				ValueColumnName = PredefinedTextClassifierTags.LocalsWindowValue,
				TypeColumnName = PredefinedTextClassifierTags.LocalsWindowType,
				VariablesWindowKind = VariablesWindowKind.Locals,
				VariablesWindowGuid = VariablesWindowGuid,
			};
			return options;
		}
	}
}
