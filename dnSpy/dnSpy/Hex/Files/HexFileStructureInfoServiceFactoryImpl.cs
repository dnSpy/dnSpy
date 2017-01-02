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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	[Export(typeof(HexFileStructureInfoServiceFactory))]
	sealed class HexFileStructureInfoServiceFactoryImpl : HexFileStructureInfoServiceFactory {
		readonly HexBufferFileServiceFactory hexBufferFileServiceFactory;
		readonly Lazy<HexFileStructureInfoProviderFactory, VSUTIL.IOrderable>[] hexFileStructureInfoProviderFactories;

		[ImportingConstructor]
		HexFileStructureInfoServiceFactoryImpl(HexBufferFileServiceFactory hexBufferFileServiceFactory, [ImportMany] IEnumerable<Lazy<HexFileStructureInfoProviderFactory, VSUTIL.IOrderable>> hexFileStructureInfoProviderFactories) {
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
			this.hexFileStructureInfoProviderFactories = VSUTIL.Orderer.Order(hexFileStructureInfoProviderFactories).ToArray();
		}

		public override HexFileStructureInfoService Create(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			return hexView.Properties.GetOrCreateSingletonProperty(typeof(HexFileStructureInfoServiceImpl),
				() => new HexFileStructureInfoServiceImpl(hexView, hexBufferFileServiceFactory, hexFileStructureInfoProviderFactories));
		}
	}
}
