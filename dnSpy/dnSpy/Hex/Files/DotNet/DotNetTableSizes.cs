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

// from dnlib

using System;
using System.Diagnostics;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	/// <summary>
	/// Initializes .NET table row sizes
	/// </summary>
	sealed class DotNetTableSizes {
		bool bigStrings;
		bool bigGuid;
		bool bigBlob;
		uint[]? rowCounts;
		TableInfo[]? tableInfos;

		/// <summary>
		/// Initializes the table sizes
		/// </summary>
		/// <param name="bigStrings"><c>true</c> if #Strings size >= 0x10000</param>
		/// <param name="bigGuid"><c>true</c> if #GUID size >= 0x10000</param>
		/// <param name="bigBlob"><c>true</c> if #Blob size >= 0x10000</param>
		/// <param name="rowCounts">Count of rows in each table</param>
		public void InitializeSizes(bool bigStrings, bool bigGuid, bool bigBlob, uint[] rowCounts) {
			Debug2.Assert(!(tableInfos is null));
			this.bigStrings = bigStrings;
			this.bigGuid = bigGuid;
			this.bigBlob = bigBlob;
			this.rowCounts = rowCounts;
			for (int i = 0; i < tableInfos.Length; i++) {
				var tableInfo = tableInfos[i];
				int offset = 0;
				var cols = tableInfo.Columns;
				var newCols = new ColumnInfo[cols.Count];
				for (int j = 0; j < cols.Count; j++) {
					var col = cols[j];
					var size = GetSize(col.ColumnSize);
					newCols[j] = new ColumnInfo(col.Index, col.Name, col.ColumnSize, offset, size);
					offset += size;
				}
				tableInfos[i] = new TableInfo(tableInfo.Table, tableInfo.Name, newCols, offset);
			}
		}

		int GetSize(ColumnSize columnSize) {
			Debug2.Assert(!(rowCounts is null));
			if (ColumnSize.Module <= columnSize && columnSize <= ColumnSize.CustomDebugInformation) {
				int table = (int)(columnSize - ColumnSize.Module);
				uint count = table >= rowCounts.Length ? 0 : rowCounts[table];
				return count > 0xFFFF ? 4 : 2;
			}
			else if (ColumnSize.TypeDefOrRef <= columnSize && columnSize <= ColumnSize.HasCustomDebugInformation) {
				CodedToken info;
				switch (columnSize) {
				case ColumnSize.TypeDefOrRef:		info = CodedToken.TypeDefOrRef; break;
				case ColumnSize.HasConstant:		info = CodedToken.HasConstant; break;
				case ColumnSize.HasCustomAttribute:	info = CodedToken.HasCustomAttribute; break;
				case ColumnSize.HasFieldMarshal:	info = CodedToken.HasFieldMarshal; break;
				case ColumnSize.HasDeclSecurity:	info = CodedToken.HasDeclSecurity; break;
				case ColumnSize.MemberRefParent:	info = CodedToken.MemberRefParent; break;
				case ColumnSize.HasSemantic:		info = CodedToken.HasSemantic; break;
				case ColumnSize.MethodDefOrRef:		info = CodedToken.MethodDefOrRef; break;
				case ColumnSize.MemberForwarded:	info = CodedToken.MemberForwarded; break;
				case ColumnSize.Implementation:		info = CodedToken.Implementation; break;
				case ColumnSize.CustomAttributeType:info = CodedToken.CustomAttributeType; break;
				case ColumnSize.ResolutionScope:	info = CodedToken.ResolutionScope; break;
				case ColumnSize.TypeOrMethodDef:	info = CodedToken.TypeOrMethodDef; break;
				case ColumnSize.HasCustomDebugInformation:info = CodedToken.HasCustomDebugInformation; break;
				default: throw new InvalidOperationException($"Invalid ColumnSize: {columnSize}");
				}
				uint maxRows = 0;
				foreach (var tableType in info.TableTypes) {
					int index = (int)tableType;
					var tableRows = index >= rowCounts.Length ? 0 : rowCounts[index];
					if (tableRows > maxRows)
						maxRows = tableRows;
				}
				// Can't overflow since maxRows <= 0x00FFFFFF and info.Bits < 8
				uint finalRows = maxRows << info.Bits;
				return finalRows > 0xFFFF ? 4 : 2;
			}
			else {
				switch (columnSize) {
				case ColumnSize.Byte:	return 1;
				case ColumnSize.Int16:	return 2;
				case ColumnSize.UInt16:	return 2;
				case ColumnSize.Int32:	return 4;
				case ColumnSize.UInt32:	return 4;
				case ColumnSize.Strings:return bigStrings ? 4 : 2;
				case ColumnSize.GUID:	return bigGuid ? 4 : 2;
				case ColumnSize.Blob:	return bigBlob ? 4 : 2;
				}
			}
			throw new InvalidOperationException($"Invalid ColumnSize: {columnSize}");
		}

		/// <summary>
		/// Creates the table infos
		/// </summary>
		/// <param name="majorVersion">Major table version</param>
		/// <param name="minorVersion">Minor table version</param>
		/// <returns>All table infos (not completely initialized)</returns>
		public TableInfo[] CreateTables(byte majorVersion, byte minorVersion) => CreateTables(majorVersion, minorVersion, out int maxPresentTables);

		/// <summary>
		/// Creates the table infos
		/// </summary>
		/// <param name="majorVersion">Major table version</param>
		/// <param name="minorVersion">Minor table version</param>
		/// <param name="maxPresentTables">Initialized to max present tables (eg. 42 or 45)</param>
		/// <returns>All table infos (not completely initialized)</returns>
		public TableInfo[] CreateTables(byte majorVersion, byte minorVersion, out int maxPresentTables) {
			// The three extra generics tables aren't used by CLR 1.x
			const int normalMaxTables = (int)Table.CustomDebugInformation + 1;
			maxPresentTables = (majorVersion == 1 && minorVersion == 0) ? (int)Table.NestedClass + 1 : normalMaxTables;

			var tableInfos = new TableInfo[normalMaxTables];

			tableInfos[(int)Table.Module] = new TableInfo(Table.Module, "Module", new ColumnInfo[] {
				new ColumnInfo(0, "Generation", ColumnSize.UInt16),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "Mvid", ColumnSize.GUID),
				new ColumnInfo(3, "EncId", ColumnSize.GUID),
				new ColumnInfo(4, "EncBaseId", ColumnSize.GUID),
			});
			tableInfos[(int)Table.TypeRef] = new TableInfo(Table.TypeRef, "TypeRef", new ColumnInfo[] {
				new ColumnInfo(0, "ResolutionScope", ColumnSize.ResolutionScope),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "Namespace", ColumnSize.Strings),
			});
			tableInfos[(int)Table.TypeDef] = new TableInfo(Table.TypeDef, "TypeDef", new ColumnInfo[] {
				new ColumnInfo(0, "Flags", ColumnSize.UInt32),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "Namespace", ColumnSize.Strings),
				new ColumnInfo(3, "Extends", ColumnSize.TypeDefOrRef),
				new ColumnInfo(4, "FieldList", ColumnSize.Field),
				new ColumnInfo(5, "MethodList", ColumnSize.Method),
			});
			tableInfos[(int)Table.FieldPtr] = new TableInfo(Table.FieldPtr, "FieldPtr", new ColumnInfo[] {
				new ColumnInfo(0, "Field", ColumnSize.Field),
			});
			tableInfos[(int)Table.Field] = new TableInfo(Table.Field, "Field", new ColumnInfo[] {
				new ColumnInfo(0, "Flags", ColumnSize.UInt16),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.MethodPtr] = new TableInfo(Table.MethodPtr, "MethodPtr", new ColumnInfo[] {
				new ColumnInfo(0, "Method", ColumnSize.Method),
			});
			tableInfos[(int)Table.Method] = new TableInfo(Table.Method, "Method", new ColumnInfo[] {
				new ColumnInfo(0, "RVA", ColumnSize.UInt32),
				new ColumnInfo(1, "ImplFlags", ColumnSize.UInt16),
				new ColumnInfo(2, "Flags", ColumnSize.UInt16),
				new ColumnInfo(3, "Name", ColumnSize.Strings),
				new ColumnInfo(4, "Signature", ColumnSize.Blob),
				new ColumnInfo(5, "ParamList", ColumnSize.Param),
			});
			tableInfos[(int)Table.ParamPtr] = new TableInfo(Table.ParamPtr, "ParamPtr", new ColumnInfo[] {
				new ColumnInfo(0, "Param", ColumnSize.Param),
			});
			tableInfos[(int)Table.Param] = new TableInfo(Table.Param, "Param", new ColumnInfo[] {
				new ColumnInfo(0, "Flags", ColumnSize.UInt16),
				new ColumnInfo(1, "Sequence", ColumnSize.UInt16),
				new ColumnInfo(2, "Name", ColumnSize.Strings),
			});
			tableInfos[(int)Table.InterfaceImpl] = new TableInfo(Table.InterfaceImpl, "InterfaceImpl", new ColumnInfo[] {
				new ColumnInfo(0, "Class", ColumnSize.TypeDef),
				new ColumnInfo(1, "Interface", ColumnSize.TypeDefOrRef),
			});
			tableInfos[(int)Table.MemberRef] = new TableInfo(Table.MemberRef, "MemberRef", new ColumnInfo[] {
				new ColumnInfo(0, "Class", ColumnSize.MemberRefParent),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.Constant] = new TableInfo(Table.Constant, "Constant", new ColumnInfo[] {
				new ColumnInfo(0, "Type", ColumnSize.Byte),
				new ColumnInfo(1, "Padding", ColumnSize.Byte),
				new ColumnInfo(2, "Parent", ColumnSize.HasConstant),
				new ColumnInfo(3, "Value", ColumnSize.Blob),
			});
			tableInfos[(int)Table.CustomAttribute] = new TableInfo(Table.CustomAttribute, "CustomAttribute", new ColumnInfo[] {
				new ColumnInfo(0, "Parent", ColumnSize.HasCustomAttribute),
				new ColumnInfo(1, "Type", ColumnSize.CustomAttributeType),
				new ColumnInfo(2, "Value", ColumnSize.Blob),
			});
			tableInfos[(int)Table.FieldMarshal] = new TableInfo(Table.FieldMarshal, "FieldMarshal", new ColumnInfo[] {
				new ColumnInfo(0, "Parent", ColumnSize.HasFieldMarshal),
				new ColumnInfo(1, "NativeType", ColumnSize.Blob),
			});
			tableInfos[(int)Table.DeclSecurity] = new TableInfo(Table.DeclSecurity, "DeclSecurity", new ColumnInfo[] {
				new ColumnInfo(0, "Action", ColumnSize.Int16),
				new ColumnInfo(1, "Parent", ColumnSize.HasDeclSecurity),
				new ColumnInfo(2, "PermissionSet", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ClassLayout] = new TableInfo(Table.ClassLayout, "ClassLayout", new ColumnInfo[] {
				new ColumnInfo(0, "PackingSize", ColumnSize.UInt16),
				new ColumnInfo(1, "ClassSize", ColumnSize.UInt32),
				new ColumnInfo(2, "Parent", ColumnSize.TypeDef),
			});
			tableInfos[(int)Table.FieldLayout] = new TableInfo(Table.FieldLayout, "FieldLayout", new ColumnInfo[] {
				new ColumnInfo(0, "OffSet", ColumnSize.UInt32),
				new ColumnInfo(1, "Field", ColumnSize.Field),
			});
			tableInfos[(int)Table.StandAloneSig] = new TableInfo(Table.StandAloneSig, "StandAloneSig", new ColumnInfo[] {
				new ColumnInfo(0, "Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.EventMap] = new TableInfo(Table.EventMap, "EventMap", new ColumnInfo[] {
				new ColumnInfo(0, "Parent", ColumnSize.TypeDef),
				new ColumnInfo(1, "EventList", ColumnSize.Event),
			});
			tableInfos[(int)Table.EventPtr] = new TableInfo(Table.EventPtr, "EventPtr", new ColumnInfo[] {
				new ColumnInfo(0, "Event", ColumnSize.Event),
			});
			tableInfos[(int)Table.Event] = new TableInfo(Table.Event, "Event", new ColumnInfo[] {
				new ColumnInfo(0, "EventFlags", ColumnSize.UInt16),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "EventType", ColumnSize.TypeDefOrRef),
			});
			tableInfos[(int)Table.PropertyMap] = new TableInfo(Table.PropertyMap, "PropertyMap", new ColumnInfo[] {
				new ColumnInfo(0, "Parent", ColumnSize.TypeDef),
				new ColumnInfo(1, "PropertyList", ColumnSize.Property),
			});
			tableInfos[(int)Table.PropertyPtr] = new TableInfo(Table.PropertyPtr, "PropertyPtr", new ColumnInfo[] {
				new ColumnInfo(0, "Property", ColumnSize.Property),
			});
			tableInfos[(int)Table.Property] = new TableInfo(Table.Property, "Property", new ColumnInfo[] {
				new ColumnInfo(0, "PropFlags", ColumnSize.UInt16),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "Type", ColumnSize.Blob),
			});
			tableInfos[(int)Table.MethodSemantics] = new TableInfo(Table.MethodSemantics, "MethodSemantics", new ColumnInfo[] {
				new ColumnInfo(0, "Semantic", ColumnSize.UInt16),
				new ColumnInfo(1, "Method", ColumnSize.Method),
				new ColumnInfo(2, "Association", ColumnSize.HasSemantic),
			});
			tableInfos[(int)Table.MethodImpl] = new TableInfo(Table.MethodImpl, "MethodImpl", new ColumnInfo[] {
				new ColumnInfo(0, "Class", ColumnSize.TypeDef),
				new ColumnInfo(1, "MethodBody", ColumnSize.MethodDefOrRef),
				new ColumnInfo(2, "MethodDeclaration", ColumnSize.MethodDefOrRef),
			});
			tableInfos[(int)Table.ModuleRef] = new TableInfo(Table.ModuleRef, "ModuleRef", new ColumnInfo[] {
				new ColumnInfo(0, "Name", ColumnSize.Strings),
			});
			tableInfos[(int)Table.TypeSpec] = new TableInfo(Table.TypeSpec, "TypeSpec", new ColumnInfo[] {
				new ColumnInfo(0, "Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ImplMap] = new TableInfo(Table.ImplMap, "ImplMap", new ColumnInfo[] {
				new ColumnInfo(0, "MappingFlags", ColumnSize.UInt16),
				new ColumnInfo(1, "MemberForwarded", ColumnSize.MemberForwarded),
				new ColumnInfo(2, "ImportName", ColumnSize.Strings),
				new ColumnInfo(3, "ImportScope", ColumnSize.ModuleRef),
			});
			tableInfos[(int)Table.FieldRVA] = new TableInfo(Table.FieldRVA, "FieldRVA", new ColumnInfo[] {
				new ColumnInfo(0, "RVA", ColumnSize.UInt32),
				new ColumnInfo(1, "Field", ColumnSize.Field),
			});
			tableInfos[(int)Table.ENCLog] = new TableInfo(Table.ENCLog, "ENCLog", new ColumnInfo[] {
				new ColumnInfo(0, "Token", ColumnSize.UInt32),
				new ColumnInfo(1, "FuncCode", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.ENCMap] = new TableInfo(Table.ENCMap, "ENCMap", new ColumnInfo[] {
				new ColumnInfo(0, "Token", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.Assembly] = new TableInfo(Table.Assembly, "Assembly", new ColumnInfo[] {
				new ColumnInfo(0, "HashAlgId", ColumnSize.UInt32),
				new ColumnInfo(1, "MajorVersion", ColumnSize.UInt16),
				new ColumnInfo(2, "MinorVersion", ColumnSize.UInt16),
				new ColumnInfo(3, "BuildNumber", ColumnSize.UInt16),
				new ColumnInfo(4, "RevisionNumber", ColumnSize.UInt16),
				new ColumnInfo(5, "Flags", ColumnSize.UInt32),
				new ColumnInfo(6, "PublicKey", ColumnSize.Blob),
				new ColumnInfo(7, "Name", ColumnSize.Strings),
				new ColumnInfo(8, "Locale", ColumnSize.Strings),
			});
			tableInfos[(int)Table.AssemblyProcessor] = new TableInfo(Table.AssemblyProcessor, "AssemblyProcessor", new ColumnInfo[] {
				new ColumnInfo(0, "Processor", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.AssemblyOS] = new TableInfo(Table.AssemblyOS, "AssemblyOS", new ColumnInfo[] {
				new ColumnInfo(0, "OSPlatformId", ColumnSize.UInt32),
				new ColumnInfo(1, "OSMajorVersion", ColumnSize.UInt32),
				new ColumnInfo(2, "OSMinorVersion", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.AssemblyRef] = new TableInfo(Table.AssemblyRef, "AssemblyRef", new ColumnInfo[] {
				new ColumnInfo(0, "MajorVersion", ColumnSize.UInt16),
				new ColumnInfo(1, "MinorVersion", ColumnSize.UInt16),
				new ColumnInfo(2, "BuildNumber", ColumnSize.UInt16),
				new ColumnInfo(3, "RevisionNumber", ColumnSize.UInt16),
				new ColumnInfo(4, "Flags", ColumnSize.UInt32),
				new ColumnInfo(5, "PublicKeyOrToken", ColumnSize.Blob),
				new ColumnInfo(6, "Name", ColumnSize.Strings),
				new ColumnInfo(7, "Locale", ColumnSize.Strings),
				new ColumnInfo(8, "HashValue", ColumnSize.Blob),
			});
			tableInfos[(int)Table.AssemblyRefProcessor] = new TableInfo(Table.AssemblyRefProcessor, "AssemblyRefProcessor", new ColumnInfo[] {
				new ColumnInfo(0, "Processor", ColumnSize.UInt32),
				new ColumnInfo(1, "AssemblyRef", ColumnSize.AssemblyRef),
			});
			tableInfos[(int)Table.AssemblyRefOS] = new TableInfo(Table.AssemblyRefOS, "AssemblyRefOS", new ColumnInfo[] {
				new ColumnInfo(0, "OSPlatformId", ColumnSize.UInt32),
				new ColumnInfo(1, "OSMajorVersion", ColumnSize.UInt32),
				new ColumnInfo(2, "OSMinorVersion", ColumnSize.UInt32),
				new ColumnInfo(3, "AssemblyRef", ColumnSize.AssemblyRef),
			});
			tableInfos[(int)Table.File] = new TableInfo(Table.File, "File", new ColumnInfo[] {
				new ColumnInfo(0, "Flags", ColumnSize.UInt32),
				new ColumnInfo(1, "Name", ColumnSize.Strings),
				new ColumnInfo(2, "HashValue", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ExportedType] = new TableInfo(Table.ExportedType, "ExportedType", new ColumnInfo[] {
				new ColumnInfo(0, "Flags", ColumnSize.UInt32),
				new ColumnInfo(1, "TypeDefId", ColumnSize.UInt32),
				new ColumnInfo(2, "TypeName", ColumnSize.Strings),
				new ColumnInfo(3, "TypeNamespace", ColumnSize.Strings),
				new ColumnInfo(4, "Implementation", ColumnSize.Implementation),
			});
			tableInfos[(int)Table.ManifestResource] = new TableInfo(Table.ManifestResource, "ManifestResource", new ColumnInfo[] {
				new ColumnInfo(0, "Offset", ColumnSize.UInt32),
				new ColumnInfo(1, "Flags", ColumnSize.UInt32),
				new ColumnInfo(2, "Name", ColumnSize.Strings),
				new ColumnInfo(3, "Implementation", ColumnSize.Implementation),
			});
			tableInfos[(int)Table.NestedClass] = new TableInfo(Table.NestedClass, "NestedClass", new ColumnInfo[] {
				new ColumnInfo(0, "NestedClass", ColumnSize.TypeDef),
				new ColumnInfo(1, "EnclosingClass", ColumnSize.TypeDef),
			});
			if (majorVersion == 1 && minorVersion == 1) {
				tableInfos[(int)Table.GenericParam] = new TableInfo(Table.GenericParam, "GenericParam", new ColumnInfo[] {
					new ColumnInfo(0, "Number", ColumnSize.UInt16),
					new ColumnInfo(1, "Flags", ColumnSize.UInt16),
					new ColumnInfo(2, "Owner", ColumnSize.TypeOrMethodDef),
					new ColumnInfo(3, "Name", ColumnSize.Strings),
					new ColumnInfo(4, "Kind", ColumnSize.TypeDefOrRef),
				});
			}
			else {
				tableInfos[(int)Table.GenericParam] = new TableInfo(Table.GenericParam, "GenericParam", new ColumnInfo[] {
					new ColumnInfo(0, "Number", ColumnSize.UInt16),
					new ColumnInfo(1, "Flags", ColumnSize.UInt16),
					new ColumnInfo(2, "Owner", ColumnSize.TypeOrMethodDef),
					new ColumnInfo(3, "Name", ColumnSize.Strings),
				});
			}
			tableInfos[(int)Table.MethodSpec] = new TableInfo(Table.MethodSpec, "MethodSpec", new ColumnInfo[] {
				new ColumnInfo(0, "Method", ColumnSize.MethodDefOrRef),
				new ColumnInfo(1, "Instantiation", ColumnSize.Blob),
			});
			tableInfos[(int)Table.GenericParamConstraint] = new TableInfo(Table.GenericParamConstraint, "GenericParamConstraint", new ColumnInfo[] {
				new ColumnInfo(0, "Owner", ColumnSize.GenericParam),
				new ColumnInfo(1, "Constraint", ColumnSize.TypeDefOrRef),
			});
			tableInfos[0x2D] = new TableInfo((Table)0x2D, string.Empty, new ColumnInfo[] { });
			tableInfos[0x2E] = new TableInfo((Table)0x2E, string.Empty, new ColumnInfo[] { });
			tableInfos[0x2F] = new TableInfo((Table)0x2F, string.Empty, new ColumnInfo[] { });
			tableInfos[(int)Table.Document] = new TableInfo(Table.Document, "Document", new ColumnInfo[] {
				new ColumnInfo(0, "Name", ColumnSize.Blob),
				new ColumnInfo(1, "HashAlgorithm", ColumnSize.GUID),
				new ColumnInfo(2, "Hash", ColumnSize.Blob),
				new ColumnInfo(3, "Language", ColumnSize.GUID),
			});
			tableInfos[(int)Table.MethodDebugInformation] = new TableInfo(Table.MethodDebugInformation, "MethodDebugInformation", new ColumnInfo[] {
				new ColumnInfo(0, "Document", ColumnSize.Document),
				new ColumnInfo(1, "SequencePoints", ColumnSize.Blob),
			});
			tableInfos[(int)Table.LocalScope] = new TableInfo(Table.LocalScope, "LocalScope", new ColumnInfo[] {
				new ColumnInfo(0, "Method", ColumnSize.Method),
				new ColumnInfo(1, "ImportScope", ColumnSize.ImportScope),
				new ColumnInfo(2, "VariableList", ColumnSize.LocalVariable),
				new ColumnInfo(3, "ConstantList", ColumnSize.LocalConstant),
				new ColumnInfo(4, "StartOffset", ColumnSize.UInt32),
				new ColumnInfo(5, "Length", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.LocalVariable] = new TableInfo(Table.LocalVariable, "LocalVariable", new ColumnInfo[] {
				new ColumnInfo(0, "Attributes", ColumnSize.UInt16),
				new ColumnInfo(1, "Index", ColumnSize.UInt16),
				new ColumnInfo(2, "Name", ColumnSize.Strings),
			});
			tableInfos[(int)Table.LocalConstant] = new TableInfo(Table.LocalConstant, "LocalConstant", new ColumnInfo[] {
				new ColumnInfo(0, "Name", ColumnSize.Strings),
				new ColumnInfo(1, "Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ImportScope] = new TableInfo(Table.ImportScope, "ImportScope", new ColumnInfo[] {
				new ColumnInfo(0, "Parent", ColumnSize.ImportScope),
				new ColumnInfo(1, "Imports", ColumnSize.Blob),
			});
			tableInfos[(int)Table.StateMachineMethod] = new TableInfo(Table.StateMachineMethod, "StateMachineMethod", new ColumnInfo[] {
				new ColumnInfo(0, "MoveNextMethod", ColumnSize.Method),
				new ColumnInfo(1, "KickoffMethod", ColumnSize.Method),
			});
			tableInfos[(int)Table.CustomDebugInformation] = new TableInfo(Table.CustomDebugInformation, "CustomDebugInformation", new ColumnInfo[] {
				new ColumnInfo(0, "Parent", ColumnSize.HasCustomDebugInformation),
				new ColumnInfo(1, "Kind", ColumnSize.GUID),
				new ColumnInfo(2, "Value", ColumnSize.Blob),
			});
			return this.tableInfos = tableInfos;
		}
	}
}
