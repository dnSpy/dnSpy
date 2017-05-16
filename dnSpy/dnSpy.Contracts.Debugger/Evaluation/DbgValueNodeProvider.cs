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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Provides <see cref="DbgValueNode"/>s for the locals and autos windows
	/// </summary>
	public abstract class DbgValueNodeProvider {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Gets all values
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <returns></returns>
		public abstract DbgValueNode[] GetNodes(DbgStackFrame frame);

		/// <summary>
		/// Gets all values
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="callback">Called when the method is complete</param>
		public abstract void GetNodes(DbgStackFrame frame, Action<DbgValueNode[]> callback);
	}
}
