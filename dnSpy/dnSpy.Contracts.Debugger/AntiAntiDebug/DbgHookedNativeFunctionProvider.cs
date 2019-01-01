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

namespace dnSpy.Contracts.Debugger.AntiAntiDebug {
	/// <summary>
	/// Finds exported functions in native dlls
	/// </summary>
	public abstract class DbgHookedNativeFunctionProvider {
		/// <summary>
		/// Gets a function
		/// </summary>
		/// <param name="dllName">DLL name including the dll extension</param>
		/// <param name="funcName">Name of function</param>
		/// <returns></returns>
		public abstract DbgHookedNativeFunction GetFunction(string dllName, string funcName);

		/// <summary>
		/// Creates a hooked function
		/// </summary>
		/// <param name="dllName">DLL name including the dll extension</param>
		/// <param name="funcName">Name of function</param>
		/// <param name="address">Address of function</param>
		/// <returns></returns>
		public abstract DbgHookedNativeFunction GetFunction(string dllName, string funcName, ulong address);

		/// <summary>
		/// Gets the address of a module
		/// </summary>
		/// <param name="dllName">DLL name including the dll extension</param>
		/// <param name="address">Start address of module</param>
		/// <param name="endAddress">End address of module (exclusive)</param>
		/// <returns></returns>
		public abstract bool TryGetModuleAddress(string dllName, out ulong address, out ulong endAddress);

		/// <summary>
		/// Gets the address of a function
		/// </summary>
		/// <param name="dllName">DLL name including the dll extension</param>
		/// <param name="funcName">Name of function</param>
		/// <param name="address">Address of function</param>
		/// <returns></returns>
		public abstract bool TryGetFunction(string dllName, string funcName, out ulong address);
	}
}
