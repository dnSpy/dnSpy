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
using System.Collections.ObjectModel;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class TupleValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => tupleName;
		public override string Expression => nodeInfo.Expression;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Structure;
		public override bool? HasChildren => tupleFields.Length > 0;
		static readonly DbgDotNetText tupleName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Punctuation, "()"));

		readonly bool addParens;
		readonly DmdType slotType;
		readonly DbgDotNetValueNodeInfo nodeInfo;
		readonly TupleField[] tupleFields;

		public TupleValueNodeProvider(bool addParens, DmdType slotType, DbgDotNetValueNodeInfo nodeInfo, TupleField[] tupleFields) {
			this.addParens = addParens;
			this.slotType = slotType;
			this.nodeInfo = nodeInfo;
			this.tupleFields = tupleFields;
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) => (uint)tupleFields.Length;

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string>? formatSpecifiers) {
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			var valueResults = new List<DbgDotNetValueResult>();
			DbgDotNetValueResult valueResult = default;
			try {
				for (int i = 0; i < res.Length; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					ref readonly var info = ref tupleFields[(int)index + i];
					var castType = NeedCast(slotType, nodeInfo.Value.Type) ? nodeInfo.Value.Type : null;
					var expression = valueNodeFactory.GetFieldExpression(nodeInfo.Expression, info.DefaultName, castType, addParens);
					const string imageName = PredefinedDbgValueNodeImageNames.FieldPublic;
					const bool isReadOnly = false;
					var expectedType = info.Fields[info.Fields.Length - 1].FieldType;

					var objValue = nodeInfo.Value;
					string? errorMessage = null;
					bool valueIsException = false;
					for (int j = 0; j < info.Fields.Length; j++) {
						evalInfo.CancellationToken.ThrowIfCancellationRequested();
						valueResult = runtime.LoadField(evalInfo, objValue, info.Fields[j]);
						objValue = valueResult.Value!;
						if (valueResult.HasError) {
							valueResults.Add(valueResult);
							errorMessage = valueResult.ErrorMessage;
							valueResult = default;
							break;
						}
						if (valueResult.ValueIsException) {
							valueIsException = true;
							valueResult = default;
							break;
						}
						if (j + 1 != info.Fields.Length)
							valueResults.Add(valueResult);
						valueResult = default;
					}

					var name = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.InstanceField, info.DefaultName));
					DbgDotNetValueNode newNode;
					if (errorMessage is not null)
						newNode = valueNodeFactory.CreateError(evalInfo, name, errorMessage, expression, false);
					else if (valueIsException)
						newNode = valueNodeFactory.Create(evalInfo, name, objValue, formatSpecifiers, options, expression, PredefinedDbgValueNodeImageNames.Error, true, false, expectedType, false);
					else
						newNode = valueNodeFactory.Create(evalInfo, name, objValue, formatSpecifiers, options, expression, imageName, isReadOnly, false, expectedType, false);

					foreach (var vr in valueResults)
						vr.Value?.Dispose();
					valueResults.Clear();
					res[i] = newNode;
				}
			}
			catch {
				evalInfo.Runtime.Process.DbgManager.Close(res.Where(a => a is not null));
				foreach (var vr in valueResults)
					vr.Value?.Dispose();
				valueResult.Value?.Dispose();
				throw;
			}
			return res;
		}

		public override void Dispose() { }
	}
}
