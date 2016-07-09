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

using System.Collections.Generic;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// An assembly in the debugged process
	/// </summary>
	public interface IDebuggerAssembly {
		/// <summary>
		/// Unique id per debugger
		/// </summary>
		int UniqueId { get; }

		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the modules
		/// </summary>
		IEnumerable<IDebuggerModule> Modules { get; }

		/// <summary>
		/// true if the assembly has been granted full trust by the runtime security system
		/// </summary>
		bool IsFullyTrusted { get; }

		/// <summary>
		/// Assembly name, and is usually the full path to the manifest (first) module on disk
		/// (the EXE or DLL file).
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the full name, identical to the dnlib assembly full name
		/// </summary>
		string FullName { get; }

		/// <summary>
		/// Gets the manifest module or null
		/// </summary>
		IDebuggerModule ManifestModule { get; }

		/// <summary>
		/// true if the assembly has been unloaded
		/// </summary>
		bool HasUnloaded { get; }
	}
}
