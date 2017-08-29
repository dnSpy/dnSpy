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

using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// .NET code location
	/// </summary>
	public interface IDbgDotNetCodeLocation {
		/// <summary>
		/// Gets the module
		/// </summary>
		ModuleId Module { get; }

		/// <summary>
		/// Gets the token of a method within the module
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets the IL offset within the method body
		/// </summary>
		uint Offset { get; }

		/// <summary>
		/// Gets the debugger module or null
		/// </summary>
		DbgModule DbgModule { get; }
	}
}
