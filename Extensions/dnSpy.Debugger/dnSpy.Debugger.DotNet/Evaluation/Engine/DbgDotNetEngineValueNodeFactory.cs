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
using System.Collections.ObjectModel;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgDotNetEngineValueNodeFactory {
		public abstract DbgEngineValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, CancellationToken cancellationToken);
		public abstract DbgEngineValueNode CreateException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);
		public abstract DbgEngineValueNode CreateStowedException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);
		public abstract DbgEngineValueNode CreateReturnValue(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, DmdMethodBase method, CancellationToken cancellationToken);
		public abstract DbgEngineValueNode CreateError(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, string errorMessage, string expression, bool causesSideEffects, CancellationToken cancellationToken);
		public abstract DbgEngineValueNode CreateTypeVariables(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetTypeVariableInfo[] typeVariableInfos, CancellationToken cancellationToken);
	}

	sealed class DbgDotNetEngineValueNodeFactoryImpl : DbgDotNetEngineValueNodeFactory {
		internal DbgDotNetFormatter Formatter => formatter;
		internal IPredefinedEvaluationErrorMessagesHelper ErrorMessagesHelper { get; }

		readonly DbgDotNetFormatter formatter;
		readonly DbgDotNetValueNodeFactory factory;

		public DbgDotNetEngineValueNodeFactoryImpl(DbgDotNetFormatter formatter, DbgDotNetValueNodeFactory factory, IPredefinedEvaluationErrorMessagesHelper errorMessagesHelper) {
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
			ErrorMessagesHelper = errorMessagesHelper ?? throw new ArgumentNullException(nameof(errorMessagesHelper));
		}

		internal DbgEngineValueNode Create(DbgDotNetValueNode node) => new DbgEngineValueNodeImpl(this, node);

		public override DbgEngineValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, CancellationToken cancellationToken) =>
			new DbgEngineValueNodeImpl(this, factory.Create(context, frame, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, expectedType, cancellationToken));

		public override DbgEngineValueNode CreateException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			new DbgEngineValueNodeImpl(this, factory.CreateException(context, frame, id, value, formatSpecifiers, options, cancellationToken));

		public override DbgEngineValueNode CreateStowedException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			new DbgEngineValueNodeImpl(this, factory.CreateStowedException(context, frame, id, value, formatSpecifiers, options, cancellationToken));

		public override DbgEngineValueNode CreateReturnValue(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, DmdMethodBase method, CancellationToken cancellationToken) =>
			new DbgEngineValueNodeImpl(this, factory.CreateReturnValue(context, frame, id, value, formatSpecifiers, options, method, cancellationToken));

		public override DbgEngineValueNode CreateError(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetText name, string errorMessage, string expression, bool causesSideEffects, CancellationToken cancellationToken) =>
			new DbgEngineValueNodeImpl(this, factory.CreateError(context, frame, name, errorMessage, expression, causesSideEffects, cancellationToken));

		public override DbgEngineValueNode CreateTypeVariables(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetTypeVariableInfo[] typeVariableInfos, CancellationToken cancellationToken) =>
			new DbgEngineValueNodeImpl(this, factory.CreateTypeVariables(context, frame, typeVariableInfos, cancellationToken));
	}
}
