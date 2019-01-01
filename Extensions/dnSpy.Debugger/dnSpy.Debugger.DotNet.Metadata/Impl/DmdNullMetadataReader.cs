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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdNullMetadataReader : DmdMetadataReaderBase {
		public override Guid ModuleVersionId => Guid.Empty;
		public override int MDStreamVersion => 0x00020000;
		public override string ModuleScopeName => string.Empty;
		public override string ImageRuntimeVersion => string.Empty;
		public override DmdMethodInfo EntryPoint => null;

		readonly DmdTypeDef globalType;
		public DmdNullMetadataReader(DmdModule module) => globalType = new DmdNullGlobalType(module, null);

		public override DmdTypeDef[] GetTypes() => new[] { globalType };
		public override DmdTypeRef[] GetExportedTypes() => Array.Empty<DmdTypeRef>();
		protected override DmdTypeRef ResolveTypeRef(uint rid) => null;
		protected override DmdTypeDef ResolveTypeDef(uint rid) => rid == 1 ? globalType : null;
		protected override DmdFieldDef ResolveFieldDef(uint rid) => null;
		protected override DmdMethodBase ResolveMethodDef(uint rid) => null;
		protected override DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => null;
		protected override DmdEventDef ResolveEventDef(uint rid) => null;
		protected override DmdPropertyDef ResolvePropertyDef(uint rid) => null;
		protected override DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => null;
		protected override DmdTypeRef ResolveExportedType(uint rid) => null;
		protected override DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => null;
		protected override DmdMethodSignature ResolveMethodSignature(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => null;
		protected override byte[] ResolveFieldSignature(uint rid) => null;
		protected override byte[] ResolveMethodSignature(uint rid) => null;
		protected override byte[] ResolveMemberRefSignature(uint rid) => null;
		protected override byte[] ResolveStandAloneSigSignature(uint rid) => null;
		protected override byte[] ResolveTypeSpecSignature(uint rid) => null;
		protected override byte[] ResolveMethodSpecSignature(uint rid) => null;
		protected override string ResolveStringCore(uint offset) => null;
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			peKind = DmdPortableExecutableKinds.ILOnly;
			machine = DmdImageFileMachine.I386;
		}
		public override DmdReadOnlyAssemblyName GetName() => new DmdReadOnlyAssemblyName("NullAssembly");
		public override DmdReadOnlyAssemblyName[] GetReferencedAssemblies() => Array.Empty<DmdReadOnlyAssemblyName>();
		public override unsafe bool ReadMemory(uint rva, void* destination, int size) => false;
		protected override DmdCustomAttributeData[] ReadAssemblyCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadModuleCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadTypeDefCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadFieldCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadMethodCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadParamCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadEventCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadPropertyCustomAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadAssemblySecurityAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadTypeDefSecurityAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
		protected override DmdCustomAttributeData[] ReadMethodSecurityAttributes(uint rid) => Array.Empty<DmdCustomAttributeData>();
	}
}
