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
	sealed class DmdGenericParameterTypeImpl : DmdGenericParameterType {
		public override DmdAppDomain AppDomain { get; }

		public DmdGenericParameterTypeImpl(DmdAppDomain appDomain, DmdTypeBase declaringType, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers) : base(0, declaringType, name, position, attributes, customModifiers) =>
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));

		public DmdGenericParameterTypeImpl(DmdAppDomain appDomain, DmdMethodBase declaringMethod, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers) : base(0, declaringMethod, name, position, attributes, customModifiers) =>
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));

		protected override DmdType[] CreateGenericParameterConstraints() => Array.Empty<DmdType>();

		DmdGenericParameterTypeImpl Clone(IList<DmdCustomModifier> customModifiers) =>
			(object)DeclaringMethod != null ?
			new DmdGenericParameterTypeImpl(AppDomain, DeclaringMethod, MetadataName, GenericParameterPosition, GenericParameterAttributes, customModifiers) :
			new DmdGenericParameterTypeImpl(AppDomain, (DmdTypeBase)DeclaringType, MetadataName, GenericParameterPosition, GenericParameterAttributes, customModifiers);

		// Don't intern these since only the generic parameter position is checked and not the decl type / method
		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => Clone(VerifyCustomModifiers(customModifiers));
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : Clone(null);
	}
}
