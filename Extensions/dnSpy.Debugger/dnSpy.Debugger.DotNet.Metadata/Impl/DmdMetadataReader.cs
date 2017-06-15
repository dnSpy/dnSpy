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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdMetadataReader {
		public abstract Guid ModuleVersionId { get; }
		public abstract int MDStreamVersion { get; }
		public abstract string ModuleScopeName { get; }
		public abstract string ImageRuntimeVersion { get; }
		public abstract DmdMethodInfo EntryPoint { get; }
		public abstract DmdType[] GetTypes();
		public abstract DmdType[] GetExportedTypes();
		public abstract DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);
		public abstract DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);
		public abstract DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);
		public abstract DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);
		public abstract byte[] ResolveSignature(int metadataToken);
		public abstract string ResolveString(int metadataToken);
		public abstract void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine);
		public abstract DmdAssemblyName GetName();
		public abstract DmdAssemblyName[] GetReferencedAssemblies();
		public abstract DmdCustomAttributeData[] ReadCustomAttributes(int metadataToken);
		public abstract DmdType GetNonNestedType(string @namespace, string name, bool ignoreCase);
	}
}
