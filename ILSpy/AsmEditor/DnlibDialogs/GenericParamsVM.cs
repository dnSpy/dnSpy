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
	sealed class GenericParamsVM : ListVM<GenericParamVM, GenericParam>
	{
		public GenericParamsVM(ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod)
			: base("Edit Generic Parameter", "Create Generic Parameter", ownerModule, language, ownerType, ownerMethod)
		{
		}

		protected override GenericParamVM Create(GenericParam model)
		{
			return new GenericParamVM(new GenericParamOptions(model), ownerModule, language, ownerType, ownerMethod);
		}

		protected override GenericParamVM Clone(GenericParamVM obj)
		{
			return new GenericParamVM(obj.CreateGenericParamOptions(), ownerModule, language, ownerType, ownerMethod);
		}

		protected override GenericParamVM Create()
		{
			return new GenericParamVM(new GenericParamOptions(), ownerModule, language, ownerType, ownerMethod);
		}

		protected override int GetAddIndex(GenericParamVM obj)
		{
			ushort number = obj.Number.Value;
			for (int i = 0; i < Collection.Count; i++) {
				if (number < Collection[i].Number.Value)
					return i;
			}
			return Collection.Count;
		}
	}
}
