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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.DotNet {
	[Export(typeof(StructureProviderFactory))]
	[VSUTIL.Name(PredefinedStructureProviderFactoryNames.DotNetMultiResource)]
	sealed class MultiResourceStructureProviderFactory : StructureProviderFactory {
		public override StructureProvider? Create(HexBufferFile file) => new MultiResourceStructureProvider(file);
	}

	sealed class MultiResourceStructureProvider : StructureProvider {
		readonly HexBufferFile file;
		DotNetMultiFileResourcesImpl? multiFileResources;

		public MultiResourceStructureProvider(HexBufferFile file) => this.file = file ?? throw new ArgumentNullException(nameof(file));

		public override bool Initialize() {
			multiFileResources = DotNetMultiFileResourcesImpl.TryRead(file);
			return !(multiFileResources is null);
		}

		public override ComplexData? GetStructure(HexPosition position) =>
			multiFileResources?.GetStructure(position);

		public override ComplexData? GetStructure(string id) {
			if (id == PredefinedDotNetDataIds.MultiFileResource)
				return multiFileResources?.Header;
			return null;
		}

		public override THeader? GetHeaders<THeader>() where THeader : class =>
			multiFileResources as THeader;
	}
}
