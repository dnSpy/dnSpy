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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	abstract class LanguageValueNodeFactory : DbgDotNetValueNodeFactory {
		readonly DbgDotNetValueNodeProviderFactory valueNodeProviderFactory;

		protected LanguageValueNodeFactory(DbgDotNetValueNodeProviderFactory valueNodeProviderFactory) =>
			this.valueNodeProviderFactory = valueNodeProviderFactory ?? throw new ArgumentNullException(nameof(valueNodeProviderFactory));

		public abstract string GetExpression(string baseExpression, DmdFieldInfo field);
		public abstract string GetExpression(string baseExpression, DmdPropertyInfo property);
		public abstract string GetExpression(string baseExpression, string name);
		public abstract string GetExpression(string baseExpression, int index);
		public abstract string GetExpression(string baseExpression, int[] indexes);
		public abstract string EscapeIdentifier(string identifier);

		internal DbgDotNetValueNode Create(DbgEvaluationContext context, DbgDotNetText name, DbgDotNetValueNodeProvider provider, DbgValueNodeEvaluationOptions options, string expression, string imageName) =>
			new DbgDotNetValueNodeImpl(this, provider, name, null, null, expression, imageName, true, false, null, null, null);

		DbgDotNetValueNode CreateValue(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, DbgDotNetValue value, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, CancellationToken cancellationToken) {
			var info = valueNodeProviderFactory.Create(context, frame, value, expression, isReadOnly, options, cancellationToken);
			return new DbgDotNetValueNodeImpl(this, info.provider, name, value, info.value, expression, imageName, isReadOnly, causesSideEffects, expectedType, value.Type, null);
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
			throw new NotImplementedException();//TODO:
		}

		public sealed override DbgDotNetValueNode CreateError(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, string errorMessage, string expression, CancellationToken cancellationToken) =>
			new DbgDotNetValueNodeImpl(this, null, name, null, null, expression, PredefinedDbgValueNodeImageNames.Error, true, false, null, null, errorMessage);

		public sealed override DbgDotNetValueNode CreateTypeVariables(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetTypeVariableInfo[] typeVariableInfos, CancellationToken cancellationToken) =>
			new DbgDotNetTypeVariablesNode(this, typeVariableInfos);
	}
}
