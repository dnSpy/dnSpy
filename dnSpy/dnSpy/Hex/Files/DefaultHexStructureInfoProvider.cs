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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	[Export(typeof(HexStructureInfoProviderFactory))]
	[VSUTIL.Name(PredefinedHexStructureInfoProviderFactoryNames.DefaultHexFileStructure)]
	sealed class DefaultHexStructureInfoProviderFactory : HexStructureInfoProviderFactory {
		readonly HexBufferFileServiceFactory hexBufferFileServiceFactory;
		readonly Lazy<HexFileStructureInfoProviderFactory, VSUTIL.IOrderable>[] hexFileStructureInfoProviderFactories;

		[ImportingConstructor]
		DefaultHexStructureInfoProviderFactory(HexBufferFileServiceFactory hexBufferFileServiceFactory, [ImportMany] IEnumerable<Lazy<HexFileStructureInfoProviderFactory, VSUTIL.IOrderable>> hexFileStructureInfoProviderFactories) {
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
			this.hexFileStructureInfoProviderFactories = VSUTIL.Orderer.Order(hexFileStructureInfoProviderFactories).ToArray();
		}

		public override HexStructureInfoProvider Create(HexView hexView) =>
			new DefaultHexStructureInfoProvider(hexView, hexBufferFileServiceFactory, hexFileStructureInfoProviderFactories);
	}

	sealed class DefaultHexStructureInfoProvider : HexStructureInfoProvider {
		readonly HexView hexView;
		readonly HexBufferFileService hexBufferFileService;
		readonly Lazy<HexFileStructureInfoProviderFactory, VSUTIL.IOrderable>[] hexFileStructureInfoProviderFactories;

		HexFileStructureInfoProvider[] HexFileStructureInfoProviders {
			get {
				if (hexFileStructureInfoProviders == null)
					hexFileStructureInfoProviders = CreateProviders();
				return hexFileStructureInfoProviders;
			}
		}
		HexFileStructureInfoProvider[] hexFileStructureInfoProviders;

		public DefaultHexStructureInfoProvider(HexView hexView, HexBufferFileServiceFactory hexBufferFileServiceFactory, Lazy<HexFileStructureInfoProviderFactory, VSUTIL.IOrderable>[] hexFileStructureInfoProviderFactories) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (hexBufferFileServiceFactory == null)
				throw new ArgumentNullException(nameof(hexBufferFileServiceFactory));
			if (hexFileStructureInfoProviderFactories == null)
				throw new ArgumentNullException(nameof(hexFileStructureInfoProviderFactories));
			this.hexView = hexView;
			hexBufferFileService = hexBufferFileServiceFactory.Create(hexView.Buffer);
			this.hexFileStructureInfoProviderFactories = hexFileStructureInfoProviderFactories;
		}

		HexFileStructureInfoProvider[] CreateProviders() {
			var providers = new List<HexFileStructureInfoProvider>(hexFileStructureInfoProviderFactories.Length);
			foreach (var lz in hexFileStructureInfoProviderFactories) {
				var provider = lz.Value.Create(hexView);
				if (provider != null)
					providers.Add(provider);
			}
			return providers.ToArray();
		}

		KeyValuePair<HexBufferFile, ComplexData>? GetStructure(HexPosition position) {
			var file = hexBufferFileService.GetFile(position, checkNestedFiles: true);
			if (file == null)
				return null;
			var structure = file.GetStructure(position, checkNestedFiles: true);
			if (structure == null || structure.Span.IsEmpty)
				return null;
			return new KeyValuePair<HexBufferFile, ComplexData>(file, structure);
		}

		public override IEnumerable<HexStructureField> GetFields(HexPosition position) {
			var info = GetStructure(position);
			if (info == null)
				yield break;

			var structure = info.Value.Value;
			var field = GetField(structure, position);
			Debug.Assert(field != null);
			if (field == null)
				yield break;
			yield return new HexStructureField(field.Data.Span, HexStructureFieldKind.CurrentField);

			var indexes = GetSubStructureIndexes(info.Value.Key, position, structure);
			if (indexes != null) {
				if (indexes.Length == 0) {
					for (int i = 0; i < structure.FieldCount; i++) {
						var span = structure.GetFieldByIndex(i).Data.Span;
						yield return new HexStructureField(span, HexStructureFieldKind.SubStructure);
					}
				}
				else {
					for (int i = 0; i < indexes.Length; i++) {
						var start = structure.GetFieldByIndex(indexes[i]).Data.Span.Start;
						var end = i + 1 < indexes.Length ?
							structure.GetFieldByIndex(indexes[i + 1]).Data.Span.Start :
							structure.GetFieldByIndex(structure.FieldCount - 1).Data.Span.End;
						var span = HexBufferSpan.FromBounds(start, end);
						yield return new HexStructureField(span, HexStructureFieldKind.SubStructure);
					}
				}
			}
			else
				yield return new HexStructureField(structure.Span, HexStructureFieldKind.Structure);
		}

		static BufferField GetField(ComplexData structure, HexPosition position) {
			for (;;) {
				var field = structure.GetFieldByPosition(position);
				if (field == null)
					return null;
				structure = field.Data as ComplexData;
				if (structure == null)
					return field;
			}
		}

		int[] GetSubStructureIndexes(HexBufferFile file, HexPosition position, ComplexData structure) {
			foreach (var provider in HexFileStructureInfoProviders) {
				var indexes = provider.GetSubStructureIndexes(file, position, structure);
				if (IsValidIndexes(indexes, structure))
					return indexes;
			}
			return null;
		}

		static bool IsValidIndexes(int[] indexes, ComplexData structure) {
			if (indexes == null)
				return false;
			if (indexes.Length == 0)
				return true;
			if (indexes[0] != 0)
				return false;
			if ((uint)indexes[0] >= (uint)structure.FieldCount)
				return false;
			for (int i = 1; i < indexes.Length; i++) {
				if (indexes[i - 1] >= indexes[i])
					return false;
				if ((uint)indexes[i] >= (uint)structure.FieldCount)
					return false;
			}
			return true;
		}

		public override object GetToolTip(HexPosition position) {
			var info = GetStructure(position);
			if (info == null)
				return null;

			foreach (var provider in HexFileStructureInfoProviders) {
				var toolTip = provider.GetToolTip(info.Value.Key, position, info.Value.Value);
				if (toolTip != null)
					return toolTip;
			}

			return null;
		}

		public override object GetReference(HexPosition position) {
			var info = GetStructure(position);
			if (info == null)
				return null;

			foreach (var provider in HexFileStructureInfoProviders) {
				var toolTip = provider.GetReference(info.Value.Key, position, info.Value.Value);
				if (toolTip != null)
					return toolTip;
			}

			return null;
		}
	}
}
