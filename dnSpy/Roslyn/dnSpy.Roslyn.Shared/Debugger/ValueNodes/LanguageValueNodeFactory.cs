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

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	abstract class LanguageValueNodeFactory : DbgDotNetValueNodeFactory {
		readonly DbgDotNetValueNodeProviderFactory valueNodeProviderFactory;

		protected abstract DbgDotNetValueNodeProviderFactory CreateValueNodeProviderFactory();

		protected LanguageValueNodeFactory() => valueNodeProviderFactory = CreateValueNodeProviderFactory();

		public abstract string GetFieldExpression(string baseExpression, string name);
		public abstract string GetPropertyExpression(string baseExpression, string name);
		public abstract string GetExpression(string baseExpression, int index);
		public abstract string GetExpression(string baseExpression, int[] indexes);
		public abstract string EscapeIdentifier(string identifier);

		internal DbgDotNetValueNode Create(DbgEvaluationContext context, DbgDotNetText name, DbgDotNetValueNodeProvider provider, DbgValueNodeEvaluationOptions options, string expression, string imageName, DbgDotNetText valueText) =>
			new DbgDotNetValueNodeImpl(this, provider, name, default, expression, imageName, true, false, null, null, null, valueText);

		DbgDotNetValueNode CreateValue(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, DbgDotNetValue value, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, CancellationToken cancellationToken) {
			var nodeInfo = new DbgDotNetValueNodeInfo(value, expression);
			var provider = valueNodeProviderFactory.Create(context, frame, nodeInfo, options, cancellationToken);
			return new DbgDotNetValueNodeImpl(this, provider, name, nodeInfo, expression, imageName, isReadOnly, causesSideEffects, expectedType, value.Type, null, default);
		}

		public sealed override DbgDotNetValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, DbgDotNetValue value, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, CancellationToken cancellationToken) =>
			CreateValue(context, frame, name, value, options, expression, imageName, isReadOnly, causesSideEffects, expectedType, cancellationToken);

		public sealed override DbgDotNetValueNode CreateException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var output = ObjectCache.AllocDotNetTextOutput();
			context.Language.Formatter.FormatExceptionName(context, output, id);
			var name = ObjectCache.FreeAndToText(ref output);
			var expression = name.ToString();
			const bool isReadOnly = true;
			const bool causesSideEffects = false;
			const string imageName = PredefinedDbgValueNodeImageNames.Exception;
			return CreateValue(context, frame, name, value, options, expression, imageName, isReadOnly, causesSideEffects, value.Type, cancellationToken);
		}

		public sealed override DbgDotNetValueNode CreateStowedException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var output = ObjectCache.AllocDotNetTextOutput();
			context.Language.Formatter.FormatStowedExceptionName(context, output, id);
			var name = ObjectCache.FreeAndToText(ref output);
			var expression = name.ToString();
			const bool isReadOnly = true;
			const bool causesSideEffects = false;
			const string imageName = PredefinedDbgValueNodeImageNames.StowedException;
			return CreateValue(context, frame, name, value, options, expression, imageName, isReadOnly, causesSideEffects, value.Type, cancellationToken);
		}

		public sealed override DbgDotNetValueNode CreateReturnValue(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, DbgValueNodeEvaluationOptions options, DmdMethodBase method, CancellationToken cancellationToken) {
			var output = ObjectCache.AllocDotNetTextOutput();
			FormatReturnValueName(context, output, method);
			var name = ObjectCache.FreeAndToText(ref output);
			var expression = "$ReturnValue" + id.ToString();
			const bool isReadOnly = true;
			const bool causesSideEffects = false;
			const string imageName = PredefinedDbgValueNodeImageNames.ReturnValue;
			return CreateValue(context, frame, name, value, options, expression, imageName, isReadOnly, causesSideEffects, value.Type, cancellationToken);
		}

		void FormatReturnValueName(DbgEvaluationContext context, DbgDotNetTextOutput output, DmdMethodBase method) {
			var formatString = dnSpy_Roslyn_Shared_Resources.LocalsWindow_MethodOrProperty_Returned;
			const string pattern = "{0}";
			int index = formatString.IndexOf(pattern);
			Debug.Assert(index >= 0);
			if (index < 0) {
				formatString = "{0} returned";
				index = formatString.IndexOf(pattern);
			}

			if (index != 0)
				output.Write(BoxedTextColor.Text, formatString.Substring(0, index));
			FormatReturnValueMethodName(output, method, PropertyState.TryGetProperty(method));
			if (index + pattern.Length != formatString.Length)
				output.Write(BoxedTextColor.Text, formatString.Substring(index + pattern.Length));
		}

		protected abstract void FormatReturnValueMethodName(ITextColorWriter output, DmdMethodBase method, DmdPropertyInfo property);

		public sealed override DbgDotNetValueNode CreateError(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, string errorMessage, string expression, CancellationToken cancellationToken) =>
			new DbgDotNetValueNodeImpl(this, null, name, default, expression, PredefinedDbgValueNodeImageNames.Error, true, false, null, null, errorMessage, default);

		public sealed override DbgDotNetValueNode CreateTypeVariables(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetTypeVariableInfo[] typeVariableInfos, CancellationToken cancellationToken) =>
			new DbgDotNetTypeVariablesNode(this, typeVariableInfos);

		sealed class PropertyState {
			readonly Dictionary<DmdMethodBase, DmdPropertyInfo> toProperty;

			PropertyState(DmdType type) {
				toProperty = new Dictionary<DmdMethodBase, DmdPropertyInfo>(DmdMemberInfoEqualityComparer.DefaultMember);
				foreach (var property in type.DeclaredProperties) {
					foreach (var method in property.GetAccessors(DmdGetAccessorOptions.All))
						toProperty[method] = property;
				}
			}

			public static DmdPropertyInfo TryGetProperty(DmdMethodBase method) {
				var m = method as DmdMethodInfo;
				if ((object)m == null)
					return null;
				var state = GetState(m.DeclaringType);
				if (state.toProperty.TryGetValue(method, out var property))
					return property;
				return null;
			}

			static PropertyState GetState(DmdType type) {
				if (type.TryGetData(out PropertyState state))
					return state;
				return CreateState(type);

				PropertyState CreateState(DmdType type2) {
					var state2 = new PropertyState(type2);
					return type2.GetOrCreateData(() => state2);
				}
			}
		}
	}
}
