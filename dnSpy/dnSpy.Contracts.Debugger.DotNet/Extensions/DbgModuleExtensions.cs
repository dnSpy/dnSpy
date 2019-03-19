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

using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DbgModuleExtensions {
		/// <summary>
		/// Gets the reflection module or null if this isn't a managed module
		/// </summary>
		/// <param name="module">Debugger module</param>
		/// <returns></returns>
		public static DmdModule GetReflectionModule(this DbgModule module) => (module.InternalModule as DbgDotNetInternalModule)?.ReflectionModule;

		/// <summary>
		/// Gets the internal .NET module or null if it's not a managed module
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public static DbgDotNetInternalModule GetDotNetInternalModule(this DbgModule module) => module.InternalModule as DbgDotNetInternalModule;
	}
}
