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

using System.Linq;

namespace dnSpy.Contracts.Debugger.DotNet.Metadata {
	/// <summary>
	/// Assembly info provider
	/// </summary>
	public abstract class DbgAssemblyInfoProvider {
		/// <summary>
		/// Returns the manifest module (first module) or null if it's not part of an assembly
		/// </summary>
		/// <param name="module">A module in some assembly</param>
		/// <returns></returns>
		public DbgModule GetManifestModule(DbgModule module) => GetAssemblyModules(module).FirstOrDefault();

		/// <summary>
		/// Gets all modules in an assembly or an empty array if it's not part of an assembly.
		/// The manifest module is always the first module.
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public abstract DbgModule[] GetAssemblyModules(DbgModule module);
	}
}
