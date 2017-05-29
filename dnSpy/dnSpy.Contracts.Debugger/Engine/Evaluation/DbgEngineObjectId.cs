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
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// References a value in the debugged process
	/// </summary>
	public abstract class DbgEngineObjectId : DbgObject {
		/// <summary>
		/// Gets the unique id in the runtime
		/// </summary>
		public abstract uint Id { get; }

		/// <summary>
		/// Creates a new value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineValue GetValue(DbgEvaluationContext context, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a new value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract void GetValue(DbgEvaluationContext context, Action<DbgEngineValue> callback, CancellationToken cancellationToken);
	}
}
