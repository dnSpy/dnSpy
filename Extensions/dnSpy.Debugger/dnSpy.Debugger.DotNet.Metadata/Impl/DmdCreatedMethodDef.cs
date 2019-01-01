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
	sealed class DmdCreatedMethodDef : DmdMethodDef {
		public override DmdSpecialMethodKind SpecialMethodKind { get; }
		public override string Name { get; }
		public override DmdMethodImplAttributes MethodImplementationFlags => DmdMethodImplAttributes.IL | DmdMethodImplAttributes.Managed;
		public override DmdMethodAttributes Attributes => DmdMethodAttributes.Public | DmdMethodAttributes.ReuseSlot;

		readonly DmdMethodSignature methodSignature;

		public DmdCreatedMethodDef(DmdSpecialMethodKind specialMethodKind, string name, DmdMethodSignature methodSignature, DmdType declaringType, DmdType reflectedType) : base(0, declaringType, reflectedType) {
			SpecialMethodKind = specialMethodKind;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			this.methodSignature = methodSignature ?? throw new ArgumentNullException(nameof(methodSignature));
		}

		protected override (DmdParameterInfo returnParameter, DmdParameterInfo[] parameters) CreateParameters() {
			var returnParameter = new DmdCreatedParameterDef(this, -1, methodSignature.ReturnType);
			var parameterTypes = methodSignature.GetParameterTypes();
			var parameters = parameterTypes.Count == 0 ? Array.Empty<DmdParameterInfo>() : new DmdParameterInfo[parameterTypes.Count];
			for (int i = 0; i < parameters.Length; i++)
				parameters[i] = new DmdCreatedParameterDef(this, i, parameterTypes[i]);
			return (returnParameter, parameters);
		}

		protected override DmdType[] CreateGenericParameters() => null;
		protected override (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas, DmdImplMap? implMap) CreateCustomAttributes() => (null, null, null);
		public override DmdMethodBody GetMethodBody() => null;
		internal override DmdMethodBody GetMethodBody(IList<DmdType> genericMethodArguments) => null;
		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		protected override uint GetRVA() => 0;
	}
}
