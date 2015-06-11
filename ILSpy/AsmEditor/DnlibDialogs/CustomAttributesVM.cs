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

using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class CustomAttributesVM : ListVM<CustomAttributeVM, CustomAttribute>
	{
		public CustomAttributesVM(ModuleDef ownerModule, Language language, TypeDef ownerType = null, MethodDef ownerMethod = null)
			: base("Edit Custom Attribute", "Create Custom Attribute", ownerModule, language, ownerType, ownerMethod)
		{
		}

		protected override CustomAttributeVM Create(CustomAttribute model)
		{
			return new CustomAttributeVM(new CustomAttributeOptions(model), ownerModule, language, ownerType, ownerMethod);
		}

		protected override CustomAttributeVM Clone(CustomAttributeVM obj)
		{
			return new CustomAttributeVM(obj.CreateCustomAttributeOptions(), ownerModule, language, ownerType, ownerMethod);
		}

		protected override CustomAttributeVM Create()
		{
			return new CustomAttributeVM(new CustomAttributeOptions(), ownerModule, language, ownerType, ownerMethod);
		}
	}
}
