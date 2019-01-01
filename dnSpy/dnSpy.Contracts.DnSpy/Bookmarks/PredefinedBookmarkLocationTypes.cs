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

namespace dnSpy.Contracts.Bookmarks {
	/// <summary>
	/// Predefined <see cref="BookmarkLocation"/> types
	/// </summary>
	public static class PredefinedBookmarkLocationTypes {
		/// <summary>
		/// .NET method body location (module, method token, IL offset)
		/// </summary>
		public const string DotNetBody = nameof(DotNetBody);

		/// <summary>
		/// .NET member definition (module, token)
		/// </summary>
		public const string DotNetToken = nameof(DotNetToken);
	}
}
