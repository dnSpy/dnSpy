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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Returns info to the COM MetaDataImport reader code that isn't made available by the COM MetaDataImport API
	/// </summary>
	public abstract class DmdDynamicModuleHelper {
		/// <summary>
		/// Called to get the method body stream or null if there's no method body.
		/// 
		/// This method should use the CLR debugger API to get the address of the method body.
		/// 
		/// This method is only called on the COM thread.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token of the method</param>
		/// <param name="rva">RVA of method body</param>
		/// <returns></returns>
		public abstract DmdDataStream? TryGetMethodBody(DmdModule module, int metadataToken, uint rva);

		/// <summary>
		/// Raised when a new type in this module is loaded. It must be raised on the COM thread.
		/// </summary>
		public abstract event EventHandler<DmdTypeLoadedEventArgs>? TypeLoaded;
	}

	/// <summary>
	/// Class loaded event args
	/// </summary>
	public readonly struct DmdTypeLoadedEventArgs {
		/// <summary>
		/// Gets the metadata token of the type that got loaded
		/// </summary>
		public int MetadataToken { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadataToken">Metadata token of the type that got loaded</param>
		public DmdTypeLoadedEventArgs(int metadataToken) => MetadataToken = metadataToken;
	}
}
