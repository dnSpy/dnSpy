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

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.AsmEditor.Method {
	sealed class MethodDefOptions {
		public MethodImplAttributes ImplAttributes;
		public MethodAttributes Attributes;
		public MethodSemanticsAttributes SemanticsAttributes;
		public RVA RVA;
		public UTF8String Name;
		public MethodSig MethodSig;
		public ImplMap ImplMap;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
		public List<DeclSecurity> DeclSecurities = new List<DeclSecurity>();
		public List<ParamDef> ParamDefs = new List<ParamDef>();
		public List<GenericParam> GenericParameters = new List<GenericParam>();
		public List<MethodOverride> Overrides = new List<MethodOverride>();

		public MethodDefOptions() {
		}

		public MethodDefOptions(MethodDef method) {
			ImplAttributes = method.ImplAttributes;
			Attributes = method.Attributes;
			SemanticsAttributes = method.SemanticsAttributes;
			RVA = method.RVA;
			Name = method.Name;
			MethodSig = method.MethodSig;
			ImplMap = method.ImplMap;
			CustomAttributes.AddRange(method.CustomAttributes);
			DeclSecurities.AddRange(method.DeclSecurities);
			ParamDefs.AddRange(method.ParamDefs);
			GenericParameters.AddRange(method.GenericParameters);
			Overrides.AddRange(method.Overrides);
		}

		public MethodDef CopyTo(MethodDef method) {
			method.ImplAttributes = ImplAttributes;
			method.Attributes = Attributes;
			method.SemanticsAttributes = SemanticsAttributes;
			method.RVA = RVA;
			method.Name = Name ?? UTF8String.Empty;
			method.MethodSig = MethodSig;
			method.ImplMap = ImplMap;
			method.CustomAttributes.Clear();
			method.CustomAttributes.AddRange(CustomAttributes);
			method.DeclSecurities.Clear();
			method.DeclSecurities.AddRange(DeclSecurities);
			method.ParamDefs.Clear();
			method.ParamDefs.AddRange(ParamDefs);
			method.GenericParameters.Clear();
			method.GenericParameters.AddRange(GenericParameters);
			method.Overrides.Clear();
			method.Overrides.AddRange(Overrides.Select(e => e.MethodBody != null ? e : new MethodOverride(method, e.MethodDeclaration)));
			method.Parameters.UpdateParameterTypes();
			return method;
		}

		public MethodDef CreateMethodDef(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new MethodDefUser()));

		public static MethodDefOptions Create(UTF8String name, MethodSig methodSig) => new MethodDefOptions {
			ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
			Attributes = MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig | (methodSig.HasThis ? 0 : MethodAttributes.Static),
			Name = name,
			MethodSig = methodSig,
			ImplMap = null,
		};
	}
}
