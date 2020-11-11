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
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	[Export(typeof(HexStructureInfoProviderFactory))]
	[VSUTIL.Name(PredefinedHexStructureInfoProviderFactoryNames.DefaultHexFileStructure)]
	sealed class DefaultHexStructureInfoProviderFactory : HexStructureInfoProviderFactory {
		readonly HexBufferFileServiceFactory hexBufferFileServiceFactory;
		readonly HexFileStructureInfoServiceFactory hexFileStructureInfoServiceFactory;

		[ImportingConstructor]
		DefaultHexStructureInfoProviderFactory(HexBufferFileServiceFactory hexBufferFileServiceFactory, HexFileStructureInfoServiceFactory hexFileStructureInfoServiceFactory) {
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
			this.hexFileStructureInfoServiceFactory = hexFileStructureInfoServiceFactory;
		}

		public override HexStructureInfoProvider Create(HexView hexView) =>
			new DefaultHexStructureInfoProvider(hexView, hexBufferFileServiceFactory, hexFileStructureInfoServiceFactory);
	}

	sealed class DefaultHexStructureInfoProvider : HexStructureInfoProvider {
		readonly HexBufferFileService hexBufferFileService;
		readonly HexFileStructureInfoService hexFileStructureInfoService;

		public DefaultHexStructureInfoProvider(HexView hexView, HexBufferFileServiceFactory hexBufferFileServiceFactory, HexFileStructureInfoServiceFactory hexFileStructureInfoServiceFactory) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			if (hexBufferFileServiceFactory is null)
				throw new ArgumentNullException(nameof(hexBufferFileServiceFactory));
			if (hexFileStructureInfoServiceFactory is null)
				throw new ArgumentNullException(nameof(hexFileStructureInfoServiceFactory));
			hexBufferFileService = hexBufferFileServiceFactory.Create(hexView.Buffer);
			hexFileStructureInfoService = hexFileStructureInfoServiceFactory.Create(hexView);
		}

		public override IEnumerable<HexStructureField> GetFields(HexPosition position) {
			var info = hexBufferFileService.GetFileAndStructure(position);
			if (info is null)
				yield break;

			var structure = info.Value.Structure;
			var field = structure.GetSimpleField(position);
			Debug2.Assert(field is not null);
			if (field is null)
				yield break;
			yield return new HexStructureField(field.Data.Span, HexStructureFieldKind.CurrentField);

			var indexes = hexFileStructureInfoService.GetSubStructureIndexes(position);
			if (indexes is not null) {
				if (indexes.Length == 0) {
					for (int i = 0; i < structure.FieldCount; i++) {
						var span = structure.GetFieldByIndex(i).Data.Span;
						yield return new HexStructureField(span, HexStructureFieldKind.SubStructure);
					}
				}
				else {
					for (int i = 0; i < indexes.Length; i++) {
						var start = structure.GetFieldByIndex(indexes[i].Start).Data.Span.Start;
						var end = structure.GetFieldByIndex(indexes[i].End - 1).Data.Span.End;
						var span = HexBufferSpan.FromBounds(start, end);
						yield return new HexStructureField(span, HexStructureFieldKind.SubStructure);
					}
				}
			}
			else
				yield return new HexStructureField(structure.Span, HexStructureFieldKind.Structure);
		}

		public override object? GetToolTip(HexPosition position) => hexFileStructureInfoService.GetToolTip(position);
		public override object? GetReference(HexPosition position) => hexFileStructureInfoService.GetReference(position);
	}
}
