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

using System;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class CANamedArgumentsVM : ListVM<CANamedArgumentVM, CANamedArgument>
	{
		readonly Predicate<CANamedArgumentsVM> canAdd;

		public CANamedArgumentsVM(ModuleDef module, Language language, TypeDef ownerType, MethodDef ownerMethod, Predicate<CANamedArgumentsVM> canAdd)
			: base(null, null, module, language, ownerType, ownerMethod, true)
		{
			this.canAdd = canAdd;
		}

		protected override CANamedArgumentVM Create(CANamedArgument model)
		{
			return new CANamedArgumentVM(model, new TypeSigCreatorOptions(module, language));
		}

		protected override CANamedArgumentVM Clone(CANamedArgumentVM obj)
		{
			return new CANamedArgumentVM(obj.CreateCANamedArgument(), new TypeSigCreatorOptions(module, language));
		}

		protected override CANamedArgumentVM Create()
		{
			return new CANamedArgumentVM(new CANamedArgument(false, module.CorLibTypes.Int32, "AttributeProperty", new CAArgument(module.CorLibTypes.Int32, 0)), new TypeSigCreatorOptions(module, language));
		}

		protected override bool AddCurrentCanExecute()
		{
			return canAdd == null || canAdd(this);
		}
	}
}
