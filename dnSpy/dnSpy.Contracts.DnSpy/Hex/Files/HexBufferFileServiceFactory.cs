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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Creates <see cref="HexBufferFileService"/> instances
	/// </summary>
	public abstract class HexBufferFileServiceFactory {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferFileServiceFactory() { }

		/// <summary>
		/// Gets or creates a <see cref="HexBufferFileService"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <returns></returns>
		public abstract HexBufferFileService Create(HexBuffer buffer);

		/// <summary>
		/// Raised after a new <see cref="HexBufferFileService"/> is created
		/// </summary>
		public abstract event EventHandler<BufferFileServiceCreatedEventArgs>? BufferFileServiceCreated;
	}

	/// <summary>
	/// <see cref="HexBufferFileService"/> created event args
	/// </summary>
	public sealed class BufferFileServiceCreatedEventArgs : EventArgs {
		/// <summary>
		/// Gets the created instance
		/// </summary>
		public HexBufferFileService BufferFileService { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferFileService">Created instance</param>
		public BufferFileServiceCreatedEventArgs(HexBufferFileService bufferFileService) => BufferFileService = bufferFileService ?? throw new ArgumentNullException(nameof(bufferFileService));
	}
}
