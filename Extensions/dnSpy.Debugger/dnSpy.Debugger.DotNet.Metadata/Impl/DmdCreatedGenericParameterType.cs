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
	sealed class DmdCreatedGenericParameterType : DmdGenericParameterType {
		public override DmdAppDomain AppDomain => module.AppDomain;
		public override DmdTypeSignatureKind TypeSignatureKind => isGenericTypeParameter ? DmdTypeSignatureKind.TypeGenericParameter : DmdTypeSignatureKind.MethodGenericParameter;
		public override DmdModule Module => module;

		readonly DmdModule module;
		readonly bool isGenericTypeParameter;

		public DmdCreatedGenericParameterType(DmdModule module, bool isGenericTypeParameter, int position, IList<DmdCustomModifier>? customModifiers) : base(position, customModifiers) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			this.isGenericTypeParameter = isGenericTypeParameter;
		}

		protected override DmdType[]? CreateGenericParameterConstraints() => Array.Empty<DmdType>();
		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => new DmdCreatedGenericParameterType(module, isGenericTypeParameter, GenericParameterPosition, customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : new DmdCreatedGenericParameterType(module, isGenericTypeParameter, GenericParameterPosition, null);
	}
}
