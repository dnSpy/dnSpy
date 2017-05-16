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
using dnSpy.Contracts.Debugger.CallStack;

namespace dnSpy.Contracts.Debugger.Evaluation.Engine {
	/// <summary>
	/// Creates <see cref="DbgEngineValueNode"/>s
	/// </summary>
	public abstract class DbgEngineValueNodeFactory {
		/// <summary>
		/// Creates a <see cref="DbgEngineValueNode"/> or returns null if there was an error (eg. <see cref="DbgEvaluationOptions.NoSideEffects"/>
		/// is set and the expression has side effects)
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options);

		/// <summary>
		/// Creates a <see cref="DbgEngineValueNode"/> or returns null if there was an error (eg. <see cref="DbgEvaluationOptions.NoSideEffects"/>
		/// is set and the expression has side effects)
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		public abstract void Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNode> callback);
	}
}
