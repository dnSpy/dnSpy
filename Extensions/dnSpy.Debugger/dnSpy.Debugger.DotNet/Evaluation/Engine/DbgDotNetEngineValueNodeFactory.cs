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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgDotNetEngineValueNodeFactory {
		public abstract DbgEngineValueNode Create(DbgEvaluationContext context, DbgDotNetText name, DbgDotNetValue value, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType);
		public abstract DbgEngineValueNode CreateError(DbgEvaluationContext context, DbgDotNetText name, string errorMessage, string expression);
		//TODO: Add the remaining methods, eg. CreateException() etc
	}

	sealed class DbgDotNetEngineValueNodeFactoryImpl : DbgDotNetEngineValueNodeFactory {
		readonly DbgDotNetFormatter formatter;
		readonly DbgDotNetValueNodeFactory factory;

		public DbgDotNetEngineValueNodeFactoryImpl(DbgDotNetFormatter formatter, DbgDotNetValueNodeFactory factory) {
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}

		public override DbgEngineValueNode Create(DbgEvaluationContext context, DbgDotNetText name, DbgDotNetValue value, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType) =>
			new DbgEngineValueNodeImpl(formatter, factory.Create(context, name, value, expression, imageName, isReadOnly, causesSideEffects, expectedType));

		public override DbgEngineValueNode CreateError(DbgEvaluationContext context, DbgDotNetText name, string errorMessage, string expression) =>
			new DbgEngineValueNodeImpl(formatter, factory.CreateError(context, name, errorMessage, expression));
	}
}
