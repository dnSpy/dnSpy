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

using System;
using dnlib.DotNet;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Metadata {
	/// <summary>
	/// Provides .NET metadata
	/// </summary>
	public abstract class DbgMetadataService {
		/// <summary>
		/// Returns a <see cref="ModuleDef"/> or null if none could be loaded
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="options">Load options</param>
		/// <returns></returns>
		public abstract ModuleDef TryGetMetadata(DbgModule module, DbgLoadModuleOptions options = DbgLoadModuleOptions.None);

		/// <summary>
		/// Returns a <see cref="ModuleDef"/> or null if none could be loaded
		/// </summary>
		/// <param name="moduleId">Module id</param>
		/// <param name="options">Load options</param>
		/// <returns></returns>
		public abstract ModuleDef TryGetMetadata(ModuleId moduleId, DbgLoadModuleOptions options = DbgLoadModuleOptions.None);
	}

	/// <summary>
	/// Options used when loading modules
	/// </summary>
	[Flags]
	public enum DbgLoadModuleOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// The module load was caused by a non-user action
		/// </summary>
		AutoLoaded				= 0x00000001,

		/// <summary>
		/// Always load the module from the process' address space instead of from the module's file on disk
		/// </summary>
		ForceMemory				= 0x00000002,
	}
}
