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
		public override int MDStreamVersion => ((metadata.TablesStream.Version & 0xFF00) << 8) | (metadata.TablesStream.Version & 0xFF);
		public override string ModuleScopeName { get; }
		public override string ImageRuntimeVersion => metadata.VersionString;
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

		readonly DmdModuleImpl module;
		readonly IMetaData metadata;

		DmdEcma335MetadataReader(DmdModuleImpl module, IMetaData metadata) {
			this.module = module;
			this.metadata = metadata;

			var row = metadata.TablesStream.ReadModuleRow(1);
			ModuleScopeName = metadata.StringsStream.ReadNoNull(row.Name);
			ModuleVersionId = metadata.GuidStream.Read(row.Mvid) ?? Guid.Empty;
		}

		public override DmdType[] GetTypes() => throw new NotImplementedException();//TODO:

		internal override DmdType ResolveTypeRef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdType ResolveTypeDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdFieldInfo ResolveFieldDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdMethodBase ResolveMethodDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdMemberInfo ResolveMemberRef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdEventInfo ResolveEventDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdPropertyInfo ResolvePropertyDef(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdType ResolveTypeSpec(uint rid) => throw new NotImplementedException();//TODO:
		internal override DmdMethodBase ResolveMethodSpec(uint rid) => throw new NotImplementedException();//TODO:

		internal override byte[] ResolveFieldSignature(uint rid) => throw new NotImplementedException();//TODO:
		internal override byte[] ResolveMethodSignature(uint rid) => throw new NotImplementedException();//TODO:
		internal override byte[] ResolveMemberRefSignature(uint rid) => throw new NotImplementedException();//TODO:
		internal override byte[] ResolveStandAloneSigSignature(uint rid) => throw new NotImplementedException();//TODO:
		internal override byte[] ResolveTypeSpecSignature(uint rid) => throw new NotImplementedException();//TODO:
		internal override byte[] ResolveMethodSpecSignature(uint rid) => throw new NotImplementedException();//TODO:

		internal override string ResolveStringCore(uint offset) => metadata.USStream.ReadNoNull(offset);

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			machine = (DmdImageFileMachine)metadata.PEImage.ImageNTHeaders.FileHeader.Machine;
			peKind = 0;
			if ((metadata.ImageCor20Header.Flags & ComImageFlags.ILOnly) != 0)
				peKind |= DmdPortableExecutableKinds.ILOnly;
			if (metadata.PEImage.ImageNTHeaders.OptionalHeader.Magic != 0x010B)
				peKind |= DmdPortableExecutableKinds.PE32Plus;
			if ((metadata.ImageCor20Header.Flags & ComImageFlags._32BitRequired) != 0)
				peKind |= DmdPortableExecutableKinds.Required32Bit;
			if ((metadata.ImageCor20Header.Flags & ComImageFlags._32BitPreferred) != 0)
				peKind |= DmdPortableExecutableKinds.Preferred32Bit;
		}

		public override DmdAssemblyName GetName() => throw new NotImplementedException();//TODO:
		public override DmdType[] GetExportedTypes() => throw new NotImplementedException();//TODO:

		public override DmdAssemblyName[] GetReferencedAssemblies() {
			var tbl = metadata.TablesStream.AssemblyRefTable;
			if (tbl.Rows == 0)
				return Array.Empty<DmdAssemblyName>();
			var res = new DmdAssemblyName[tbl.Rows];
			for (int i = 0; i < res.Length; i++) {
				var row = metadata.TablesStream.ReadAssemblyRefRow((uint)i + 1);
				var asmName = new DmdAssemblyName();
				asmName.Name = metadata.StringsStream.ReadNoNull(row.Name);
				asmName.CultureName = metadata.StringsStream.ReadNoNull(row.Locale);
				asmName.Version = new Version(row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber);
				if (row.PublicKeyOrToken != 0) {
					var bytes = metadata.BlobStream.ReadNoNull(row.PublicKeyOrToken);
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
