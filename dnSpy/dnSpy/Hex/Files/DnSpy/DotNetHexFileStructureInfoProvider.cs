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
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DnSpy;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Text;
using dnSpy.Contracts.Images;
using dnSpy.Hex.Files.DotNet;
using dnSpy.Properties;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.DnSpy {
	[Export(typeof(HexFileStructureInfoProviderFactory))]
	[VSUTIL.Name("dnSpy-DotNet")]
	[VSUTIL.Order(Before = PredefinedHexFileStructureInfoProviderFactoryNames.Default)]
	sealed class DotNetHexFileStructureInfoProviderFactory : HexFileStructureInfoProviderFactory {
		readonly ToolTipCreatorFactory toolTipCreatorFactory;

		[ImportingConstructor]
		DotNetHexFileStructureInfoProviderFactory(ToolTipCreatorFactory toolTipCreatorFactory) => this.toolTipCreatorFactory = toolTipCreatorFactory;

		public override HexFileStructureInfoProvider? Create(HexView hexView) =>
			new DotNetHexFileStructureInfoProvider(toolTipCreatorFactory);
	}

	sealed class DotNetHexFileStructureInfoProvider : HexFileStructureInfoProvider {
		readonly ToolTipCreatorFactory toolTipCreatorFactory;

		public DotNetHexFileStructureInfoProvider(ToolTipCreatorFactory toolTipCreatorFactory) => this.toolTipCreatorFactory = toolTipCreatorFactory ?? throw new ArgumentNullException(nameof(toolTipCreatorFactory));

		public override object? GetToolTip(HexBufferFile file, ComplexData structure, HexPosition position) {
			if (structure is DotNetMethodBody body)
				return GetToolTip(body, position);

			if (structure is DotNetEmbeddedResource resource)
				return GetToolTip(resource, position);

			if (structure is MultiResourceDataHeaderData resDataHdr)
				return GetToolTip(resDataHdr, position);

			if (structure is GuidHeapRecordData guidRecord)
				return GetToolTip(guidRecord, position);

			if (structure is StringsHeapRecordData stringsRecord)
				return GetToolTip(stringsRecord, position);

			if (structure is USHeapRecordData usRecord)
				return GetToolTip(usRecord, position);

			if (structure is BlobHeapRecordData blobRecord)
				return GetToolTip(blobRecord, position);

			if (structure is TableRecordData tableRecord)
				return GetToolTip(tableRecord, position);

			return null;
		}

		void WriteTokens(HexFieldFormatter writer, IList<uint> tokens) {
			const int maxTokens = 10;
			for (int i = 0; i < tokens.Count; i++) {
				if (i > 0) {
					writer.Write(",", PredefinedClassifiedTextTags.Punctuation);
					writer.WriteSpace();
				}
				if (i >= maxTokens) {
					writer.Write("...", PredefinedClassifiedTextTags.Error);
					break;
				}
				writer.Write("0x" + tokens[i].ToString("X8"), PredefinedClassifiedTextTags.Number);
			}
		}

		object? GetToolTip(DotNetMethodBody body, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = DsImages.MethodPublic;

			var writer = contentCreator.Writer;
			writer.Write("Method", PredefinedClassifiedTextTags.Text);
			writer.WriteSpace();
			WriteTokens(writer, body.Tokens);
			contentCreator.CreateNewWriter();

			contentCreator.Writer.WriteFieldAndValue(body, position);

			return toolTipCreator.Create();
		}

		object? GetToolTip(DotNetEmbeddedResource resource, HexPosition position) {
			var mdHeaders = resource.ResourceProvider.File.GetHeaders<DotNetMetadataHeaders>();
			Debug2.Assert(mdHeaders is not null);
			if (mdHeaders is null)
				return null;
			var rec = mdHeaders.TablesStream?.GetRecord(resource.Token);
			Debug2.Assert(rec is not null);
			if (rec is null)
				return null;
			const int NameColumn = 2;
			var filteredName = NameUtils.FilterName(mdHeaders.StringsStream?.Read(rec.ReadColumn(NameColumn)) ?? string.Empty);

			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = GetResourceImage(resource, filteredName);

			contentCreator.Writer.WriteFilename(filteredName);
			contentCreator.Writer.WriteSpace();
			contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
			contentCreator.Writer.WriteToken(resource.Token.Raw);
			contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
			contentCreator.CreateNewWriter();
			contentCreator.Writer.WriteFieldAndValue(resource, position);

			return toolTipCreator.Create();
		}

		ImageReference GetResourceImage(DotNetEmbeddedResource resource, string filename) {
			var span = resource.Content.Data.Span;
			// Check if it's a multi-file resource
			if (span.Length >= 4 && span.Buffer.ReadUInt32(span.Start) == 0xBEEFCACE)
				return DsImages.SourceFileGroup;
			return ImageReferenceUtils.GetImageReference(filename) ?? DsImages.Dialog;
		}

		object? GetToolTip(MultiResourceDataHeaderData resDataHdr, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = ImageReferenceUtils.GetImageReference(resDataHdr.ResourceInfo.Name) ??
						ImageReferenceUtils.GetImageReference(resDataHdr.ResourceInfo.TypeCode);

			contentCreator.Writer.WriteFilename(resDataHdr.ResourceInfo.Name);
			if (string.IsNullOrEmpty(resDataHdr.ResourceInfo.UserTypeName)) {
				contentCreator.Writer.WriteSpace();
				contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				var typeCode = resDataHdr.ResourceInfo.TypeCode;
				if (typeCode < ResourceTypeCode.UserTypes)
					contentCreator.Writer.Write(typeCode.ToString(), PredefinedClassifiedTextTags.EnumField);
				else
					contentCreator.Writer.Write("UserType" + (typeCode - ResourceTypeCode.UserTypes).ToString(), PredefinedClassifiedTextTags.EnumField);
				contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
				contentCreator.CreateNewWriter();
			}
			else {
				contentCreator.CreateNewWriter();
				contentCreator.Writer.Write(resDataHdr.ResourceInfo.UserTypeName, PredefinedClassifiedTextTags.Text);
				contentCreator.Writer.WriteLine();
			}
			contentCreator.Writer.WriteFieldAndValue(resDataHdr, position);

			return toolTipCreator.Create();
		}

		object? GetToolTip(GuidHeapRecordData guidRecord, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = DsImages.StructurePublic;
			contentCreator.Writer.Write("#GUID", PredefinedClassifiedTextTags.DotNetHeapName);
			contentCreator.Writer.Write(", ", PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.Write(dnSpy_Resources.HexToolTipIndex, PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.WriteSpace();
			contentCreator.Writer.WriteUInt32(guidRecord.Index);
			contentCreator.Writer.WriteSpace();
			contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
			contentCreator.Writer.Write(new Guid(guidRecord.Span.GetData()).ToString(), PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
			contentCreator.CreateNewWriter();
			contentCreator.Writer.WriteFieldAndValue(guidRecord, position);

			return toolTipCreator.Create();
		}

		object? GetToolTip(StringsHeapRecordData stringsRecord, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = DsImages.String;
			contentCreator.Writer.Write("#Strings", PredefinedClassifiedTextTags.DotNetHeapName);
			contentCreator.Writer.Write(", ", PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.Write(dnSpy_Resources.HexToolTipOffset, PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.WriteSpace();
			uint offset = (uint)(stringsRecord.Span.Span.Start - stringsRecord.Heap.Span.Span.Start).ToUInt64();
			contentCreator.Writer.WriteUInt32(offset);
			if (stringsRecord.Tokens.Count > 0) {
				contentCreator.Writer.WriteSpace();
				contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				WriteTokens(contentCreator.Writer, stringsRecord.Tokens);
				contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
			}
			contentCreator.CreateNewWriter();
			contentCreator.Writer.WriteFieldAndValue(stringsRecord, position);

			return toolTipCreator.Create();
		}

		object? GetToolTip(USHeapRecordData usRecord, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = DsImages.String;
			contentCreator.Writer.Write("#US", PredefinedClassifiedTextTags.DotNetHeapName);
			contentCreator.Writer.Write(", ", PredefinedClassifiedTextTags.Text);
			uint token = 0x70000000 + (uint)(usRecord.Span.Span.Start - usRecord.Heap.Span.Span.Start).ToUInt64();
			contentCreator.Writer.Write("0x" + token.ToString("X8"), PredefinedClassifiedTextTags.Number);
			contentCreator.CreateNewWriter();
			contentCreator.Writer.WriteFieldAndValue(usRecord, position);

			return toolTipCreator.Create();
		}

		object? GetToolTip(BlobHeapRecordData blobRecord, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = DsImages.Binary;
			contentCreator.Writer.Write("#Blob", PredefinedClassifiedTextTags.DotNetHeapName);
			contentCreator.Writer.Write(", ", PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.Write(dnSpy_Resources.HexToolTipOffset, PredefinedClassifiedTextTags.Text);
			contentCreator.Writer.WriteSpace();
			uint offset = (uint)(blobRecord.Span.Span.Start - blobRecord.Heap.Span.Span.Start).ToUInt64();
			contentCreator.Writer.WriteUInt32(offset);
			if (blobRecord.Tokens.Count != 0) {
				contentCreator.Writer.WriteSpace();
				contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				WriteTokens(contentCreator.Writer, blobRecord.Tokens);
				contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
			}
			contentCreator.CreateNewWriter();
			contentCreator.Writer.WriteFieldAndValue(blobRecord, position);

			return toolTipCreator.Create();
		}

		object? GetToolTip(TableRecordData tableRecord, HexPosition position) {
			var tablesHeap = tableRecord.TablesHeap;
			Debug.Assert((uint)tableRecord.Token.Table < (uint)tablesHeap.MDTables.Count);
			if ((uint)tableRecord.Token.Table >= (uint)tablesHeap.MDTables.Count)
				return null;
			var mdTable = tablesHeap.MDTables[(int)tableRecord.Token.Table];
			int offset = (int)(position - tableRecord.Span.Span.Start).ToUInt64();
			var column = mdTable.Columns.FirstOrDefault(a => a.Offset <= offset && offset < a.Offset + a.Size);
			Debug2.Assert(column is not null);
			if (column is null)
				return null;
			var mdHeaders = tablesHeap.Metadata;

			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = ImageReferenceUtils.GetImageReference(tableRecord.Token.Table);
			contentCreator.Writer.WriteFieldAndValue(tableRecord, position);

			var pos = tableRecord.Span.Span.Start + column.Offset;
			switch (column.ColumnSize) {
			case ColumnSize.Strings:
				var s = GetString(mdHeaders.StringsStream, column, pos);
				if (s is null)
					break;
				contentCreator.Writer.WriteSpace();
				contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				contentCreator.Writer.WriteString(s);
				contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
				break;

			case ColumnSize.GUID:
				var g = GetGuid(mdHeaders.GUIDStream, column, pos);
				if (g is null)
					break;
				contentCreator.Writer.WriteSpace();
				contentCreator.Writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				contentCreator.Writer.Write(g.Value.ToString(), PredefinedClassifiedTextTags.Text);
				contentCreator.Writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
				break;
			}

			return toolTipCreator.Create();
		}

		static string? GetString(StringsHeap? heap, ColumnInfo column, HexPosition position) {
			if (heap is null)
				return null;
			uint value = column.Size == 2 ? heap.Span.Buffer.ReadUInt16(position) : heap.Span.Buffer.ReadUInt32(position);
			if (value == 0)
				return null;
			const int MAX_LEN = 0x400;
			return heap.Read(value, MAX_LEN);
		}

		static Guid? GetGuid(GUIDHeap? heap, ColumnInfo column, HexPosition position) {
			if (heap is null)
				return null;
			uint index = column.Size == 2 ? heap.Span.Buffer.ReadUInt16(position) : heap.Span.Buffer.ReadUInt32(position);
			return heap.Read(index);
		}

		public override object? GetReference(HexBufferFile file, ComplexData structure, HexPosition position) {
			if (structure is DotNetMethodBody body)
				return GetReference(file, body, position);
			return null;
		}

		object GetReference(HexBufferFile file, DotNetMethodBody body, HexPosition position) {
			if (body.Instructions.Data.Span.Span.Contains(position))
				return new HexMethodReference(file, body.Tokens[0], (uint)(position - body.Instructions.Data.Span.Span.Start).ToUInt64());
			return new HexMethodReference(file, body.Tokens[0], null);
		}
	}
}
