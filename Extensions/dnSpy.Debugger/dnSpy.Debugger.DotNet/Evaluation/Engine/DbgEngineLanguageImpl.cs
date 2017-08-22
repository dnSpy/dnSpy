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
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineLanguageImpl : DbgEngineLanguage {
		public override string Name { get; }
		public override string DisplayName { get; }
		public override DbgEngineExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgEngineValueFormatter ValueFormatter => throw new NotImplementedException();//TODO:
		public override DbgEngineObjectIdFormatter ObjectIdFormatter => throw new NotImplementedException();//TODO:
		public override DbgEngineValueNodeProvider LocalsProvider => throw new NotImplementedException();//TODO:
		public override DbgEngineValueNodeProvider AutosProvider => throw new NotImplementedException();//TODO:
		public override DbgEngineValueNodeProvider ExceptionsProvider => throw new NotImplementedException();//TODO:
		public override DbgEngineValueNodeProvider ReturnValuesProvider => throw new NotImplementedException();//TODO:
		public override DbgEngineValueNodeFactory ValueNodeFactory => throw new NotImplementedException();//TODO:

		readonly IDecompiler decompiler;

		public DbgEngineLanguageImpl(string name, string displayName, DbgDotNetExpressionCompiler expressionCompiler, IDecompiler decompiler) {
			if (expressionCompiler == null)
				throw new ArgumentNullException(nameof(expressionCompiler));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			this.decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
			ExpressionEvaluator = new DbgEngineExpressionEvaluatorImpl(expressionCompiler);
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location) {
			//TODO:
		}
	}
}
