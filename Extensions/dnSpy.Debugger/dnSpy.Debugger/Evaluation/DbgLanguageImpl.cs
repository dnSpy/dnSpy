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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgLanguageImpl : DbgLanguage {
		public override Guid RuntimeKindGuid { get; }
		public override string Name { get; }
		public override string DisplayName { get; }
		public override DbgExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgFormatter Formatter { get; }
		public override DbgLocalsValueNodeProvider LocalsProvider { get; }
		public override DbgValueNodeProvider AutosProvider { get; }
		public override DbgValueNodeProvider ExceptionsProvider { get; }
		public override DbgValueNodeProvider ReturnValuesProvider { get; }
		public override DbgValueNodeProvider TypeVariablesProvider { get; }
		public override DbgValueNodeFactory ValueNodeFactory { get; }

		readonly DbgEngineLanguage engineLanguage;

		public DbgLanguageImpl(Guid runtimeKindGuid, DbgEngineLanguage engineLanguage) {
			RuntimeKindGuid = runtimeKindGuid;
			this.engineLanguage = engineLanguage ?? throw new ArgumentNullException(nameof(engineLanguage));
			Name = engineLanguage.Name ?? throw new ArgumentException();
			DisplayName = engineLanguage.DisplayName ?? throw new ArgumentException();
			ExpressionEvaluator = new DbgExpressionEvaluatorImpl(this, runtimeKindGuid, engineLanguage.ExpressionEvaluator);
			Formatter = new DbgFormatterImpl(this, runtimeKindGuid, engineLanguage.Formatter);
			LocalsProvider = new DbgLocalsValueNodeProviderImpl(this, runtimeKindGuid, engineLanguage.LocalsProvider);
			AutosProvider = new DbgValueNodeProviderImpl(this, runtimeKindGuid, engineLanguage.AutosProvider);
			ExceptionsProvider = new DbgValueNodeProviderImpl(this, runtimeKindGuid, engineLanguage.ExceptionsProvider);
			ReturnValuesProvider = new DbgValueNodeProviderImpl(this, runtimeKindGuid, engineLanguage.ReturnValuesProvider);
			TypeVariablesProvider = new DbgValueNodeProviderImpl(this, runtimeKindGuid, engineLanguage.TypeVariablesProvider);
			ValueNodeFactory = new DbgValueNodeFactoryImpl(this, runtimeKindGuid, engineLanguage.ValueNodeFactory);
		}

		public override DbgEvaluationContext CreateContext(DbgRuntime runtime, DbgCodeLocation location, DbgEvaluationContextOptions options, TimeSpan funcEvalTimeout, CancellationToken cancellationToken) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			if (runtime.RuntimeKindGuid != RuntimeKindGuid)
				throw new ArgumentException();
			if (funcEvalTimeout == TimeSpan.Zero)
				funcEvalTimeout = DefaultFuncEvalTimeout;
			var context = new DbgEvaluationContextImpl(this, runtime, funcEvalTimeout, options);
			try {
				engineLanguage.InitializeContext(context, location, cancellationToken);
			}
			catch {
				context.Close();
				throw;
			}
			return context;
		}
	}
}
