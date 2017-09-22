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
	sealed class StaticMembersValueNodeProvider : MembersValueNodeProvider {
		public override string ImageName => PredefinedDbgValueNodeImageNames.StaticMembers;

		public StaticMembersValueNodeProvider(LanguageValueNodeFactory valueNodeFactory, DbgDotNetText name, string expression, MemberValueNodeInfoCollection membersCollection, DbgValueNodeEvaluationOptions evalOptions)
			: base(valueNodeFactory, name, expression, membersCollection, evalOptions) {
		}

		protected override DbgDotNetValueNode CreateValueNode(DbgEvaluationContext context, DbgStackFrame frame, int index, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var runtime = context.Runtime.GetDotNetRuntime();
			DbgDotNetValueResult valueResult = default;
			try {
				ref var info = ref membersCollection.Members[index];
				string expression, imageName;
				bool isReadOnly;
				DmdType expectedType;
				switch (info.Member.MemberType) {
				case DmdMemberTypes.Field:
					var field = (DmdFieldInfo)info.Member;
					expression = valueNodeFactory.GetFieldExpression(Expression, field.Name);
					expectedType = field.FieldType;
					imageName = ImageNameUtils.GetImageName(field);
					valueResult = runtime.LoadField(context, frame, null, field, cancellationToken);
					// Ingore field.IsInitOnly
					isReadOnly = field.IsLiteral;
					break;

				case DmdMemberTypes.Property:
					var property = (DmdPropertyInfo)info.Member;
					expression = valueNodeFactory.GetPropertyExpression(Expression, property.Name);
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
				return newNode;
			}
			catch {
				valueResult.Value?.Dispose();
				throw;
			}
		}
	}
}
