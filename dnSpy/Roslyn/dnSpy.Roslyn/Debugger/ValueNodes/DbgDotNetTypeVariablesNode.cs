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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Properties;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class DbgDotNetTypeVariablesNode : DbgDotNetValueNode {
		public override DmdType ExpectedType => null;
		public override DmdType ActualType => null;
		public override string ErrorMessage => null;
		public override DbgDotNetValue Value => null;
		public override DbgDotNetText Name => typeVariablesName;
		public override string Expression => "<type variables>";
		public override string ImageName => PredefinedDbgValueNodeImageNames.TypeVariables;
		public override bool IsReadOnly => true;
		public override ReadOnlyCollection<string> FormatSpecifiers => null;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => typeVariableInfos.Length > 0;

		static readonly DbgDotNetText typeVariablesName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Text, dnSpy_Roslyn_Resources.LocalsWindow_TypeVariables));

		readonly LanguageValueNodeFactory valueNodeFactory;
		readonly DbgDotNetTypeVariableInfo[] typeVariableInfos;

		public DbgDotNetTypeVariablesNode(LanguageValueNodeFactory valueNodeFactory, DbgDotNetTypeVariableInfo[] typeVariableInfos) {
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.typeVariableInfos = typeVariableInfos ?? throw new ArgumentNullException(nameof(typeVariableInfos));
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) => (uint)typeVariableInfos.Length;

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) {
			var res = new DbgDotNetValueNode[count];
			try {
				for (int i = 0, j = (int)index; i < count; i++, j++)
					res[i] = new TypeVariableValueNode(valueNodeFactory, typeVariableInfos[j]);
			}
			catch {
				evalInfo.Context.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
			return res;
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}

	sealed class TypeVariableValueNode : DbgDotNetValueNode {
		public override DmdType ExpectedType { get; }
		public override DmdType ActualType => ExpectedType;
		public override string ErrorMessage => null;
		public override DbgDotNetValue Value { get; }
		public override DbgDotNetText Name { get; }
		public override string Expression => "<generic type variable>";
		public override string ImageName { get; }
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override ReadOnlyCollection<string> FormatSpecifiers => null;
		public override bool? HasChildren => false;

		public TypeVariableValueNode(LanguageValueNodeFactory valueNodeFactory, DbgDotNetTypeVariableInfo info) {
			ExpectedType = info.GenericArgumentType;
			Value = new TypeVariableValue(info.GenericArgumentType);
			var paramType = info.GenericParameterType;
			bool isMethodParam = (object)paramType.DeclaringMethod != null;
			ImageName = isMethodParam ? PredefinedDbgValueNodeImageNames.GenericMethodParameter : PredefinedDbgValueNodeImageNames.GenericTypeParameter;
			Name = valueNodeFactory.GetTypeParameterName(paramType);
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) => 0;
		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) => Array.Empty<DbgDotNetValueNode>();
		protected override void CloseCore(DbgDispatcher dispatcher) => Value.Dispose();
	}

	sealed class TypeVariableValue : DbgDotNetValue {
		public override DmdType Type => type;
		readonly DmdType type;

		public TypeVariableValue(DmdType type) => this.type = type ?? throw new ArgumentNullException(nameof(type));

		public override DbgDotNetRawValue GetRawValue() => new DbgDotNetRawValue(DbgSimpleValueType.Other, type);
	}
}
