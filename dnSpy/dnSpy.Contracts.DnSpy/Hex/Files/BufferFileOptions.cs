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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// <see cref="HexBufferFile"/> options
	/// </summary>
	public struct BufferFileOptions {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Name == null;

		/// <summary>
		/// Span of file
		/// </summary>
		public HexSpan Span { get; }

		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Filename if possible, otherwise any name
		/// </summary>
		public string Filename { get; }

		/// <summary>
		/// Tags, see eg. <see cref="PredefinedBufferFileTags"/>
		/// </summary>
		public string[] Tags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span of file</param>
		/// <param name="name">Name</param>
		/// <param name="filename">Filename if possible, otherwise any name</param>
		/// <param name="tags">Tags, see eg. <see cref="PredefinedBufferFileTags"/></param>
		public BufferFileOptions(HexSpan span, string name, string filename, string[] tags) {
			Span = span;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Filename = filename ?? throw new ArgumentNullException(nameof(filename));
			Tags = tags ?? throw new ArgumentNullException(nameof(tags));
		}
	}
}
