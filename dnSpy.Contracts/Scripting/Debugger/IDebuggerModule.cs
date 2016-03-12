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

using dnlib.DotNet;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A module in the debugged process
	/// </summary>
	public interface IDebuggerModule {
		/// <summary>
		/// Gets the module name instance
		/// </summary>
		ModuleName ModuleName { get; }

		/// <summary>
		/// Unique id per Assembly. Each new created module gets an incremented value.
		/// </summary>
		int IncrementedId { get; }

		/// <summary>
		/// Unique id per debugger. Incremented each time a new module is created.
		/// </summary>
		int ModuleOrder { get; }

		/// <summary>
		/// Gets the owner AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the assembly
		/// </summary>
		IDebuggerAssembly Assembly { get; }

		/// <summary>
		/// true if this is the manifest module
		/// </summary>
		bool IsManifestModule { get; }

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the name from the MD, which is the same as <see cref="ModuleDef.Name"/>
		/// </summary>
		string DnlibName { get; }

		/// <summary>
		/// Gets the name of the module. If it's an in-memory module, the hash code is included to
		/// make it uniquer since <see cref="Name"/> could have any value.
		/// </summary>
		string UniquerName { get; }

		/// <summary>
		/// Gets the base address of the module or 0
		/// </summary>
		ulong Address { get; }

		/// <summary>
		/// Gets the size of the module or 0
		/// </summary>
		uint Size { get; }

		/// <summary>
		/// true if it's a dynamic module that can add/remove types
		/// </summary>
		bool IsDynamic { get; }

		/// <summary>
		/// true if this is an in-memory module
		/// </summary>
		bool IsInMemory { get; }

		/// <summary>
		/// true if the module has been unloaded
		/// </summary>
		bool HasUnloaded { get; }

		/// <summary>
		/// Resolves an assembly reference. If the assembly hasn't been loaded, or if
		/// <paramref name="asmRefToken"/> is invalid, null is returned.
		/// </summary>
		/// <param name="asmRefToken">Valid assembly reference token in this module</param>
		/// <returns></returns>
		IDebuggerAssembly ResolveAssembly(uint asmRefToken);

		/// <summary>
		/// Gets a function in this module
		/// </summary>
		/// <param name="token"><c>Method</c> token</param>
		/// <returns></returns>
		IDebuggerFunction GetFunction(uint token);

		/// <summary>
		/// Gets a type in this module
		/// </summary>
		/// <param name="token"><c>TypeDef</c> token</param>
		/// <returns></returns>
		IDebuggerClass GetClass(uint token);

		/// <summary>
		/// Gets the value of a global field
		/// </summary>
		/// <param name="fdToken">Token of a global field</param>
		/// <returns></returns>
		IDebuggerValue GetGlobalVariableValue(uint fdToken);

		/// <summary>
		/// Set just my code flag
		/// </summary>
		/// <param name="isJustMyCode">true if it's user code</param>
		void SetJMCStatus(bool isJustMyCode);
	}
}
