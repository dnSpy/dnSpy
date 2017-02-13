/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.DotNet {
	class TableRecordDataFactory {
		protected HexBuffer Buffer => TablesHeap.Span.Buffer;
		protected TablesHeap TablesHeap { get; }
		protected MDTable MDTable { get; }

		public TableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable) {
			TablesHeap = tablesHeap ?? throw new ArgumentNullException(nameof(tablesHeap));
			MDTable = mdTable ?? throw new ArgumentNullException(nameof(mdTable));
		}

		public TableRecordData Create(uint rid) {
			if (!MDTable.IsValidRID(rid))
				return null;
			var position = MDTable.Span.Start + (rid - 1) * MDTable.RowSize;
			var columns = MDTable.TableInfo.Columns;
			var fields = new BufferField[columns.Count];
			for (int i = 0; i < fields.Length; i++) {
				var column = columns[i];
				var fieldPos = position + column.Offset;
				var field = new StructField(column.Name, CreateData(fieldPos, column));
				Debug.Assert(field.Data.Span.Length == column.Size);
				fields[i] = field;
			}
			var span = new HexBufferSpan(Buffer, new HexSpan(position, MDTable.RowSize));
			return new TableRecordData(MDTable.TableInfo.Name, new MDToken(MDTable.Table, rid), span, fields, TablesHeap);
		}

		protected virtual BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.ColumnSize < (ColumnSize)0x40)
				return CreateRidData(position, column);

			switch (column.ColumnSize) {
			case ColumnSize.Byte:
				if (column.Size != 1)
					throw new InvalidOperationException();
				return new ByteData(Buffer, position);

			case ColumnSize.Int16:
				if (column.Size != 2)
					throw new InvalidOperationException();
				return new Int16Data(Buffer, position);

			case ColumnSize.UInt16:
				if (column.Size != 2)
					throw new InvalidOperationException();
				return new UInt16Data(Buffer, position);

			case ColumnSize.Int32:
				if (column.Size != 4)
					throw new InvalidOperationException();
				return new Int32Data(Buffer, position);

			case ColumnSize.UInt32:
				if (column.Size != 4)
					throw new InvalidOperationException();
				return new UInt32Data(Buffer, position);

			case ColumnSize.Strings:
				if (column.Size == 2)
					return new StringsHeapData16(Buffer, position);
				return new StringsHeapData32(Buffer, position);

			case ColumnSize.GUID:
				if (column.Size == 2)
					return new GUIDHeapData16(Buffer, position);
				return new GUIDHeapData32(Buffer, position);

			case ColumnSize.Blob:
				if (column.Size == 2)
					return new BlobHeapData16(Buffer, position);
				return new BlobHeapData32(Buffer, position);

			case ColumnSize.TypeDefOrRef:				return CreateCodedTokenData(position, column, CodedToken.TypeDefOrRef);
			case ColumnSize.HasConstant:				return CreateCodedTokenData(position, column, CodedToken.HasConstant);
			case ColumnSize.HasCustomAttribute:			return CreateCodedTokenData(position, column, CodedToken.HasCustomAttribute);
			case ColumnSize.HasFieldMarshal:			return CreateCodedTokenData(position, column, CodedToken.HasFieldMarshal);
			case ColumnSize.HasDeclSecurity:			return CreateCodedTokenData(position, column, CodedToken.HasDeclSecurity);
			case ColumnSize.MemberRefParent:			return CreateCodedTokenData(position, column, CodedToken.MemberRefParent);
			case ColumnSize.HasSemantic:				return CreateCodedTokenData(position, column, CodedToken.HasSemantic);
			case ColumnSize.MethodDefOrRef:				return CreateCodedTokenData(position, column, CodedToken.MethodDefOrRef);
			case ColumnSize.MemberForwarded:			return CreateCodedTokenData(position, column, CodedToken.MemberForwarded);
			case ColumnSize.Implementation:				return CreateCodedTokenData(position, column, CodedToken.Implementation);
			case ColumnSize.CustomAttributeType:		return CreateCodedTokenData(position, column, CodedToken.CustomAttributeType);
			case ColumnSize.ResolutionScope:			return CreateCodedTokenData(position, column, CodedToken.ResolutionScope);
			case ColumnSize.TypeOrMethodDef:			return CreateCodedTokenData(position, column, CodedToken.TypeOrMethodDef);
			case ColumnSize.HasCustomDebugInformation:	return CreateCodedTokenData(position, column, CodedToken.HasCustomDebugInformation);

			default:
				switch (column.Size) {
				case 1:		return new ByteData(Buffer, position);
				case 2:		return new UInt16Data(Buffer, position);
				case 4:		return new UInt32Data(Buffer, position);
				default:	throw new InvalidOperationException();
				}
			}
		}

		RidData CreateRidData(HexPosition position, ColumnInfo column) =>
			column.Size == 2 ? (RidData)new Rid16Data(Buffer, position, (Table)column.ColumnSize) : new Rid32Data(Buffer, position, (Table)column.ColumnSize);

		CodedTokenData CreateCodedTokenData(HexPosition position, ColumnInfo column, CodedToken codedToken) =>
			column.Size == 2 ? (CodedTokenData)new CodedToken16Data(Buffer, position, codedToken) : new CodedToken32Data(Buffer, position, codedToken);
	}

	sealed class TypeDefTableRecordDataFactory : TableRecordDataFactory {
		public TypeDefTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		internal static readonly ReadOnlyCollection<FlagInfo> typeAttrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x00000007, "Visibility"),
			new FlagInfo(0x00000007, 0x00000000, "NotPublic"),
			new FlagInfo(0x00000007, 0x00000001, "Public"),
			new FlagInfo(0x00000007, 0x00000002, "NestedPublic"),
			new FlagInfo(0x00000007, 0x00000003, "NestedPrivate"),
			new FlagInfo(0x00000007, 0x00000004, "NestedFamily"),
			new FlagInfo(0x00000007, 0x00000005, "NestedAssembly"),
			new FlagInfo(0x00000007, 0x00000006, "NestedFamANDAssem"),
			new FlagInfo(0x00000007, 0x00000007, "NestedFamORAssem"),

			FlagInfo.CreateEnumName(0x00000018, "Layout"),
			new FlagInfo(0x00000018, 0x00000000, "AutoLayout"),
			new FlagInfo(0x00000018, 0x00000008, "SequentialLayout"),
			new FlagInfo(0x00000018, 0x00000010, "ExplicitLayout"),

			FlagInfo.CreateEnumName(0x00000020, "ClassSemantics"),
			new FlagInfo(0x00000020, 0x00000000, "Class"),
			new FlagInfo(0x00000020, 0x00000020, "Interface"),

			new FlagInfo(0x00000080, "Abstract"),
			new FlagInfo(0x00000100, "Sealed"),
			new FlagInfo(0x00000400, "SpecialName"),
			new FlagInfo(0x00000800, "RTSpecialName"),
			new FlagInfo(0x00001000, "Import"),
			new FlagInfo(0x00002000, "Serializable"),
			new FlagInfo(0x00004000, "WindowsRuntime"),
			new FlagInfo(0x00040000, "HasSecurity"),
			new FlagInfo(0x00100000, "BeforeFieldInit"),
			new FlagInfo(0x00200000, "Forwarder"),

			FlagInfo.CreateEnumName(0x00030000, "StringFormat"),
			new FlagInfo(0x00030000, 0x00000000, "AnsiClass"),
			new FlagInfo(0x00030000, 0x00010000, "UnicodeClass"),
			new FlagInfo(0x00030000, 0x00020000, "AutoClass"),
			new FlagInfo(0x00030000, 0x00030000, "CustomFormatClass"),

			FlagInfo.CreateEnumName(0x00C00000, "CustomFormatClass"),
			new FlagInfo(0x00C00000, 0x00000000, "CustomFormat0"),
			new FlagInfo(0x00C00000, 0x00400000, "CustomFormat1"),
			new FlagInfo(0x00C00000, 0x00800000, "CustomFormat2"),
			new FlagInfo(0x00C00000, 0x00C00000, "CustomFormat3"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt32FlagsData(Buffer, position, typeAttrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class FieldTableRecordDataFactory : TableRecordDataFactory {
		public FieldTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> fieldAttrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x0007, "FieldAccess"),
			new FlagInfo(0x0007, 0x0000, "PrivateScope"),
			new FlagInfo(0x0007, 0x0001, "Private"),
			new FlagInfo(0x0007, 0x0002, "FamANDAssem"),
			new FlagInfo(0x0007, 0x0003, "Assembly"),
			new FlagInfo(0x0007, 0x0004, "Family"),
			new FlagInfo(0x0007, 0x0005, "FamORAssem"),
			new FlagInfo(0x0007, 0x0006, "Public"),

			new FlagInfo(0x0010, "Static"),
			new FlagInfo(0x0020, "InitOnly"),
			new FlagInfo(0x0040, "Literal"),
			new FlagInfo(0x0080, "NotSerialized"),
			new FlagInfo(0x0100, "HasFieldRVA"),
			new FlagInfo(0x0200, "SpecialName"),
			new FlagInfo(0x0400, "RTSpecialName"),
			new FlagInfo(0x1000, "HasFieldMarshal"),
			new FlagInfo(0x2000, "PinvokeImpl"),
			new FlagInfo(0x8000, "HasDefault"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, fieldAttrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class MethodTableRecordDataFactory : TableRecordDataFactory {
		public MethodTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> implAttrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x0003, "CodeType"),
			new FlagInfo(0x0003, 0x0000, "IL"),
			new FlagInfo(0x0003, 0x0001, "Native"),
			new FlagInfo(0x0003, 0x0002, "OPTIL"),
			new FlagInfo(0x0003, 0x0003, "Runtime"),

			FlagInfo.CreateEnumName(0x0004, "Managed"),
			new FlagInfo(0x0004, 0x0000, "Managed"),
			new FlagInfo(0x0004, 0x0004, "Unmanaged"),

			new FlagInfo(0x0008, "NoInlining"),
			new FlagInfo(0x0010, "ForwardRef"),
			new FlagInfo(0x0020, "Synchronized"),
			new FlagInfo(0x0040, "NoOptimization"),
			new FlagInfo(0x0080, "PreserveSig"),
			new FlagInfo(0x0100, "AggressiveInlining"),
			new FlagInfo(0x1000, "InternalCall"),
		});

		static readonly ReadOnlyCollection<FlagInfo> attrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x0007, "MemberAccess"),
			new FlagInfo(0x0007, 0x0000, "PrivateScope"),
			new FlagInfo(0x0007, 0x0001, "Private"),
			new FlagInfo(0x0007, 0x0002, "FamANDAssem"),
			new FlagInfo(0x0007, 0x0003, "Assem"),
			new FlagInfo(0x0007, 0x0004, "Family"),
			new FlagInfo(0x0007, 0x0005, "FamORAssem"),
			new FlagInfo(0x0007, 0x0006, "Public"),

			new FlagInfo(0x0008, "UnmanagedExport"),
			new FlagInfo(0x0010, "Static"),
			new FlagInfo(0x0020, "Final"),
			new FlagInfo(0x0040, "Virtual"),
			new FlagInfo(0x0080, "HideBySig"),

			FlagInfo.CreateEnumName(0x0100, "VtableLayout"),
			new FlagInfo(0x0100, 0x0000, "ReuseSlot"),
			new FlagInfo(0x0100, 0x0100, "NewSlot"),

			new FlagInfo(0x0200, "CheckAccessOnOverride"),
			new FlagInfo(0x0400, "Abstract"),
			new FlagInfo(0x0800, "SpecialName"),
			new FlagInfo(0x1000, "RTSpecialName"),
			new FlagInfo(0x2000, "PinvokeImpl"),
			new FlagInfo(0x4000, "HasSecurity"),
			new FlagInfo(0x8000, "RequireSecObject"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new RvaData(Buffer, position);
			if (column.Index == 1)
				return new UInt16FlagsData(Buffer, position, implAttrFlagInfos);
			if (column.Index == 2)
				return new UInt16FlagsData(Buffer, position, attrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class ParamTableRecordDataFactory : TableRecordDataFactory {
		public ParamTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> paramAttrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "In"),
			new FlagInfo(0x0002, "Out"),
			new FlagInfo(0x0010, "Optional"),
			new FlagInfo(0x1000, "HasDefault"),
			new FlagInfo(0x2000, "HasFieldMarshal"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, paramAttrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class ConstantTableRecordDataFactory : TableRecordDataFactory {
		public ConstantTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<EnumFieldInfo> typeEnumInfos = new ReadOnlyCollection<EnumFieldInfo>(new EnumFieldInfo[] {
			new EnumFieldInfo(0x00, "END"),
			new EnumFieldInfo(0x01, "VOID"),
			new EnumFieldInfo(0x02, "BOOLEAN"),
			new EnumFieldInfo(0x03, "CHAR"),
			new EnumFieldInfo(0x04, "I1"),
			new EnumFieldInfo(0x05, "U1"),
			new EnumFieldInfo(0x06, "I2"),
			new EnumFieldInfo(0x07, "U2"),
			new EnumFieldInfo(0x08, "I4"),
			new EnumFieldInfo(0x09, "U4"),
			new EnumFieldInfo(0x0A, "I8"),
			new EnumFieldInfo(0x0B, "U8"),
			new EnumFieldInfo(0x0C, "R4"),
			new EnumFieldInfo(0x0D, "R8"),
			new EnumFieldInfo(0x0E, "STRING"),
			new EnumFieldInfo(0x0F, "PTR"),
			new EnumFieldInfo(0x10, "BYREF"),
			new EnumFieldInfo(0x11, "VALUETYPE"),
			new EnumFieldInfo(0x12, "CLASS"),
			new EnumFieldInfo(0x13, "VAR"),
			new EnumFieldInfo(0x14, "ARRAY"),
			new EnumFieldInfo(0x15, "GENERICINST"),
			new EnumFieldInfo(0x16, "TYPEDBYREF"),
			new EnumFieldInfo(0x17, "VALUEARRAY"),
			new EnumFieldInfo(0x18, "I"),
			new EnumFieldInfo(0x19, "U"),
			new EnumFieldInfo(0x1A, "R"),
			new EnumFieldInfo(0x1B, "FNPTR"),
			new EnumFieldInfo(0x1C, "OBJECT"),
			new EnumFieldInfo(0x1D, "SZARRAY"),
			new EnumFieldInfo(0x1E, "MVAR"),
			new EnumFieldInfo(0x1F, "CMOD_REQD"),
			new EnumFieldInfo(0x20, "CMOD_OPT"),
			new EnumFieldInfo(0x21, "INTERNAL"),
			new EnumFieldInfo(0x3F, "MODULE"),
			new EnumFieldInfo(0x40, "MODIFIER"),
			new EnumFieldInfo(0x41, "SENTINEL"),
			new EnumFieldInfo(0x45, "PINNED"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new ByteEnumData(Buffer, position, typeEnumInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class DeclSecurityTableRecordDataFactory : TableRecordDataFactory {
		public DeclSecurityTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> actionFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x001F, "Action"),
			new FlagInfo(0x001F, 0x0000, "ActionNil"),
			new FlagInfo(0x001F, 0x0001, "Request"),
			new FlagInfo(0x001F, 0x0002, "Demand"),
			new FlagInfo(0x001F, 0x0003, "Assert"),
			new FlagInfo(0x001F, 0x0004, "Deny"),
			new FlagInfo(0x001F, 0x0005, "PermitOnly"),
			new FlagInfo(0x001F, 0x0006, "LinktimeCheck"),
			new FlagInfo(0x001F, 0x0007, "InheritanceCheck"),
			new FlagInfo(0x001F, 0x0008, "RequestMinimum"),
			new FlagInfo(0x001F, 0x0009, "RequestOptional"),
			new FlagInfo(0x001F, 0x000A, "RequestRefuse"),
			new FlagInfo(0x001F, 0x000B, "PrejitGrant"),
			new FlagInfo(0x001F, 0x000C, "PrejitDenied"),
			new FlagInfo(0x001F, 0x000D, "NonCasDemand"),
			new FlagInfo(0x001F, 0x000E, "NonCasLinkDemand"),
			new FlagInfo(0x001F, 0x000F, "NonCasInheritance"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, actionFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class EventTableRecordDataFactory : TableRecordDataFactory {
		public EventTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> eventFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0200, "SpecialName"),
			new FlagInfo(0x0400, "RTSpecialName"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, eventFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class PropertyTableRecordDataFactory : TableRecordDataFactory {
		public PropertyTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> propertyFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0200, "SpecialName"),
			new FlagInfo(0x0400, "RTSpecialName"),
			new FlagInfo(0x1000, "HasDefault"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, propertyFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class MethodSemanticsTableRecordDataFactory : TableRecordDataFactory {
		public MethodSemanticsTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> methodSemanticsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "Setter"),
			new FlagInfo(0x0002, "Getter"),
			new FlagInfo(0x0004, "Other"),
			new FlagInfo(0x0008, "AddOn"),
			new FlagInfo(0x0010, "RemoveOn"),
			new FlagInfo(0x0020, "Fire"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, methodSemanticsFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class ImplMapTableRecordDataFactory : TableRecordDataFactory {
		public ImplMapTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> implMapFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "NoMangle"),

			FlagInfo.CreateEnumName(0x0006, "CharSet"),
			new FlagInfo(0x0006, 0x0000, "CharSetNotSpec"),
			new FlagInfo(0x0006, 0x0002, "Ansi"),
			new FlagInfo(0x0006, 0x0004, "Unicode"),
			new FlagInfo(0x0006, 0x0006, "CharSetAuto"),

			FlagInfo.CreateEnumName(0x0030, "BestFit"),
			new FlagInfo(0x0030, 0x0000, "BestFitUseAssem"),
			new FlagInfo(0x0030, 0x0010, "BestFitEnabled"),
			new FlagInfo(0x0030, 0x0020, "BestFitDisabled"),

			new FlagInfo(0x0040, "SupportsLastError"),

			FlagInfo.CreateEnumName(0x0700, "CallConv"),
			new FlagInfo(0x0700, 0x0100, "Winapi"),
			new FlagInfo(0x0700, 0x0200, "Cdecl"),
			new FlagInfo(0x0700, 0x0300, "Stdcall"),
			new FlagInfo(0x0700, 0x0400, "Thiscall"),
			new FlagInfo(0x0700, 0x0500, "Fastcall"),

			FlagInfo.CreateEnumName(0x3000, "ThrowOnUnmappableChar"),
			new FlagInfo(0x3000, 0x0000, "ThrowOnUnmappableCharUseAssem"),
			new FlagInfo(0x3000, 0x1000, "ThrowOnUnmappableCharEnabled"),
			new FlagInfo(0x3000, 0x2000, "ThrowOnUnmappableCharDisabled"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, implMapFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class FieldRVATableRecordDataFactory : TableRecordDataFactory {
		public FieldRVATableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new RvaData(Buffer, position);
			return base.CreateData(position, column);
		}
	}

	sealed class ENCLogTableRecordDataFactory : TableRecordDataFactory {
		public ENCLogTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new TokenData(Buffer, position);
			return base.CreateData(position, column);
		}
	}

	sealed class ENCMapTableRecordDataFactory : TableRecordDataFactory {
		public ENCMapTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new TokenData(Buffer, position);
			return base.CreateData(position, column);
		}
	}

	sealed class AssemblyTableRecordDataFactory : TableRecordDataFactory {
		public AssemblyTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<EnumFieldInfo> hashAlgEnumFields = new ReadOnlyCollection<EnumFieldInfo>(new EnumFieldInfo[] {
			new EnumFieldInfo(0x0000, "None"),
			new EnumFieldInfo(0x8001, "MD2"),
			new EnumFieldInfo(0x8002, "MD4"),
			new EnumFieldInfo(0x8003, "MD5"),
			new EnumFieldInfo(0x8004, "SHA1"),
			new EnumFieldInfo(0x8005, "MAC"),
			new EnumFieldInfo(0x8008, "SSL3_SHAMD5"),
			new EnumFieldInfo(0x8009, "HMAC"),
			new EnumFieldInfo(0x800A, "TLS1PRF"),
			new EnumFieldInfo(0x800B, "HASH_REPLACE_OWF"),
			new EnumFieldInfo(0x800C, "SHA_256"),
			new EnumFieldInfo(0x800D, "SHA_384"),
			new EnumFieldInfo(0x800E, "SHA_512"),
		});

		internal static readonly ReadOnlyCollection<FlagInfo> attrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "PublicKey"),

			FlagInfo.CreateEnumName(0x0070, "ProcessorArch"),
			new FlagInfo(0x0070, 0x0000, "PA_None"),
			new FlagInfo(0x0070, 0x0010, "MSIL"),
			new FlagInfo(0x0070, 0x0020, "x86"),
			new FlagInfo(0x0070, 0x0030, "IA64"),
			new FlagInfo(0x0070, 0x0040, "AMD64"),
			new FlagInfo(0x0070, 0x0050, "ARM"),
			new FlagInfo(0x0070, 0x0070, "NoPlatform"),

			new FlagInfo(0x0080, "PA_Specified"),
			new FlagInfo(0x0100, "Retargetable"),
			new FlagInfo(0x4000, "DisableJITcompileOptimizer"),
			new FlagInfo(0x8000, "EnableJITcompileTracking"),

			FlagInfo.CreateEnumName(0x0E00, "ContentType"),
			new FlagInfo(0x0E00, 0x0000, "Default"),
			new FlagInfo(0x0E00, 0x0200, "WindowsRuntime"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt32EnumData(Buffer, position, hashAlgEnumFields);
			if (column.Index == 5)
				return new UInt32FlagsData(Buffer, position, attrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class AssemblyRefTableRecordDataFactory : TableRecordDataFactory {
		public AssemblyRefTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 4)
				return new UInt32FlagsData(Buffer, position, AssemblyTableRecordDataFactory.attrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class FileTableRecordDataFactory : TableRecordDataFactory {
		public FileTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> attrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x0001, "ContainsNoMetaData"),
			new FlagInfo(0x0001, 0x0000, "ContainsMetaData"),
			new FlagInfo(0x0001, 0x0001, "ContainsNoMetaData"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt32FlagsData(Buffer, position, attrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class ExportedTypeTableRecordDataFactory : TableRecordDataFactory {
		public ExportedTypeTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt32FlagsData(Buffer, position, TypeDefTableRecordDataFactory.typeAttrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class ManifestResourceTableRecordDataFactory : TableRecordDataFactory {
		public ManifestResourceTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> attrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x0007, "Visibility"),
			new FlagInfo(0x0007, 0x0001, "Public"),
			new FlagInfo(0x0007, 0x0002, "Private"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 1)
				return new UInt32FlagsData(Buffer, position, attrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class GenericParamTableRecordDataFactory : TableRecordDataFactory {
		public GenericParamTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> attrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			FlagInfo.CreateEnumName(0x0003, "Variance"),
			new FlagInfo(0x0003, 0x0000, "NonVariant"),
			new FlagInfo(0x0003, 0x0001, "Covariant"),
			new FlagInfo(0x0003, 0x0002, "Contravariant"),

			new FlagInfo(0x0004, "ReferenceTypeConstraint"),
			new FlagInfo(0x0008, "NotNullableValueTypeConstraint"),
			new FlagInfo(0x0010, "DefaultConstructorConstraint"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 1)
				return new UInt16FlagsData(Buffer, position, attrFlagInfos);
			return base.CreateData(position, column);
		}
	}

	sealed class LocalVariableTableRecordDataFactory : TableRecordDataFactory {
		public LocalVariableTableRecordDataFactory(TablesHeap tablesHeap, MDTable mdTable)
			: base(tablesHeap, mdTable) {
		}

		static readonly ReadOnlyCollection<FlagInfo> attrFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "DebuggerHidden"),
		});

		protected override BufferData CreateData(HexPosition position, ColumnInfo column) {
			if (column.Index == 0)
				return new UInt16FlagsData(Buffer, position, attrFlagInfos);
			return base.CreateData(position, column);
		}
	}
}
