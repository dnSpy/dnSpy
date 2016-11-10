/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
		HexBufferFactoryServiceImpl(HexBufferStreamFactoryService hexBufferStreamFactoryService) {
			this.hexBufferStreamFactoryService = hexBufferStreamFactoryService;
		}

		public override HexBuffer Create(string filename) {
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));
			return Create(hexBufferStreamFactoryService.Create(filename));
		}

		public override HexBuffer Create(byte[] data, string name) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			return Create(hexBufferStreamFactoryService.Create(data, name));
		}

		public override HexBuffer Create(HexBufferStream stream) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			var buffer = new HexBufferImpl(stream);
			HexBufferCreated?.Invoke(this, new HexBufferCreatedEventArgs(buffer));
			return buffer;
		}
	}
}
