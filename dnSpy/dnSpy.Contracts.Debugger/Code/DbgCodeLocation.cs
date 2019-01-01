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

namespace dnSpy.Contracts.Debugger.Code {
	/// <summary>
	/// Code location. There must be one owner per <see cref="DbgCodeLocation"/> instance. If you must create a
	/// breakpoint using the same location, call <see cref="Clone"/> and use the new instance as the breakpoint location.
	/// </summary>
	public abstract class DbgCodeLocation : DbgObject {
		/// <summary>
		/// Unique type, see <see cref="PredefinedDbgCodeLocationTypes"/>. There should
		/// be a 1-1 correspondence between this string and the derived type.
		/// </summary>
		public abstract string Type { get; }

		/// <summary>
		/// Clones this instance. The returned instance can be used to create a breakpoint. Use <see cref="Close"/> to close it.
		/// </summary>
		/// <returns></returns>
		public abstract DbgCodeLocation Clone();

		/// <summary>
		/// Closes this instance
		/// </summary>
		public abstract void Close();

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public abstract override bool Equals(object obj);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public abstract override int GetHashCode();
	}
}
