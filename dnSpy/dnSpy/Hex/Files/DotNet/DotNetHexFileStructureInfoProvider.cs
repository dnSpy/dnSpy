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
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.DotNet {
	[Export(typeof(HexFileStructureInfoProviderFactory))]
	[VSUTIL.Name(PredefinedHexFileStructureInfoProviderFactoryNames.DotNet)]
	[VSUTIL.Order(Before = PredefinedHexFileStructureInfoProviderFactoryNames.Default)]
	sealed class DotNetHexFileStructureInfoProviderFactory : HexFileStructureInfoProviderFactory {
		public override HexFileStructureInfoProvider Create(HexView hexView) =>
			new DotNetHexFileStructureInfoProvider();
	}

	sealed class DotNetHexFileStructureInfoProvider : HexFileStructureInfoProvider {
		public override HexIndexes[] GetSubStructureIndexes(HexBufferFile file, ComplexData structure, HexPosition position) {
			var body = structure as DotNetMethodBody;
			if (body != null) {
				if (body.Kind == DotNetMethodBodyKind.Tiny)
					return Array.Empty<HexIndexes>();
				var fatBody = body as FatMethodBody;
				if (fatBody != null) {
					if (fatBody.EHTable == null)
						return subStructFatWithoutEH;
					return subStructFatWithEH;
				}
			}

			if (structure is DotNetEmbeddedResource)
				return Array.Empty<HexIndexes>();

			var multiResource = structure as MultiResourceDataHeaderData;
			if (multiResource != null) {
				if (multiResource is MultiResourceSimplDataHeaderData || multiResource is MultiResourceStringDataHeaderData)
					return multiResourceFields2;
				if (multiResource is MultiResourceArrayDataHeaderData)
					return multiResourceFields3;
				Debug.Fail($"Unknown multi res type: {multiResource.GetType()}");
			}

			var stringsRec = structure as StringsHeapRecordData;
			if (stringsRec?.Terminator != null)
				return stringsRecordIndexes;

			return base.GetSubStructureIndexes(file, structure, position);
		}
		static readonly HexIndexes[] subStructFatWithEH = new HexIndexes[] {
			new HexIndexes(0, 4),
			new HexIndexes(4, 1),
			// Skip padding bytes @ 5
			new HexIndexes(6, 1),
		};
		static readonly HexIndexes[] subStructFatWithoutEH = new HexIndexes[] {
			new HexIndexes(0, 4),
			new HexIndexes(4, 1),
		};
		static readonly HexIndexes[] multiResourceFields2 = new HexIndexes[] {
			new HexIndexes(0, 1),
			new HexIndexes(1, 1),
		};
		static readonly HexIndexes[] multiResourceFields3 = new HexIndexes[] {
			new HexIndexes(0, 2),
			new HexIndexes(2, 1),
		};
		static readonly HexIndexes[] stringsRecordIndexes = new HexIndexes[] {
			new HexIndexes(0, 1),
			new HexIndexes(1, 1),
		};
	}
}
