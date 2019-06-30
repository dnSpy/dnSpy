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
		public abstract DbgEngineValueNode Create(DbgEvaluationInfo evalInfo, DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType);
		public abstract DbgEngineValueNode CreateException(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options);
		public abstract DbgEngineValueNode CreateStowedException(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options);
		public abstract DbgEngineValueNode CreateReturnValue(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options, DmdMethodBase method);
		public abstract DbgEngineValueNode CreateError(DbgEvaluationInfo evalInfo, DbgDotNetText name, string errorMessage, string expression, bool causesSideEffects);
		public abstract DbgEngineValueNode CreateTypeVariables(DbgEvaluationInfo evalInfo, DbgDotNetTypeVariableInfo[] typeVariableInfos);
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

		public override DbgEngineValueNode Create(DbgEvaluationInfo evalInfo, DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType) =>
			new DbgEngineValueNodeImpl(this, factory.Create(evalInfo, name, value, formatSpecifiers, options, expression, imageName, isReadOnly, causesSideEffects, expectedType));

		public override DbgEngineValueNode CreateException(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options) =>
			new DbgEngineValueNodeImpl(this, factory.CreateException(evalInfo, id, value, formatSpecifiers, options));

		public override DbgEngineValueNode CreateStowedException(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options) =>
			new DbgEngineValueNodeImpl(this, factory.CreateStowedException(evalInfo, id, value, formatSpecifiers, options));

		public override DbgEngineValueNode CreateReturnValue(DbgEvaluationInfo evalInfo, uint id, DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options, DmdMethodBase method) =>
			new DbgEngineValueNodeImpl(this, factory.CreateReturnValue(evalInfo, id, value, formatSpecifiers, options, method));

		public override DbgEngineValueNode CreateError(DbgEvaluationInfo evalInfo, DbgDotNetText name, string errorMessage, string expression, bool causesSideEffects) =>
			new DbgEngineValueNodeImpl(this, factory.CreateError(evalInfo, name, errorMessage, expression, causesSideEffects));

		public override DbgEngineValueNode CreateTypeVariables(DbgEvaluationInfo evalInfo, DbgDotNetTypeVariableInfo[] typeVariableInfos) =>
			new DbgEngineValueNodeImpl(this, factory.CreateTypeVariables(evalInfo, typeVariableInfos));
	}
}
