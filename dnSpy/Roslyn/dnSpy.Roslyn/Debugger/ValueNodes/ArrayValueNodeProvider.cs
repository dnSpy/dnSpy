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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class ArrayValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => arrayName;
		public override string Expression => valueInfo.Expression;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Array;
		public override bool? HasChildren => arrayCount > 0;
		static readonly DbgDotNetText arrayName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Punctuation, "[]"));

		readonly DbgDotNetValueNodeProviderFactory owner;
		readonly bool addParens;
		readonly DmdType slotType;
		readonly DbgDotNetValueNodeInfo valueInfo;
		readonly uint arrayCount;
		readonly DbgDotNetArrayDimensionInfo[] dimensionInfos;
		// This one's only non-null if this is an array with 2 or more dimensions
		readonly int[]? indexes;

		public ArrayValueNodeProvider(DbgDotNetValueNodeProviderFactory owner, bool addParens, DmdType slotType, DbgDotNetValueNodeInfo valueInfo) {
			this.owner = owner;
			this.addParens = addParens;
			this.slotType = slotType;
			this.valueInfo = valueInfo;

			bool b = valueInfo.Value.GetArrayInfo(out arrayCount, out dimensionInfos!) && dimensionInfos.Length != 0;
			Debug.Assert(b);
			if (!b)
				dimensionInfos = new[] { new DbgDotNetArrayDimensionInfo(0, arrayCount) };
			if (dimensionInfos.Length > 1)
				indexes = new int[dimensionInfos.Length];
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) => arrayCount;

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string>? formatSpecifiers) {
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			DbgDotNetValueResult newValue = default;
			try {
				var output = ObjectCache.AllocDotNetTextOutput();
				var elementType = valueInfo.Value.Type.GetElementType()!;
				var castType = NeedCast(slotType, valueInfo.Value.Type) ? valueInfo.Value.Type : null;
				for (int i = 0; i < res.Length; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();

					string expression;
					uint arrayIndex = (uint)index + (uint)i;
					newValue = valueInfo.Value.GetArrayElementAt(arrayIndex);

					if (dimensionInfos.Length == 1) {
						int baseIndex = (int)arrayIndex + dimensionInfos[0].BaseIndex;
						expression = valueNodeFactory.GetExpression(valueInfo.Expression, baseIndex, castType, addParens);
						owner.FormatArrayName(output, baseIndex);
					}
					else {
						uint indexLeft = arrayIndex;
						Debug2.Assert(indexes is not null);
						for (int j = dimensionInfos.Length - 1; j >= 0; j--) {
							indexes[j] = (int)(indexLeft % dimensionInfos[j].Length) + dimensionInfos[j].BaseIndex;
							indexLeft = indexLeft / dimensionInfos[j].Length;
						}
						expression = valueNodeFactory.GetExpression(valueInfo.Expression, indexes, castType, addParens);
						owner.FormatArrayName(output, indexes);
					}

					var name = output.CreateAndReset();
					DbgDotNetValueNode? newNode;
					if (newValue.HasError)
						newNode = valueNodeFactory.CreateError(evalInfo, name, newValue.ErrorMessage!, expression, false);
					else {
						newNode = null;
						if (CSharpDynamicPropertyHelper.IsCSharpDynamicProperty(newValue.Value!.Type)) {
							var info = CSharpDynamicPropertyHelper.GetRealValue(evalInfo, newValue.Value);
							if (info.name is not null) {
								newValue.Value.Dispose();
								name = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.DebugViewPropertyName, info.name));
								expression = valueNodeFactory.GetFieldExpression(expression, info.valueField.Name, null, false);
								newNode = valueNodeFactory.Create(evalInfo, name, info.value, formatSpecifiers, options, expression, PredefinedDbgValueNodeImageNames.DynamicViewElement, true, false, info.valueField.FieldType, false);
							}
						}
						if (newNode is null)
							newNode = valueNodeFactory.Create(evalInfo, name, newValue.Value, formatSpecifiers, options, expression, PredefinedDbgValueNodeImageNames.ArrayElement, false, false, elementType, false);
					}
					newValue = default;
					res[i] = newNode;
				}
				ObjectCache.Free(ref output);
			}
			catch {
				evalInfo.Context.Process.DbgManager.Close(res.Where(a => a is not null));
				newValue.Value?.Dispose();
				throw;
			}
			return res;
		}

		public override void Dispose() { }
	}
}
