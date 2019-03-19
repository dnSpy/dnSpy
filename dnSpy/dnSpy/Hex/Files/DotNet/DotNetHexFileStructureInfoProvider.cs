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
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;
using dnSpy.Hex.Files.PE;
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
			if (structure is DotNetMethodBody body) {
				if (body.Kind == DotNetMethodBodyKind.Tiny)
					return Array.Empty<HexIndexes>();
				if (body is FatMethodBody fatBody) {
					if (fatBody.EHTable == null)
						return subStructFatWithoutEH;
					return subStructFatWithEH;
				}
			}

			if (structure is DotNetEmbeddedResource)
				return Array.Empty<HexIndexes>();

			if (structure is MultiResourceDataHeaderData multiResource) {
				if (multiResource is MultiResourceSimplDataHeaderData || multiResource is MultiResourceStringDataHeaderData)
					return multiResourceFields2;
				if (multiResource is MultiResourceArrayDataHeaderData)
					return multiResourceFields3;
				Debug.Fail($"Unknown multi res type: {multiResource.GetType()}");
			}

			var stringsRec = structure as StringsHeapRecordData;
			if (stringsRec?.Terminator != null)
				return stringsRecordIndexes;

			if (structure is USHeapRecordData usRec) {
				if (usRec.TerminalByte != null)
					return usRecordIndexes3;
				return usRecordIndexes2;
			}

			if (structure is BlobHeapRecordData)
				return blobRecordIndexes2;

			return null;
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
		static readonly HexIndexes[] usRecordIndexes2 = new HexIndexes[] {
			new HexIndexes(0, 1),
			new HexIndexes(1, 1),
		};
		static readonly HexIndexes[] usRecordIndexes3 = new HexIndexes[] {
			new HexIndexes(0, 1),
			new HexIndexes(1, 1),
			new HexIndexes(2, 1),
		};
		static readonly HexIndexes[] blobRecordIndexes2 = new HexIndexes[] {
			new HexIndexes(0, 1),
			new HexIndexes(1, 1),
		};

		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file, ComplexData structure, HexPosition position) {
			if (structure is MultiResourceUnicodeNameAndOffsetData resourceNameOffs)
				return GetFieldReferenceSpan(file, resourceNameOffs, position);

			if (structure is DotNetCor20Data cor20)
				return GetFieldReferenceSpan(file, cor20, position);

			if (structure is DotNetMultiFileResourceHeaderData multiResourceHeader)
				return GetFieldReferenceSpan(file, multiResourceHeader, position);

			if (structure is DotNetMetadataHeaderData mdHeader)
				return GetFieldReferenceSpan(file, mdHeader, position);

			if (structure is FatMethodBody fatBody)
				return GetFieldReferenceSpan(file, fatBody, position);

			if (structure is TableRecordData record)
				return GetFieldReferenceSpan(file, record, position);

			return null;
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, MultiResourceUnicodeNameAndOffsetData resourceNameOffs, HexPosition position) {
			if (resourceNameOffs.DataOffset.Data.Span.Span.Contains(position)) {
				uint offs = resourceNameOffs.DataOffset.Data.ReadValue();
				var pos = resourceNameOffs.ResourceProvider.DataSectionPosition + offs;
				if (pos >= file.Span.End)
					return null;
				return new HexSpan(pos, 0);
			}

			return null;
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, DotNetCor20Data cor20, HexPosition position) {
			HexSpan? span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.Metadata.Data, position)) != null)
				return span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.Resources.Data, position)) != null)
				return span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.StrongNameSignature.Data, position)) != null)
				return span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.CodeManagerTable.Data, position)) != null)
				return span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.VTableFixups.Data, position)) != null)
				return span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.ExportAddressTableJumps.Data, position)) != null)
				return span;
			if ((span = DataDirectoryDataUtils.TryGetSpan(file, cor20.ManagedNativeHeader.Data, position)) != null)
				return span;

			if (cor20.EntryPointTokenOrRVA.Data.Span.Span.Contains(position)) {
				uint value = cor20.EntryPointTokenOrRVA.Data.ReadValue();
				if (value == 0)
					return null;
				if ((cor20.Flags.Data.ReadValue() & 0x10) != 0) {
					var peHeaders = file.GetHeaders<PeHeaders>();
					if (peHeaders == null)
						return null;
					return new HexSpan(peHeaders.RvaToBufferPosition(value), 0);
				}
				else
					return TryGetTokenSpan(file, value);
			}

			return null;
		}

		static HexSpan? TryGetTokenSpan(HexBufferFile file, uint token) {
			var tablesStream = file.GetHeaders<DotNetHeaders>()?.MetadataHeaders?.TablesStream;
			if (tablesStream == null)
				return null;
			var mdToken = new MDToken(token);
			if ((uint)mdToken.Table >= tablesStream.MDTables.Count)
				return null;
			var mdTable = tablesStream.MDTables[(int)mdToken.Table];
			if (!mdTable.IsValidRID(mdToken.Rid))
				return null;
			return new HexSpan(mdTable.Span.Start + (mdToken.Rid - 1) * mdTable.RowSize, mdTable.RowSize);
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, DotNetMultiFileResourceHeaderData multiResourceHeader, HexPosition position) {
			if (multiResourceHeader.NamePositions.Data.Span.Span.Contains(position)) {
				var data = (UInt32Data)multiResourceHeader.NamePositions.Data.GetFieldByPosition(position).Data;
				uint offset = data.ReadValue();
				var nameOffs = multiResourceHeader.Span.Span.End;
				var pos = nameOffs + offset;
				if (pos >= file.Span.End)
					return null;
				return new HexSpan(pos, 0);
			}

			return null;
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, DotNetMetadataHeaderData header, HexPosition position) {
			if (header.StreamHeaders.Data.Span.Span.Contains(position)) {
				var stream = (DotNetStorageStream)header.StreamHeaders.Data.GetFieldByPosition(position).Data;
				if (stream.Offset.Data.Span.Span.Contains(position)) {
					uint offset = stream.Offset.Data.ReadValue();
					uint size = stream.Size.Data.ReadValue();
					var pos = header.Span.Span.Start + offset;
					if (pos >= file.Span.End)
						return null;
					var mdHeaders = file.GetHeaders<DotNetMetadataHeaders>();
					if (mdHeaders == null)
						return new HexSpan(pos, 0);
					if (pos >= mdHeaders.MetadataSpan.End)
						return null;
					var end = pos + size;
					if (end > mdHeaders.MetadataSpan.End)
						return new HexSpan(pos, 0);
					return HexSpan.FromBounds(pos, end);
				}
				return null;
			}

			return null;
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, FatMethodBody fatBody, HexPosition position) {
			var ehTable = fatBody.EHTable;
			if (ehTable != null) {
				if (!ehTable.Data.Span.Span.Contains(position))
					return null;

				if (ehTable.Data.IsSmall) {
					var smallTable = (SmallExceptionHandlerTable)ehTable.Data;
					if (!smallTable.Clauses.Data.Span.Span.Contains(position))
						return null;
					var clause = (SmallExceptionClause)smallTable.Clauses.Data.GetFieldByPosition(position)?.Data;
					if (clause == null)
						return null;
					HexSpan? span;
					if ((span = TryGetSpan(fatBody, position, clause.TryOffset.Data, clause.TryLength.Data)) != null)
						return span;
					if ((span = TryGetSpan(fatBody, position, clause.HandlerOffset.Data, clause.HandlerLength.Data)) != null)
						return span;
					if (clause.ClassTokenOrFilterOffset.Data.Span.Span.Contains(position)) {
						if (clause.Flags.Data.ReadValue() == 0)
							return TryGetTokenSpan(file, clause.ClassTokenOrFilterOffset.Data.ReadValue());
						else
							return TryGetSpan(fatBody, clause.ClassTokenOrFilterOffset.Data.ReadValue(), 1);
					}
				}
				else {
					var fatTable = (FatExceptionHandlerTable)ehTable.Data;
					if (!fatTable.Clauses.Data.Span.Span.Contains(position))
						return null;
					var clause = (FatExceptionClause)fatTable.Clauses.Data.GetFieldByPosition(position)?.Data;
					if (clause == null)
						return null;
					HexSpan? span;
					if ((span = TryGetSpan(fatBody, position, clause.TryOffset.Data, clause.TryLength.Data)) != null)
						return span;
					if ((span = TryGetSpan(fatBody, position, clause.HandlerOffset.Data, clause.HandlerLength.Data)) != null)
						return span;
					if (clause.ClassTokenOrFilterOffset.Data.Span.Span.Contains(position)) {
						if (clause.Flags.Data.ReadValue() == 0)
							return TryGetTokenSpan(file, clause.ClassTokenOrFilterOffset.Data.ReadValue());
						else
							return TryGetSpan(fatBody, clause.ClassTokenOrFilterOffset.Data.ReadValue(), 1);
					}
				}

				return null;
			}

			return null;
		}

		HexSpan? TryGetSpan(FatMethodBody fatBody, HexPosition position, UInt16Data offsetData, ByteData lengthData) {
			if (!offsetData.Span.Span.Contains(position))
				return null;
			return TryGetSpan(fatBody, offsetData.ReadValue(), lengthData.ReadValue());
		}

		HexSpan? TryGetSpan(FatMethodBody fatBody, HexPosition position, UInt32Data offsetData, UInt32Data lengthData) {
			if (!offsetData.Span.Span.Contains(position))
				return null;
			return TryGetSpan(fatBody, offsetData.ReadValue(), lengthData.ReadValue());
		}

		HexSpan? TryGetSpan(FatMethodBody fatBody, uint offset, uint length) {
			var pos = fatBody.Instructions.Data.Span.Span.Start + offset;
			var end = pos + length;
			if (end > fatBody.Instructions.Data.Span.Span.End)
				return null;
			return HexSpan.FromBounds(pos, end);
		}

		HexSpan? GetFieldReferenceSpan(HexBufferFile file, TableRecordData record, HexPosition position) {
			if (record.Token.Table == Table.ManifestResource) {
				var recordOffset = (position - record.Span.Span.Start).ToUInt64();
				// Check if it's not Offset column
				if (recordOffset >= 4)
					return null;

				var mdTable = record.TablesHeap.MDTables[(int)Table.ManifestResource];
				Debug.Assert(mdTable.IsValidRID(record.Token.Rid));
				if (!mdTable.IsValidRID(record.Token.Rid))
					return null;
				var recordPos = mdTable.Span.Start + (record.Token.Rid - 1) * mdTable.RowSize;
				var buffer = file.Buffer;
				uint offset = buffer.ReadUInt32(recordPos);
				uint implementation = mdTable.TableInfo.Columns[3].Size == 2 ?
					buffer.ReadUInt16(recordPos + mdTable.RowSize - 2) :
					buffer.ReadUInt32(recordPos + mdTable.RowSize - 4);
				if (!CodedToken.Implementation.Decode(implementation, out MDToken implementationToken))
					return null;
				if (implementationToken.Rid != 0)
					return null;

				var resources = file.GetHeaders<DotNetHeaders>()?.ResourceProvider;
				if (resources == null)
					return null;
				if (offset >= resources.ResourcesSpan.Length)
					return null;
				var pos = resources.ResourcesSpan.Start + offset;
				uint size = pos + 4 > resources.ResourcesSpan.End ? 0 : buffer.ReadUInt32(pos);
				var end = (pos + 4) + size;
				if (end > resources.ResourcesSpan.End)
					return new HexSpan(pos, 0);
				return HexSpan.FromBounds(pos, end);
			}

			return null;
		}
	}
}
