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

namespace dnSpy.Contracts.ToolWindows.Search {
	/// <summary>
	/// Defines a column that can be searched
	/// </summary>
	sealed class SearchColumnDefinition {
		/// <summary>
		/// Column definition id
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Short option name, eg. "p"
		/// </summary>
		public string ShortOptionName { get; }

		/// <summary>
		/// Localized name of column, eg. "Name"
		/// </summary>
		public string LocalizedName { get; }

		public SearchColumnDefinition(string id, string shortOptionName, string localizedName) {
			Id = id ?? throw new ArgumentNullException(nameof(id));
			ShortOptionName = shortOptionName ?? throw new ArgumentNullException(nameof(shortOptionName));
			LocalizedName = localizedName ?? throw new ArgumentNullException(nameof(localizedName));
		}
	}
}
