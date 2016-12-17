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

namespace dnSpy.AsmEditor.Hex.PE {
	[Export(typeof(HexStructureInfoProviderFactory))]
	sealed class HexStructureInfoProviderFactoryImpl : HexStructureInfoProviderFactory {
		readonly PEStructureProviderFactory peStructureProviderFactory;

		[ImportingConstructor]
		HexStructureInfoProviderFactoryImpl(PEStructureProviderFactory peStructureProviderFactory) {
			this.peStructureProviderFactory = peStructureProviderFactory;
		}

		public override HexStructureInfoProvider Create(HexView hexView) {
			//TODO: The current code only supports file layout, not memory layout
			if (hexView.Buffer.IsMemory)
				return null;
			var pePosition = HexPosition.Zero;
			var peStructureProvider = peStructureProviderFactory.TryGetProvider(hexView.Buffer, pePosition);
			if (peStructureProvider == null)
				return null;
			return new HexStructureInfoProviderImpl(peStructureProvider);
		}
	}

	sealed class HexStructureInfoProviderImpl : HexStructureInfoProvider {
		readonly PEStructure peStructure;

		public HexStructureInfoProviderImpl(PEStructureProvider peStructureProvider) {
			if (peStructureProvider == null)
				throw new ArgumentNullException(nameof(peStructureProvider));
			peStructure = new PEStructure(peStructureProvider);
		}

		sealed class PEStructure {
			public HexBuffer Buffer => peStructureProvider.Buffer;
			readonly PEStructureProvider peStructureProvider;
			readonly HexVM[] hexStructures;
			readonly HexSpan metadataTablesSpan;

			public PEStructure(PEStructureProvider peStructureProvider) {
				this.peStructureProvider = peStructureProvider;

				var list = new List<HexVM> {
					peStructureProvider.ImageDosHeader,
					peStructureProvider.ImageFileHeader,
					peStructureProvider.ImageOptionalHeader,
				};
				if (peStructureProvider.ImageCor20Header != null)
					list.Add(peStructureProvider.ImageCor20Header);
				if (peStructureProvider.StorageSignature != null)
					list.Add(peStructureProvider.StorageSignature);
				if (peStructureProvider.StorageHeader != null)
					list.Add(peStructureProvider.StorageHeader);
				if (peStructureProvider.TablesStream != null)
					list.Add(peStructureProvider.TablesStream);
				list.AddRange(peStructureProvider.Sections);
				list.AddRange(peStructureProvider.StorageStreams);
				hexStructures = list.ToArray();

				var tblsStream = peStructureProvider.TablesStream;
				if (tblsStream != null) {
					var first = tblsStream.MetaDataTables.FirstOrDefault(a => a != null);
					var last = tblsStream.MetaDataTables.LastOrDefault(a => a != null);
					Debug.Assert(first != null);
					if (first != null)
						metadataTablesSpan = HexSpan.FromBounds(first.Span.Start, last.Span.End);
				}
			}

			public FieldAndStructure? GetField(HexPosition position) {
				foreach (var structure in hexStructures) {
					if (structure.Span.Contains(position)) {
						foreach (var field in structure.HexFields) {
							if (field.IsVisible && field.Span.Contains(position))
								return new FieldAndStructure(structure, field);
						}
					}
				}
				if (metadataTablesSpan.Contains(position)) {
					foreach (var mdTbl in peStructureProvider.TablesStream.MetaDataTables) {
						if (mdTbl == null || !mdTbl.Span.Contains(position))
							continue;
						var offset = position - mdTbl.Span.Start;
						if (offset >= uint.MaxValue)
							break;
						uint index = (uint)(offset.ToUInt64() / (uint)mdTbl.TableInfo.RowSize);
						Debug.Assert(index < mdTbl.Rows);
						if (index >= mdTbl.Rows)
							break;
						var record = mdTbl.Get((int)index);
						foreach (var field in record.HexFields) {
							if (field.IsVisible && field.Span.Contains(position))
								return new FieldAndStructure(record, field);
						}
						break;
					}
				}
				return null;
			}
		}

		struct FieldAndStructure {
			public HexVM Structure { get; }
			public HexField Field { get; }
			public FieldAndStructure(HexVM structure, HexField field) {
				if (structure == null)
					throw new ArgumentNullException(nameof(structure));
				if (field == null)
					throw new ArgumentNullException(nameof(field));
				Structure = structure;
				Field = field;
			}
		}

		FieldAndStructure? GetField(HexPosition position) => peStructure.GetField(position);

		public override IEnumerable<HexStructureField> GetFields(HexPosition position) {
			var info = GetField(position);
			if (info != null) {
				var buffer = peStructure.Buffer;
				yield return new HexStructureField(new HexBufferSpan(buffer, info.Value.Structure.Span), HexStructureFieldKind.Structure);
				yield return new HexStructureField(new HexBufferSpan(buffer, info.Value.Field.Span), HexStructureFieldKind.CurrentField);
			}
		}

		public override object GetReference(HexPosition position) => GetField(position)?.Field;

		public override object GetToolTip(HexPosition position) {
			var info = GetField(position);
			if (info != null)
				return info.Value.Structure.Name + "." + info.Value.Field.Name;
			return null;
		}
	}
}
