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

using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Contains a method and its return value
	/// </summary>
	public struct DbgDotNetReturnValueInfo {
		/// <summary>
		/// Gets the return value id
		/// </summary>
		public uint Id { get; }

		/// <summary>
		/// Gets the value returned by the method
		/// </summary>
		public DbgDotNetValue Value { get; }

		/// <summary>
		/// Gets the method
		/// </summary>
		public DmdMethodInfo Method { get; }
	}
}
