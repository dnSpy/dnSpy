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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Method {
	enum CodeType {
		IL			= (int)MethodImplAttributes.IL >> 0,
		Native		= (int)MethodImplAttributes.Native >> 0,
		OPTIL		= (int)MethodImplAttributes.OPTIL >> 0,
		Runtime		= (int)MethodImplAttributes.Runtime >> 0,
	}

	enum ManagedType {
		Unmanaged	= (int)MethodImplAttributes.Unmanaged >> 2,
		Managed		= (int)MethodImplAttributes.Managed >> 2,
	}

	enum MethodAccess {
		PrivateScope = (int)MethodAttributes.PrivateScope >> 0,
		Private		= (int)MethodAttributes.Private >> 0,
		FamANDAssem	= (int)MethodAttributes.FamANDAssem >> 0,
		Assembly	= (int)MethodAttributes.Assembly >> 0,
		Family		= (int)MethodAttributes.Family >> 0,
		FamORAssem	= (int)MethodAttributes.FamORAssem >> 0,
		Public		= (int)MethodAttributes.Public >> 0,
	}

	enum VtableLayout {
		ReuseSlot	= (int)MethodAttributes.ReuseSlot >> 8,
		NewSlot		= (int)MethodAttributes.NewSlot >> 8,
	}

	sealed class MethodOptionsVM : ViewModelBase {
		readonly MethodDefOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		internal static readonly EnumVM[] codeTypeList = EnumVM.Create(typeof(CodeType));
		public EnumListVM CodeType { get; } = new EnumListVM(codeTypeList);

		static readonly EnumVM[] managedTypeList = EnumVM.Create(typeof(ManagedType));
		public EnumListVM ManagedType { get; } = new EnumListVM(managedTypeList);

		static readonly EnumVM[] methodAccessList = EnumVM.Create(typeof(MethodAccess));
		public EnumListVM MethodAccess { get; } = new EnumListVM(methodAccessList);

		static readonly EnumVM[] vtableLayoutList = EnumVM.Create(typeof(VtableLayout));
		public EnumListVM VtableLayout { get; } = new EnumListVM(vtableLayoutList);

		public MethodImplAttributes ImplAttributes {
			get {
				var mask = MethodImplAttributes.CodeTypeMask |
							MethodImplAttributes.ManagedMask;
				return (implAttributes & ~mask) |
					(MethodImplAttributes)((int)(CodeType)CodeType.SelectedItem! << 0) |
					(MethodImplAttributes)((int)(ManagedType)ManagedType.SelectedItem! << 2);
			}
			set {
				if (implAttributes != value) {
					implAttributes = value;
					OnPropertyChanged(nameof(ImplAttributes));
					OnPropertyChanged(nameof(ForwardRef));
					OnPropertyChanged(nameof(PreserveSig));
					OnPropertyChanged(nameof(InternalCall));
					OnPropertyChanged(nameof(Synchronized));
					OnPropertyChanged(nameof(NoInlining));
					OnPropertyChanged(nameof(AggressiveInlining));
					OnPropertyChanged(nameof(NoOptimization));
					OnPropertyChanged(nameof(AggressiveOptimization));
					OnPropertyChanged(nameof(SecurityMitigations));
					HasErrorUpdated();
				}
			}
		}
		MethodImplAttributes implAttributes;

		public bool ForwardRef {
			get => GetFlagValue(MethodImplAttributes.ForwardRef);
			set => SetFlagValue(MethodImplAttributes.ForwardRef, value);
		}

		public bool PreserveSig {
			get => GetFlagValue(MethodImplAttributes.PreserveSig);
			set => SetFlagValue(MethodImplAttributes.PreserveSig, value);
		}

		public bool InternalCall {
			get => GetFlagValue(MethodImplAttributes.InternalCall);
			set => SetFlagValue(MethodImplAttributes.InternalCall, value);
		}

		public bool Synchronized {
			get => GetFlagValue(MethodImplAttributes.Synchronized);
			set => SetFlagValue(MethodImplAttributes.Synchronized, value);
		}

		public bool NoInlining {
			get => GetFlagValue(MethodImplAttributes.NoInlining);
			set => SetFlagValue(MethodImplAttributes.NoInlining, value);
		}

		public bool AggressiveInlining {
			get => GetFlagValue(MethodImplAttributes.AggressiveInlining);
			set => SetFlagValue(MethodImplAttributes.AggressiveInlining, value);
		}

		public bool NoOptimization {
			get => GetFlagValue(MethodImplAttributes.NoOptimization);
			set => SetFlagValue(MethodImplAttributes.NoOptimization, value);
		}

		public bool AggressiveOptimization {
			get => GetFlagValue(MethodImplAttributes.AggressiveOptimization);
			set => SetFlagValue(MethodImplAttributes.AggressiveOptimization, value);
		}

		public bool SecurityMitigations {
			get => GetFlagValue(MethodImplAttributes.SecurityMitigations);
			set => SetFlagValue(MethodImplAttributes.SecurityMitigations, value);
		}

		bool GetFlagValue(MethodImplAttributes flag) => (ImplAttributes & flag) != 0;

		void SetFlagValue(MethodImplAttributes flag, bool value) {
			if (value)
				ImplAttributes |= flag;
			else
				ImplAttributes &= ~flag;
		}

		public MethodAttributes Attributes {
			get {
				var mask = MethodAttributes.MemberAccessMask |
							MethodAttributes.VtableLayoutMask;
				return (attributes & ~mask) |
					(MethodAttributes)((int)(MethodAccess)MethodAccess.SelectedItem! << 0) |
					(MethodAttributes)((int)(VtableLayout)VtableLayout.SelectedItem! << 8);
			}
			set {
				if (attributes != value) {
					bool oldStatic = Static;
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(Static));
					OnPropertyChanged(nameof(Final));
					OnPropertyChanged(nameof(Virtual));
					OnPropertyChanged(nameof(HideBySig));
					OnPropertyChanged(nameof(CheckAccessOnOverride));
					OnPropertyChanged(nameof(Abstract));
					OnPropertyChanged(nameof(SpecialName));
					OnPropertyChanged(nameof(PinvokeImpl));
					OnPropertyChanged(nameof(UnmanagedExport));
					OnPropertyChanged(nameof(RTSpecialName));
					OnPropertyChanged(nameof(HasSecurity));
					OnPropertyChanged(nameof(RequireSecObject));
					ImplMapVM.IsEnabled = PinvokeImpl;
					if (oldStatic != Static && MethodSigCreator.HasThis != !Static)
						MethodSigCreator.HasThis = !Static;
					HasErrorUpdated();
				}
			}
		}
		MethodAttributes attributes;

		public bool Static {
			get => GetFlagValue(MethodAttributes.Static);
			set => SetFlagValue(MethodAttributes.Static, value);
		}

		public bool Final {
			get => GetFlagValue(MethodAttributes.Final);
			set => SetFlagValue(MethodAttributes.Final, value);
		}

		public bool Virtual {
			get => GetFlagValue(MethodAttributes.Virtual);
			set => SetFlagValue(MethodAttributes.Virtual, value);
		}

		public bool HideBySig {
			get => GetFlagValue(MethodAttributes.HideBySig);
			set => SetFlagValue(MethodAttributes.HideBySig, value);
		}

		public bool CheckAccessOnOverride {
			get => GetFlagValue(MethodAttributes.CheckAccessOnOverride);
			set => SetFlagValue(MethodAttributes.CheckAccessOnOverride, value);
		}

		public bool Abstract {
			get => GetFlagValue(MethodAttributes.Abstract);
			set => SetFlagValue(MethodAttributes.Abstract, value);
		}

		public bool SpecialName {
			get => GetFlagValue(MethodAttributes.SpecialName);
			set => SetFlagValue(MethodAttributes.SpecialName, value);
		}

		public bool PinvokeImpl {
			get => GetFlagValue(MethodAttributes.PinvokeImpl);
			set => SetFlagValue(MethodAttributes.PinvokeImpl, value);
		}

		public bool UnmanagedExport {
			get => GetFlagValue(MethodAttributes.UnmanagedExport);
			set => SetFlagValue(MethodAttributes.UnmanagedExport, value);
		}

		public bool RTSpecialName {
			get => GetFlagValue(MethodAttributes.RTSpecialName);
			set => SetFlagValue(MethodAttributes.RTSpecialName, value);
		}

		public bool HasSecurity {
			get => GetFlagValue(MethodAttributes.HasSecurity);
			set => SetFlagValue(MethodAttributes.HasSecurity, value);
		}

		public bool RequireSecObject {
			get => GetFlagValue(MethodAttributes.RequireSecObject);
			set => SetFlagValue(MethodAttributes.RequireSecObject, value);
		}

		bool GetFlagValue(MethodAttributes flag) => (Attributes & flag) != 0;

		void SetFlagValue(MethodAttributes flag, bool value) {
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

		public ImplMap? ImplMap {
			get => ImplMapVM.ImplMap;
			set => ImplMapVM.ImplMap = value;
		}

		public ImplMapVM ImplMapVM { get; }

		public MethodSig? MethodSig {
			get => MethodSigCreator.MethodSig;
			set => MethodSigCreator.MethodSig = value;
		}

		public string MethodSigHeader => $"MethodSig: {(MethodSigCreator.HasError ? "null" : MethodSigCreator.MethodSig!.ToString())}";

		public MethodSigCreatorVM MethodSigCreator { get; }
		public CustomAttributesVM CustomAttributesVM { get; }
		public DeclSecuritiesVM DeclSecuritiesVM { get; }
		public ParamDefsVM ParamDefsVM { get; }
		public GenericParamsVM GenericParamsVM { get; }
		public MethodOverridesVM MethodOverridesVM { get; }

		readonly ModuleDef ownerModule;

		public MethodOptionsVM(MethodDefOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef? ownerType, MethodDef? ownerMethod) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = ownerMethod is null || ownerMethod.GenericParameters.Count > 0,
				OwnerType = ownerType,
				OwnerMethod = ownerMethod,
			};
			if (ownerType is not null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;

			var methodSigCreatorOptions = new MethodSigCreatorOptions(typeSigCreatorOptions);
			methodSigCreatorOptions.IsPropertySig = false;
			MethodSigCreator = new MethodSigCreatorVM(methodSigCreatorOptions);
			MethodSigCreator.PropertyChanged += methodSigCreator_PropertyChanged;
			MethodSigCreator.ParametersCreateTypeSigArray.PropertyChanged += methodSigCreator_PropertyChanged;
			MethodSigCreator.ParametersCreateTypeSigArray.TypeSigCreator.ShowTypeFullName = true;
			MethodSigCreator.ParametersCreateTypeSigArray.TypeSigCreator.CanAddFnPtr = false;

			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService, ownerType, ownerMethod);
			DeclSecuritiesVM = new DeclSecuritiesVM(ownerModule, decompilerService, ownerType, ownerMethod);
			ParamDefsVM = new ParamDefsVM(ownerModule, decompilerService, ownerType, ownerMethod);
			GenericParamsVM = new GenericParamsVM(ownerModule, decompilerService, ownerType, ownerMethod);
			MethodOverridesVM = new MethodOverridesVM(ownerModule, decompilerService, ownerType, ownerMethod);

			origOptions = options;

			ImplMapVM = new ImplMapVM(ownerModule);
			ImplMapVM.PropertyChanged += implMapVM_PropertyChanged;

			ImplMapVM.IsEnabled = PinvokeImpl;
			Reinitialize();
		}

		void methodSigCreator_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(MethodSigCreator.HasThis)) {
				if (Static != !MethodSigCreator.HasThis)
					Static = !MethodSigCreator.HasThis;
			}
			HasErrorUpdated();
			OnPropertyChanged(nameof(MethodSigHeader));
		}

		void implMapVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ImplMapVM.IsEnabled))
				PinvokeImpl = ImplMapVM.IsEnabled;
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public MethodDefOptions CreateMethodDefOptions() => CopyTo(new MethodDefOptions());

		void InitializeFrom(MethodDefOptions options) {
			ImplAttributes = options.ImplAttributes;
			Attributes = options.Attributes;
			Name = options.Name;
			MethodSig = options.MethodSig;
			ImplMap = options.ImplMap;
			CodeType.SelectedItem = (CodeType)((int)(options.ImplAttributes & MethodImplAttributes.CodeTypeMask) >> 0);
			ManagedType.SelectedItem = (ManagedType)((int)(options.ImplAttributes & MethodImplAttributes.ManagedMask) >> 2);
			MethodAccess.SelectedItem = (MethodAccess)((int)(options.Attributes & MethodAttributes.MemberAccessMask) >> 0);
			VtableLayout.SelectedItem = (VtableLayout)((int)(options.Attributes & MethodAttributes.VtableLayoutMask) >> 8);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
			DeclSecuritiesVM.InitializeFrom(options.DeclSecurities);
			ParamDefsVM.InitializeFrom(options.ParamDefs);
			GenericParamsVM.InitializeFrom(options.GenericParameters);
			MethodOverridesVM.InitializeFrom(options.Overrides);
		}

		MethodDefOptions CopyTo(MethodDefOptions options) {
			options.ImplAttributes = ImplAttributes;
			options.Attributes = Attributes;
			options.Name = Name;
			options.MethodSig = MethodSig!;
			options.ImplMap = PinvokeImpl ? ImplMap : null;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			options.DeclSecurities.Clear();
			options.DeclSecurities.AddRange(DeclSecuritiesVM.Collection.Select(a => a.CreateDeclSecurityOptions().Create(ownerModule)));
			options.ParamDefs.Clear();
			options.ParamDefs.AddRange(ParamDefsVM.Collection.Select(a => a.CreateParamDefOptions().Create(ownerModule)));
			options.GenericParameters.Clear();
			options.GenericParameters.AddRange(GenericParamsVM.Collection.Select(a => a.CreateGenericParamOptions().Create(ownerModule)));
			options.Overrides.Clear();
			options.Overrides.AddRange(MethodOverridesVM.Collection.Select(a => a.CreateMethodOverrideOptions().Create()));
			if (ModelUtils.GetHasSecurityBit(options.DeclSecurities, options.CustomAttributes))
				options.Attributes |= MethodAttributes.HasSecurity;
			else
				options.Attributes &= ~MethodAttributes.HasSecurity;
			return options;
		}

		public override bool HasError {
			get {
				return (PinvokeImpl && ImplMapVM.HasError) ||
					MethodSigCreator.HasError;
			}
		}
	}
}
