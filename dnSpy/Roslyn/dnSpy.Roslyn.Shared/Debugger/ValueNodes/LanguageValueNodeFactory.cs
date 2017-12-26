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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
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

		public abstract string GetFieldExpression(string baseExpression, string name, DmdType castType, bool addParens);
		public abstract string GetPropertyExpression(string baseExpression, string name, DmdType castType, bool addParens);
		public abstract string GetExpression(string baseExpression, int index, DmdType castType, bool addParens);
		public abstract string GetExpression(string baseExpression, int[] indexes, DmdType castType, bool addParens);
		public abstract string EscapeIdentifier(string identifier);
		protected abstract bool SupportsModuleTypes { get; }

		internal DbgDotNetValueNode Create(DbgEvaluationInfo evalInfo, in DbgDotNetText name, DbgDotNetValueNodeProvider provider, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, in DbgDotNetText valueText) =>
			new DbgDotNetValueNodeImpl(this, provider, name, null, expression, imageName, true, false, null, null, null, valueText, formatSpecifiers);

		DbgDotNetValueNode CreateValue(DbgEvaluationInfo evalInfo, in DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, bool isRootExpression) {
			options = PredefinedFormatSpecifiers.GetValueNodeEvaluationOptions(formatSpecifiers, options);
			var nodeInfo = new DbgDotNetValueNodeInfo(value, expression);
			bool addParens = isRootExpression && NeedsParentheses(expression);
			DbgDotNetValueNodeProviderResult info;
			bool useProvider = false;
			var specialViewOptions = (options & ~(DbgValueNodeEvaluationOptions.ResultsView | DbgValueNodeEvaluationOptions.DynamicView));
			if ((options & DbgValueNodeEvaluationOptions.ResultsView) != 0) {
				info = valueNodeProviderFactory.CreateResultsView(evalInfo, addParens, expectedType, nodeInfo, specialViewOptions);
				useProvider = info.ErrorMessage != null;
			}
			else if ((options & DbgValueNodeEvaluationOptions.DynamicView) != 0) {
				info = valueNodeProviderFactory.CreateDynamicView(evalInfo, addParens, expectedType, nodeInfo, specialViewOptions);
				useProvider = true;
			}
			else
				info = valueNodeProviderFactory.Create(evalInfo, addParens, expectedType, nodeInfo, options);
			if (useProvider) {
				if (info.ErrorMessage != null)
					return new DbgDotNetValueNodeImpl(this, info.Provider, name, nodeInfo, expression, PredefinedDbgValueNodeImageNames.Error, true, false, null, null, info.ErrorMessage, new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Error, info.ErrorMessage)), formatSpecifiers);
				Debug.Assert(info.Provider != null);
				return new DbgDotNetValueNodeImpl(this, info.Provider, name, nodeInfo, expression, info.Provider?.ImageName ?? imageName, true, false, null, null, info.ErrorMessage, info.Provider?.ValueText ?? default, formatSpecifiers);
			}
			return new DbgDotNetValueNodeImpl(this, info.Provider, name, nodeInfo, expression, imageName, isReadOnly, causesSideEffects, expectedType, value.Type, info.ErrorMessage, default, formatSpecifiers);
		}

		public sealed override DbgDotNetValueNode Create(DbgEvaluationInfo evalInfo, in DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType) =>
			Create(evalInfo, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, expectedType, true);

		internal DbgDotNetValueNode Create(DbgEvaluationInfo evalInfo, in DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, bool isRootExpression) =>
			CreateValue(evalInfo, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, expectedType, isRootExpression);

		public sealed override DbgDotNetValueNode CreateException(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options) {
			var output = ObjectCache.AllocDotNetTextOutput();
			evalInfo.Context.Language.Formatter.FormatExceptionName(evalInfo.Context, output, id);
			var name = ObjectCache.FreeAndToText(ref output);
			var expression = name.ToString();
			const bool isReadOnly = true;
			const bool causesSideEffects = false;
			const string imageName = PredefinedDbgValueNodeImageNames.Exception;
			return CreateValue(evalInfo, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, value.Type, false);
		}

		public sealed override DbgDotNetValueNode CreateStowedException(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options) {
			var output = ObjectCache.AllocDotNetTextOutput();
			evalInfo.Context.Language.Formatter.FormatStowedExceptionName(evalInfo.Context, output, id);
			var name = ObjectCache.FreeAndToText(ref output);
			var expression = name.ToString();
			const bool isReadOnly = true;
			const bool causesSideEffects = false;
			const string imageName = PredefinedDbgValueNodeImageNames.StowedException;
			return CreateValue(evalInfo, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, value.Type, false);
		}

		public sealed override DbgDotNetValueNode CreateReturnValue(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, DmdMethodBase method) {
			var output = ObjectCache.AllocDotNetTextOutput();
			FormatReturnValueName(evalInfo.Context, output, method);
			var name = ObjectCache.FreeAndToText(ref output);
			output = ObjectCache.AllocDotNetTextOutput();
			evalInfo.Context.Language.Formatter.FormatReturnValueName(evalInfo.Context, output, id);
			var expression = ObjectCache.FreeAndToText(ref output).ToString();
			const bool isReadOnly = true;
			const bool causesSideEffects = false;
			var property = PropertyState.TryGetProperty(method);
			var imageName = (object)property != null ? ImageNameUtils.GetImageName(property) : ImageNameUtils.GetImageName(method, SupportsModuleTypes);
			return CreateValue(evalInfo, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, value.Type, false);
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

		public sealed override DbgDotNetValueNode CreateError(DbgEvaluationInfo evalInfo, in DbgDotNetText name, string errorMessage, string expression, bool causesSideEffects) =>
			new DbgDotNetValueNodeImpl(this, null, name, null, expression, PredefinedDbgValueNodeImageNames.Error, true, causesSideEffects, null, null, errorMessage, default, null);

		public sealed override DbgDotNetValueNode CreateTypeVariables(DbgEvaluationInfo evalInfo, DbgDotNetTypeVariableInfo[] typeVariableInfos) =>
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

		protected void AddParens(StringBuilder sb, string expression, bool forceAddParens) {
			if (forceAddParens) {
				sb.Append('(');
				sb.Append(expression);
				sb.Append(')');
			}
			else
				sb.Append(expression);
		}

		// Roslyn: Microsoft.CodeAnalysis.ExpressionEvaluator.Formatter
		bool NeedsParentheses(string expr) {
			int parens = 0;
			for (int i = 0; i < expr.Length; i++) {
				var ch = expr[i];
				switch (ch) {
				case '(':
					// Cast, "(A)b", requires parentheses.
					if ((parens == 0) && FollowsCloseParen(expr, i))
						return true;
					parens++;
					break;
				case '[':
					parens++;
					break;
				case ')':
				case ']':
					parens--;
					break;
				case '.':
					break;
				default:
					if (parens == 0) {
						if (IsIdentifierPartCharacter(ch)) {
							// Cast, "(A)b", requires parentheses.
							if (FollowsCloseParen(expr, i))
								return true;
						}
						else
							return true;
					}
					break;
				}
			}
			return false;

			bool FollowsCloseParen(string expr2, int index2) => (index2 > 0) && (expr2[index2 - 1] == ')');
		}

		protected abstract bool IsIdentifierPartCharacter(char c);

		internal static string RemoveFormatSpecifiers(string expression) {
			int i = expression.Length - 1;
			int lastComma = -1;
			for (;;) {
				while ((uint)i < (uint)expression.Length && char.IsWhiteSpace(expression[i]))
					i--;
				while ((uint)i < (uint)expression.Length && IsFormatSpecifierChar(expression[i]))
					i--;
				while ((uint)i < (uint)expression.Length && char.IsWhiteSpace(expression[i]))
					i--;
				if ((uint)i >= (uint)expression.Length || expression[i] != ',')
					break;
				lastComma = i;
				i--;
			}
			if (lastComma >= 0)
				return expression.Substring(0, lastComma);
			return expression;
		}

		static bool IsFormatSpecifierChar(char c) => ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || char.IsDigit(c);
	}
}
