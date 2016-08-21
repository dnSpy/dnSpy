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
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Property {
	sealed class PropertyDefOptions {
		public PropertyAttributes Attributes;
		public UTF8String Name;
		public PropertySig PropertySig;
		public Constant Constant;
		public List<MethodDef> GetMethods = new List<MethodDef>();
		public List<MethodDef> SetMethods = new List<MethodDef>();
		public List<MethodDef> OtherMethods = new List<MethodDef>();
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public PropertyDefOptions() {
		}

		public PropertyDefOptions(PropertyDef prop) {
			this.Attributes = prop.Attributes;
			this.Name = prop.Name;
			this.PropertySig = prop.PropertySig;
			this.Constant = prop.Constant;
			this.GetMethods.AddRange(prop.GetMethods);
			this.SetMethods.AddRange(prop.SetMethods);
			this.OtherMethods.AddRange(prop.OtherMethods);
			this.CustomAttributes.AddRange(prop.CustomAttributes);
		}

		public PropertyDef CopyTo(PropertyDef prop) {
			prop.Attributes = this.Attributes;
			prop.Name = this.Name ?? UTF8String.Empty;
			prop.PropertySig = this.PropertySig;
			prop.Constant = this.Constant;
			prop.GetMethods.Clear();
			prop.GetMethods.AddRange(this.GetMethods);
			prop.SetMethods.Clear();
			prop.SetMethods.AddRange(this.SetMethods);
			prop.OtherMethods.Clear();
			prop.OtherMethods.AddRange(this.OtherMethods);
			prop.CustomAttributes.Clear();
			prop.CustomAttributes.AddRange(CustomAttributes);
			return prop;
		}

		public PropertyDef CreatePropertyDef(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new PropertyDefUser()));

		public static PropertyDefOptions Create(ModuleDef module, UTF8String name, bool isInstance) {
			return new PropertyDefOptions {
				Attributes = 0,
				Name = name,
				PropertySig = isInstance ?
								PropertySig.CreateInstance(module.CorLibTypes.Int32) :
								PropertySig.CreateStatic(module.CorLibTypes.Int32),
				Constant = null,
			};
		}
	}
}
