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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnSpy.AsmEditor.Compiler.MDEditor;

namespace dnSpy.AsmEditor.Compiler {
	unsafe struct MDEditorPatcher {
		readonly RawModuleBytes moduleData;
		readonly byte* peFile;
		readonly IAssembly tempAssembly;
		readonly TypeDef nonNestedEditedTypeOrNull;
		readonly Dictionary<uint, uint> remappedTypeTokens;
		readonly List<byte> sigBuilder;
		readonly MetadataEditor mdEditor;
		uint tempAssemblyRefRid;

		public MDEditorPatcher(RawModuleBytes moduleData, MetadataEditor mdEditor, IAssembly tempAssembly, TypeDef nonNestedEditedTypeOrNull) {
			this.moduleData = moduleData;
			peFile = (byte*)moduleData.Pointer;
			this.tempAssembly = tempAssembly;
			this.nonNestedEditedTypeOrNull = nonNestedEditedTypeOrNull;
			remappedTypeTokens = new Dictionary<uint, uint>();
			sigBuilder = new List<byte>();
			this.mdEditor = mdEditor;
			tempAssemblyRefRid = 0;
		}

		uint GetOrCreateTempAssemblyRid() {
			if (tempAssemblyRefRid == 0)
				tempAssemblyRefRid = mdEditor.CreateAssemblyRef(tempAssembly);
			return tempAssemblyRefRid;
		}

		public void Patch(ModuleDef module) {
			if (nonNestedEditedTypeOrNull.Module == module)
				DeleteTypeDef(mdEditor, nonNestedEditedTypeOrNull);
			PatchTypeRefsToEditedType(nonNestedEditedTypeOrNull);
			PatchTypeTokenReferences(nonNestedEditedTypeOrNull);
		}

		void DeleteTypeDef(MetadataEditor mdEditor, TypeDef nonNestedTypeDef) {
			Debug.Assert(nonNestedTypeDef.DeclaringType == null);

			var dict = new Dictionary<TypeDef, (TypeDef Type, uint TypeRefRid, RawTypeRefRow TypeRefRow)>();
			foreach (var type in MDPatcherUtils.GetMetadataTypes(nonNestedTypeDef)) {
				var typeRefRid = mdEditor.TablesHeap.TypeRefTable.Create();
				var deletedType = (Type: type, TypeRefRid: typeRefRid, TypeRefRow: mdEditor.TablesHeap.TypeRefTable.Get(typeRefRid));
				dict.Add(type, deletedType);
				remappedTypeTokens.Add(type.MDToken.Raw, new MDToken(Table.TypeRef, typeRefRid).Raw);

				// Remove the type by renaming it and making it private/internal
				var tdRow = mdEditor.TablesHeap.TypeDefTable.Get(type.Rid);
				deletedType.TypeRefRow.Name = tdRow.Name;
				deletedType.TypeRefRow.Namespace = tdRow.Namespace;
				tdRow.Name = mdEditor.StringsHeap.Create(Guid.NewGuid().ToString());
				tdRow.Namespace = mdEditor.StringsHeap.Create(string.Empty);
				if ((tdRow.Flags & 7) <= 1)
					tdRow.Flags = tdRow.Flags & ~7U;        // NotPublic
				else
					tdRow.Flags = (tdRow.Flags & ~7U) | 3;  // NestedPrivate
				mdEditor.TablesHeap.TypeDefTable.Set(type.Rid, tdRow);
			}

			foreach (var kv in dict) {
				var deletedType = kv.Value;
				if (deletedType.Type.DeclaringType != null) {
					var declType = dict[deletedType.Type.DeclaringType];
					deletedType.TypeRefRow.ResolutionScope = CodedToken.ResolutionScope.Encode(new MDToken(Table.TypeRef, declType.TypeRefRid));
				}
				else
					deletedType.TypeRefRow.ResolutionScope = CodedToken.ResolutionScope.Encode(new MDToken(Table.AssemblyRef, GetOrCreateTempAssemblyRid()));
			}
		}

		/// <summary>
		/// Updates all TypeRefs in the TypeRef table that reference the non-nested edited type to now
		/// reference it in the temporary edit-assembly.
		/// </summary>
		void PatchTypeRefsToEditedType(TypeDef nonNestedEditedType) {
			Debug.Assert(nonNestedEditedType.DeclaringType == null);
			if (remappedTypeTokens.Count == 0)
				return;

			var resolutionScopeCodedTokenCache = new Dictionary<uint, bool>();
			var md = mdEditor.RealMetadata;
			using (var stringsStream = md.StringsStream.GetClonedImageStream()) {
				var table = md.TablesStream.TypeRefTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var columns = table.Columns;
				var nameData = nonNestedEditedType.Name?.Data ?? Array.Empty<byte>();
				var namespaceData = nonNestedEditedType.Namespace?.Data ?? Array.Empty<byte>();
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint nameOffset = ReadColumn(p, columns[1]);
					if (!StringsStreamNameEquals(stringsStream, nameOffset, nameData))
						continue;
					uint namespaceOffset = ReadColumn(p, columns[2]);
					if (!StringsStreamNameEquals(stringsStream, namespaceOffset, namespaceData))
						continue;

					uint resolutionScopeCodedToken = ReadColumn(p, columns[0]);
					if (!resolutionScopeCodedTokenCache.TryGetValue(resolutionScopeCodedToken, out var res)) {
						if (!CodedToken.ResolutionScope.Decode(resolutionScopeCodedToken, out MDToken resolutionScope))
							continue;
						if (resolutionScope.Table == Table.AssemblyRef)
							continue;
						resolutionScopeCodedTokenCache.Add(resolutionScopeCodedToken, res = CheckResolutionScopeIsSameModule(resolutionScope, nonNestedEditedType.Module));
					}
					if (!res)
						continue;

					var typeRefRow = mdEditor.TablesHeap.TypeRefTable.Get(i + 1);
					typeRefRow.ResolutionScope = CodedToken.ResolutionScope.Encode(new MDToken(Table.AssemblyRef, GetOrCreateTempAssemblyRid()));
					mdEditor.TablesHeap.TypeRefTable.Set(i + 1, typeRefRow);
				}
			}
		}

		bool CheckResolutionScopeIsSameModule(MDToken resolutionScope, ModuleDef module) {
			switch (resolutionScope.Table) {
			case Table.Module:
				return resolutionScope.Rid == 1;

			case Table.ModuleRef:
				var moduleRefRow = mdEditor.RealMetadata.TablesStream.ReadModuleRefRow(resolutionScope.Rid);
				if (moduleRefRow == null)
					return false;
				var moduleRefName = mdEditor.RealMetadata.StringsStream.ReadNoNull(moduleRefRow.Name);
				return StringComparer.OrdinalIgnoreCase.Equals(moduleRefName.String, module.Name.String);

			case Table.AssemblyRef:
				var asm = module.Assembly;
				if (asm == null)
					return false;
				var assemblyRefRow = mdEditor.RealMetadata.TablesStream.ReadAssemblyRefRow(resolutionScope.Rid);
				// The version number isn't checked due to binding redirects
				var assemblyRefName = mdEditor.RealMetadata.StringsStream.ReadNoNull(assemblyRefRow.Name);
				if (!StringComparer.OrdinalIgnoreCase.Equals(assemblyRefName.String, UTF8String.ToSystemStringOrEmpty(asm.Name)))
					return false;
				var assemblyRefLocale = mdEditor.RealMetadata.StringsStream.ReadNoNull(assemblyRefRow.Name);
				if (!StringComparer.OrdinalIgnoreCase.Equals(assemblyRefLocale.String, UTF8String.ToSystemStringOrEmpty(asm.Culture)))
					return false;
				var data = mdEditor.RealMetadata.BlobStream.ReadNoNull(assemblyRefRow.PublicKeyOrToken);
				byte[] asmPublicKeyOrTokenData;
				if ((assemblyRefRow.Flags & (uint)AssemblyAttributes.PublicKey) != 0)
					asmPublicKeyOrTokenData = asm.PublicKey?.Data ?? Array.Empty<byte>();
				else
					asmPublicKeyOrTokenData = asm.PublicKeyToken?.Data ?? Array.Empty<byte>();
				return DataEquals(data, asmPublicKeyOrTokenData);

			default:
				return false;
			}
		}

		bool StringsStreamNameEquals(IImageStream stringsStream, uint offset, byte[] nameData) {
			if (offset == 0)
				return nameData.Length == 0;
			stringsStream.Position = offset;
			long pos = offset;
			var end = stringsStream.Length;
			for (int i = 0; i < nameData.Length; i++) {
				if (pos >= end)
					return false;
				if (stringsStream.ReadByte() != nameData[i])
					return false;
			}
			if (pos >= end)
				return false;
			return stringsStream.ReadByte() == 0;
		}

		unsafe static uint ReadColumn(byte* p, ColumnInfo column) {
			p += column.Offset;
			switch (column.Size) {
			case 1:		return *p;
			case 2:		return *p++ | ((uint)*p << 8);
			case 4:		return *p++ | ((uint)*p++ << 8) | ((uint)*p++ << 16) | ((uint)*p << 24);
			default:
				Debug.Fail("Unreachable code");
				return 0;
			}
		}

		static bool DataEquals(byte[] a, byte[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Patches all columns and blob signatures that reference a moved TypeDef so they reference the new TypeRef
		/// </summary>
		void PatchTypeTokenReferences(TypeDef nonNestedEditedType) {
			if (remappedTypeTokens.Count == 0)
				return;

			// NOTE: We don't patch the following:
			//	- Method bodies
			//	- All MemberRefs referenced by method bodies
			//	- StandAloneSig.Signature. It's only used by method bodies
			//	- MethodSpec.Instantiation. It's only used by method bodies
			//	- Custom attribute blobs. Could reference the edited type but not likely to cause a problem.
			//	- Marshal blobs. Could reference the edited type but not likely to cause a problem.
			//	  The MD writer doesn't write the FieldMarshal table.

			var tablesHeap = mdEditor.TablesHeap;

			var typeSigDict = new Dictionary<uint, uint>();
			var callConvSigDict = new Dictionary<uint, uint>();

			// Patch the TypeDef table
			{
				var table = mdEditor.RealMetadata.TablesStream.TypeDefTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[3];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint codedToken = ReadColumn(p, column);
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out MDToken token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out var newToken)) {
						var row = tablesHeap.TypeDefTable.Get(i + 1);
						row.Extends = CodedToken.TypeDefOrRef.Encode(newToken);
						tablesHeap.TypeDefTable.Set(i + 1, row);
					}
				}
			}

			// Patch the Field table
			{
				var table = mdEditor.RealMetadata.TablesStream.FieldTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[2];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint sig = ReadColumn(p, column);
					uint newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.FieldTable.Get(i + 1);
						row.Signature = newSig;
						tablesHeap.FieldTable.Set(i + 1, row);
					}
				}
			}

			// Patch the Method table
			{
				var table = mdEditor.RealMetadata.TablesStream.MethodTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[4];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint sig = ReadColumn(p, column);
					uint newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.MethodTable.Get(i + 1);
						row.Signature = newSig;
						tablesHeap.MethodTable.Set(i + 1, row);
					}
				}
			}

			// Patch the InterfaceImpl table
			{
				var table = mdEditor.RealMetadata.TablesStream.InterfaceImplTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[1];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint codedToken = ReadColumn(p, column);
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out MDToken token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out var newToken)) {
						var row = tablesHeap.InterfaceImplTable.Get(i + 1);
						row.Interface = CodedToken.TypeDefOrRef.Encode(newToken);
						tablesHeap.InterfaceImplTable.Set(i + 1, row);
					}
				}
			}

			//TODO: PERF: If needed, this table could mostly be skipped. We only need to update refs from:
			//			CustomAttribute.Type, MethodImpl.MethodBody, MethodImpl.MethodDeclaration
			//		CustomAttribute.Type almost never references the edited type, and if it does, the
			//		edited type, or any of its nested types, is an attribute. Quick test: this block
			//		took ~25% of the total time (patch file, write new MD, test file was dnSpy.exe = 3.3MB).
			// Patch the MemberRef table
			{
				var table = mdEditor.RealMetadata.TablesStream.MemberRefTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var columnClass = table.Columns[0];
				var columnSignature = table.Columns[2];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint codedToken = ReadColumn(p, columnClass);
					if (!CodedToken.MemberRefParent.Decode(codedToken, out MDToken token))
						continue;
					uint rid = i + 1;
					switch (token.Table) {
					case Table.TypeRef:
					case Table.TypeDef:
					case Table.TypeSpec:
						if (remappedTypeTokens.TryGetValue(token.Raw, out var newToken)) {
							var row = tablesHeap.MemberRefTable.Get(rid);
							row.Class = CodedToken.MemberRefParent.Encode(newToken);
							tablesHeap.MemberRefTable.Set(rid, row);
						}
						break;

					case Table.ModuleRef:
						if (nonNestedEditedType.IsGlobalModuleType && CheckResolutionScopeIsSameModule(token, nonNestedEditedType.Module)) {
							if (remappedTypeTokens.TryGetValue(nonNestedEditedType.MDToken.Raw, out newToken)) {
								var row = tablesHeap.MemberRefTable.Get(rid);
								row.Class = CodedToken.MemberRefParent.Encode(newToken);
								tablesHeap.MemberRefTable.Set(rid, row);
							}
						}
						break;

					case Table.Method:
						break;

					default:
						Debug.Fail("Impossible");
						break;
					}

					uint sig = ReadColumn(p, columnSignature);
					uint newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (sig != newSig) {
						var row = tablesHeap.MemberRefTable.Get(rid);
						row.Signature = newSig;
						tablesHeap.MemberRefTable.Set(rid, row);
					}
				}
			}

			// Patch the Event table
			{
				var table = mdEditor.RealMetadata.TablesStream.EventTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[2];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint codedToken = ReadColumn(p, column);
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out MDToken token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out var newToken)) {
						var row = tablesHeap.EventTable.Get(i + 1);
						row.EventType = CodedToken.TypeDefOrRef.Encode(newToken);
						tablesHeap.EventTable.Set(i + 1, row);
					}
				}
			}

			// Patch the Property table
			{
				var table = mdEditor.RealMetadata.TablesStream.PropertyTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[2];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint sig = ReadColumn(p, column);
					uint newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.PropertyTable.Get(i + 1);
						row.Type = newSig;
						tablesHeap.PropertyTable.Set(i + 1, row);
					}
				}
			}

			// Patch the TypeSpec table
			{
				var table = mdEditor.RealMetadata.TablesStream.TypeSpecTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[0];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint sig = ReadColumn(p, column);
					uint newSig = PatchTypeSignature(typeSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.TypeSpecTable.Get(i + 1);
						row.Signature = newSig;
						tablesHeap.TypeSpecTable.Set(i + 1, row);
					}
				}
			}

			// Patch the GenericParam table
			if (mdEditor.RealMetadata.TablesStream.GenericParamTable.Columns.Count == 5) {
				var table = mdEditor.RealMetadata.TablesStream.GenericParamTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[4];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint codedToken = ReadColumn(p, column);
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out MDToken token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out var newToken)) {
						var row = tablesHeap.GenericParamTable.Get(i + 1);
						row.Kind = CodedToken.TypeDefOrRef.Encode(newToken);
						tablesHeap.GenericParamTable.Set(i + 1, row);
					}
				}
			}

			// Patch the GenericParamConstraint table
			{
				var table = mdEditor.RealMetadata.TablesStream.GenericParamConstraintTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var column = table.Columns[1];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint codedToken = ReadColumn(p, column);
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out MDToken token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out var newToken)) {
						var row = tablesHeap.GenericParamConstraintTable.Get(i + 1);
						row.Constraint = CodedToken.TypeDefOrRef.Encode(newToken);
						tablesHeap.GenericParamConstraintTable.Set(i + 1, row);
					}
				}
			}
		}

		/// <summary>
		/// Patches the signature. If it didn't need to be patched or if it was an in-place patch,
		/// the same signature is returned.
		/// </summary>
		uint PatchTypeSignature(Dictionary<uint, uint> typeSigDict, uint sig) {
			if (typeSigDict.TryGetValue(sig, out var newSig))
				return newSig;

			var data = MDSigPatcher.PatchTypeSignature(sigBuilder, remappedTypeTokens, moduleData, (uint)mdEditor.RealMetadata.BlobStream.StartOffset, sig);
			if (data != null)
				newSig = mdEditor.BlobHeap.Create(data);
			else
				newSig = sig;

			typeSigDict.Add(sig, newSig);
			return newSig;
		}

		/// <summary>
		/// Patches the signature. If it didn't need to be patched or if it was an in-place patch,
		/// the same signature is returned.
		/// </summary>
		uint PatchCallingConventionSignature(Dictionary<uint, uint> callConvSigDict, uint sig) {
			if (callConvSigDict.TryGetValue(sig, out var newSig))
				return newSig;

			var data = MDSigPatcher.PatchCallingConventionSignature(sigBuilder, remappedTypeTokens, moduleData, (uint)mdEditor.RealMetadata.BlobStream.StartOffset, sig);
			if (data != null)
				newSig = mdEditor.BlobHeap.Create(data);
			else
				newSig = sig;

			callConvSigDict.Add(sig, newSig);
			return newSig;
		}
	}
}
