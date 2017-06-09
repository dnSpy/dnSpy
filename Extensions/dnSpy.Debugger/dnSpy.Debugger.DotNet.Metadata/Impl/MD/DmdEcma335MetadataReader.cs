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
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdEcma335MetadataReader : DmdMetadataReaderBase {
		public override Guid ModuleVersionId { get; }
		public override int MDStreamVersion => ((TablesStream.Version & 0xFF00) << 8) | (TablesStream.Version & 0xFF);
		public override string ModuleScopeName { get; }
		public override string ImageRuntimeVersion => Metadata.VersionString;
		public override DmdMethodInfo EntryPoint => throw new NotImplementedException();//TODO:

		public static DmdEcma335MetadataReader Create(DmdModuleImpl module, IntPtr address, uint size, bool isFileLayout) {
			var peImage = new PEImage(address, size, isFileLayout ? ImageLayout.File : ImageLayout.Memory, true);
			return Create(module, peImage);
		}

		public static DmdEcma335MetadataReader Create(DmdModuleImpl module, byte[] bytes, bool isFileLayout) {
			var peImage = new PEImage(bytes, isFileLayout ? ImageLayout.File : ImageLayout.Memory, true);
			return Create(module, peImage);
		}

		public static DmdEcma335MetadataReader Create(DmdModuleImpl module, string filename, bool isFileLayout) =>
			Create(module, File.ReadAllBytes(filename), isFileLayout);

		static DmdEcma335MetadataReader Create(DmdModuleImpl module, IPEImage peImage) {
			var metadata = MetaDataCreator.CreateMetaData(peImage);
			return new DmdEcma335MetadataReader(module, metadata);
		}

		internal DmdModule Module => module;
		internal IMetaData Metadata { get; }
		internal TablesStream TablesStream => Metadata.TablesStream;
		internal StringsStream StringsStream => Metadata.StringsStream;
		internal GuidStream GuidStream => Metadata.GuidStream;
		internal BlobStream BlobStream => Metadata.BlobStream;

		readonly DmdModuleImpl module;
		readonly LazyList<DmdTypeRef> typeRefList;
		readonly LazyList<DmdType> typeDefList;
		readonly LazyList2<DmdType> typeSpecList;
		readonly LazyList<DmdTypeRef> exportedTypeList;
		readonly DmdNullGlobalType globalTypeIfThereAreNoTypes;

		DmdEcma335MetadataReader(DmdModuleImpl module, IMetaData metadata) {
			this.module = module;
			Metadata = metadata;

			var row = TablesStream.ReadModuleRow(1);
			ModuleScopeName = metadata.StringsStream.ReadNoNull(row?.Name ?? 0);
			ModuleVersionId = metadata.GuidStream.Read(row?.Mvid ?? 0) ?? Guid.Empty;

			var ts = TablesStream;
			typeRefList = new LazyList<DmdTypeRef>(ts.TypeRefTable.Rows, rid => new DmdTypeRefMD(this, rid, null));
			typeDefList = new LazyList<DmdType>(ts.TypeDefTable.Rows, rid => new DmdTypeDefMD(this, rid, null));
			typeSpecList = new LazyList2<DmdType>(ts.TypeSpecTable.Rows, (rid, genericTypeArguments) => ReadTypeSpec(rid, genericTypeArguments));
			exportedTypeList = new LazyList<DmdTypeRef>(ts.ExportedTypeTable.Rows, rid => new DmdExportedTypeMD(this, rid, null));

			globalTypeIfThereAreNoTypes = new DmdNullGlobalType(module, null);
		}

		(DmdType type, bool containedGenericParams) ReadTypeSpec(uint rid, IList<DmdType> genericTypeArguments) {
			var row = Metadata.TablesStream.ReadTypeSpecRow(rid);
			var stream = BlobStream.CreateStream(row.Signature);
			const bool resolve = true;
			return DmdSignatureReader.ReadTypeSignature(module, new DmdDataStreamImpl(stream), genericTypeArguments, resolve);
		}

		public override DmdType[] GetTypes() {
			uint typeDefRows = TablesStream.TypeDefTable.Rows;
			// This should never happen but we must return at least one type
			if (typeDefRows == 0)
				return new DmdType[] { globalTypeIfThereAreNoTypes };
			var result = new DmdType[typeDefRows];
			for (int i = 0; i < result.Length; i++) {
				var type = ResolveTypeDef((uint)i + 1);
				result[i] = type ?? throw new InvalidOperationException();
			}
			return result;
		}

		public override DmdType[] GetExportedTypes() {
			if (TablesStream.ExportedTypeTable.Rows == 0)
				return Array.Empty<DmdType>();
			var result = new DmdType[TablesStream.ExportedTypeTable.Rows];
			for (int i = 0; i < result.Length; i++) {
				var type = ResolveExportedType((uint)i + 1);
				result[i] = type ?? throw new InvalidOperationException();
			}
			return result;
		}

		protected override DmdType ResolveTypeRef(uint rid) => typeRefList[rid - 1];
		protected override DmdType ResolveTypeDef(uint rid) => typeDefList[rid - 1];
		protected override DmdFieldInfo ResolveFieldDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdMethodBase ResolveMethodDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments) => throw new NotImplementedException();//TODO:
		protected override DmdEventInfo ResolveEventDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdPropertyInfo ResolvePropertyDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments) => typeSpecList[rid - 1, genericTypeArguments];
		protected override DmdTypeRef ResolveExportedType(uint rid) => exportedTypeList[rid - 1];
		protected override DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => throw new NotImplementedException();//TODO:

		protected override byte[] ResolveFieldSignature(uint rid) {
			var row = TablesStream.ReadFieldRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveMethodSignature(uint rid) {
			var row = TablesStream.ReadMethodRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveMemberRefSignature(uint rid) {
			var row = TablesStream.ReadMemberRefRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveStandAloneSigSignature(uint rid) {
			var row = TablesStream.ReadStandAloneSigRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveTypeSpecSignature(uint rid) {
			var row = TablesStream.ReadTypeSpecRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		protected override byte[] ResolveMethodSpecSignature(uint rid) {
			var row = TablesStream.ReadMethodSpecRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Instantiation);
		}

		protected override string ResolveStringCore(uint offset) => Metadata.USStream.Read(offset);

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			machine = (DmdImageFileMachine)Metadata.PEImage.ImageNTHeaders.FileHeader.Machine;
			peKind = 0;
			if ((Metadata.ImageCor20Header.Flags & ComImageFlags.ILOnly) != 0)
				peKind |= DmdPortableExecutableKinds.ILOnly;
			if (Metadata.PEImage.ImageNTHeaders.OptionalHeader.Magic != 0x010B)
				peKind |= DmdPortableExecutableKinds.PE32Plus;
			if ((Metadata.ImageCor20Header.Flags & ComImageFlags._32BitRequired) != 0)
				peKind |= DmdPortableExecutableKinds.Required32Bit;
			if ((Metadata.ImageCor20Header.Flags & ComImageFlags._32BitPreferred) != 0)
				peKind |= DmdPortableExecutableKinds.Preferred32Bit;
		}

		public override DmdAssemblyName GetName() {
			var name = new DmdAssemblyName();
			var row = TablesStream.ReadAssemblyRow(1);
			if (row == null) {
				name.Name = "no-asm-" + Guid.NewGuid().ToString();
				return name;
			}

			name.Version = new Version(row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber);
			name.Name = StringsStream.ReadNoNull(row.Name);
			name.CultureName = StringsStream.ReadNoNull(row.Locale);
			name.HashAlgorithm = (DmdAssemblyHashAlgorithm)row.HashAlgId;
			name.SetPublicKey(BlobStream.ReadNoNull(row.PublicKey));
			name.Flags = (DmdAssemblyNameFlags)row.Flags;
			return name;
		}

		public override DmdAssemblyName[] GetReferencedAssemblies() {
			var tbl = TablesStream.AssemblyRefTable;
			if (tbl.Rows == 0)
				return Array.Empty<DmdAssemblyName>();
			var res = new DmdAssemblyName[tbl.Rows];
			for (int i = 0; i < res.Length; i++)
				res[i] = ReadAssemblyName((uint)i + 1);
			return res;
		}

		internal DmdAssemblyName ReadAssemblyName(uint rid) {
			var asmName = new DmdAssemblyName();
			var row = TablesStream.ReadAssemblyRefRow(rid) ?? new RawAssemblyRefRow();
			asmName.Name = Metadata.StringsStream.ReadNoNull(row.Name);
			asmName.CultureName = Metadata.StringsStream.ReadNoNull(row.Locale);
			asmName.Version = new Version(row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber);
			if (row.PublicKeyOrToken != 0) {
				var bytes = Metadata.BlobStream.ReadNoNull(row.PublicKeyOrToken);
				if ((row.Flags & (int)DmdAssemblyNameFlags.PublicKey) != 0)
					asmName.SetPublicKey(bytes);
				else
					asmName.SetPublicKeyToken(bytes);
			}
			asmName.Flags = (DmdAssemblyNameFlags)row.Flags;
			return asmName;
		}
	}
}
