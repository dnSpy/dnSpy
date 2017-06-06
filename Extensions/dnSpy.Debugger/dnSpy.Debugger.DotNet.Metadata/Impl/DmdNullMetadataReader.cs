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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdNullMetadataReader : DmdMetadataReader {
		public override Guid ModuleVersionId => Guid.Empty;
		public override int MDStreamVersion => 0x00020000;
		public override string ModuleScopeName => string.Empty;
		public override string ImageRuntimeVersion => string.Empty;
		public override DmdMethodInfo EntryPoint => null;

		readonly DmdType globalType;
		public DmdNullMetadataReader(DmdModule module) => globalType = new DmdNullGlobalType(module);

		public override DmdType[] GetTypes() => new[] { globalType };
		internal override DmdType ResolveTypeRef(uint rid) => null;
		internal override DmdType ResolveTypeDef(uint rid) => rid == 1 ? globalType : null;
		internal override DmdFieldInfo ResolveFieldDef(uint rid) => null;
		internal override DmdMethodBase ResolveMethodDef(uint rid) => null;
		internal override DmdMemberInfo ResolveMemberRef(uint rid, DmdType[] genericTypeArguments) => null;
		internal override DmdEventInfo ResolveEventDef(uint rid) => null;
		internal override DmdPropertyInfo ResolvePropertyDef(uint rid) => null;
		internal override DmdType ResolveTypeSpec(uint rid, DmdType[] genericTypeArguments) => null;
		internal override DmdMethodBase ResolveMethodSpec(uint rid, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => null;
		internal override byte[] ResolveFieldSignature(uint rid) => null;
		internal override byte[] ResolveMethodSignature(uint rid) => null;
		internal override byte[] ResolveMemberRefSignature(uint rid) => null;
		internal override byte[] ResolveStandAloneSigSignature(uint rid) => null;
		internal override byte[] ResolveTypeSpecSignature(uint rid) => null;
		internal override byte[] ResolveMethodSpecSignature(uint rid) => null;
		internal override string ResolveStringCore(uint offset) => null;
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			peKind = DmdPortableExecutableKinds.ILOnly;
			machine = DmdImageFileMachine.I386;
		}
		public override DmdAssemblyName GetName() => new DmdAssemblyName("NullAssembly");
		public override DmdType[] GetExportedTypes() => Array.Empty<DmdType>();
		public override DmdAssemblyName[] GetReferencedAssemblies() => Array.Empty<DmdAssemblyName>();
	}
}
