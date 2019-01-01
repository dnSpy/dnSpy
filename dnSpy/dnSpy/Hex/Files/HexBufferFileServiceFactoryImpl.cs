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
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	[Export(typeof(HexBufferFileServiceFactory))]
	sealed class HexBufferFileServiceFactoryImpl : HexBufferFileServiceFactory {
		public override event EventHandler<BufferFileServiceCreatedEventArgs> BufferFileServiceCreated;
		readonly Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories;
		readonly Lazy<BufferFileHeadersProviderFactory>[] bufferFileHeadersProviderFactories;

		[ImportingConstructor]
		HexBufferFileServiceFactoryImpl([ImportMany] IEnumerable<Lazy<StructureProviderFactory, VSUTIL.IOrderable>> structureProviderFactories, [ImportMany] IEnumerable<Lazy<BufferFileHeadersProviderFactory>> bufferFileHeadersProviderFactories) {
			this.structureProviderFactories = VSUTIL.Orderer.Order(structureProviderFactories).ToArray();
			this.bufferFileHeadersProviderFactories = bufferFileHeadersProviderFactories.ToArray();
		}

		public override HexBufferFileService Create(HexBuffer buffer) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (buffer.Properties.TryGetProperty(typeof(HexBufferFileServiceImpl), out HexBufferFileServiceImpl impl))
				return impl;
			impl = new HexBufferFileServiceImpl(buffer, structureProviderFactories, bufferFileHeadersProviderFactories);
			buffer.Properties.AddProperty(typeof(HexBufferFileServiceImpl), impl);
			BufferFileServiceCreated?.Invoke(this, new BufferFileServiceCreatedEventArgs(impl));
			return impl;
		}
	}
}
