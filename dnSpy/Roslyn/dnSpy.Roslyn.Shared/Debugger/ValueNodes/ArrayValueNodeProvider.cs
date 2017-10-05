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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class ArrayValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => throw new NotSupportedException();
		public override string Expression => valueInfo.Expression;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Array;
		public override bool? HasChildren => arrayCount > 0;

		readonly DbgDotNetValueNodeProviderFactory owner;
		readonly DbgDotNetValueNodeInfo valueInfo;
		readonly uint arrayCount;
		readonly DbgDotNetArrayDimensionInfo[] dimensionInfos;
		// This one's only non-null if this is an array with 2 or more dimensions
		readonly int[] indexes;

		public ArrayValueNodeProvider(DbgDotNetValueNodeProviderFactory owner, DbgDotNetValueNodeInfo valueInfo) {
			this.owner = owner;
			this.valueInfo = valueInfo;

			bool b = valueInfo.Value.GetArrayInfo(out arrayCount, out dimensionInfos) && dimensionInfos.Length != 0;
			Debug.Assert(b);
			if (!b)
				dimensionInfos = new[] { new DbgDotNetArrayDimensionInfo(0, arrayCount) };
			if (dimensionInfos.Length > 1)
				indexes = new int[dimensionInfos.Length];
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) => arrayCount;

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			DbgDotNetValue newValue = null;
			try {
				var output = ObjectCache.AllocDotNetTextOutput();
				var elementType = valueInfo.Value.Type.GetElementType();
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();

					string expression;
					uint arrayIndex = (uint)index + (uint)i;
					newValue = valueInfo.Value.GetArrayElementAt(arrayIndex);
					Debug.Assert(newValue != null);

					if (dimensionInfos.Length == 1) {
						int baseIndex = (int)arrayIndex + dimensionInfos[0].BaseIndex;
						expression = valueNodeFactory.GetExpression(valueInfo.Expression, baseIndex);
						owner.FormatArrayName(output, baseIndex);
					}
					else {
						uint indexLeft = arrayIndex;
						for (int j = dimensionInfos.Length - 1; j >= 0; j--) {
							indexes[j] = (int)(indexLeft % dimensionInfos[j].Length) + dimensionInfos[j].BaseIndex;
							indexLeft = indexLeft / dimensionInfos[j].Length;
						}
						expression = valueNodeFactory.GetExpression(valueInfo.Expression, indexes);
						owner.FormatArrayName(output, indexes);
					}

					var name = output.CreateAndReset();
					const bool isReadOnly = false;
					DbgDotNetValueNode newNode;
					if (newValue == null)
						newNode = valueNodeFactory.CreateError(context, frame, name, PredefinedEvaluationErrorMessages.InternalDebuggerError, expression, false, cancellationToken);
					else
						newNode = valueNodeFactory.Create(context, frame, name, newValue, options, expression, PredefinedDbgValueNodeImageNames.ArrayElement, isReadOnly, false, elementType, cancellationToken);
					newValue = null;
					res[i] = newNode;
				}
				ObjectCache.Free(ref output);
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
				newValue?.Dispose();
				throw;
			}
			return res;
		}

		public override void Dispose() { }
	}
}
