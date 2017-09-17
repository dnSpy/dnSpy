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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class DbgDotNetTypeVariablesNode : DbgDotNetValueNode {
		public override DmdType ExpectedType => null;
		public override string ErrorMessage => null;
		public override DbgDotNetValue Value => null;
		public override DbgDotNetText Name => typeVariablesName;
		public override string Expression => "<type variables>";
		public override string ImageName => PredefinedDbgValueNodeImageNames.TypeVariables;
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => typeVariableInfos.Length > 0;
		public override ulong ChildCount => (uint)typeVariableInfos.Length;

		static readonly DbgDotNetText typeVariablesName = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, dnSpy_Roslyn_Shared_Resources.LocalsWindow_TypeVariables));

		readonly LanguageValueNodeFactory valueNodeFactory;
		readonly DbgDotNetTypeVariableInfo[] typeVariableInfos;

		public DbgDotNetTypeVariablesNode(LanguageValueNodeFactory valueNodeFactory, DbgDotNetTypeVariableInfo[] typeVariableInfos) {
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.typeVariableInfos = typeVariableInfos ?? throw new ArgumentNullException(nameof(typeVariableInfos));
		}

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var res = new DbgDotNetValueNode[count];
			try {
				for (int i = 0, j = (int)index; i < count; i++, j++)
					res[i] = new TypeVariableValueNode(valueNodeFactory, typeVariableInfos[j]);
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
			return res;
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}

	sealed class TypeVariableValueNode : DbgDotNetValueNode {
		public override DmdType ExpectedType { get; }
		public override string ErrorMessage => null;
		public override DbgDotNetValue Value { get; }
		public override DbgDotNetText Name { get; }
		public override string Expression => "<generic type variable>";
		public override string ImageName { get; }
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => false;
		public override ulong ChildCount => 0;

		public TypeVariableValueNode(LanguageValueNodeFactory valueNodeFactory, DbgDotNetTypeVariableInfo info) {
			ExpectedType = info.GenericArgumentType;
			Value = new TypeVariableValue(info.GenericArgumentType);
			var paramType = info.GenericParameterType;
			bool isMethodParam = (object)paramType.DeclaringMethod != null;
			ImageName = isMethodParam ? PredefinedDbgValueNodeImageNames.GenericMethodParameter : PredefinedDbgValueNodeImageNames.GenericTypeParameter;
			Name = new DbgDotNetText(new DbgDotNetTextPart(isMethodParam ? BoxedTextColor.MethodGenericParameter : BoxedTextColor.TypeGenericParameter, valueNodeFactory.EscapeIdentifier(paramType.Name ?? string.Empty)));
		}

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => Array.Empty<DbgDotNetValueNode>();
		protected override void CloseCore(DbgDispatcher dispatcher) => Value.Dispose();
	}

	sealed class TypeVariableValue : DbgDotNetValue {
		public override DmdType Type => type;
		public override bool IsReference => false;
		public override bool IsNullReference => false;
		public override bool IsBox => false;
		public override bool IsArray => false;

		readonly DmdType type;

		public TypeVariableValue(DmdType type) => this.type = type ?? throw new ArgumentNullException(nameof(type));

		public override ulong? GetReferenceAddress() => null;
		public override DbgDotNetValue Dereference() => null;
		public override DbgDotNetValue Unbox() => null;

		public override bool GetArrayCount(out uint elementCount) {
			elementCount = 0;
			return false;
		}

		public override bool GetArrayInfo(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			elementCount = 0;
			dimensionInfos = null;
			return false;
		}

		public override DbgDotNetValue GetArrayElementAt(uint index) => null;
		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) => null;
		public override DbgDotNetRawValue GetRawValue() => new DbgDotNetRawValue(DbgSimpleValueType.Other, type);
		public override void Dispose() { }
	}
}
