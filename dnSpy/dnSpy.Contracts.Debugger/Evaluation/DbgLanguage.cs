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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Debugger language that evaluates expressions and formats values
	/// </summary>
	public abstract class DbgLanguage {
		/// <summary>
		/// Gets the runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public abstract Guid RuntimeKindGuid { get; }

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
		/// Gets the formatter
		/// </summary>
		public abstract DbgFormatter Formatter { get; }

		/// <summary>
		/// Gets the locals and parameters provider
		/// </summary>
		public abstract DbgLocalsValueNodeProvider LocalsProvider { get; }

		/// <summary>
		/// Gets the autos provider
		/// </summary>
		public abstract DbgValueNodeProvider AutosProvider { get; }

		/// <summary>
		/// Gets the exceptions provider
		/// </summary>
		public abstract DbgValueNodeProvider ExceptionsProvider { get; }

		/// <summary>
		/// Gets the return values provider
		/// </summary>
		public abstract DbgValueNodeProvider ReturnValuesProvider { get; }

		/// <summary>
		/// Gets the type variables provider
		/// </summary>
		public abstract DbgValueNodeProvider TypeVariablesProvider { get; }

		/// <summary>
		/// Gets the <see cref="DbgValueNode"/> factory
		/// </summary>
		public abstract DbgValueNodeFactory ValueNodeFactory { get; }

		/// <summary>
		/// Default func-eval timeout value
		/// </summary>
		public static readonly TimeSpan DefaultFuncEvalTimeout = TimeSpan.FromSeconds(1);

		/// <summary>
		/// Creates an evaluation context
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="location">Location or null</param>
		/// <param name="options">Options</param>
		/// <param name="funcEvalTimeout">Func-eval timeout (func-eval = calling functions in the debugged process) or default instance to use default timeout value</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEvaluationContext CreateContext(DbgRuntime runtime, DbgCodeLocation? location, DbgEvaluationContextOptions options = DbgEvaluationContextOptions.None, TimeSpan funcEvalTimeout = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates an evaluation context
		/// </summary>
		/// <param name="frame">Stack frame</param>
		/// <param name="options">Options</param>
		/// <param name="funcEvalTimeout">Func-eval timeout (func-eval = calling functions in the debugged process) or default instance to use default timeout value</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public DbgEvaluationContext CreateContext(DbgStackFrame frame, DbgEvaluationContextOptions options = DbgEvaluationContextOptions.None, TimeSpan funcEvalTimeout = default, CancellationToken cancellationToken = default) {
			if (frame is null)
				throw new ArgumentNullException(nameof(frame));
			return CreateContext(frame.Runtime, frame.Location, options, funcEvalTimeout, cancellationToken);
		}
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

		/// <summary>
		/// If method body info isn't needed, this option should be used. It prevents decompiling the
		/// method to get sequence points and other debug info. Can be used when formatting stack frames.
		/// </summary>
		NoMethodBody				= 0x00000002,
	}
}
