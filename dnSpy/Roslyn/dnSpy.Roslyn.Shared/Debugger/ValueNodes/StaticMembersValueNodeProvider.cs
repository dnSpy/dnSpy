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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine;
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
		public override ulong ChildCount => (uint)members.Length;

		readonly MemberValueNodeInfo[] members;

		public StaticMembersValueNodeProvider(DbgDotNetText name, string expression, DbgDotNetInstanceValueInfo valueInfo, MemberValueNodeInfo[] members) {
			Name = name;
			Expression = expression;
			this.members = members;
		}

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			DbgDotNetValue newValue = null;
			try {
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					ref var info = ref members[(int)index + i];
					if (info.HasDebuggerBrowsableState_RootHidden && (options & DbgValueNodeEvaluationOptions.RawView) == 0) {
						//TODO:
					}
					DbgDotNetValueNode newNode;
					string expression, imageName;
					bool isReadOnly;
					switch (info.Member.MemberType) {
					case DmdMemberTypes.Field:
						var field = (DmdFieldInfo)info.Member;
						expression = valueNodeFactory.GetExpression(Expression, field);
						imageName = ImageNameUtils.GetImageName(field);
						newValue = (DbgDotNetValue)field.AppDomain.LoadField(context, field, null, cancellationToken);
						isReadOnly = field.IsInitOnly;
						newNode = valueNodeFactory.Create(context, info.Name, newValue, options, expression, imageName, isReadOnly, false, field.FieldType);
						break;

					case DmdMemberTypes.Property:
						var property = (DmdPropertyInfo)info.Member;
						expression = valueNodeFactory.GetExpression(Expression, property);
						if ((options & DbgValueNodeEvaluationOptions.NoFuncEval) != 0)
							newNode = valueNodeFactory.CreateError(context, info.Name, PredefinedEvaluationErrorMessages.FunctionEvaluationDisabled, expression);
						else {
							var getter = property.GetGetMethod(DmdGetAccessorOptions.All) ?? throw new InvalidOperationException();
							imageName = ImageNameUtils.GetImageName(property);
							newValue = (DbgDotNetValue)property.AppDomain.Invoke(context, getter, null, Array.Empty<object>(), cancellationToken);
							isReadOnly = (object)property.GetSetMethod(DmdGetAccessorOptions.All) == null;
							newNode = valueNodeFactory.Create(context, info.Name, newValue, options, expression, imageName, isReadOnly, false, property.PropertyType);
						}
						break;

					default:
						throw new InvalidOperationException();
					}

					newValue = null;
					res[i] = newNode;
				}
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
