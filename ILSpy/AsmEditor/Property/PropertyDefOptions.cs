/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace ICSharpCode.ILSpy.AsmEditor.Property
{
	sealed class PropertyDefOptions
	{
		public PropertyAttributes Attributes;
		public UTF8String Name;
		public PropertySig PropertySig;
		public Constant Constant;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public PropertyDefOptions()
		{
		}

		public PropertyDefOptions(PropertyDef evt)
		{
			this.Attributes = evt.Attributes;
			this.Name = evt.Name;
			this.PropertySig = evt.PropertySig;
			this.Constant = evt.Constant;
			this.CustomAttributes.AddRange(evt.CustomAttributes);
		}

		public PropertyDef CopyTo(PropertyDef evt)
		{
			evt.Attributes = this.Attributes;
			evt.Name = this.Name;
			evt.PropertySig = this.PropertySig;
			evt.Constant = this.Constant;
			evt.CustomAttributes.Clear();
			evt.CustomAttributes.AddRange(CustomAttributes);
			return evt;
		}

		public PropertyDef CreatePropertyDef(ModuleDef ownerModule)
		{
			return ownerModule.UpdateRowId(CopyTo(new PropertyDefUser()));
		}

		public static PropertyDefOptions Create(ModuleDef module, UTF8String name, bool isInstance)
		{
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
