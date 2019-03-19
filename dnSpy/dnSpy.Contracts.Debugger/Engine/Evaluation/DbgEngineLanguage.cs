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

using System.Threading;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Debugger language that evaluates expressions and formats values
	/// </summary>
	public abstract class DbgEngineLanguage {
		/// <summary>
		/// Gets the language name, see <see cref="PredefinedDbgLanguageNames"/>
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the language's display name (shown in the UI)
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// Gets the expression evaluator
		/// </summary>
		public abstract DbgEngineExpressionEvaluator ExpressionEvaluator { get; }

		/// <summary>
		/// Gets the formatter
		/// </summary>
		public abstract DbgEngineFormatter Formatter { get; }

		/// <summary>
		/// Gets the locals and parameters provider
		/// </summary>
		public abstract DbgEngineLocalsValueNodeProvider LocalsProvider { get; }

		/// <summary>
		/// Gets the autos provider
		/// </summary>
		public abstract DbgEngineValueNodeProvider AutosProvider { get; }

		/// <summary>
		/// Gets the exceptions provider
		/// </summary>
		public abstract DbgEngineValueNodeProvider ExceptionsProvider { get; }

		/// <summary>
		/// Gets the return values provider
		/// </summary>
		public abstract DbgEngineValueNodeProvider ReturnValuesProvider { get; }

		/// <summary>
		/// Gets the type variables provider
		/// </summary>
		public abstract DbgEngineValueNodeProvider TypeVariablesProvider { get; }

		/// <summary>
		/// Gets the <see cref="DbgEngineValueNode"/> factory
		/// </summary>
		public abstract DbgEngineValueNodeFactory ValueNodeFactory { get; }

		/// <summary>
		/// Initializes an evaluation context
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="location">Location or null</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location, CancellationToken cancellationToken);
	}
}
