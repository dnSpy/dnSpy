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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Debugger language that evaluates expressions and formats values
	/// </summary>
	public abstract class DbgLanguage {
		/// <summary>
		/// Gets the runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public abstract Guid RuntimeGuid { get; }

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
		public abstract DbgExpressionEvaluator ExpressionEvaluator { get; }

		/// <summary>
		/// Gets the value formatter
		/// </summary>
		public abstract DbgValueFormatter ValueFormatter { get; }

		/// <summary>
		/// Gets the object id formatter
		/// </summary>
		public abstract DbgObjectIdFormatter ObjectIdFormatter { get; }

		/// <summary>
		/// Gets the locals provider
		/// </summary>
		public abstract DbgValueNodeProvider LocalsProvider { get; }

		/// <summary>
		/// Gets the autos provider
		/// </summary>
		public abstract DbgValueNodeProvider AutosProvider { get; }

		/// <summary>
		/// Gets the exceptions
		/// </summary>
		public abstract DbgValueNodeProvider ExceptionProvider { get; }

		/// <summary>
		/// Gets the return values
		/// </summary>
		public abstract DbgValueNodeProvider ReturnValueProvider { get; }

		/// <summary>
		/// Gets the <see cref="DbgValueNode"/> factory
		/// </summary>
		public abstract DbgValueNodeFactory ValueNodeFactory { get; }

		/// <summary>
		/// Creates an evaluation context
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="location">Location</param>
		/// <param name="funcEvalTimeout">Func-eval timeout (func-eval = calling functions in the debugged process)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEvaluationContext CreateContext(DbgRuntime runtime, DbgCodeLocation location, TimeSpan funcEvalTimeout, DbgEvaluationContextOptions options);
	}

	/// <summary>
	/// Evaluation context options
	/// </summary>
	[Flags]
	public enum DbgEvaluationContextOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Set if all threads should run when func-evaluating (calling code in the debugged process),
		/// or cleared if only one thread should run. Usually only one thread is run, but that can
		/// lead to a deadlock if the thread calls a suspended thread or if it tries to acquire a
		/// lock owned by a suspended thread.
		/// </summary>
		RunAllThreads				= 0x00000001,
	}
}
