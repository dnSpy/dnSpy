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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	[Export(typeof(HexBufferFactoryService))]
	sealed class HexBufferFactoryServiceImpl : HexBufferFactoryService {
		readonly HexBufferStreamFactoryService hexBufferStreamFactoryService;

		public override event EventHandler<HexBufferCreatedEventArgs> HexBufferCreated;

		[ImportingConstructor]
		HexBufferFactoryServiceImpl(HexBufferStreamFactoryService hexBufferStreamFactoryService) => this.hexBufferStreamFactoryService = hexBufferStreamFactoryService;

		public override HexBuffer Create(string filename, HexTags tags) {
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));
			return Create(hexBufferStreamFactoryService.Create(filename), tags ?? DefaultFileTags, disposeStream: true);
		}

		public override HexBuffer Create(byte[] data, string name, HexTags tags) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			return Create(hexBufferStreamFactoryService.Create(data, name), tags ?? DefaultFileTags, disposeStream: true);
		}

		public override HexBuffer Create(HexBufferStream stream, HexTags tags, bool disposeStream) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (tags == null)
				throw new ArgumentNullException(nameof(tags));
			var buffer = new HexBufferImpl(stream, tags, disposeStream);
			HexBufferCreated?.Invoke(this, new HexBufferCreatedEventArgs(buffer));
			return buffer;
		}
	}
}
