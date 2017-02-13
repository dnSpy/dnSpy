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

namespace dnSpy.Contracts.Debugger.DotNet {
	/// <summary>
	/// A .NET module in a process
	/// </summary>
	public abstract class DbgClrModule : DbgModule {
		/// <summary>
		/// Gets the assembly
		/// </summary>
		public abstract DbgClrAssembly Assembly { get; }

		/// <summary>
		/// true if it's a dynamic module (the application can add more types and members to the module at runtime)
		/// </summary>
		public abstract bool IsDynamic { get; }

		/// <summary>
		/// true if it's an optimized module
		/// </summary>
		public abstract bool IsOptimized { get; }

		/// <summary>
		/// Gets the version found in the metadata
		/// </summary>
		public abstract Version Version { get; }
	}
}
