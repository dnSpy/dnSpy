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
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Hex.PE {
	[Export(typeof(HexStructureInfoProviderFactory))]
	sealed class HexStructureInfoProviderFactoryImpl : HexStructureInfoProviderFactory {
		readonly HexTextElementCreatorProvider hexTextElementCreatorProvider;
		readonly PEStructureProviderFactory peStructureProviderFactory;
		readonly HexBufferFileServiceFactory hexBufferFileServiceFactory;

		[ImportingConstructor]
		HexStructureInfoProviderFactoryImpl(HexTextElementCreatorProvider hexTextElementCreatorProvider, PEStructureProviderFactory peStructureProviderFactory, HexBufferFileServiceFactory hexBufferFileServiceFactory) {
			this.hexTextElementCreatorProvider = hexTextElementCreatorProvider;
			this.peStructureProviderFactory = peStructureProviderFactory;
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
		}

		public override HexStructureInfoProvider Create(HexView hexView) =>
			new HexStructureInfoProviderImpl(hexTextElementCreatorProvider, peStructureProviderFactory, hexBufferFileServiceFactory.Create(hexView.Buffer));
	}

	sealed class HexStructureInfoProviderImpl : HexStructureInfoProvider {
		readonly HexTextElementCreatorProvider hexTextElementCreatorProvider;
		readonly PEStructureProviderFactory peStructureProviderFactory;
		readonly HexBufferFileService hexBufferFileService;
		readonly Dictionary<HexBufferFile, PEStructure> peStructures;

		public HexStructureInfoProviderImpl(HexTextElementCreatorProvider hexTextElementCreatorProvider, PEStructureProviderFactory peStructureProviderFactory, HexBufferFileService hexBufferFileService) {
			if (hexTextElementCreatorProvider == null)
				throw new ArgumentNullException(nameof(hexTextElementCreatorProvider));
			if (peStructureProviderFactory == null)
				throw new ArgumentNullException(nameof(peStructureProviderFactory));
			if (hexBufferFileService == null)
				throw new ArgumentNullException(nameof(hexBufferFileService));
			this.hexTextElementCreatorProvider = hexTextElementCreatorProvider;
			this.peStructureProviderFactory = peStructureProviderFactory;
			this.hexBufferFileService = hexBufferFileService;
			peStructures = new Dictionary<HexBufferFile, PEStructure>();
			hexBufferFileService.BufferFilesRemoved += HexBufferFileService_BufferFilesRemoved;
			foreach (var file in hexBufferFileService.Files)
				AddFile(file);
		}

		void HexBufferFileService_BufferFilesRemoved(object sender, BufferFilesRemovedEventArgs e) {
			foreach (var file in e.Files) {
				bool b = peStructures.Remove(file);
				Debug.Assert(b);
			}
		}

		void AddFile(HexBufferFile file) {
			if (file.IsRemoved)
				return;
			var provider = peStructureProviderFactory.TryGetProvider(file);
			if (provider == null)
				return;
			peStructures.Add(file, new PEStructure(provider));
		}

		PEStructure GetPEStructure(HexPosition position) {
			var file = hexBufferFileService.GetFile(position, checkNestedFiles: false);
			if (file == null)
				return null;
			PEStructure peStructure;
			bool b = peStructures.TryGetValue(file, out peStructure);
			return peStructure;
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

		FieldAndStructure? GetField(HexPosition position) => GetPEStructure(position)?.GetField(position);

		public override IEnumerable<HexStructureField> GetFields(HexPosition position) {
			var peStructure = GetPEStructure(position);
			if (peStructure == null)
				yield break;

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
			var peStructure = GetPEStructure(position);
			if (peStructure == null)
				return null;

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
			var peStructure = GetPEStructure(position);
			if (peStructure == null)
				return null;

			var info = GetField(position);
			if (info != null) {
				var creator = new ToolTipCreator(hexTextElementCreatorProvider);
				bool create = CreateFieldToolTip(peStructure, creator, info.Value);
				return create ? creator.Create() : null;
			}

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null) {
				var creator = new ToolTipCreator(hexTextElementCreatorProvider);
				bool create = CreateMethodBodyToolTip(peStructure, creator, mfInfo.Value.MethodBodyInfo, mfInfo.Value.FieldInfo, position, mfInfo.Value.MethodBodyInfo.IsSmallExceptionClauses);
				return create ? creator.Create() : null;
			}

			return null;
		}

		bool CreateFieldToolTip(PEStructure peStructure, ToolTipCreator creator, FieldAndStructure info) {
			var output = new DataFormatter(creator.Writer);
			var mdRec = info.Structure as MetaDataTableRecordVM;
			if (mdRec != null) {
				creator.Image = GetImageReference(mdRec.Token.Table);
				output.Write(BoxedTextColor.ValueType, mdRec.Token.Table.ToString());
				output.Write(BoxedTextColor.Punctuation, "[");
				output.Write(BoxedTextColor.Number, mdRec.Token.Rid.ToString("X6"));
				output.Write(BoxedTextColor.Punctuation, "]");
				output.Write(BoxedTextColor.Operator, ".");
				output.Write(BoxedTextColor.InstanceField, info.Field.Name);
			}
			else {
				creator.Image = DsImages.FieldPublic;
				output.WriteStructAndField(info.Structure.Name, info.Field.Name);
			}
			output.WriteEquals();
			WriteValue(peStructure, output, info.Field);
			return true;
		}

		void WriteValue(PEStructure peStructure, DataFormatter output, HexField field) {
			var stringField = field as StringHexField;
			if (stringField != null) {
				output.Write(BoxedTextColor.String, SimpleTypeConverter.ToString(stringField.FormattedValue, false));
				return;
			}

			var buffer = peStructure.Buffer;
			var fieldPosition = field.Span.Start;

			switch (field.Size.ToUInt64()) {
			case 1:
				output.WriteByte(buffer.ReadByte(fieldPosition));
				break;

			case 2:
				output.WriteUInt16(buffer.ReadUInt16(fieldPosition));
				break;

			case 4:
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case 8:
				output.WriteUInt64(buffer.ReadUInt64(fieldPosition));
				break;
			}
		}

		static ImageReference GetImageReference(Table table) {
			switch (table) {
			case Table.Module:					return DsImages.ModulePublic;
			case Table.TypeRef:					return DsImages.ClassPublic;
			case Table.TypeDef:					return DsImages.ClassPublic;
			case Table.FieldPtr:				return DsImages.FieldPublic;
			case Table.Field:					return DsImages.FieldPublic;
			case Table.MethodPtr:				return DsImages.MethodPublic;
			case Table.Method:					return DsImages.MethodPublic;
			case Table.ParamPtr:				return DsImages.Parameter;
			case Table.Param:					return DsImages.Parameter;
			case Table.InterfaceImpl:			return DsImages.InterfacePublic;
			case Table.MemberRef:				return DsImages.Property;
			case Table.Constant:				return DsImages.ConstantPublic;
			case Table.CustomAttribute:			break;
			case Table.FieldMarshal:			break;
			case Table.DeclSecurity:			break;
			case Table.ClassLayout:				break;
			case Table.FieldLayout:				break;
			case Table.StandAloneSig:			return DsImages.LocalVariable;
			case Table.EventMap:				return DsImages.EventPublic;
			case Table.EventPtr:				return DsImages.EventPublic;
			case Table.Event:					return DsImages.EventPublic;
			case Table.PropertyMap:				return DsImages.Property;
			case Table.PropertyPtr:				return DsImages.Property;
			case Table.Property:				return DsImages.Property;
			case Table.MethodSemantics:			break;
			case Table.MethodImpl:				break;
			case Table.ModuleRef:				return DsImages.ModulePublic;
			case Table.TypeSpec:				return DsImages.Template;
			case Table.ImplMap:					break;
			case Table.FieldRVA:				break;
			case Table.ENCLog:					break;
			case Table.ENCMap:					break;
			case Table.Assembly:				return DsImages.Assembly;
			case Table.AssemblyProcessor:		return DsImages.Assembly;
			case Table.AssemblyOS:				return DsImages.Assembly;
			case Table.AssemblyRef:				return DsImages.Assembly;
			case Table.AssemblyRefProcessor:	return DsImages.Assembly;
			case Table.AssemblyRefOS:			return DsImages.Assembly;
			case Table.File:					return DsImages.ModuleFile;
			case Table.ExportedType:			return DsImages.ClassPublic;
			case Table.ManifestResource:		return DsImages.SourceFileGroup;
			case Table.NestedClass:				return DsImages.ClassPublic;
			case Table.GenericParam:			break;
			case Table.MethodSpec:				return DsImages.MethodPublic;
			case Table.GenericParamConstraint:	break;
			case Table.Document:				break;
			case Table.MethodDebugInformation:	break;
			case Table.LocalScope:				break;
			case Table.LocalVariable:			break;
			case Table.LocalConstant:			break;
			case Table.ImportScope:				break;
			case Table.StateMachineMethod:		break;
			case Table.CustomDebugInformation:	break;
			default:
				Debug.Fail($"Unknown table: {table}");
				break;
			}
			return DsImages.Metadata;
		}

		static readonly FlagInfo[] fatHeaderFlagsFlagInfos = new FlagInfo[] {
			FlagInfo.CreateEnumName(0x7, "Format"),
			new FlagInfo(0x7, 0x2, "TinyFormat"),
			new FlagInfo(0x7, 0x3, "FatFormat"),
			new FlagInfo(0x8, "MoreSects"),
			new FlagInfo(0x10, "InitLocals"),
		};

		static readonly FlagInfo[] exceptionHeaderFlagInfos = new FlagInfo[] {
			FlagInfo.CreateEnumName(0x3F, "Table"),
			new FlagInfo(0x3F, 0x1, "EHTable"),
			new FlagInfo(0x3F, 0x2, "OptILTable"),
			new FlagInfo(0x40, "FatFormat"),
			new FlagInfo(0x80, "MoreSects"),
		};

		static readonly EnumFieldInfo[] exceptionClauseEnumFieldInfos = new EnumFieldInfo[] {
			new EnumFieldInfo(0, "EXCEPTION"),
			new EnumFieldInfo(1, "FILTER"),
			new EnumFieldInfo(2, "FINALLY"),
			new EnumFieldInfo(4, "FAULT"),
		};

		bool CreateMethodBodyToolTip(PEStructure peStructure, ToolTipCreator creator, MethodBodyInfo minfo, MethodBodyFieldInfo finfo, HexPosition position, bool isSmallExceptionClauses) {
			const string tinyMethodHeader = "TinyMethodHeader";
			const string fatMethodHeader = "FatMethodHeader";
			const string methodBody = "MethodBody";
			const string exceptionHeader = "ExceptionHeader";
			const string exceptionClauses = "ExceptionClauses";

			creator.Image = DsImages.MethodPublic;
			var output = new DataFormatter(creator.Writer);
			var buffer = peStructure.Buffer;
			var fieldPosition = finfo.Span.Start;

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
			output = new DataFormatter(creator.CreateNewWriter());

			switch (finfo.FieldKind) {
			case MethodBodyFieldKind.None:
				return false;

			case MethodBodyFieldKind.Unknown:
			case MethodBodyFieldKind.InvalidBody:
				output.Write(BoxedTextColor.ValueType, methodBody);
				break;

			case MethodBodyFieldKind.LargeHeaderFlags:
				output.WriteStructAndField(fatMethodHeader, "Flags");
				output.WriteEquals();
				output.WriteFlags((uint)buffer.ReadUInt16(fieldPosition) & 0x0FFF, fatHeaderFlagsFlagInfos);
				break;

			case MethodBodyFieldKind.LargeHeaderMaxStack:
				output.WriteStructAndField(fatMethodHeader, "MaxStack");
				output.WriteEquals();
				output.WriteUInt16(buffer.ReadUInt16(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallHeaderCodeSize:
				output.WriteStructAndField(tinyMethodHeader, "CodeSize");
				output.WriteEquals();
				output.WriteByte((byte)(buffer.ReadByte(fieldPosition) >> 2));
				break;

			case MethodBodyFieldKind.LargeHeaderCodeSize:
				output.WriteStructAndField(fatMethodHeader, "CodeSize");
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeHeaderLocalVarSigTok:
				output.WriteStructAndField(fatMethodHeader, "LocalVarSigTok");
				output.WriteEquals();
				output.WriteToken(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeHeaderUnknown:
				output.WriteStructAndErrorField(fatMethodHeader);
				break;

			case MethodBodyFieldKind.InstructionBytes:
				//TODO: Disassemble the instruction
				output.WriteStructAndField(methodBody, "Instructions");
				output.Write(BoxedTextColor.Punctuation, ":");
				output.WriteSpace();
				output.Write(BoxedTextColor.Text, "offset");
				output.WriteSpace();
				output.Write(BoxedTextColor.Number, (position - minfo.InstructionsSpan.Start).ToString());
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderKind:
			case MethodBodyFieldKind.LargeExceptionHeaderKind:
				output.WriteStructAndField(exceptionHeader, "Kind");
				output.WriteEquals();
				output.WriteFlags(buffer.ReadByte(fieldPosition), exceptionHeaderFlagInfos);
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderDataSize:
				output.WriteStructAndField(exceptionHeader, "DataSize");
				output.WriteEquals();
				output.WriteByte(buffer.ReadByte(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeExceptionHeaderDataSize:
				output.WriteStructAndField(exceptionHeader, "DataSize");
				output.WriteEquals();
				output.WriteUInt24(buffer.ReadUInt32(fieldPosition - 1) >> 8);
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderPadding:
				output.WriteStructAndField(exceptionHeader, "Padding");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseFlags:
				WriteExceptionClause(output, minfo, position, "Flags", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteEnum(buffer.ReadUInt16(fieldPosition), exceptionClauseEnumFieldInfos);
				break;

			case MethodBodyFieldKind.LargeExceptionClauseFlags:
				WriteExceptionClause(output, minfo, position, "Flags", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteEnum(buffer.ReadUInt32(fieldPosition), exceptionClauseEnumFieldInfos);
				break;

			case MethodBodyFieldKind.SmallExceptionClauseTryOffset:
				WriteExceptionClause(output, minfo, position, "TryOffset", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt16(buffer.ReadUInt16(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeExceptionClauseTryOffset:
				WriteExceptionClause(output, minfo, position, "TryOffset", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallExceptionClauseTryLength:
				WriteExceptionClause(output, minfo, position, "TryLength", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteByte(buffer.ReadByte(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeExceptionClauseTryLength:
				WriteExceptionClause(output, minfo, position, "TryLength", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallExceptionClauseHandlerOffset:
				WriteExceptionClause(output, minfo, position, "HandlerOffset", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt16(buffer.ReadUInt16(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeExceptionClauseHandlerOffset:
				WriteExceptionClause(output, minfo, position, "HandlerOffset", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallExceptionClauseHandlerLength:
				WriteExceptionClause(output, minfo, position, "HandlerLength", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteByte(buffer.ReadByte(fieldPosition));
				break;

			case MethodBodyFieldKind.LargeExceptionClauseHandlerLength:
				WriteExceptionClause(output, minfo, position, "HandlerLength", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallExceptionClauseClassToken:
			case MethodBodyFieldKind.LargeExceptionClauseClassToken:
				WriteExceptionClause(output, minfo, position, "ClassToken", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteToken(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallExceptionClauseFilterOffset:
			case MethodBodyFieldKind.LargeExceptionClauseFilterOffset:
				WriteExceptionClause(output, minfo, position, "FilterOffset", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.SmallExceptionClauseReserved:
			case MethodBodyFieldKind.LargeExceptionClauseReserved:
				WriteExceptionClause(output, minfo, position, "Reserved", isSmallExceptionClauses);
				output.WriteEquals();
				output.WriteUInt32(buffer.ReadUInt32(fieldPosition));
				break;

			case MethodBodyFieldKind.ExceptionClausesUnknown:
				output.WriteStructAndErrorField(exceptionClauses);
				break;

			default:
				throw new InvalidOperationException();
			}

			return true;
		}

		void WriteExceptionClause(DataFormatter output, MethodBodyInfo minfo, HexPosition position, string fieldName, bool isSmallExceptionClauses) {
			const string exceptionClauses = "ExceptionClauses";
			int ehIndex = (int)((position - minfo.ExceptionsSpan.Start - 4).ToUInt64() / (isSmallExceptionClauses ? 12U : 24));
			output.Write(BoxedTextColor.ValueType, exceptionClauses);
			output.Write(BoxedTextColor.Punctuation, "[");
			output.Write(BoxedTextColor.Number, ehIndex.ToString());
			output.Write(BoxedTextColor.Punctuation, "]");
			output.Write(BoxedTextColor.Operator, ".");
			output.Write(BoxedTextColor.InstanceField, fieldName);
		}
	}
}
