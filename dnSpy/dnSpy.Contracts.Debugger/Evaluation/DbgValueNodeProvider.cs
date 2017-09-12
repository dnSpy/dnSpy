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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Provides <see cref="DbgValueNode"/>s for the variables windows
	/// </summary>
	public abstract class DbgValueNodeProvider {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Gets all values. It blocks the current thread until the method is complete.
		/// The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all values. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, Action<DbgValueNode[]> callback, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Locals value node provider options
	/// </summary>
	[Flags]
	public enum DbgLocalsValueNodeEvaluationOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Sort parameters (<see cref="DebuggerSettings.SortParameters"/>)
		/// </summary>
		SortParameters			= 0x00000001,

		/// <summary>
		/// Sort locals (<see cref="DebuggerSettings.SortLocals"/>)
		/// </summary>
		SortLocals				= 0x00000002,

		/// <summary>
		/// Group parameters and locals together (<see cref="DebuggerSettings.GroupParametersAndLocalsTogether"/>
		/// </summary>
		GroupParamLocals		= 0x00000004,

		/// <summary>
		/// Show compiler generated variables (<see cref="DebuggerSettings.ShowCompilerGeneratedVariables"/>)
		/// </summary>
		ShowCompilerVariables	= 0x00000008,

		/// <summary>
		/// Show decompiler generated variables (<see cref="DebuggerSettings.ShowDecompilerGeneratedVariables"/>)
		/// </summary>
		ShowDecompilerVariables	= 0x00000010,
	}

	/// <summary>
	/// Provides <see cref="DbgValueNode"/>s for the locals window
	/// </summary>
	public abstract class DbgLocalsValueNodeProvider {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Gets all values. It blocks the current thread until the method is complete.
		/// The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="localsOptions">Locals value node provider options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all values. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="localsOptions">Locals value node provider options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, Action<DbgValueNode[]> callback, CancellationToken cancellationToken = default);
	}
}
