/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using dnlib.DotNet;
using Microsoft.CodeAnalysis;

namespace dnSpy.AsmEditor.Compile.Roslyn {
	sealed class RoslynMetadataReferenceResolver : MetadataReferenceResolver {
		readonly object lockObj = new object();
		readonly IAssemblyReferenceResolver assemblyReferenceResolver;
		readonly Dictionary<IAssembly, PortableExecutableReference> asmRefToPeRef;

		public RoslynMetadataReferenceResolver(IAssemblyReferenceResolver assemblyReferenceResolver) {
			this.assemblyReferenceResolver = assemblyReferenceResolver;
			this.asmRefToPeRef = new Dictionary<IAssembly, PortableExecutableReference>();
		}

		public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties) =>
			default(ImmutableArray<PortableExecutableReference>);

		public override bool ResolveMissingAssemblies => true;

		static AssemblyRef ToAssemblyRef(AssemblyIdentity referenceIdentity) {
			return new AssemblyRefUser(referenceIdentity.Name, referenceIdentity.Version, ToPublicKeyBase(referenceIdentity), referenceIdentity.CultureName) {
				Attributes = (AssemblyAttributes)referenceIdentity.Flags,
			};
		}

		static PublicKeyBase ToPublicKeyBase(AssemblyIdentity referenceIdentity) {
			if (referenceIdentity.HasPublicKey)
				return new PublicKey(referenceIdentity.PublicKey.ToArray());
			if (!referenceIdentity.PublicKeyToken.IsDefault)
				return new PublicKeyToken(referenceIdentity.PublicKeyToken.ToArray());
			return null;
		}

		public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity) {
			var asmRef = ToAssemblyRef(referenceIdentity);
			lock (lockObj) {
				PortableExecutableReference peRef;
				if (asmRefToPeRef.TryGetValue(asmRef, out peRef))
					return peRef;

				var mdRef = assemblyReferenceResolver.Resolve(asmRef);
				if (mdRef != null)
					return AddReference(mdRef.Value);
			}

			return base.ResolveMissingAssembly(definition, referenceIdentity);
		}

		PortableExecutableReference AddReference(CompilerMetadataReference mdRef) {
			var peRef = MetadataReference.CreateFromImage(mdRef.Data,
				mdRef.IsAssemblyReference ? MetadataReferenceProperties.Assembly : MetadataReferenceProperties.Module);
			var asmRef = mdRef.Assembly;
			if (asmRef != null) {
				lock (lockObj)
					asmRefToPeRef[asmRef] = peRef;
			}
			return peRef;
		}

		public override bool Equals(object other) => this == other;
		public override int GetHashCode() => obj.GetHashCode();
		readonly object obj = new object();
	}
}
