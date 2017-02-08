/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class CANamedArgumentsVM : ListVM<CANamedArgumentVM, CANamedArgument> {
		readonly Predicate<CANamedArgumentsVM> canAdd;

		public CANamedArgumentsVM(ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod, Predicate<CANamedArgumentsVM> canAdd)
			: base(null, null, ownerModule, decompilerService, ownerType, ownerMethod, true) {
			this.canAdd = canAdd;
		}

		protected override CANamedArgumentVM Create(CANamedArgument model) => new CANamedArgumentVM(OwnerModule, model, new TypeSigCreatorOptions(OwnerModule, decompilerService));
		protected override CANamedArgumentVM Clone(CANamedArgumentVM obj) => new CANamedArgumentVM(OwnerModule, obj.CreateCANamedArgument(), new TypeSigCreatorOptions(OwnerModule, decompilerService));
		protected override CANamedArgumentVM Create() => new CANamedArgumentVM(OwnerModule, new CANamedArgument(false, OwnerModule.CorLibTypes.Int32, "AttributeProperty", new CAArgument(OwnerModule.CorLibTypes.Int32, 0)), new TypeSigCreatorOptions(OwnerModule, decompilerService));
		protected override bool AddItemCanExecute() => canAdd == null || canAdd(this);
	}
}
