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
using System.Threading;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.Files.TreeView.Resources {
	/// <summary>
	/// Implemented by all resource nodes, and contains all raw data, RVA, and size
	/// </summary>
	public interface IResourceDataProvider {
		/// <summary>
		/// RVA of resource or 0
		/// </summary>
		uint RVA { get; }

		/// <summary>
		/// File offset of resource or 0
		/// </summary>
		ulong FileOffset { get; }

		/// <summary>
		/// Length of the resource
		/// </summary>
		ulong Length { get; }

		/// <summary>
		/// Gets the resource data
		/// </summary>
		/// <param name="type">Type of data</param>
		/// <returns></returns>
		IEnumerable<ResourceData> GetResourceData(ResourceDataType type);

		/// <summary>
		/// Write a short string (typically one line) to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="language">Language</param>
		/// <param name="showOffset">true to write offset and size of resource in the PE image, if
		/// that info is available</param>
		void WriteShort(ITextOutput output, ILanguage language, bool showOffset);

		/// <summary>
		/// Used by the searcher. Should only return a string if the data is text or compiled text.
		/// I.e., null should be returned if it's an <see cref="int"/>, but a string if it's eg. an
		/// XML doc.
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="canDecompile">true if the callee can decompile (eg. XAML), false otherwise</param>
		/// <returns></returns>
		string ToString(CancellationToken token, bool canDecompile);
	}
}
