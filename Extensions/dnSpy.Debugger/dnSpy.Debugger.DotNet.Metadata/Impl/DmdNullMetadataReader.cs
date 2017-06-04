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
		public override int ModuleMetadataToken => 0x00000001;
		public override DmdType GlobalType { get; }
		public override int MDStreamVersion => 0x00020000;
		public override string ModuleScopeName => string.Empty;
		public override string ImageRuntimeVersion => string.Empty;
		public override DmdMethodInfo EntryPoint => null;

		public DmdNullMetadataReader(DmdModule module) => GlobalType = new DmdNullType(module);

		public override DmdType[] GetTypes() => new[] { GlobalType };
		public override DmdMethodBase ResolveMethod(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => null;
		public override DmdFieldInfo ResolveField(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => null;
		public override DmdType ResolveType(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => metadataToken == GlobalType.MetadataToken ? GlobalType : null;
		public override DmdMemberInfo ResolveMember(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => metadataToken == GlobalType.MetadataToken ? GlobalType : null;
		public override byte[] ResolveSignature(int metadataToken) => null;
		public override string ResolveString(int metadataToken) => null;
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			peKind = DmdPortableExecutableKinds.ILOnly;
			machine = DmdImageFileMachine.I386;
		}
		public override DmdAssemblyName GetName() => new DmdAssemblyName("NullAssembly");
		public override DmdType[] GetExportedTypes() => Array.Empty<DmdType>();
		public override DmdAssemblyName[] GetReferencedAssemblies() => Array.Empty<DmdAssemblyName>();
	}
}
