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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IHexBufferServiceListener))]
	sealed class HexBufferFileBufferServiceListener : IHexBufferServiceListener {
		readonly HexBufferFileServiceFactory hexBufferFileServiceFactory;

		[ImportingConstructor]
		HexBufferFileBufferServiceListener(HexBufferFileServiceFactory hexBufferFileServiceFactory) => this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;

		public void BufferCreated(HexBuffer buffer) {
			var service = hexBufferFileServiceFactory.Create(buffer);
			service.CreateFile(buffer.Span, buffer.Name, buffer.Name, Array.Empty<string>());
		}

		public void BuffersCleared(IEnumerable<HexBuffer> buffers) {
			foreach (var buffer in buffers) {
				var service = hexBufferFileServiceFactory.Create(buffer);
				service.RemoveAllFiles();
			}
		}
	}
}
