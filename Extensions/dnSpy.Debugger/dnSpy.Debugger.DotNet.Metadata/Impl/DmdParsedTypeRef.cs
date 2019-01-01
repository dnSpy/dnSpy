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
	sealed class DmdParsedTypeRef : DmdTypeRef {
		public override DmdTypeScope TypeScope => typeScope;
		public override string MetadataNamespace { get; }
		public override string MetadataName { get; }

		public override DmdTypeRef DeclaringTypeRef => declaringTypeRef;
		readonly DmdParsedTypeRef declaringTypeRef;
		DmdTypeScope typeScope;

		public DmdParsedTypeRef(DmdModule ownerModule, DmdParsedTypeRef declaringTypeRef, DmdTypeScope typeScope, string @namespace, string name, IList<DmdCustomModifier> customModifiers) : base(ownerModule, 0, customModifiers) {
			this.typeScope = typeScope;
			this.declaringTypeRef = declaringTypeRef;
			MetadataNamespace = @namespace;
			MetadataName = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal void SetTypeScope(DmdTypeScope typeScope) => this.typeScope = typeScope;

		protected override int GetDeclaringTypeRefToken() => throw new NotSupportedException();
		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => new DmdParsedTypeRef(OwnerModule, declaringTypeRef, TypeScope, MetadataNamespace, MetadataName, VerifyCustomModifiers(customModifiers));
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : new DmdParsedTypeRef(OwnerModule, declaringTypeRef, TypeScope, MetadataNamespace, MetadataName, null);
	}
}
