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

using System.Linq;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Bookmarks.DotNet {
	/// <summary>
	/// Creates bookmarks
	/// </summary>
	public abstract class DotNetBookmarkFactory {
		/// <summary>
		/// Creates an enabled bookmark. If there's already a bookmark at the location, null is returned.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the bookmark within the method body</param>
		/// <returns></returns>
		public Bookmark? Create(ModuleId module, uint token, uint offset) =>
			Create(module, token, offset, new BookmarkSettings { IsEnabled = true });

		/// <summary>
		/// Creates a bookmark. If there's already a bookmark at the location, null is returned.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the bookmark within the method body</param>
		/// <param name="settings">Bookmark settings</param>
		/// <returns></returns>
		public Bookmark? Create(ModuleId module, uint token, uint offset, BookmarkSettings settings) =>
			Create(new[] { new DotNetMethodBodyBookmarkInfo(module, token, offset, settings) }).FirstOrDefault();

		/// <summary>
		/// Creates bookmarks. Duplicate bookmarks are ignored.
		/// </summary>
		/// <param name="bookmarks">Bookmark infos</param>
		/// <returns></returns>
		public abstract Bookmark[] Create(DotNetMethodBodyBookmarkInfo[] bookmarks);

		/// <summary>
		/// Creates an enabled bookmark. If there's already a bookmark at the location, null is returned.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a definition (type, method, field, property, event)</param>
		/// <returns></returns>
		public Bookmark? Create(ModuleId module, uint token) =>
			Create(module, token, new BookmarkSettings { IsEnabled = true });

		/// <summary>
		/// Creates a bookmark. If there's already a bookmark at the location, null is returned.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a definition (type, method, field, property, event)</param>
		/// <param name="settings">Bookmark settings</param>
		/// <returns></returns>
		public Bookmark? Create(ModuleId module, uint token, BookmarkSettings settings) =>
			Create(new[] { new DotNetTokenBookmarkInfo(module, token, settings) }).FirstOrDefault();

		/// <summary>
		/// Creates bookmarks. Duplicate bookmarks are ignored.
		/// </summary>
		/// <param name="bookmarks">Bookmark infos</param>
		/// <returns></returns>
		public abstract Bookmark[] Create(DotNetTokenBookmarkInfo[] bookmarks);
	}

	/// <summary>
	/// Contains all required data to create a bookmark in a .NET method body
	/// </summary>
	public readonly struct DotNetMethodBodyBookmarkInfo {
		/// <summary>
		/// Module
		/// </summary>
		public ModuleId Module { get; }

		/// <summary>
		/// Token of a method within the module
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// IL offset of the bookmark within the method body
		/// </summary>
		public uint Offset { get; }

		/// <summary>
		/// Bookmark settings
		/// </summary>
		public BookmarkSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the bookmark within the method body</param>
		/// <param name="settings">Bookmark settings</param>
		public DotNetMethodBodyBookmarkInfo(ModuleId module, uint token, uint offset, BookmarkSettings settings) {
			Module = module;
			Token = token;
			Offset = offset;
			Settings = settings;
		}
	}

	/// <summary>
	/// Contains all required data to create a bookmark that references a definition (type, method, field, property, event)
	/// </summary>
	public readonly struct DotNetTokenBookmarkInfo {
		/// <summary>
		/// Module
		/// </summary>
		public ModuleId Module { get; }

		/// <summary>
		/// Token of a definition (type, method, field, property, event)
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// Bookmark settings
		/// </summary>
		public BookmarkSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a definition (type, method, field, property, event)</param>
		/// <param name="settings">Bookmark settings</param>
		public DotNetTokenBookmarkInfo(ModuleId module, uint token, BookmarkSettings settings) {
			Module = module;
			Token = token;
			Settings = settings;
		}
	}
}
