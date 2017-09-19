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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class TupleValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => throw new NotSupportedException();
		public override string Expression => throw new NotSupportedException();
		public override string ImageName => throw new NotSupportedException();
		public override bool? HasChildren => tupleFields.Length > 0;
		public override ulong ChildCount => (uint)tupleFields.Length;

		/*readonly*/ DbgDotNetInstanceValueInfo valueInfo;
		readonly TupleField[] tupleFields;

		public TupleValueNodeProvider(DbgDotNetInstanceValueInfo valueInfo, TupleField[] tupleFields) {
			this.valueInfo = valueInfo;
			this.tupleFields = tupleFields;
		}

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var runtime = context.Runtime.GetDotNetRuntime();
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			var valueResults = new List<DbgDotNetValueResult>();
			DbgDotNetValueResult valueResult = default;
			try {
				for (int i = 0; i < res.Length; i++) {
					ref var info = ref tupleFields[(int)index + i];
					var expression = valueNodeFactory.GetExpression(valueInfo.Expression, info.DefaultName);
					const string imageName = PredefinedDbgValueNodeImageNames.FieldPublic;
					const bool isReadOnly = false;
					var expectedType = info.Fields[info.Fields.Length - 1].FieldType;

					var objValue = valueInfo.Value;
					string errorMessage = null;
					bool valueIsException = false;
					for (int j = 0; j < info.Fields.Length; j++) {
						cancellationToken.ThrowIfCancellationRequested();
						valueResult = runtime.LoadField(context, frame, objValue, info.Fields[j], cancellationToken);
						objValue = valueResult.Value;
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

					var name = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.InstanceField, info.DefaultName));
					DbgDotNetValueNode newNode;
					if (errorMessage != null)
						newNode = valueNodeFactory.CreateError(context, frame, name, errorMessage, expression, cancellationToken);
					else if (valueIsException)
						newNode = valueNodeFactory.Create(context, frame, name, objValue, options, expression, PredefinedDbgValueNodeImageNames.Error, true, false, expectedType, cancellationToken);
					else
						newNode = valueNodeFactory.Create(context, frame, name, objValue, options, expression, imageName, isReadOnly, false, expectedType, cancellationToken);

					foreach (var vr in valueResults)
						vr.Value?.Dispose();
					valueResults.Clear();
					res[i] = newNode;
				}
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
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
