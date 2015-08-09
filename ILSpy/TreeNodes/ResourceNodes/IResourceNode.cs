/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	public enum ResourceDataType {
		Deserialized,
		Serialized,
	}

	interface IResourceNode {
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

		void Decompile(Language language, ITextOutput output);
	}
}
