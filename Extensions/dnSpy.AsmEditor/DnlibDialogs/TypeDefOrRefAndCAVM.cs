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

using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class TypeDefOrRefAndCAVM : ViewModelBase {
		readonly TypeDefOrRefAndCAOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public string FullName => TypeSigCreator.TypeSigDnlibFullName;
		public TypeSigCreatorVM TypeSigCreator { get; }
		public CustomAttributesVM CustomAttributesVM { get; }

		public TypeDefOrRefAndCAVM(TypeDefOrRefAndCAOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod) {
			origOptions = options;

			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = false,
				OwnerType = ownerType,
				OwnerMethod = ownerMethod,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			if (ownerMethod != null && ownerMethod.GenericParameters.Count > 0)
				typeSigCreatorOptions.CanAddGenericMethodVar = true;

			TypeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			TypeSigCreator.PropertyChanged += TypeSigCreator_PropertyChanged;
			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);

			Reinitialize();
		}

		void TypeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(TypeSigCreator.TypeSigDnlibFullName))
				OnPropertyChanged(nameof(FullName));
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);

		public TypeDefOrRefAndCAOptions CreateTypeDefOrRefAndCAOptions() => CopyTo(new TypeDefOrRefAndCAOptions());

		void InitializeFrom(TypeDefOrRefAndCAOptions options) {
			TypeSigCreator.TypeSig = options.TypeDefOrRef.ToTypeSig();
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		TypeDefOrRefAndCAOptions CopyTo(TypeDefOrRefAndCAOptions options) {
			options.TypeDefOrRef = TypeSigCreator.TypeSig.ToTypeDefOrRef();
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError => TypeSigCreator.HasError;
	}
}
