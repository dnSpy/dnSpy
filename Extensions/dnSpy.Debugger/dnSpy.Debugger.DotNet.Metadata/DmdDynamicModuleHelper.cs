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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Returns info to the COM MetaDataImport reader code that isn't made available by the COM MetaDataImport API
	/// </summary>
	public abstract class DmdDynamicModuleHelper {
		/// <summary>
		/// Called to get the address of the method body. Returns true on success. This method should use the
		/// CLR debugger API to get the address of the method body. This method is only called on the COM thread.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token of the method</param>
		/// <param name="rva">RVA of method body</param>
		/// <param name="body">Updated with address of body</param>
		/// <param name="bodySize">Updated with size of body</param>
		/// <returns></returns>
		public abstract bool TryGetMethodBody(DmdModule module, int metadataToken, uint rva, out IntPtr body, out int bodySize);
	}
}
