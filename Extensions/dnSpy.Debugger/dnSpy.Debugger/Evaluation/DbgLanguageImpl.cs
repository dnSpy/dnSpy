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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgLanguageImpl : DbgLanguage {
		public override string Name { get; }
		public override string DisplayName { get; }
		public override DbgExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgValueFormatter ValueFormatter { get; }
		public override DbgValueNodeProvider LocalsProvider { get; }
		public override DbgValueNodeProvider AutosProvider { get; }
		public override DbgValueNodeFactory ValueNodeFactory { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineLanguage engineLanguage;

		public DbgLanguageImpl(Guid runtimeGuid, DbgEngineLanguage engineLanguage) {
			this.runtimeGuid = runtimeGuid;
			this.engineLanguage = engineLanguage ?? throw new ArgumentNullException(nameof(engineLanguage));
			Name = engineLanguage.Name ?? throw new ArgumentException();
			DisplayName = engineLanguage.DisplayName ?? throw new ArgumentException();
			ExpressionEvaluator = new DbgExpressionEvaluatorImpl(this, runtimeGuid, engineLanguage.ExpressionEvaluator);
			ValueFormatter = new DbgValueFormatterImpl(this, runtimeGuid, engineLanguage.ValueFormatter);
			LocalsProvider = new DbgValueNodeProviderImpl(this, runtimeGuid, engineLanguage.LocalsProvider);
			AutosProvider = new DbgValueNodeProviderImpl(this, runtimeGuid, engineLanguage.AutosProvider);
			ValueNodeFactory = new DbgValueNodeFactoryImpl(this, runtimeGuid, engineLanguage.ValueNodeFactory);
		}

		public override DbgEvaluationContext CreateContext(DbgRuntime runtime, DbgCodeLocation location, TimeSpan funcEvalTimeout, DbgEvaluationContextOptions options) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			if (runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (location == null)
				throw new ArgumentNullException(nameof(location));
			var context = new DbgEvaluationContextImpl(this, runtime, funcEvalTimeout, options);
			engineLanguage.InitializeContext(context, runtime, location);
			return context;
		}
	}
}
