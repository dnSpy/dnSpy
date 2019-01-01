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

using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgEngineValueNode"/>s
	/// </summary>
	public abstract class DbgEngineValueNodeFactory {
		/// <summary>
		/// Creates <see cref="DbgEngineValueNode"/>s
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expressions">Expressions to evaluate</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] Create(DbgEvaluationInfo evalInfo, DbgExpressionEvaluationInfo[] expressions);

		/// <summary>
		/// Creates <see cref="DbgEngineValueNode"/>s
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="objectIds">Object ids</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] Create(DbgEvaluationInfo evalInfo, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options);
	}
}
