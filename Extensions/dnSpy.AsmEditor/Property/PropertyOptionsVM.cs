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

using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Property {
	sealed class PropertyOptionsVM : ViewModelBase {
		readonly PropertyDefOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		public PropertyAttributes Attributes {
			get => attributes;
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(SpecialName));
					OnPropertyChanged(nameof(RTSpecialName));
					OnPropertyChanged(nameof(HasDefault));
					ConstantVM.IsEnabled = HasDefault;
					HasErrorUpdated();
				}
			}
		}
		PropertyAttributes attributes;

		public bool SpecialName {
			get => GetFlagValue(PropertyAttributes.SpecialName);
			set => SetFlagValue(PropertyAttributes.SpecialName, value);
		}

		public bool RTSpecialName {
			get => GetFlagValue(PropertyAttributes.RTSpecialName);
			set => SetFlagValue(PropertyAttributes.RTSpecialName, value);
		}

		public bool HasDefault {
			get => GetFlagValue(PropertyAttributes.HasDefault);
			set => SetFlagValue(PropertyAttributes.HasDefault, value);
		}

		bool GetFlagValue(PropertyAttributes flag) => (Attributes & flag) != 0;

		void SetFlagValue(PropertyAttributes flag, bool value) {
			if (value)
				Attributes |= flag;
			else
				Attributes &= ~flag;
		}

		public string? Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		UTF8String? name;

		public PropertySig? PropertySig {
			get => MethodSigCreator.PropertySig;
			set => MethodSigCreator.PropertySig = value;
		}

		public string PropertySigHeader => string.Format(dnSpy_AsmEditor_Resources.PropertyX, MethodSigCreator.HasError ? "null" : MethodSigCreator.PropertySig!.ToString());
		public MethodSigCreatorVM MethodSigCreator { get; }
		public Constant? Constant => HasDefault ? ownerModule.UpdateRowId(new ConstantUser(ConstantVM.Value)) : null;
		public ConstantVM ConstantVM { get; }
		public MethodDefsVM GetMethodsVM { get; }
		public MethodDefsVM SetMethodsVM { get; }
		public MethodDefsVM OtherMethodsVM { get; }
		public CustomAttributesVM CustomAttributesVM { get; }

		readonly ModuleDef ownerModule;

		public PropertyOptionsVM(PropertyDefOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType) {
			this.ownerModule = ownerModule;
			origOptions = options;

			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = true,
				OwnerType = ownerType,
			};
			if (!(ownerType is null) && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			var methodSigCreatorOptions = new MethodSigCreatorOptions(typeSigCreatorOptions);
			methodSigCreatorOptions.IsPropertySig = true;
			MethodSigCreator = new MethodSigCreatorVM(methodSigCreatorOptions);
			MethodSigCreator.PropertyChanged += methodSigCreator_PropertyChanged;
			MethodSigCreator.ParametersCreateTypeSigArray.PropertyChanged += methodSigCreator_PropertyChanged;
			MethodSigCreator.ParametersCreateTypeSigArray.TypeSigCreator.CanAddFnPtr = false;
			GetMethodsVM = new MethodDefsVM(ownerModule, decompilerService);
			SetMethodsVM = new MethodDefsVM(ownerModule, decompilerService);
			OtherMethodsVM = new MethodDefsVM(ownerModule, decompilerService);
			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);
			ConstantVM = new ConstantVM(ownerModule, options.Constant is null ? null : options.Constant.Value, dnSpy_AsmEditor_Resources.Property_DefaultValue);
			ConstantVM.PropertyChanged += constantVM_PropertyChanged;

			ConstantVM.IsEnabled = HasDefault;
			Reinitialize();
		}

		void constantVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ConstantVM.IsEnabled))
				HasDefault = ConstantVM.IsEnabled;
			HasErrorUpdated();
		}

		void methodSigCreator_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			HasErrorUpdated();
			OnPropertyChanged(nameof(PropertySigHeader));
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public PropertyDefOptions CreatePropertyDefOptions() => CopyTo(new PropertyDefOptions());

		void InitializeFrom(PropertyDefOptions options) {
			Attributes = options.Attributes;
			Name = options.Name;
			PropertySig = options.PropertySig;
			if (!(options.Constant is null)) {
				HasDefault = true;
				ConstantVM.Value = options.Constant.Value;
			}
			else {
				HasDefault = false;
				ConstantVM.Value = null;
			}
			GetMethodsVM.InitializeFrom(options.GetMethods);
			SetMethodsVM.InitializeFrom(options.SetMethods);
			OtherMethodsVM.InitializeFrom(options.OtherMethods);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		PropertyDefOptions CopyTo(PropertyDefOptions options) {
			options.Attributes = Attributes;
			options.Name = Name;
			options.PropertySig = PropertySig;
			options.Constant = HasDefault ? Constant : null;
			options.GetMethods.Clear();
			options.GetMethods.AddRange(GetMethodsVM.Collection.Select(a => a.Method!));
			options.SetMethods.Clear();
			options.SetMethods.AddRange(SetMethodsVM.Collection.Select(a => a.Method!));
			options.OtherMethods.Clear();
			options.OtherMethods.AddRange(OtherMethodsVM.Collection.Select(a => a.Method!));
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError {
			get {
				return MethodSigCreator.HasError ||
					(HasDefault && ConstantVM.HasError);
			}
		}
	}
}
