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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Evaluation context
	/// </summary>
	public abstract class DbgEvaluationContext : DbgObject {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// This object gets closed (and recreated) when the process continues
		/// </summary>
		public abstract DbgObject ContinueContext { get; }

		/// <summary>
		/// Func-eval timeout (func-eval = calling functions in debugged process)
		/// </summary>
		public abstract TimeSpan FuncEvalTimeout { get; }

		/// <summary>
		/// Context options
		/// </summary>
		public abstract DbgEvaluationContextOptions Options { get; }

		/// <summary>
		/// Closes this instance
		/// </summary>
		public void Close() => Process.DbgManager.Close(this);
	}
}
