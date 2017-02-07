/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dnSpy.Contracts.Debugger.DotNet {
	/// <summary>
	/// A .NET app domain
	/// </summary>
	public abstract class DbgDnAppDomain : DbgObject {
		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgDnRuntime Runtime { get; }

		/// <summary>
		/// Gets the name of the app domain
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the app domain id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets all modules
		/// </summary>
		public abstract DbgDnModule[] Modules { get; }

		/// <summary>
		/// Gets the core module (eg. mscorlib)
		/// </summary>
		public abstract DbgDnModule CorModule { get; }
	}
}
