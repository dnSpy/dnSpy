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
using dnSpy.Contracts.Documents;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Search a type
	/// </summary>
	sealed class SearchTypeInfo {
		/// <summary>
		/// Owner file
		/// </summary>
		public IDsDocument Document { get; }

		/// <summary>
		/// Type to search
		/// </summary>
		public TypeDef Type { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">Document</param>
		/// <param name="type">Type</param>
		public SearchTypeInfo(IDsDocument document, TypeDef type) {
			Document = document ?? throw new ArgumentNullException(nameof(document));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}
	}
}
