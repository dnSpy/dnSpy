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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class StaticMembersValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name { get; }
		public override string Expression { get; }
		public override string ImageName => PredefinedDbgValueNodeImageNames.StaticMembers;
		public override bool? HasChildren => members.Length > 0;

		readonly MemberValueNodeInfo[] members;

		public StaticMembersValueNodeProvider(DbgDotNetText name, string expression, MemberValueNodeInfo[] members) {
			Name = name;
			Expression = expression;
			this.members = members;
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) => (uint)members.Length;

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var runtime = context.Runtime.GetDotNetRuntime();
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			DbgDotNetValueResult valueResult = default;
			try {
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					ref var info = ref members[(int)index + i];
					if (info.HasDebuggerBrowsableState_RootHidden && (options & DbgValueNodeEvaluationOptions.RawView) == 0) {
						//TODO:
					}
					string expression, imageName;
					bool isReadOnly;
					DmdType expectedType;
					switch (info.Member.MemberType) {
					case DmdMemberTypes.Field:
						var field = (DmdFieldInfo)info.Member;
						expression = valueNodeFactory.GetExpression(Expression, field);
						expectedType = field.FieldType;
						imageName = ImageNameUtils.GetImageName(field);
						valueResult = runtime.LoadField(context, frame, null, field, cancellationToken);
						isReadOnly = field.IsInitOnly || field.IsLiteral;
						break;

					case DmdMemberTypes.Property:
						var property = (DmdPropertyInfo)info.Member;
						expression = valueNodeFactory.GetExpression(Expression, property);
						expectedType = property.PropertyType;
						imageName = ImageNameUtils.GetImageName(property);
						if ((options & DbgValueNodeEvaluationOptions.NoFuncEval) != 0) {
							isReadOnly = true;
							valueResult = new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
						}
						else {
							var getter = property.GetGetMethod(DmdGetAccessorOptions.All) ?? throw new InvalidOperationException();
							valueResult = runtime.Call(context, frame, null, getter, Array.Empty<object>(), cancellationToken);
							isReadOnly = (object)property.GetSetMethod(DmdGetAccessorOptions.All) == null;
						}
						break;

					default:
						throw new InvalidOperationException();
					}

					DbgDotNetValueNode newNode;
					if (valueResult.HasError)
						newNode = valueNodeFactory.CreateError(context, frame, info.Name, valueResult.ErrorMessage, expression, cancellationToken);
					else if (valueResult.ValueIsException)
						newNode = valueNodeFactory.Create(context, frame, info.Name, valueResult.Value, options, expression, PredefinedDbgValueNodeImageNames.Error, true, false, expectedType, cancellationToken);
					else
						newNode = valueNodeFactory.Create(context, frame, info.Name, valueResult.Value, options, expression, imageName, isReadOnly, false, expectedType, cancellationToken);

					valueResult = default;
					res[i] = newNode;
				}
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
				valueResult.Value?.Dispose();
				throw;
			}
			return res;
		}

		public override void Dispose() { }
	}
}
