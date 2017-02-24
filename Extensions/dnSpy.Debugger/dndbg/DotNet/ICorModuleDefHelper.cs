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

using dnlib.DotNet;
using dnlib.IO;

namespace dndbg.DotNet {
	interface ICorModuleDefHelper {
		/// <summary>
		/// Gets the currently loaded core library (eg. mscorlib)
		/// </summary>
		IAssembly CorLib { get; }

		/// <summary>
		/// true if it's a dynamic module (types can be added at runtime)
		/// </summary>
		bool IsDynamic { get; }

		/// <summary>
		/// true if it's a module that doesn't exist on disk, only in memory
		/// </summary>
		bool IsInMemory { get; }

		/// <summary>
		/// true if this is the core library (eg. mscorlib), false if it's not the core library,
		/// and null if it's unknown (caller has to guess)
		/// </summary>
		bool? IsCorLib { get; }

		/// <summary>
		/// Gets the filename if the module is available on disk, else null
		/// </summary>
		string Filename { get; }

		/// <summary>
		/// Returns true if <paramref name="module"/> is the manifest (first) module
		/// </summary>
		bool IsManifestModule { get; }

		/// <summary>
		/// Creates a method body reader. If <paramref name="module"/> is a dynamic module, the
		/// RVA in the method record is an RVA relative to the dynamic module which is not known
		/// by the module. The address can be found in
		/// <c>CorModule.GetFunctionFromToken(mdToken).ILCode.Address</c>
		/// </summary>
		/// <param name="bodyRva">RVA of method body</param>
		/// <param name="mdToken">Method token</param>
		/// <returns>A new <see cref="IBinaryReader"/> instance or null if there's no method body</returns>
		IBinaryReader CreateBodyReader(uint bodyRva, uint mdToken);

		/// <summary>
		/// Returns a field's initial value or null. It's only called if <see cref="FieldAttributes.HasFieldRVA"/> is set
		/// </summary>
		/// <param name="fieldRva">RVA of initial value of field. Can be 0 if it's a dynamic module.</param>
		/// <param name="fdToken">Field token</param>
		/// <param name="size">Size of data</param>
		/// <returns></returns>
		byte[] ReadFieldInitialValue(uint fieldRva, uint fdToken, int size);

		/// <summary>
		/// Creates a resource stream. Returns null if it couldn't be created.
		/// </summary>
		/// <param name="offset">Offset of resource</param>
		/// <returns></returns>
		IImageStream CreateResourceStream(uint offset);
	}
}
