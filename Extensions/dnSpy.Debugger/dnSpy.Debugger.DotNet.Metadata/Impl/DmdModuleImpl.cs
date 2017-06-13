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
using System.Collections.ObjectModel;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdModuleImpl : DmdModule {
		internal sealed override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override DmdAppDomain AppDomain => Assembly.AppDomain;
		public override string FullyQualifiedName { get; }
		public override DmdAssembly Assembly => assembly;
		public override bool IsDynamic { get; }
		public override bool IsInMemory { get; }
		public override Guid ModuleVersionId => metadataReader.ModuleVersionId;
		public override int MetadataToken => 0x00000001;
		public override DmdType GlobalType => ResolveType(0x02000001);
		public override int MDStreamVersion => metadataReader.MDStreamVersion;
		public override string ScopeName => scopeNameOverride ?? metadataReader.ModuleScopeName;

		readonly DmdAssemblyImpl assembly;
		readonly DmdMetadataReader metadataReader;
		string scopeNameOverride;

		public DmdModuleImpl(DmdAssemblyImpl assembly, DmdMetadataReader metadataReader, bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			this.metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
			FullyQualifiedName = fullyQualifiedName ?? throw new ArgumentNullException(nameof(fullyQualifiedName));
			IsDynamic = isDynamic;
			IsInMemory = isInMemory;
		}

		internal void SetScopeName(string scopeName) => scopeNameOverride = scopeName;

		public override DmdType[] GetTypes() => metadataReader.GetTypes();
		public override DmdType[] GetExportedTypes() => metadataReader.GetExportedTypes();
		public override DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override byte[] ResolveSignature(int metadataToken) => metadataReader.ResolveSignature(metadataToken);
		public override string ResolveString(int metadataToken) => metadataReader.ResolveString(metadataToken);
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) => metadataReader.GetPEKind(out peKind, out machine);
		public override DmdType GetType(string className, bool throwOnError, bool ignoreCase) => throw new NotImplementedException();//TODO:

		public override IList<DmdCustomAttributeData> GetCustomAttributesData() {
			if (customAttributes != null)
				return customAttributes;
			lock (LockObject) {
				if (customAttributes != null)
					return customAttributes;
				var cas = metadataReader.ReadCustomAttributes(0x00000001);
				customAttributes = CustomAttributesHelper.AddPseudoCustomAttributes(this, cas);
				return customAttributes;
			}
		}
		ReadOnlyCollection<DmdCustomAttributeData> customAttributes;
	}
}
