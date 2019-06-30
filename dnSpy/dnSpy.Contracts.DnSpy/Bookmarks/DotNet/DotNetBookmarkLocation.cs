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

using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Bookmarks.DotNet {
	/// <summary>
	/// .NET method body bookmark location
	/// </summary>
	public abstract class DotNetMethodBodyBookmarkLocation : BookmarkLocation {
		/// <summary>
		/// Gets the module
		/// </summary>
		public abstract ModuleId Module { get; }

		/// <summary>
		/// Gets the token of a method within the module
		/// </summary>
		public abstract uint Token { get; }

		/// <summary>
		/// Gets the IL offset of the bookmark within the method body
		/// </summary>
		public abstract uint Offset { get; }
	}

	/// <summary>
	/// .NET definition (type, method, field, property, event) bookmark location
	/// </summary>
	public abstract class DotNetTokenBookmarkLocation : BookmarkLocation {
		/// <summary>
		/// Gets the module
		/// </summary>
		public abstract ModuleId Module { get; }

		/// <summary>
		/// Gets the token of the definition (type, method, field, property, event)
		/// </summary>
		public abstract uint Token { get; }
	}
}
