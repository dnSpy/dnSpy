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
using System.Collections.Generic;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Creates <see cref="HexBuffer"/>s
	/// </summary>
	public abstract class HexBufferFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferFactoryService() {
			EmptyTags = new HexTags(Array.Empty<string>());
			DefaultMemoryTags = new HexTags(defaultMemoryTags);
			DefaultFileTags = new HexTags(defaultFileTags);
		}

		static readonly string[] defaultMemoryTags = new string[] {
			PredefinedHexBufferTags.Memory,
		};

		static readonly string[] defaultFileTags = new string[] {
			PredefinedHexBufferTags.File,
		};

		/// <summary>
		/// Gets the empty tags collection
		/// </summary>
		public HexTags EmptyTags { get; }

		/// <summary>
		/// Default memory tags
		/// </summary>
		public HexTags DefaultMemoryTags { get; }

		/// <summary>
		/// Default file / byte array tags
		/// </summary>
		public HexTags DefaultFileTags { get; }

		/// <summary>
		/// Creates a new <see cref="HexTags"/> instance
		/// </summary>
		/// <param name="tags">Tags</param>
		/// <returns></returns>
		public HexTags CreateTags(IEnumerable<string> tags) => new HexTags(tags);

		/// <summary>
		/// Raised when a new <see cref="HexBuffer"/> has been created
		/// </summary>
		public abstract event EventHandler<HexBufferCreatedEventArgs>? HexBufferCreated;

		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="tags">Tags or null to use the default file tags</param>
		/// <returns></returns>
		public abstract HexBuffer Create(string filename, HexTags? tags = null);

		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="name">Name, can be anything and is usually the filename</param>
		/// <param name="tags">Tags or null to use the default file tags</param>
		/// <returns></returns>
		public abstract HexBuffer Create(byte[] data, string name, HexTags? tags = null);

		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>
		/// </summary>
		/// <param name="stream">Stream to use</param>
		/// <param name="tags">Tags</param>
		/// <param name="disposeStream">true if the returned buffer owns <paramref name="stream"/> and
		/// disposes it when the buffer gets disposed</param>
		/// <returns></returns>
		public abstract HexBuffer Create(HexBufferStream stream, HexTags tags, bool disposeStream);
	}
}
