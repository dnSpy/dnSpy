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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.Locals {
	sealed class LocalsVariablesWindowValueNodesProvider : VariablesWindowValueNodesProvider {
		public override event EventHandler? NodesChanged;
		readonly DbgObjectIdService dbgObjectIdService;
		readonly DebuggerSettings debuggerSettings;
		bool forceRecreateAllNodes;

		public LocalsVariablesWindowValueNodesProvider(DbgObjectIdService dbgObjectIdService, DebuggerSettings debuggerSettings) {
			this.dbgObjectIdService = dbgObjectIdService ?? throw new ArgumentNullException(nameof(dbgObjectIdService));
			this.debuggerSettings = debuggerSettings ?? throw new ArgumentNullException(nameof(debuggerSettings));
		}

		public override void Initialize(bool enable) {
			if (enable) {
				dbgObjectIdService.ObjectIdsChanged += DbgObjectIdService_ObjectIdsChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			}
			else {
				dbgObjectIdService.ObjectIdsChanged -= DbgObjectIdService_ObjectIdsChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
		}

		void DebuggerSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(DebuggerSettings.SortParameters):
			case nameof(DebuggerSettings.SortLocals):
			case nameof(DebuggerSettings.GroupParametersAndLocalsTogether):
			case nameof(DebuggerSettings.ShowCompilerGeneratedVariables):
			case nameof(DebuggerSettings.ShowDecompilerGeneratedVariables):
			case nameof(DebuggerSettings.ShowReturnValues):
			case nameof(DebuggerSettings.ShowRawLocals):
				forceRecreateAllNodes = true;
				NodesChanged?.Invoke(this, EventArgs.Empty);
				break;
			}
		}

		void DbgObjectIdService_ObjectIdsChanged(object? sender, EventArgs e) => NodesChanged?.Invoke(this, EventArgs.Empty);

		sealed class DbgObjectIdComparer : IComparer<DbgObjectId> {
			public static readonly DbgObjectIdComparer Instance = new DbgObjectIdComparer();
			DbgObjectIdComparer() { }
			public int Compare([AllowNull] DbgObjectId x, [AllowNull] DbgObjectId y) {
				if (x == y)
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				return x.Id.CompareTo(y.Id);
			}
		}

		DbgLocalsValueNodeEvaluationOptions GetLocalsValueNodeOptions() {
			var res = DbgLocalsValueNodeEvaluationOptions.None;
			if (debuggerSettings.ShowCompilerGeneratedVariables)
				res |= DbgLocalsValueNodeEvaluationOptions.ShowCompilerGeneratedVariables;
			if (debuggerSettings.ShowDecompilerGeneratedVariables)
				res |= DbgLocalsValueNodeEvaluationOptions.ShowDecompilerGeneratedVariables;
			if (debuggerSettings.ShowRawLocals)
				res |= DbgLocalsValueNodeEvaluationOptions.ShowRawLocals;
			return res;
		}

		public override ValueNodesProviderResult GetNodes(DbgEvaluationInfo evalInfo, DbgLanguage language, DbgEvaluationOptions evalOptions, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions) {
			var recreateAllNodes = forceRecreateAllNodes;
			forceRecreateAllNodes = false;

			const CultureInfo? cultureInfo = null;
			var exceptions = language.ExceptionsProvider.GetNodes(evalInfo, nodeEvalOptions);
			var returnValues = debuggerSettings.ShowReturnValues ? language.ReturnValuesProvider.GetNodes(evalInfo, nodeEvalOptions) : Array.Empty<DbgValueNode>();
			var variables = language.LocalsProvider.GetNodes(evalInfo, nodeEvalOptions, GetLocalsValueNodeOptions());
			var typeVariables = language.TypeVariablesProvider.GetNodes(evalInfo, nodeEvalOptions);

			var objectIds = dbgObjectIdService.GetObjectIds(evalInfo.Frame.Runtime);
			Array.Sort(objectIds, DbgObjectIdComparer.Instance);

			int count = exceptions.Length + returnValues.Length + objectIds.Length + variables.Length + typeVariables.Length;
			if (count == 0)
				return new ValueNodesProviderResult(Array.Empty<DbgValueNodeInfo>(), false);
			var res = new DbgValueNodeInfo[count];
			int ri = 0;
			for (int i = 0; i < exceptions.Length; i++, ri++) {
				ulong id = (uint)i;
				var exception = exceptions[i];
				if (exception.ImageName == PredefinedDbgValueNodeImageNames.StowedException)
					id |= 1UL << 32;
				res[ri] = new DbgValueNodeInfo(exception, GetNextExceptionId(id), causesSideEffects: false);
			}
			for (int i = 0; i < returnValues.Length; i++, ri++)
				res[ri] = new DbgValueNodeInfo(returnValues[i], GetNextReturnValueId(), causesSideEffects: false);

			var objectIdNodes = language.ValueNodeFactory.Create(evalInfo, objectIds, nodeEvalOptions);
			Debug.Assert(objectIdNodes.Length == objectIds.Length);
			for (int i = 0; i < objectIdNodes.Length; i++, ri++) {
				var id = GetObjectIdNodeId(objectIds[i]);
				res[ri] = new DbgValueNodeInfo(objectIdNodes[i], id, causesSideEffects: false);
			}

			variables = GetSortedVariables(evalInfo, variables, nameFormatterOptions, cultureInfo);
			for (int i = 0; i < variables.Length; i++, ri++)
				res[ri] = new DbgValueNodeInfo(variables[i].ValueNode, causesSideEffects: false);

			for (int i = 0; i < typeVariables.Length; i++, ri++)
				res[ri] = new DbgValueNodeInfo(typeVariables[i], GetNextTypeVariableId((uint)i), causesSideEffects: false);

			if (res.Length != ri)
				throw new InvalidOperationException();

			return new ValueNodesProviderResult(res, recreateAllNodes);
		}

		DbgLocalsValueNodeInfo[] GetSortedVariables(DbgEvaluationInfo evalInfo, DbgLocalsValueNodeInfo[] variables, DbgValueFormatterOptions nameFormatterOptions, CultureInfo? cultureInfo) {
			if (variables.Length <= 1)
				return variables;

			var sortParameters = debuggerSettings.SortParameters;
			var sortLocals = debuggerSettings.SortLocals;

			// If the default options are used, don't sort at all. Let the locals provider
			// decide how to sort them.
			if (!sortParameters && !sortLocals)
				return variables;

			var output = new DbgStringBuilderTextWriter();
			if (debuggerSettings.GroupParametersAndLocalsTogether)
				return variables.OrderBy(a => GetName(evalInfo, output, a.ValueNode, nameFormatterOptions, cultureInfo), StringComparer.OrdinalIgnoreCase).ToArray();
			else {
				var locals = variables.Where(a => a.Kind == DbgLocalsValueNodeKind.Local).ToArray();
				var parameters = variables.Where(a => a.Kind == DbgLocalsValueNodeKind.Parameter).ToArray();
				var others = variables.Where(a => a.Kind != DbgLocalsValueNodeKind.Local && a.Kind != DbgLocalsValueNodeKind.Parameter).ToArray();

				if (sortLocals && locals.Length > 1)
					locals = locals.OrderBy(a => GetName(evalInfo, output, a.ValueNode, nameFormatterOptions, cultureInfo), StringComparer.OrdinalIgnoreCase).ToArray();
				if (sortParameters && parameters.Length > 1)
					parameters = parameters.OrderBy(a => GetName(evalInfo, output, a.ValueNode, nameFormatterOptions, cultureInfo), StringComparer.OrdinalIgnoreCase).ToArray();
				if ((sortLocals || sortParameters) && others.Length > 1)
					others = others.OrderBy(a => GetName(evalInfo, output, a.ValueNode, nameFormatterOptions, cultureInfo), StringComparer.OrdinalIgnoreCase).ToArray();

				var res = new DbgLocalsValueNodeInfo[locals.Length + parameters.Length + others.Length];
				int w = 0;
				for (int i = 0; i < parameters.Length; i++)
					res[w++] = parameters[i];
				for (int i = 0; i < locals.Length; i++)
					res[w++] = locals[i];
				for (int i = 0; i < others.Length; i++)
					res[w++] = others[i];
				if (w != res.Length)
					throw new InvalidOperationException();
				return res;
			}
		}

		string GetName(DbgEvaluationInfo evalInfo, DbgStringBuilderTextWriter output, DbgValueNode valueNode, DbgValueFormatterOptions options, CultureInfo? cultureInfo) {
			output.Reset();
			valueNode.FormatName(evalInfo, output, options, cultureInfo);
			return output.ToString();
		}

		string GetNextExceptionId(ulong id) => exceptionIdBase + id.ToString();
		readonly string exceptionIdBase = "eid-" + Guid.NewGuid().ToString() + "-";

		string GetNextTypeVariableId(uint id) => typeVariablesIdBase + id.ToString();
		readonly string typeVariablesIdBase = "eid-" + Guid.NewGuid().ToString() + "-";

		string GetNextReturnValueId() => returnValueIdBase + returnValueCounter++.ToString();
		uint returnValueCounter;
		readonly string returnValueIdBase = "rid-" + Guid.NewGuid().ToString() + "-";

		string GetObjectIdNodeId(DbgObjectId objectId) => objectIdNodeIdBase + objectId.Id.ToString();
		readonly string objectIdNodeIdBase = "oid-" + Guid.NewGuid().ToString() + "-";
	}
}
