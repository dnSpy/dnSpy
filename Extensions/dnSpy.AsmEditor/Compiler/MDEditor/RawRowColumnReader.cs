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
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	static class RawRowColumnReader {
		public delegate uint ReadColumnDelegate<TRow>(ref TRow row, int index) where TRow : struct;

		public static Delegate GetDelegate(Table table) {
			switch (table) {
			case Table.Module: return ReadModuleColumn;
			case Table.TypeRef: return ReadTypeRefColumn;
			case Table.TypeDef: return ReadTypeDefColumn;
			case Table.FieldPtr: return ReadFieldPtrColumn;
			case Table.Field: return ReadFieldColumn;
			case Table.MethodPtr: return ReadMethodPtrColumn;
			case Table.Method: return ReadMethodColumn;
			case Table.ParamPtr: return ReadParamPtrColumn;
			case Table.Param: return ReadParamColumn;
			case Table.InterfaceImpl: return ReadInterfaceImplColumn;
			case Table.MemberRef: return ReadMemberRefColumn;
			case Table.Constant: return ReadConstantColumn;
			case Table.CustomAttribute: return ReadCustomAttributeColumn;
			case Table.FieldMarshal: return ReadFieldMarshalColumn;
			case Table.DeclSecurity: return ReadDeclSecurityColumn;
			case Table.ClassLayout: return ReadClassLayoutColumn;
			case Table.FieldLayout: return ReadFieldLayoutColumn;
			case Table.StandAloneSig: return ReadStandAloneSigColumn;
			case Table.EventMap: return ReadEventMapColumn;
			case Table.EventPtr: return ReadEventPtrColumn;
			case Table.Event: return ReadEventColumn;
			case Table.PropertyMap: return ReadPropertyMapColumn;
			case Table.PropertyPtr: return ReadPropertyPtrColumn;
			case Table.Property: return ReadPropertyColumn;
			case Table.MethodSemantics: return ReadMethodSemanticsColumn;
			case Table.MethodImpl: return ReadMethodImplColumn;
			case Table.ModuleRef: return ReadModuleRefColumn;
			case Table.TypeSpec: return ReadTypeSpecColumn;
			case Table.ImplMap: return ReadImplMapColumn;
			case Table.FieldRVA: return ReadFieldRVAColumn;
			case Table.ENCLog: return ReadENCLogColumn;
			case Table.ENCMap: return ReadENCMapColumn;
			case Table.Assembly: return ReadAssemblyColumn;
			case Table.AssemblyProcessor: return ReadAssemblyProcessorColumn;
			case Table.AssemblyOS: return ReadAssemblyOSColumn;
			case Table.AssemblyRef: return ReadAssemblyRefColumn;
			case Table.AssemblyRefProcessor: return ReadAssemblyRefProcessorColumn;
			case Table.AssemblyRefOS: return ReadAssemblyRefOSColumn;
			case Table.File: return ReadFileColumn;
			case Table.ExportedType: return ReadExportedTypeColumn;
			case Table.ManifestResource: return ReadManifestResourceColumn;
			case Table.NestedClass: return ReadNestedClassColumn;
			case Table.GenericParam: return ReadGenericParamColumn;
			case Table.MethodSpec: return ReadMethodSpecColumn;
			case Table.GenericParamConstraint: return ReadGenericParamConstraintColumn;
			case Table.Document: return ReadDocumentColumn;
			case Table.MethodDebugInformation: return ReadMethodDebugInformationColumn;
			case Table.LocalScope: return ReadLocalScopeColumn;
			case Table.LocalVariable: return ReadLocalVariableColumn;
			case Table.LocalConstant: return ReadLocalConstantColumn;
			case Table.ImportScope: return ReadImportScopeColumn;
			case Table.StateMachineMethod: return ReadStateMachineMethodColumn;
			case Table.CustomDebugInformation: return ReadCustomDebugInformationColumn;
			default: throw new ArgumentOutOfRangeException(nameof(table));
			}
		}

		public static readonly ReadColumnDelegate<RawModuleRow> ReadModuleColumn = ReadModuleColumnMethod;
		public static readonly ReadColumnDelegate<RawTypeRefRow> ReadTypeRefColumn = ReadTypeRefColumnMethod;
		public static readonly ReadColumnDelegate<RawTypeDefRow> ReadTypeDefColumn = ReadTypeDefColumnMethod;
		public static readonly ReadColumnDelegate<RawFieldPtrRow> ReadFieldPtrColumn = ReadFieldPtrColumnMethod;
		public static readonly ReadColumnDelegate<RawFieldRow> ReadFieldColumn = ReadFieldColumnMethod;
		public static readonly ReadColumnDelegate<RawMethodPtrRow> ReadMethodPtrColumn = ReadMethodPtrColumnMethod;
		public static readonly ReadColumnDelegate<RawMethodRow> ReadMethodColumn = ReadMethodColumnMethod;
		public static readonly ReadColumnDelegate<RawParamPtrRow> ReadParamPtrColumn = ReadParamPtrColumnMethod;
		public static readonly ReadColumnDelegate<RawParamRow> ReadParamColumn = ReadParamColumnMethod;
		public static readonly ReadColumnDelegate<RawInterfaceImplRow> ReadInterfaceImplColumn = ReadInterfaceImplColumnMethod;
		public static readonly ReadColumnDelegate<RawMemberRefRow> ReadMemberRefColumn = ReadMemberRefColumnMethod;
		public static readonly ReadColumnDelegate<RawConstantRow> ReadConstantColumn = ReadConstantColumnMethod;
		public static readonly ReadColumnDelegate<RawCustomAttributeRow> ReadCustomAttributeColumn = ReadCustomAttributeColumnMethod;
		public static readonly ReadColumnDelegate<RawFieldMarshalRow> ReadFieldMarshalColumn = ReadFieldMarshalColumnMethod;
		public static readonly ReadColumnDelegate<RawDeclSecurityRow> ReadDeclSecurityColumn = ReadDeclSecurityColumnMethod;
		public static readonly ReadColumnDelegate<RawClassLayoutRow> ReadClassLayoutColumn = ReadClassLayoutColumnMethod;
		public static readonly ReadColumnDelegate<RawFieldLayoutRow> ReadFieldLayoutColumn = ReadFieldLayoutColumnMethod;
		public static readonly ReadColumnDelegate<RawStandAloneSigRow> ReadStandAloneSigColumn = ReadStandAloneSigColumnMethod;
		public static readonly ReadColumnDelegate<RawEventMapRow> ReadEventMapColumn = ReadEventMapColumnMethod;
		public static readonly ReadColumnDelegate<RawEventPtrRow> ReadEventPtrColumn = ReadEventPtrColumnMethod;
		public static readonly ReadColumnDelegate<RawEventRow> ReadEventColumn = ReadEventColumnMethod;
		public static readonly ReadColumnDelegate<RawPropertyMapRow> ReadPropertyMapColumn = ReadPropertyMapColumnMethod;
		public static readonly ReadColumnDelegate<RawPropertyPtrRow> ReadPropertyPtrColumn = ReadPropertyPtrColumnMethod;
		public static readonly ReadColumnDelegate<RawPropertyRow> ReadPropertyColumn = ReadPropertyColumnMethod;
		public static readonly ReadColumnDelegate<RawMethodSemanticsRow> ReadMethodSemanticsColumn = ReadMethodSemanticsColumnMethod;
		public static readonly ReadColumnDelegate<RawMethodImplRow> ReadMethodImplColumn = ReadMethodImplColumnMethod;
		public static readonly ReadColumnDelegate<RawModuleRefRow> ReadModuleRefColumn = ReadModuleRefColumnMethod;
		public static readonly ReadColumnDelegate<RawTypeSpecRow> ReadTypeSpecColumn = ReadTypeSpecColumnMethod;
		public static readonly ReadColumnDelegate<RawImplMapRow> ReadImplMapColumn = ReadImplMapColumnMethod;
		public static readonly ReadColumnDelegate<RawFieldRVARow> ReadFieldRVAColumn = ReadFieldRVAColumnMethod;
		public static readonly ReadColumnDelegate<RawENCLogRow> ReadENCLogColumn = ReadENCLogColumnMethod;
		public static readonly ReadColumnDelegate<RawENCMapRow> ReadENCMapColumn = ReadENCMapColumnMethod;
		public static readonly ReadColumnDelegate<RawAssemblyRow> ReadAssemblyColumn = ReadAssemblyColumnMethod;
		public static readonly ReadColumnDelegate<RawAssemblyProcessorRow> ReadAssemblyProcessorColumn = ReadAssemblyProcessorColumnMethod;
		public static readonly ReadColumnDelegate<RawAssemblyOSRow> ReadAssemblyOSColumn = ReadAssemblyOSColumnMethod;
		public static readonly ReadColumnDelegate<RawAssemblyRefRow> ReadAssemblyRefColumn = ReadAssemblyRefColumnMethod;
		public static readonly ReadColumnDelegate<RawAssemblyRefProcessorRow> ReadAssemblyRefProcessorColumn = ReadAssemblyRefProcessorColumnMethod;
		public static readonly ReadColumnDelegate<RawAssemblyRefOSRow> ReadAssemblyRefOSColumn = ReadAssemblyRefOSColumnMethod;
		public static readonly ReadColumnDelegate<RawFileRow> ReadFileColumn = ReadFileColumnMethod;
		public static readonly ReadColumnDelegate<RawExportedTypeRow> ReadExportedTypeColumn = ReadExportedTypeColumnMethod;
		public static readonly ReadColumnDelegate<RawManifestResourceRow> ReadManifestResourceColumn = ReadManifestResourceColumnMethod;
		public static readonly ReadColumnDelegate<RawNestedClassRow> ReadNestedClassColumn = ReadNestedClassColumnMethod;
		public static readonly ReadColumnDelegate<RawGenericParamRow> ReadGenericParamColumn = ReadGenericParamColumnMethod;
		public static readonly ReadColumnDelegate<RawMethodSpecRow> ReadMethodSpecColumn = ReadMethodSpecColumnMethod;
		public static readonly ReadColumnDelegate<RawGenericParamConstraintRow> ReadGenericParamConstraintColumn = ReadGenericParamConstraintColumnMethod;
		public static readonly ReadColumnDelegate<RawDocumentRow> ReadDocumentColumn = ReadDocumentColumnMethod;
		public static readonly ReadColumnDelegate<RawMethodDebugInformationRow> ReadMethodDebugInformationColumn = ReadMethodDebugInformationColumnMethod;
		public static readonly ReadColumnDelegate<RawLocalScopeRow> ReadLocalScopeColumn = ReadLocalScopeColumnMethod;
		public static readonly ReadColumnDelegate<RawLocalVariableRow> ReadLocalVariableColumn = ReadLocalVariableColumnMethod;
		public static readonly ReadColumnDelegate<RawLocalConstantRow> ReadLocalConstantColumn = ReadLocalConstantColumnMethod;
		public static readonly ReadColumnDelegate<RawImportScopeRow> ReadImportScopeColumn = ReadImportScopeColumnMethod;
		public static readonly ReadColumnDelegate<RawStateMachineMethodRow> ReadStateMachineMethodColumn = ReadStateMachineMethodColumnMethod;
		public static readonly ReadColumnDelegate<RawCustomDebugInformationRow> ReadCustomDebugInformationColumn = ReadCustomDebugInformationColumnMethod;

		static uint ReadModuleColumnMethod(ref RawModuleRow row, int index) => row[index];
		static uint ReadTypeRefColumnMethod(ref RawTypeRefRow row, int index) => row[index];
		static uint ReadTypeDefColumnMethod(ref RawTypeDefRow row, int index) => row[index];
		static uint ReadFieldPtrColumnMethod(ref RawFieldPtrRow row, int index) => row[index];
		static uint ReadFieldColumnMethod(ref RawFieldRow row, int index) => row[index];
		static uint ReadMethodPtrColumnMethod(ref RawMethodPtrRow row, int index) => row[index];
		static uint ReadMethodColumnMethod(ref RawMethodRow row, int index) => row[index];
		static uint ReadParamPtrColumnMethod(ref RawParamPtrRow row, int index) => row[index];
		static uint ReadParamColumnMethod(ref RawParamRow row, int index) => row[index];
		static uint ReadInterfaceImplColumnMethod(ref RawInterfaceImplRow row, int index) => row[index];
		static uint ReadMemberRefColumnMethod(ref RawMemberRefRow row, int index) => row[index];
		static uint ReadConstantColumnMethod(ref RawConstantRow row, int index) => row[index];
		static uint ReadCustomAttributeColumnMethod(ref RawCustomAttributeRow row, int index) => row[index];
		static uint ReadFieldMarshalColumnMethod(ref RawFieldMarshalRow row, int index) => row[index];
		static uint ReadDeclSecurityColumnMethod(ref RawDeclSecurityRow row, int index) => row[index];
		static uint ReadClassLayoutColumnMethod(ref RawClassLayoutRow row, int index) => row[index];
		static uint ReadFieldLayoutColumnMethod(ref RawFieldLayoutRow row, int index) => row[index];
		static uint ReadStandAloneSigColumnMethod(ref RawStandAloneSigRow row, int index) => row[index];
		static uint ReadEventMapColumnMethod(ref RawEventMapRow row, int index) => row[index];
		static uint ReadEventPtrColumnMethod(ref RawEventPtrRow row, int index) => row[index];
		static uint ReadEventColumnMethod(ref RawEventRow row, int index) => row[index];
		static uint ReadPropertyMapColumnMethod(ref RawPropertyMapRow row, int index) => row[index];
		static uint ReadPropertyPtrColumnMethod(ref RawPropertyPtrRow row, int index) => row[index];
		static uint ReadPropertyColumnMethod(ref RawPropertyRow row, int index) => row[index];
		static uint ReadMethodSemanticsColumnMethod(ref RawMethodSemanticsRow row, int index) => row[index];
		static uint ReadMethodImplColumnMethod(ref RawMethodImplRow row, int index) => row[index];
		static uint ReadModuleRefColumnMethod(ref RawModuleRefRow row, int index) => row[index];
		static uint ReadTypeSpecColumnMethod(ref RawTypeSpecRow row, int index) => row[index];
		static uint ReadImplMapColumnMethod(ref RawImplMapRow row, int index) => row[index];
		static uint ReadFieldRVAColumnMethod(ref RawFieldRVARow row, int index) => row[index];
		static uint ReadENCLogColumnMethod(ref RawENCLogRow row, int index) => row[index];
		static uint ReadENCMapColumnMethod(ref RawENCMapRow row, int index) => row[index];
		static uint ReadAssemblyColumnMethod(ref RawAssemblyRow row, int index) => row[index];
		static uint ReadAssemblyProcessorColumnMethod(ref RawAssemblyProcessorRow row, int index) => row[index];
		static uint ReadAssemblyOSColumnMethod(ref RawAssemblyOSRow row, int index) => row[index];
		static uint ReadAssemblyRefColumnMethod(ref RawAssemblyRefRow row, int index) => row[index];
		static uint ReadAssemblyRefProcessorColumnMethod(ref RawAssemblyRefProcessorRow row, int index) => row[index];
		static uint ReadAssemblyRefOSColumnMethod(ref RawAssemblyRefOSRow row, int index) => row[index];
		static uint ReadFileColumnMethod(ref RawFileRow row, int index) => row[index];
		static uint ReadExportedTypeColumnMethod(ref RawExportedTypeRow row, int index) => row[index];
		static uint ReadManifestResourceColumnMethod(ref RawManifestResourceRow row, int index) => row[index];
		static uint ReadNestedClassColumnMethod(ref RawNestedClassRow row, int index) => row[index];
		static uint ReadGenericParamColumnMethod(ref RawGenericParamRow row, int index) => row[index];
		static uint ReadMethodSpecColumnMethod(ref RawMethodSpecRow row, int index) => row[index];
		static uint ReadGenericParamConstraintColumnMethod(ref RawGenericParamConstraintRow row, int index) => row[index];
		static uint ReadDocumentColumnMethod(ref RawDocumentRow row, int index) => row[index];
		static uint ReadMethodDebugInformationColumnMethod(ref RawMethodDebugInformationRow row, int index) => row[index];
		static uint ReadLocalScopeColumnMethod(ref RawLocalScopeRow row, int index) => row[index];
		static uint ReadLocalVariableColumnMethod(ref RawLocalVariableRow row, int index) => row[index];
		static uint ReadLocalConstantColumnMethod(ref RawLocalConstantRow row, int index) => row[index];
		static uint ReadImportScopeColumnMethod(ref RawImportScopeRow row, int index) => row[index];
		static uint ReadStateMachineMethodColumnMethod(ref RawStateMachineMethodRow row, int index) => row[index];
		static uint ReadCustomDebugInformationColumnMethod(ref RawCustomDebugInformationRow row, int index) => row[index];
	}
}
