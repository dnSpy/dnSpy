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
using dnSpy.Contracts.Hex.Classification.DnSpy;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.PE {
	[Export(typeof(HexStructureInfoProviderFactory))]
	sealed class HexStructureInfoProviderFactoryImpl : HexStructureInfoProviderFactory {
		readonly HexTextElementCreatorProvider hexTextElementCreatorProvider;
		readonly PEStructureProviderFactory peStructureProviderFactory;

		[ImportingConstructor]
		HexStructureInfoProviderFactoryImpl(HexTextElementCreatorProvider hexTextElementCreatorProvider, PEStructureProviderFactory peStructureProviderFactory) {
			this.hexTextElementCreatorProvider = hexTextElementCreatorProvider;
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
			return new HexStructureInfoProviderImpl(hexTextElementCreatorProvider, peStructureProvider);
		}
	}

	sealed class HexStructureInfoProviderImpl : HexStructureInfoProvider {
		readonly HexTextElementCreatorProvider hexTextElementCreatorProvider;
		readonly PEStructure peStructure;

		public HexStructureInfoProviderImpl(HexTextElementCreatorProvider hexTextElementCreatorProvider, PEStructureProvider peStructureProvider) {
			if (hexTextElementCreatorProvider == null)
				throw new ArgumentNullException(nameof(hexTextElementCreatorProvider));
			if (peStructureProvider == null)
				throw new ArgumentNullException(nameof(peStructureProvider));
			this.hexTextElementCreatorProvider = hexTextElementCreatorProvider;
			peStructure = new PEStructure(peStructureProvider);
		}

		sealed class PEStructure {
			public HexBuffer Buffer => peStructureProvider.Buffer;
			public HexSpan PESpan => peStructureProvider.PESpan;
			readonly PEStructureProvider peStructureProvider;
			readonly HexVM[] hexStructures;
			readonly HexSpan metadataTablesSpan;
			readonly MethodBodyInfoProvider methodBodyInfoProvider;

			public PEStructure(PEStructureProvider peStructureProvider) {
				this.peStructureProvider = peStructureProvider;
				methodBodyInfoProvider = MethodBodyInfoProvider.TryCreate(peStructureProvider);

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

			public MethodBodyInfoAndField? GetMethodBodyInfoAndField(HexPosition position) =>
				methodBodyInfoProvider.GetMethodBodyInfoAndField(position);
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

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null) {
				var minfo = mfInfo.Value.MethodBodyInfo;
				var buffer = peStructure.Buffer;
				yield return new HexStructureField(new HexBufferSpan(buffer, minfo.HeaderSpan), HexStructureFieldKind.SubStructure);
				if (minfo.InstructionsSpan.Length != 0)
					yield return new HexStructureField(new HexBufferSpan(buffer, minfo.InstructionsSpan), HexStructureFieldKind.SubStructure);
				if (minfo.ExceptionsSpan.Length != 0)
					yield return new HexStructureField(new HexBufferSpan(buffer, minfo.ExceptionsSpan), HexStructureFieldKind.SubStructure);
				yield return new HexStructureField(new HexBufferSpan(buffer, mfInfo.Value.FieldInfo.Span), HexStructureFieldKind.CurrentField);
			}
		}

		public override object GetReference(HexPosition position) {
			var info = GetField(position);
			if (info != null)
				return new HexFieldReference(new HexBufferSpan(peStructure.Buffer, peStructure.PESpan), info.Value.Structure, info.Value.Field);

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null) {
				var minfo = mfInfo.Value.MethodBodyInfo;
				if (minfo.InstructionsSpan.Contains(position))
					return new HexMethodReference(new HexBufferSpan(peStructure.Buffer, peStructure.PESpan), minfo.Rids[0], (uint)(position - minfo.InstructionsSpan.Start).ToUInt64());
				return new HexMethodReference(new HexBufferSpan(peStructure.Buffer, peStructure.PESpan), minfo.Rids[0], null);
			}

			return null;
		}

		public override object GetToolTip(HexPosition position) {
			var info = GetField(position);
			if (info != null) {
				var creator = new ToolTipCreator(hexTextElementCreatorProvider);
				bool create = CreateFieldToolTip(creator, info.Value);
				return create ? creator.Create() : null;
			}

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null) {
				var creator = new ToolTipCreator(hexTextElementCreatorProvider);
				bool create = CreateMethodBodyToolTip(creator, mfInfo.Value.MethodBodyInfo, mfInfo.Value.FieldInfo, position, mfInfo.Value.MethodBodyInfo.IsSmallExceptionClauses);
				return create ? creator.Create() : null;
			}

			return null;
		}

		bool CreateFieldToolTip(ToolTipCreator creator, FieldAndStructure info) {
			var output = creator.Writer;
			creator.Image = DsImages.FieldPublic;
			output.Write(BoxedTextColor.ValueType, info.Structure.Name);
			output.Write(BoxedTextColor.Punctuation, ".");
			output.Write(BoxedTextColor.InstanceField, info.Field.Name);
			return true;
		}

		bool CreateMethodBodyToolTip(ToolTipCreator creator, MethodBodyInfo minfo, MethodBodyFieldInfo finfo, HexPosition position, bool isSmallExceptionClauses) {
			const string methodHeader = "MethodHeader";
			const string methodBody = "MethodBody";
			const string exceptionHeader = "ExceptionHeader";
			const string exceptionClauses = "ExceptionClauses";
			const string unknown = "???";

			creator.Image = DsImages.MethodPublic;
			var output = creator.Writer;

			output.Write(BoxedTextColor.Text, "Method");
			output.WriteSpace();
			const int maxRids = 10;
			for (int i = 0; i < minfo.Rids.Count; i++) {
				if (i > 0) {
					output.Write(BoxedTextColor.Punctuation, ",");
					output.WriteSpace();
				}
				if (i >= maxRids) {
					output.Write(BoxedTextColor.Error, "...");
					break;
				}
				output.Write(BoxedTextColor.Number, "0x06" + minfo.Rids[i].ToString("X6"));
			}
			output = creator.CreateNewWriter();

			switch (finfo.FieldKind) {
			case MethodBodyFieldKind.None:
				return false;

			case MethodBodyFieldKind.Unknown:
			case MethodBodyFieldKind.InvalidBody:
				output.Write(BoxedTextColor.ValueType, methodBody);
				break;

			case MethodBodyFieldKind.LargeHeaderFlags:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Flags");
				break;

			case MethodBodyFieldKind.LargeHeaderMaxStack:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "MaxStack");
				break;

			case MethodBodyFieldKind.SmallHeaderCodeSize:
			case MethodBodyFieldKind.LargeHeaderCodeSize:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "CodeSize");
				break;

			case MethodBodyFieldKind.LargeHeaderLocalVarSigTok:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "LocalVarSigTok");
				break;

			case MethodBodyFieldKind.LargeHeaderUnknown:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.Error, unknown);
				break;

			case MethodBodyFieldKind.InstructionBytes:
				//TODO: Disassemble the instruction
				output.Write(BoxedTextColor.ValueType, methodBody);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Instructions");
				output.Write(BoxedTextColor.Punctuation, ":");
				output.WriteSpace();
				output.Write(BoxedTextColor.Text, "offset");
				output.WriteSpace();
				output.Write(BoxedTextColor.Number, (position - minfo.InstructionsSpan.Start).ToString());
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderKind:
			case MethodBodyFieldKind.LargeExceptionHeaderKind:
				output.Write(BoxedTextColor.ValueType, exceptionHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Kind");
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderDataSize:
			case MethodBodyFieldKind.LargeExceptionHeaderDataSize:
				output.Write(BoxedTextColor.ValueType, exceptionHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "DataSize");
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderPadding:
				output.Write(BoxedTextColor.ValueType, exceptionHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Padding");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseFlags:
			case MethodBodyFieldKind.LargeExceptionClauseFlags:
				WriteExceptionClause(output, minfo, position, "Flags", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseTryOffset:
			case MethodBodyFieldKind.LargeExceptionClauseTryOffset:
				WriteExceptionClause(output, minfo, position, "TryOffset", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseTryLength:
			case MethodBodyFieldKind.LargeExceptionClauseTryLength:
				WriteExceptionClause(output, minfo, position, "TryLength", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseHandlerOffset:
			case MethodBodyFieldKind.LargeExceptionClauseHandlerOffset:
				WriteExceptionClause(output, minfo, position, "HandlerOffset", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseHandlerLength:
			case MethodBodyFieldKind.LargeExceptionClauseHandlerLength:
				WriteExceptionClause(output, minfo, position, "HandlerLength", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseClassToken:
			case MethodBodyFieldKind.LargeExceptionClauseClassToken:
				WriteExceptionClause(output, minfo, position, "ClassToken", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseFilterOffset:
			case MethodBodyFieldKind.LargeExceptionClauseFilterOffset:
				WriteExceptionClause(output, minfo, position, "FilterOffset", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseReserved:
			case MethodBodyFieldKind.LargeExceptionClauseReserved:
				WriteExceptionClause(output, minfo, position, "Reserved", isSmallExceptionClauses);
				break;

			case MethodBodyFieldKind.ExceptionClausesUnknown:
				output.Write(BoxedTextColor.ValueType, exceptionClauses);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.Error, unknown);
				break;

			default:
				throw new InvalidOperationException();
			}

			return true;
		}

		void WriteExceptionClause(ITextColorWriter output, MethodBodyInfo minfo, HexPosition position, string fieldName, bool isSmallExceptionClauses) {
			const string exceptionClauses = "ExceptionClauses";
			int ehIndex = (int)((position - minfo.ExceptionsSpan.Start - 4).ToUInt64() / (isSmallExceptionClauses ? 12U : 24));
			output.Write(BoxedTextColor.ValueType, exceptionClauses);
			output.Write(BoxedTextColor.Punctuation, "[");
			output.Write(BoxedTextColor.Number, ehIndex.ToString());
			output.Write(BoxedTextColor.Punctuation, "]");
			output.Write(BoxedTextColor.Punctuation, ".");
			output.Write(BoxedTextColor.InstanceField, fieldName);
		}
	}
}
