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
using System.IO;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdEcma335MetadataReader : DmdMetadataReader {
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
		readonly LazyList<DmdType> typeDefList;

		DmdEcma335MetadataReader(DmdModuleImpl module, IMetaData metadata) {
			this.module = module;
			Metadata = metadata;

			var row = TablesStream.ReadModuleRow(1);
			ModuleScopeName = metadata.StringsStream.ReadNoNull(row?.Name ?? 0);
			ModuleVersionId = metadata.GuidStream.Read(row?.Mvid ?? 0) ?? Guid.Empty;

			var ts = TablesStream;
			typeDefList = new LazyList<DmdType>(ts.TypeDefTable.Rows, rid => new DmdTypeDefMD(this, rid));
		}

		public override DmdType[] GetTypes() {
			var result = new DmdType[TablesStream.TypeDefTable.Rows];
			for (int i = 0; i < result.Length; i++) {
				var type = ResolveTypeDef((uint)i + 1);
				result[i] = type ?? throw new InvalidOperationException();
			}
			return result;
		}

		internal override DmdType ResolveTypeRef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdType ResolveTypeDef(uint rid) => typeDefList[rid - 1];
		internal override DmdFieldInfo ResolveFieldDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdMethodBase ResolveMethodDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdMemberInfo ResolveMemberRef(uint rid, DmdType[] genericTypeArguments) => throw new NotImplementedException();//TODO:
		internal override DmdEventInfo ResolveEventDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdPropertyInfo ResolvePropertyDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdType ResolveTypeSpec(uint rid, DmdType[] genericTypeArguments) => throw new NotImplementedException();//TODO:
		internal override DmdMethodBase ResolveMethodSpec(uint rid, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => throw new NotImplementedException();//TODO:

		internal override byte[] ResolveFieldSignature(uint rid) {
			var row = TablesStream.ReadFieldRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		internal override byte[] ResolveMethodSignature(uint rid) {
			var row = TablesStream.ReadMethodRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		internal override byte[] ResolveMemberRefSignature(uint rid) {
			var row = TablesStream.ReadMemberRefRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		internal override byte[] ResolveStandAloneSigSignature(uint rid) {
			var row = TablesStream.ReadStandAloneSigRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		internal override byte[] ResolveTypeSpecSignature(uint rid) {
			var row = TablesStream.ReadTypeSpecRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Signature);
		}

		internal override byte[] ResolveMethodSpecSignature(uint rid) {
			var row = TablesStream.ReadMethodSpecRow(rid);
			if (row == null)
				return null;
			return Metadata.BlobStream.Read(row.Instantiation);
		}

		internal override string ResolveStringCore(uint offset) => Metadata.USStream.Read(offset);

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

		public override DmdAssemblyName GetName() => throw new NotImplementedException();//TODO:
		public override DmdType[] GetExportedTypes() => throw new NotImplementedException();//TODO:

		public override DmdAssemblyName[] GetReferencedAssemblies() {
			var tbl = TablesStream.AssemblyRefTable;
			if (tbl.Rows == 0)
				return Array.Empty<DmdAssemblyName>();
			var res = new DmdAssemblyName[tbl.Rows];
			for (int i = 0; i < res.Length; i++) {
				var row = TablesStream.ReadAssemblyRefRow((uint)i + 1);
				var asmName = new DmdAssemblyName();
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
				res[i] = asmName;
			}
			return res;
		}
	}
}
