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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdGenericParameterTypeCOMD : DmdGenericParameterType {
		public override DmdAppDomain AppDomain => reader.Module.AppDomain;

		readonly DmdComMetadataReader reader;

		public DmdGenericParameterTypeCOMD(DmdComMetadataReader reader, uint rid, DmdTypeBase declaringType, string? name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers)
			: base(rid, declaringType, name, position, attributes, customModifiers) =>
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

		public DmdGenericParameterTypeCOMD(DmdComMetadataReader reader, uint rid, DmdMethodBase declaringMethod, string? name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers)
			: base(rid, declaringMethod, name, position, attributes, customModifiers) =>
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		protected override DmdType[]? CreateGenericParameterConstraints() => COMThread(CreateGenericParameterConstraints_COMThread);
		DmdType[]? CreateGenericParameterConstraints_COMThread() {
			reader.Dispatcher.VerifyAccess();
			var tokens = MDAPI.GetGenericParamConstraintTokens(reader.MetaDataImport, 0x2A000000 + Rid);
			if (tokens.Length == 0)
				return null;

			IList<DmdType> genericTypeArguments;
			IList<DmdType>? genericMethodArguments;
			if (!(DeclaringMethod is null)) {
				genericTypeArguments = DeclaringMethod.DeclaringType!.GetGenericArguments();
				genericMethodArguments = DeclaringMethod.GetGenericArguments();
			}
			else {
				genericTypeArguments = DeclaringType!.GetGenericArguments();
				genericMethodArguments = null;
			}

			var gpcList = new DmdType[tokens.Length];
			for (int i = 0; i < tokens.Length; i++) {
				var token = MDAPI.GetGenericParamConstraintTypeToken(reader.MetaDataImport, tokens[i]);
				var type = Module.ResolveType((int)token, genericTypeArguments, genericMethodArguments, DmdResolveOptions.None);
				if (type is null)
					return null;
				gpcList[i] = type;
			}
			return gpcList;
		}

		DmdGenericParameterTypeCOMD Clone(IList<DmdCustomModifier>? customModifiers) =>
			!(DeclaringMethod is null) ?
			new DmdGenericParameterTypeCOMD(reader, Rid, DeclaringMethod, MetadataName, GenericParameterPosition, GenericParameterAttributes, customModifiers) :
			new DmdGenericParameterTypeCOMD(reader, Rid, (DmdTypeBase)DeclaringType!, MetadataName, GenericParameterPosition, GenericParameterAttributes, customModifiers);

		// Don't intern these since only the generic parameter position is checked and not the decl type / method
		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => Clone(VerifyCustomModifiers(customModifiers));
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : Clone(null);
	}
}
