/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class TablesMDHeap : MDHeap {
		readonly MetadataEditor mdEditor;
		readonly TablesStream tablesStream;

		public string Name => tablesStream.Name;

		public abstract class TableInfo {
			public abstract MDTable MDTable { get; }
			public abstract bool HasChanges { get; }
			public abstract uint FirstModifiedRowId { get; }
			public bool HasNewRows => Rows > MDTable.Rows;
			public bool IsEmpty => Rows == 0;
			public abstract uint Rows { get; }
			public abstract void WriteRow(uint rowIndex, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex);
		}

		public unsafe sealed class TableInfo<TRow> : TableInfo where TRow : class, IRawRow, new() {
			public override MDTable MDTable { get; }

			readonly RawModuleBytes moduleData;
			readonly byte* peFile;
			readonly Func<uint, TRow> readRow;
			readonly Dictionary<uint, TRow> rowsDict;
			readonly uint originalLastRid;
			uint nextRid;
			uint firstModifiedRid;

			public TableInfo(RawModuleBytes moduleData, MDTable mdTable, Func<uint, TRow> readRow) {
				this.moduleData = moduleData;
				peFile = (byte*)moduleData.Pointer;
				MDTable = mdTable;
				this.readRow = readRow;
				rowsDict = new Dictionary<uint, TRow>();
				originalLastRid = mdTable.Rows + 1;
				nextRid = originalLastRid;
				firstModifiedRid = originalLastRid;
			}

			public uint Create() {
				uint rid = nextRid++;
				rowsDict.Add(rid, new TRow());
				Debug.Assert(firstModifiedRid <= rid);
				return rid;
			}

			public TRow Get(uint rid) {
				Debug.Assert(rid != 0);
				if (rid >= firstModifiedRid && rowsDict.TryGetValue(rid, out var row))
					return row;
				return readRow(rid);
			}

			public void Set(uint rid, TRow row) {
				Debug.Assert(rid != 0);
				rowsDict[rid] = row;
				firstModifiedRid = Math.Min(firstModifiedRid, rid);
			}

			public override bool HasChanges => rowsDict.Count > 0;
			public override uint FirstModifiedRowId => firstModifiedRid;
			public override uint Rows => nextRid - 1;

			public override void WriteRow(uint rowIndex, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex) {
				var oldColumns = MDTable.Columns;

				var rid = rowIndex + 1;
				if (rid >= firstModifiedRid && rowsDict.TryGetValue(rid, out var row)) {
					for (int i = 0; i < newColumns.Count; i++) {
						uint value = row.Read(i);
						switch (newColumns[i].Size) {
						case 1:
							Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
							Debug.Assert(value <= byte.MaxValue);
							destination[destinationIndex++] = (byte)value;
							break;

						case 2:
							// The tables can only grow in size so it's not possible for a column to change
							// from 4 bytes in size to 2 bytes in size. Since this column is 2 bytes in size,
							// the old column is also 2 bytes in size.
							Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
							Debug.Assert(value <= ushort.MaxValue);
							destination[destinationIndex++] = (byte)value;
							destination[destinationIndex++] = (byte)(value >> 8);
							break;

						case 4:
							destination[destinationIndex++] = (byte)value;
							destination[destinationIndex++] = (byte)(value >> 8);
							destination[destinationIndex++] = (byte)(value >> 16);
							destination[destinationIndex++] = (byte)(value >> 24);
							break;

						default:
							throw new InvalidOperationException();
						}
					}
				}
				else {
					// We're here if the original metadata row hasn't changed. Convert it from the
					// old layout to the new layout (most likely they're identical)

					Debug.Assert(rowIndex < MDTable.Rows);
					var p = peFile + (int)MDTable.StartOffset + (int)(rowIndex * MDTable.RowSize);

					Debug.Assert(oldColumns.Count == newColumns.Count);
					for (int i = 0; i < newColumns.Count; i++) {
						switch (newColumns[i].Size) {
						case 1:
							Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
							destination[destinationIndex++] = *p++;
							break;

						case 2:
							Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
							destination[destinationIndex++] = *p++;
							destination[destinationIndex++] = *p++;
							break;

						case 4:
							Debug.Assert(oldColumns[i].Size == 2 || oldColumns[i].Size == 4);
							if (oldColumns[i].Size == 2) {
								destination[destinationIndex++] = *p++;
								destination[destinationIndex++] = *p++;
								destination[destinationIndex++] = 0;
								destination[destinationIndex++] = 0;
							}
							else {
								destination[destinationIndex++] = *p++;
								destination[destinationIndex++] = *p++;
								destination[destinationIndex++] = *p++;
								destination[destinationIndex++] = *p++;
							}
							break;

						default:
							throw new InvalidOperationException();
						}
					}
				}
			}
		}

		public TableInfo<RawModuleRow> ModuleTable { get; }
		public TableInfo<RawTypeRefRow> TypeRefTable { get; }
		public TableInfo<RawTypeDefRow> TypeDefTable { get; }
		public TableInfo<RawFieldPtrRow> FieldPtrTable { get; }
		public TableInfo<RawFieldRow> FieldTable { get; }
		public TableInfo<RawMethodPtrRow> MethodPtrTable { get; }
		public TableInfo<RawMethodRow> MethodTable { get; }
		public TableInfo<RawParamPtrRow> ParamPtrTable { get; }
		public TableInfo<RawParamRow> ParamTable { get; }
		public TableInfo<RawInterfaceImplRow> InterfaceImplTable { get; }
		public TableInfo<RawMemberRefRow> MemberRefTable { get; }
		public TableInfo<RawConstantRow> ConstantTable { get; }
		public TableInfo<RawCustomAttributeRow> CustomAttributeTable { get; }
		public TableInfo<RawFieldMarshalRow> FieldMarshalTable { get; }
		public TableInfo<RawDeclSecurityRow> DeclSecurityTable { get; }
		public TableInfo<RawClassLayoutRow> ClassLayoutTable { get; }
		public TableInfo<RawFieldLayoutRow> FieldLayoutTable { get; }
		public TableInfo<RawStandAloneSigRow> StandAloneSigTable { get; }
		public TableInfo<RawEventMapRow> EventMapTable { get; }
		public TableInfo<RawEventPtrRow> EventPtrTable { get; }
		public TableInfo<RawEventRow> EventTable { get; }
		public TableInfo<RawPropertyMapRow> PropertyMapTable { get; }
		public TableInfo<RawPropertyPtrRow> PropertyPtrTable { get; }
		public TableInfo<RawPropertyRow> PropertyTable { get; }
		public TableInfo<RawMethodSemanticsRow> MethodSemanticsTable { get; }
		public TableInfo<RawMethodImplRow> MethodImplTable { get; }
		public TableInfo<RawModuleRefRow> ModuleRefTable { get; }
		public TableInfo<RawTypeSpecRow> TypeSpecTable { get; }
		public TableInfo<RawImplMapRow> ImplMapTable { get; }
		public TableInfo<RawFieldRVARow> FieldRVATable { get; }
		public TableInfo<RawENCLogRow> ENCLogTable { get; }
		public TableInfo<RawENCMapRow> ENCMapTable { get; }
		public TableInfo<RawAssemblyRow> AssemblyTable { get; }
		public TableInfo<RawAssemblyProcessorRow> AssemblyProcessorTable { get; }
		public TableInfo<RawAssemblyOSRow> AssemblyOSTable { get; }
		public TableInfo<RawAssemblyRefRow> AssemblyRefTable { get; }
		public TableInfo<RawAssemblyRefProcessorRow> AssemblyRefProcessorTable { get; }
		public TableInfo<RawAssemblyRefOSRow> AssemblyRefOSTable { get; }
		public TableInfo<RawFileRow> FileTable { get; }
		public TableInfo<RawExportedTypeRow> ExportedTypeTable { get; }
		public TableInfo<RawManifestResourceRow> ManifestResourceTable { get; }
		public TableInfo<RawNestedClassRow> NestedClassTable { get; }
		public TableInfo<RawGenericParamRow> GenericParamTable { get; }
		public TableInfo<RawMethodSpecRow> MethodSpecTable { get; }
		public TableInfo<RawGenericParamConstraintRow> GenericParamConstraintTable { get; }
		public TableInfo<RawDocumentRow> DocumentTable { get; }
		public TableInfo<RawMethodDebugInformationRow> MethodDebugInformationTable { get; }
		public TableInfo<RawLocalScopeRow> LocalScopeTable { get; }
		public TableInfo<RawLocalVariableRow> LocalVariableTable { get; }
		public TableInfo<RawLocalConstantRow> LocalConstantTable { get; }
		public TableInfo<RawImportScopeRow> ImportScopeTable { get; }
		public TableInfo<RawStateMachineMethodRow> StateMachineMethodTable { get; }
		public TableInfo<RawCustomDebugInformationRow> CustomDebugInformationTable { get; }

		public TableInfo[] TableInfos => allTableInfos;

		readonly TableInfo[] allTableInfos;

		public TablesMDHeap(MetadataEditor mdEditor, TablesStream tablesStream) {
			this.mdEditor = mdEditor ?? throw new ArgumentNullException(nameof(mdEditor));
			this.tablesStream = tablesStream ?? throw new ArgumentNullException(nameof(tablesStream));

			allTableInfos = new TableInfo[64];
			allTableInfos[(int)Table.Module] = ModuleTable = new TableInfo<RawModuleRow>(mdEditor.ModuleData, tablesStream.ModuleTable, rid => this.tablesStream.ReadModuleRow(rid));
			allTableInfos[(int)Table.TypeRef] = TypeRefTable = new TableInfo<RawTypeRefRow>(mdEditor.ModuleData, tablesStream.TypeRefTable, rid => this.tablesStream.ReadTypeRefRow(rid));
			allTableInfos[(int)Table.TypeDef] = TypeDefTable = new TableInfo<RawTypeDefRow>(mdEditor.ModuleData, tablesStream.TypeDefTable, rid => this.tablesStream.ReadTypeDefRow(rid));
			allTableInfos[(int)Table.FieldPtr] = FieldPtrTable = new TableInfo<RawFieldPtrRow>(mdEditor.ModuleData, tablesStream.FieldPtrTable, rid => this.tablesStream.ReadFieldPtrRow(rid));
			allTableInfos[(int)Table.Field] = FieldTable = new TableInfo<RawFieldRow>(mdEditor.ModuleData, tablesStream.FieldTable, rid => this.tablesStream.ReadFieldRow(rid));
			allTableInfos[(int)Table.MethodPtr] = MethodPtrTable = new TableInfo<RawMethodPtrRow>(mdEditor.ModuleData, tablesStream.MethodPtrTable, rid => this.tablesStream.ReadMethodPtrRow(rid));
			allTableInfos[(int)Table.Method] = MethodTable = new TableInfo<RawMethodRow>(mdEditor.ModuleData, tablesStream.MethodTable, rid => this.tablesStream.ReadMethodRow(rid));
			allTableInfos[(int)Table.ParamPtr] = ParamPtrTable = new TableInfo<RawParamPtrRow>(mdEditor.ModuleData, tablesStream.ParamPtrTable, rid => this.tablesStream.ReadParamPtrRow(rid));
			allTableInfos[(int)Table.Param] = ParamTable = new TableInfo<RawParamRow>(mdEditor.ModuleData, tablesStream.ParamTable, rid => this.tablesStream.ReadParamRow(rid));
			allTableInfos[(int)Table.InterfaceImpl] = InterfaceImplTable = new TableInfo<RawInterfaceImplRow>(mdEditor.ModuleData, tablesStream.InterfaceImplTable, rid => this.tablesStream.ReadInterfaceImplRow(rid));
			allTableInfos[(int)Table.MemberRef] = MemberRefTable = new TableInfo<RawMemberRefRow>(mdEditor.ModuleData, tablesStream.MemberRefTable, rid => this.tablesStream.ReadMemberRefRow(rid));
			allTableInfos[(int)Table.Constant] = ConstantTable = new TableInfo<RawConstantRow>(mdEditor.ModuleData, tablesStream.ConstantTable, rid => this.tablesStream.ReadConstantRow(rid));
			allTableInfos[(int)Table.CustomAttribute] = CustomAttributeTable = new TableInfo<RawCustomAttributeRow>(mdEditor.ModuleData, tablesStream.CustomAttributeTable, rid => this.tablesStream.ReadCustomAttributeRow(rid));
			allTableInfos[(int)Table.FieldMarshal] = FieldMarshalTable = new TableInfo<RawFieldMarshalRow>(mdEditor.ModuleData, tablesStream.FieldMarshalTable, rid => this.tablesStream.ReadFieldMarshalRow(rid));
			allTableInfos[(int)Table.DeclSecurity] = DeclSecurityTable = new TableInfo<RawDeclSecurityRow>(mdEditor.ModuleData, tablesStream.DeclSecurityTable, rid => this.tablesStream.ReadDeclSecurityRow(rid));
			allTableInfos[(int)Table.ClassLayout] = ClassLayoutTable = new TableInfo<RawClassLayoutRow>(mdEditor.ModuleData, tablesStream.ClassLayoutTable, rid => this.tablesStream.ReadClassLayoutRow(rid));
			allTableInfos[(int)Table.FieldLayout] = FieldLayoutTable = new TableInfo<RawFieldLayoutRow>(mdEditor.ModuleData, tablesStream.FieldLayoutTable, rid => this.tablesStream.ReadFieldLayoutRow(rid));
			allTableInfos[(int)Table.StandAloneSig] = StandAloneSigTable = new TableInfo<RawStandAloneSigRow>(mdEditor.ModuleData, tablesStream.StandAloneSigTable, rid => this.tablesStream.ReadStandAloneSigRow(rid));
			allTableInfos[(int)Table.EventMap] = EventMapTable = new TableInfo<RawEventMapRow>(mdEditor.ModuleData, tablesStream.EventMapTable, rid => this.tablesStream.ReadEventMapRow(rid));
			allTableInfos[(int)Table.EventPtr] = EventPtrTable = new TableInfo<RawEventPtrRow>(mdEditor.ModuleData, tablesStream.EventPtrTable, rid => this.tablesStream.ReadEventPtrRow(rid));
			allTableInfos[(int)Table.Event] = EventTable = new TableInfo<RawEventRow>(mdEditor.ModuleData, tablesStream.EventTable, rid => this.tablesStream.ReadEventRow(rid));
			allTableInfos[(int)Table.PropertyMap] = PropertyMapTable = new TableInfo<RawPropertyMapRow>(mdEditor.ModuleData, tablesStream.PropertyMapTable, rid => this.tablesStream.ReadPropertyMapRow(rid));
			allTableInfos[(int)Table.PropertyPtr] = PropertyPtrTable = new TableInfo<RawPropertyPtrRow>(mdEditor.ModuleData, tablesStream.PropertyPtrTable, rid => this.tablesStream.ReadPropertyPtrRow(rid));
			allTableInfos[(int)Table.Property] = PropertyTable = new TableInfo<RawPropertyRow>(mdEditor.ModuleData, tablesStream.PropertyTable, rid => this.tablesStream.ReadPropertyRow(rid));
			allTableInfos[(int)Table.MethodSemantics] = MethodSemanticsTable = new TableInfo<RawMethodSemanticsRow>(mdEditor.ModuleData, tablesStream.MethodSemanticsTable, rid => this.tablesStream.ReadMethodSemanticsRow(rid));
			allTableInfos[(int)Table.MethodImpl] = MethodImplTable = new TableInfo<RawMethodImplRow>(mdEditor.ModuleData, tablesStream.MethodImplTable, rid => this.tablesStream.ReadMethodImplRow(rid));
			allTableInfos[(int)Table.ModuleRef] = ModuleRefTable = new TableInfo<RawModuleRefRow>(mdEditor.ModuleData, tablesStream.ModuleRefTable, rid => this.tablesStream.ReadModuleRefRow(rid));
			allTableInfos[(int)Table.TypeSpec] = TypeSpecTable = new TableInfo<RawTypeSpecRow>(mdEditor.ModuleData, tablesStream.TypeSpecTable, rid => this.tablesStream.ReadTypeSpecRow(rid));
			allTableInfos[(int)Table.ImplMap] = ImplMapTable = new TableInfo<RawImplMapRow>(mdEditor.ModuleData, tablesStream.ImplMapTable, rid => this.tablesStream.ReadImplMapRow(rid));
			allTableInfos[(int)Table.FieldRVA] = FieldRVATable = new TableInfo<RawFieldRVARow>(mdEditor.ModuleData, tablesStream.FieldRVATable, rid => this.tablesStream.ReadFieldRVARow(rid));
			allTableInfos[(int)Table.ENCLog] = ENCLogTable = new TableInfo<RawENCLogRow>(mdEditor.ModuleData, tablesStream.ENCLogTable, rid => this.tablesStream.ReadENCLogRow(rid));
			allTableInfos[(int)Table.ENCMap] = ENCMapTable = new TableInfo<RawENCMapRow>(mdEditor.ModuleData, tablesStream.ENCMapTable, rid => this.tablesStream.ReadENCMapRow(rid));
			allTableInfos[(int)Table.Assembly] = AssemblyTable = new TableInfo<RawAssemblyRow>(mdEditor.ModuleData, tablesStream.AssemblyTable, rid => this.tablesStream.ReadAssemblyRow(rid));
			allTableInfos[(int)Table.AssemblyProcessor] = AssemblyProcessorTable = new TableInfo<RawAssemblyProcessorRow>(mdEditor.ModuleData, tablesStream.AssemblyProcessorTable, rid => this.tablesStream.ReadAssemblyProcessorRow(rid));
			allTableInfos[(int)Table.AssemblyOS] = AssemblyOSTable = new TableInfo<RawAssemblyOSRow>(mdEditor.ModuleData, tablesStream.AssemblyOSTable, rid => this.tablesStream.ReadAssemblyOSRow(rid));
			allTableInfos[(int)Table.AssemblyRef] = AssemblyRefTable = new TableInfo<RawAssemblyRefRow>(mdEditor.ModuleData, tablesStream.AssemblyRefTable, rid => this.tablesStream.ReadAssemblyRefRow(rid));
			allTableInfos[(int)Table.AssemblyRefProcessor] = AssemblyRefProcessorTable = new TableInfo<RawAssemblyRefProcessorRow>(mdEditor.ModuleData, tablesStream.AssemblyRefProcessorTable, rid => this.tablesStream.ReadAssemblyRefProcessorRow(rid));
			allTableInfos[(int)Table.AssemblyRefOS] = AssemblyRefOSTable = new TableInfo<RawAssemblyRefOSRow>(mdEditor.ModuleData, tablesStream.AssemblyRefOSTable, rid => this.tablesStream.ReadAssemblyRefOSRow(rid));
			allTableInfos[(int)Table.File] = FileTable = new TableInfo<RawFileRow>(mdEditor.ModuleData, tablesStream.FileTable, rid => this.tablesStream.ReadFileRow(rid));
			allTableInfos[(int)Table.ExportedType] = ExportedTypeTable = new TableInfo<RawExportedTypeRow>(mdEditor.ModuleData, tablesStream.ExportedTypeTable, rid => this.tablesStream.ReadExportedTypeRow(rid));
			allTableInfos[(int)Table.ManifestResource] = ManifestResourceTable = new TableInfo<RawManifestResourceRow>(mdEditor.ModuleData, tablesStream.ManifestResourceTable, rid => this.tablesStream.ReadManifestResourceRow(rid));
			allTableInfos[(int)Table.NestedClass] = NestedClassTable = new TableInfo<RawNestedClassRow>(mdEditor.ModuleData, tablesStream.NestedClassTable, rid => this.tablesStream.ReadNestedClassRow(rid));
			allTableInfos[(int)Table.GenericParam] = GenericParamTable = new TableInfo<RawGenericParamRow>(mdEditor.ModuleData, tablesStream.GenericParamTable, rid => this.tablesStream.ReadGenericParamRow(rid));
			allTableInfos[(int)Table.MethodSpec] = MethodSpecTable = new TableInfo<RawMethodSpecRow>(mdEditor.ModuleData, tablesStream.MethodSpecTable, rid => this.tablesStream.ReadMethodSpecRow(rid));
			allTableInfos[(int)Table.GenericParamConstraint] = GenericParamConstraintTable = new TableInfo<RawGenericParamConstraintRow>(mdEditor.ModuleData, tablesStream.GenericParamConstraintTable, rid => this.tablesStream.ReadGenericParamConstraintRow(rid));
			allTableInfos[(int)Table.Document] = DocumentTable = new TableInfo<RawDocumentRow>(mdEditor.ModuleData, tablesStream.DocumentTable, rid => this.tablesStream.ReadDocumentRow(rid));
			allTableInfos[(int)Table.MethodDebugInformation] = MethodDebugInformationTable = new TableInfo<RawMethodDebugInformationRow>(mdEditor.ModuleData, tablesStream.MethodDebugInformationTable, rid => this.tablesStream.ReadMethodDebugInformationRow(rid));
			allTableInfos[(int)Table.LocalScope] = LocalScopeTable = new TableInfo<RawLocalScopeRow>(mdEditor.ModuleData, tablesStream.LocalScopeTable, rid => this.tablesStream.ReadLocalScopeRow(rid));
			allTableInfos[(int)Table.LocalVariable] = LocalVariableTable = new TableInfo<RawLocalVariableRow>(mdEditor.ModuleData, tablesStream.LocalVariableTable, rid => this.tablesStream.ReadLocalVariableRow(rid));
			allTableInfos[(int)Table.LocalConstant] = LocalConstantTable = new TableInfo<RawLocalConstantRow>(mdEditor.ModuleData, tablesStream.LocalConstantTable, rid => this.tablesStream.ReadLocalConstantRow(rid));
			allTableInfos[(int)Table.ImportScope] = ImportScopeTable = new TableInfo<RawImportScopeRow>(mdEditor.ModuleData, tablesStream.ImportScopeTable, rid => this.tablesStream.ReadImportScopeRow(rid));
			allTableInfos[(int)Table.StateMachineMethod] = StateMachineMethodTable = new TableInfo<RawStateMachineMethodRow>(mdEditor.ModuleData, tablesStream.StateMachineMethodTable, rid => this.tablesStream.ReadStateMachineMethodRow(rid));
			allTableInfos[(int)Table.CustomDebugInformation] = CustomDebugInformationTable = new TableInfo<RawCustomDebugInformationRow>(mdEditor.ModuleData, tablesStream.CustomDebugInformationTable, rid => this.tablesStream.ReadCustomDebugInformationRow(rid));
		}

		public override bool MustRewriteHeap() {
			foreach (var table in allTableInfos) {
				if (table?.HasChanges == true)
					return true;
			}
			return false;
		}

		public override bool ExistsInMetadata => tablesStream.StreamHeader != null;
	}
}
