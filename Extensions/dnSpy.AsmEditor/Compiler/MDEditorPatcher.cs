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
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnSpy.AsmEditor.Compiler.MDEditor;

namespace dnSpy.AsmEditor.Compiler {
	[Flags]
	enum MDEditorPatcherOptions {
		None							= 0,
		UpdateTypeReferences			= 0x0000_0001,
		AllowInternalAccess				= 0x0000_0002,
	}

	unsafe struct MDEditorPatcher {
		readonly RawModuleBytes moduleData;
		readonly byte* peFile;
		readonly IAssembly tempAssembly;
		readonly TypeDef? nonNestedEditedType;
		readonly RemappedTypeTokens remappedTypeTokens;
		readonly List<byte> sigBuilder;
		readonly MetadataEditor mdEditor;
		readonly MDEditorPatcherOptions options;
		uint tempAssemblyRefRid;

		bool UpdateTypeReferences => (options & MDEditorPatcherOptions.UpdateTypeReferences) != 0;
		bool AllowInternalAccess => (options & MDEditorPatcherOptions.AllowInternalAccess) != 0;

		public MDEditorPatcher(RawModuleBytes moduleData, MetadataEditor mdEditor, IAssembly tempAssembly, TypeDef? nonNestedEditedType, MDEditorPatcherOptions options) {
			this.moduleData = moduleData;
			peFile = (byte*)moduleData.Pointer;
			this.tempAssembly = tempAssembly;
			this.nonNestedEditedType = nonNestedEditedType;
			remappedTypeTokens = new RemappedTypeTokens(nonNestedEditedType);
			sigBuilder = new List<byte>();
			this.mdEditor = mdEditor;
			this.options = options;
			tempAssemblyRefRid = 0;
		}

		uint GetOrCreateTempAssemblyRid() {
			if (tempAssemblyRefRid == 0)
				tempAssemblyRefRid = mdEditor.CreateAssemblyRef(tempAssembly);
			return tempAssemblyRefRid;
		}

		public void Patch(ModuleDef module) {
			if (UpdateTypeReferences && nonNestedEditedType is not null) {
				if (nonNestedEditedType.Module == module)
					DeleteTypeDef(mdEditor, nonNestedEditedType);
				PatchTypeRefsToEditedType(nonNestedEditedType);
				PatchTypeTokenReferences(nonNestedEditedType);
			}
			if (AllowInternalAccess)
				AddInternalsVisibleToAttribute();
		}

		void DeleteTypeDef(MetadataEditor mdEditor, TypeDef nonNestedTypeDef) {
			Debug2.Assert(nonNestedTypeDef.DeclaringType is null);

			var dict = new Dictionary<TypeDef, (TypeDef Type, uint TypeRefRid, RawTypeRefRow TypeRefRow)>();
			foreach (var type in MDPatcherUtils.GetMetadataTypes(nonNestedTypeDef)) {
				var typeRefRid = mdEditor.TablesHeap.TypeRefTable.Create();
				remappedTypeTokens.Add(type.MDToken.Raw, new MDToken(Table.TypeRef, typeRefRid).Raw);

				// Remove the type by renaming it and making it private/internal
				var tdRow = mdEditor.TablesHeap.TypeDefTable.Get(type.Rid);
				var flags = tdRow.Flags;
				if ((flags & 7) <= 1)
					flags = flags & ~7U;		// NotPublic
				else
					flags = (flags & ~7U) | 3;  // NestedPrivate
				var deletedType = (Type: type, TypeRefRid: typeRefRid, TypeRefRow: new RawTypeRefRow(0, tdRow.Name, tdRow.Namespace));
				tdRow = new RawTypeDefRow(flags, mdEditor.StringsHeap.Create(Guid.NewGuid().ToString()), mdEditor.StringsHeap.Create(string.Empty), tdRow.Extends, tdRow.FieldList, tdRow.MethodList);
				mdEditor.TablesHeap.TypeDefTable.Set(type.Rid, ref tdRow);
				dict.Add(type, deletedType);
			}
			remappedTypeTokens.SetReadOnly();

			foreach (var kv in dict) {
				var deletedType = kv.Value;
				uint resolutionScope;
				if (deletedType.Type.DeclaringType is not null) {
					var declType = dict[deletedType.Type.DeclaringType];
					resolutionScope = CodedToken.ResolutionScope.Encode(new MDToken(Table.TypeRef, declType.TypeRefRid));
				}
				else
					resolutionScope = CodedToken.ResolutionScope.Encode(new MDToken(Table.AssemblyRef, GetOrCreateTempAssemblyRid()));
				deletedType.TypeRefRow = new RawTypeRefRow(resolutionScope, deletedType.TypeRefRow.Name, deletedType.TypeRefRow.Namespace);
				mdEditor.TablesHeap.TypeRefTable.Set(deletedType.TypeRefRid, ref deletedType.TypeRefRow);
			}
		}

		/// <summary>
		/// Updates all TypeRefs in the TypeRef table that reference the non-nested edited type to now
		/// reference it in the temporary edit-assembly.
		/// </summary>
		void PatchTypeRefsToEditedType(TypeDef nonNestedEditedType) {
			Debug2.Assert(nonNestedEditedType.DeclaringType is null);
			if (remappedTypeTokens.Count == 0)
				return;

			var resolutionScopeCodedTokenCache = new Dictionary<uint, bool>();
			var md = mdEditor.RealMetadata;
			var stringsStream = md.StringsStream.CreateReader();
			var table = md.TablesStream.TypeRefTable;
			var p = peFile + (int)table.StartOffset;
			int rowSize = (int)table.RowSize;
			var columns = table.Columns;
			var nameData = nonNestedEditedType.Name?.Data ?? Array.Empty<byte>();
			var namespaceData = nonNestedEditedType.Namespace?.Data ?? Array.Empty<byte>();
			var columnResolutionScope = columns[0];
			var columnName = columns[1];
			var columnNamespace = columns[2];
			p += columnName.Offset;
			for (uint i = 0; i < table.Rows; i++, p += rowSize) {
				uint nameOffset = columnName.Size == 2 ? *(ushort*)p : *(uint*)p;
				if (!StringsStreamNameEquals(ref stringsStream, nameOffset, nameData))
					continue;
				uint namespaceOffset = columnNamespace.Size == 2 ? *(ushort*)(p + columnName.Size) : *(uint*)(p + columnName.Size);
				if (!StringsStreamNameEquals(ref stringsStream, namespaceOffset, namespaceData))
					continue;

				uint resolutionScopeCodedToken = columnResolutionScope.Size == 2 ? *(ushort*)(p - 2) : *(uint*)(p - 4);
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
				typeRefRow = new RawTypeRefRow(CodedToken.ResolutionScope.Encode(new MDToken(Table.AssemblyRef, GetOrCreateTempAssemblyRid())), typeRefRow.Name, typeRefRow.Namespace);
				mdEditor.TablesHeap.TypeRefTable.Set(i + 1, ref typeRefRow);
			}
		}

		bool CheckResolutionScopeIsSameModule(MDToken resolutionScope, ModuleDef module) {
			switch (resolutionScope.Table) {
			case Table.Module:
				return resolutionScope.Rid == 1;

			case Table.ModuleRef:
				if (!mdEditor.RealMetadata.TablesStream.TryReadModuleRefRow(resolutionScope.Rid, out var moduleRefRow))
					return false;
				var moduleRefName = mdEditor.RealMetadata.StringsStream.ReadNoNull(moduleRefRow.Name);
				return StringComparer.OrdinalIgnoreCase.Equals(moduleRefName.String, module.Name.String);

			case Table.AssemblyRef:
				var asm = module.Assembly;
				if (asm is null)
					return false;
				if (!mdEditor.RealMetadata.TablesStream.TryReadAssemblyRefRow(resolutionScope.Rid, out var assemblyRefRow))
					return false;
				// The version number isn't checked due to binding redirects
				var assemblyRefName = mdEditor.RealMetadata.StringsStream.ReadNoNull(assemblyRefRow.Name);
				if (!StringComparer.OrdinalIgnoreCase.Equals(assemblyRefName.String, UTF8String.ToSystemStringOrEmpty(asm.Name)))
					return false;
				var assemblyRefLocale = mdEditor.RealMetadata.StringsStream.ReadNoNull(assemblyRefRow.Name);
				if (!StringComparer.OrdinalIgnoreCase.Equals(assemblyRefLocale.String, UTF8String.ToSystemStringOrEmpty(asm.Culture)))
					return false;
				if (!mdEditor.RealMetadata.BlobStream.TryCreateReader(assemblyRefRow.PublicKeyOrToken, out var reader))
					return false;
				byte[] asmPublicKeyOrTokenData;
				if ((assemblyRefRow.Flags & (uint)AssemblyAttributes.PublicKey) != 0)
					asmPublicKeyOrTokenData = asm.PublicKey?.Data ?? Array.Empty<byte>();
				else
					asmPublicKeyOrTokenData = asm.PublicKeyToken?.Data ?? Array.Empty<byte>();
				return DataEquals(ref reader, asmPublicKeyOrTokenData);

			default:
				return false;
			}
		}

		bool StringsStreamNameEquals(ref DataReader stringsStream, uint offset, byte[] nameData) {
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

		static bool DataEquals(ref DataReader a, byte[] b) {
			if (b is null)
				return false;
			if (a.Length != (uint)b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a.ReadByte() != b[i])
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

			MDTable table;
			byte* p;
			int rowSize;
			ColumnInfo column, column2;
			uint i, codedToken, newToken, sig, newSig, rid;
			MDToken token;

			// Patch the TypeDef table
			{
				table = mdEditor.RealMetadata.TablesStream.TypeDefTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[3];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					codedToken = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out newToken)) {
						var row = tablesHeap.TypeDefTable.Get(i + 1);
						row = new RawTypeDefRow(row.Flags, row.Name, row.Namespace, CodedToken.TypeDefOrRef.Encode(newToken), row.FieldList, row.MethodList);
						tablesHeap.TypeDefTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the Field table
			{
				table = mdEditor.RealMetadata.TablesStream.FieldTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[2];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					sig = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.FieldTable.Get(i + 1);
						row = new RawFieldRow(row.Flags, row.Name, newSig);
						tablesHeap.FieldTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the Method table
			{
				table = mdEditor.RealMetadata.TablesStream.MethodTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[4];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					sig = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.MethodTable.Get(i + 1);
						row = new RawMethodRow(row.RVA, row.ImplFlags, row.Flags, row.Name, newSig, row.ParamList);
						tablesHeap.MethodTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the InterfaceImpl table
			{
				table = mdEditor.RealMetadata.TablesStream.InterfaceImplTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[1];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					codedToken = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out newToken)) {
						var row = tablesHeap.InterfaceImplTable.Get(i + 1);
						row = new RawInterfaceImplRow(row.Class, CodedToken.TypeDefOrRef.Encode(newToken));
						tablesHeap.InterfaceImplTable.Set(i + 1, ref row);
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
				table = mdEditor.RealMetadata.TablesStream.MemberRefTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[0];
				column2 = table.Columns[2];
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					codedToken = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					if (!CodedToken.MemberRefParent.Decode(codedToken, out token))
						continue;
					rid = i + 1;
					RawMemberRefRow row;
					switch (token.Table) {
					case Table.TypeRef:
					case Table.TypeDef:
					case Table.TypeSpec:
						if (remappedTypeTokens.TryGetValue(token.Raw, out newToken)) {
							row = tablesHeap.MemberRefTable.Get(rid);
							row = new RawMemberRefRow(CodedToken.MemberRefParent.Encode(newToken), row.Name, row.Signature);
							tablesHeap.MemberRefTable.Set(rid, ref row);
						}
						break;

					case Table.ModuleRef:
						if (nonNestedEditedType.IsGlobalModuleType && CheckResolutionScopeIsSameModule(token, nonNestedEditedType.Module)) {
							if (remappedTypeTokens.TryGetValue(nonNestedEditedType.MDToken.Raw, out newToken)) {
								row = tablesHeap.MemberRefTable.Get(rid);
								row = new RawMemberRefRow(CodedToken.MemberRefParent.Encode(newToken), row.Name, row.Signature);
								tablesHeap.MemberRefTable.Set(rid, ref row);
							}
						}
						break;

					case Table.Method:
						break;

					default:
						Debug.Fail("Impossible");
						break;
					}

					sig = column2.Size == 2 ? *(ushort*)(p + column2.Offset) : *(uint*)(p + column2.Offset);
					newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (sig != newSig) {
						row = tablesHeap.MemberRefTable.Get(rid);
						row = new RawMemberRefRow(row.Class, row.Name, newSig);
						tablesHeap.MemberRefTable.Set(rid, ref row);
					}
				}
			}

			// Patch the Event table
			{
				table = mdEditor.RealMetadata.TablesStream.EventTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[2];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					codedToken = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out newToken)) {
						var row = tablesHeap.EventTable.Get(i + 1);
						row = new RawEventRow(row.EventFlags, row.Name, CodedToken.TypeDefOrRef.Encode(newToken));
						tablesHeap.EventTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the Property table
			{
				table = mdEditor.RealMetadata.TablesStream.PropertyTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[2];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					sig = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					newSig = PatchCallingConventionSignature(callConvSigDict, sig);
					if (newSig != sig) {
						var row = tablesHeap.PropertyTable.Get(i + 1);
						row = new RawPropertyRow(row.PropFlags, row.Name, newSig);
						tablesHeap.PropertyTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the TypeSpec table
			{
				table = mdEditor.RealMetadata.TablesStream.TypeSpecTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[0];
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					sig = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					newSig = PatchTypeSignature(typeSigDict, sig);
					if (newSig != sig) {
						var row = new RawTypeSpecRow(newSig);
						tablesHeap.TypeSpecTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the GenericParam table
			if (mdEditor.RealMetadata.TablesStream.GenericParamTable.Columns.Count == 5) {
				table = mdEditor.RealMetadata.TablesStream.GenericParamTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[4];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					codedToken = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out newToken)) {
						var row = tablesHeap.GenericParamTable.Get(i + 1);
						row = new RawGenericParamRow(row.Number, row.Flags, row.Owner, row.Name, CodedToken.TypeDefOrRef.Encode(newToken));
						tablesHeap.GenericParamTable.Set(i + 1, ref row);
					}
				}
			}

			// Patch the GenericParamConstraint table
			{
				table = mdEditor.RealMetadata.TablesStream.GenericParamConstraintTable;
				p = peFile + (int)table.StartOffset;
				rowSize = (int)table.RowSize;
				column = table.Columns[1];
				p += column.Offset;
				for (i = 0; i < table.Rows; i++, p += rowSize) {
					codedToken = column.Size == 2 ? *(ushort*)p : *(uint*)p;
					if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
						continue;
					if (remappedTypeTokens.TryGetValue(token.Raw, out newToken)) {
						var row = tablesHeap.GenericParamConstraintTable.Get(i + 1);
						row = new RawGenericParamConstraintRow(row.Owner, CodedToken.TypeDefOrRef.Encode(newToken));
						tablesHeap.GenericParamConstraintTable.Set(i + 1, ref row);
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
			if (data is not null)
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
			if (data is not null)
				newSig = mdEditor.BlobHeap.Create(data);
			else
				newSig = sig;

			callConvSigDict.Add(sig, newSig);
			return newSig;
		}

		void AddInternalsVisibleToAttribute() {
			bool b = GetCorLibToken(out var corlibToken);
			Debug.Assert(b, "No corlib assembly found");
			if (!b)
				return;
			b = CodedToken.ResolutionScope.Encode(corlibToken, out uint corlibEncodedToken);
			Debug.Assert(b);
			if (!b)
				return;

			uint ivtTypeRefRid = mdEditor.TablesHeap.TypeRefTable.Create();
			var ivtTypeRefRow = new RawTypeRefRow(corlibEncodedToken, mdEditor.StringsHeap.Create("InternalsVisibleToAttribute"), mdEditor.StringsHeap.Create("System.Runtime.CompilerServices"));
			mdEditor.TablesHeap.TypeRefTable.Set(ivtTypeRefRid, ref ivtTypeRefRow);

			uint ivtCtorRid = mdEditor.TablesHeap.MemberRefTable.Create();
			var ivtCtorRow = new RawMemberRefRow(CodedToken.MemberRefParent.Encode(new MDToken(Table.TypeRef, ivtTypeRefRid)),
				mdEditor.StringsHeap.Create(".ctor"), mdEditor.BlobHeap.Create(ivtCtorSigBlob));
			mdEditor.TablesHeap.MemberRefTable.Set(ivtCtorRid, ref ivtCtorRow);

			uint ivtCARid = mdEditor.TablesHeap.CustomAttributeTable.Create();
			var ivtCARow = new RawCustomAttributeRow(CodedToken.HasCustomAttribute.Encode(new MDToken(Table.Assembly, 1)),
					CodedToken.CustomAttributeType.Encode(new MDToken(Table.MemberRef, ivtCtorRid)),
					mdEditor.BlobHeap.Create(MDPatcherUtils.CreateIVTBlob(tempAssembly)));
			mdEditor.TablesHeap.CustomAttributeTable.Set(ivtCARid, ref ivtCARow);
		}
		static readonly byte[] ivtCtorSigBlob = new byte[] { 0x20, 0x01, 0x01, 0x0E };

		bool GetCorLibToken(out MDToken corlibToken) {
			var stringsStream = mdEditor.RealMetadata.StringsStream.CreateReader();

			// Check if we have any assembly refs to the corlib
			{
				var table = mdEditor.RealMetadata.TablesStream.AssemblyRefTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var columnName = table.Columns[6];
				p += columnName.Offset;
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					uint nameOffset = columnName.Size == 2 ? *(ushort*)p : *(uint*)p;
					UTF8String? foundCorlibName = null;
					foreach (var corlibName in corlibSimpleNames) {
						if (StringsStreamNameEquals(ref stringsStream, nameOffset, corlibName.Data)) {
							foundCorlibName = corlibName;
							break;
						}
					}
					if (foundCorlibName is null)
						continue;

					// Don't use any WinMD mscorlib refs (version = 255.255.255.255)
					if (StringComparer.OrdinalIgnoreCase.Equals(foundCorlibName.String, "mscorlib") &&
						*(ulong*)(p - columnName.Offset) == 0x00FF_00FF_00FF_00FF) {
						continue;
					}

					corlibToken = new MDToken(Table.AssemblyRef, i + 1);
					return true;
				}
			}

			// Check if we are the corlib. We're the corlib if System.Object is defined and if there
			// are no assembly references
			if (mdEditor.RealMetadata.TablesStream.AssemblyRefTable.Rows == 0) {
				var table = mdEditor.RealMetadata.TablesStream.TypeDefTable;
				var p = peFile + (int)table.StartOffset;
				int rowSize = (int)table.RowSize;
				var columnName = table.Columns[1];
				var columnNamespace = table.Columns[2];
				var columnExtends = table.Columns[3];
				for (uint i = 0; i < table.Rows; i++, p += rowSize) {
					if ((*(uint*)p & (uint)TypeAttributes.VisibilityMask) >= (int)TypeAttributes.NestedPublic)
						continue;
					uint nameOffset = columnName.Size == 2 ? *(ushort*)(p + columnName.Offset) : *(uint*)(p + columnName.Offset);
					if (!StringsStreamNameEquals(ref stringsStream, nameOffset, nameObject.Data))
						continue;
					uint namespaceOffset = columnNamespace.Size == 2 ? *(ushort*)(p + columnNamespace.Offset) : *(uint*)(p + columnNamespace.Offset);
					if (!StringsStreamNameEquals(ref stringsStream, namespaceOffset, nameSystem.Data))
						continue;
					uint extendsCodedToken = columnExtends.Size == 2 ? *(ushort*)(p + columnExtends.Offset) : *(uint*)(p + columnExtends.Offset);
					if (!CodedToken.TypeDefOrRef.Decode(extendsCodedToken, out MDToken extends))
						continue;
					if (extends.Rid != 0)
						continue;

					corlibToken = new MDToken(Table.Module, 1);
					return true;
				}
			}

			corlibToken = default;
			return false;
		}
		static readonly UTF8String nameSystem = new UTF8String(nameof(System));
		static readonly UTF8String nameObject = new UTF8String(nameof(Object));

		// See dnlib IsCorLib()
		static readonly UTF8String[] corlibSimpleNames = new UTF8String[] {
			"mscorlib",
			"System.Runtime",
			"System.Private.CoreLib",
			"netstandard",
			"corefx",
		};
	}
}
