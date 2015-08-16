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

using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using ICSharpCode.ILSpy;

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

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		internal static readonly EnumVM[] codeTypeList = EnumVM.Create(typeof(Method.CodeType));
		public EnumListVM CodeType {
			get { return codeTypeVM; }
		}
		readonly EnumListVM codeTypeVM = new EnumListVM(codeTypeList);

		static readonly EnumVM[] managedTypeList = EnumVM.Create(typeof(Method.ManagedType));
		public EnumListVM ManagedType {
			get { return managedTypeVM; }
		}
		readonly EnumListVM managedTypeVM = new EnumListVM(managedTypeList);

		static readonly EnumVM[] methodAccessList = new EnumVM[] {
			new EnumVM(Method.MethodAccess.PrivateScope, "PrivateScope"),
			new EnumVM(Method.MethodAccess.Private, "Private"),
			new EnumVM(Method.MethodAccess.FamANDAssem, "Family and Assembly"),
			new EnumVM(Method.MethodAccess.Assembly, "Assembly"),
			new EnumVM(Method.MethodAccess.Family, "Family"),
			new EnumVM(Method.MethodAccess.FamORAssem, "Family or Assembly"),
			new EnumVM(Method.MethodAccess.Public, "Public"),
		};
		public EnumListVM MethodAccess {
			get { return methodAccessVM; }
		}
		readonly EnumListVM methodAccessVM = new EnumListVM(methodAccessList);

		static readonly EnumVM[] vtableLayoutList = EnumVM.Create(typeof(Method.VtableLayout));
		public EnumListVM VtableLayout {
			get { return vtableLayoutVM; }
		}
		readonly EnumListVM vtableLayoutVM = new EnumListVM(vtableLayoutList);

		public MethodImplAttributes ImplAttributes {
			get {
				var mask = MethodImplAttributes.CodeTypeMask |
							MethodImplAttributes.ManagedMask;
				return (implAttributes & ~mask) |
					(MethodImplAttributes)((int)(Method.CodeType)CodeType.SelectedItem << 0) |
					(MethodImplAttributes)((int)(Method.ManagedType)ManagedType.SelectedItem << 2);
			}
			set {
				if (implAttributes != value) {
					implAttributes = value;
					OnPropertyChanged("ImplAttributes");
					OnPropertyChanged("ForwardRef");
					OnPropertyChanged("PreserveSig");
					OnPropertyChanged("InternalCall");
					OnPropertyChanged("Synchronized");
					OnPropertyChanged("NoInlining");
					OnPropertyChanged("AggressiveInlining");
					OnPropertyChanged("NoOptimization");
					HasErrorUpdated();
				}
			}
		}
		MethodImplAttributes implAttributes;

		public bool ForwardRef {
			get { return GetFlagValue(MethodImplAttributes.ForwardRef); }
			set { SetFlagValue(MethodImplAttributes.ForwardRef, value); }
		}

		public bool PreserveSig {
			get { return GetFlagValue(MethodImplAttributes.PreserveSig); }
			set { SetFlagValue(MethodImplAttributes.PreserveSig, value); }
		}

		public bool InternalCall {
			get { return GetFlagValue(MethodImplAttributes.InternalCall); }
			set { SetFlagValue(MethodImplAttributes.InternalCall, value); }
		}

		public bool Synchronized {
			get { return GetFlagValue(MethodImplAttributes.Synchronized); }
			set { SetFlagValue(MethodImplAttributes.Synchronized, value); }
		}

		public bool NoInlining {
			get { return GetFlagValue(MethodImplAttributes.NoInlining); }
			set { SetFlagValue(MethodImplAttributes.NoInlining, value); }
		}

		public bool AggressiveInlining {
			get { return GetFlagValue(MethodImplAttributes.AggressiveInlining); }
			set { SetFlagValue(MethodImplAttributes.AggressiveInlining, value); }
		}

		public bool NoOptimization {
			get { return GetFlagValue(MethodImplAttributes.NoOptimization); }
			set { SetFlagValue(MethodImplAttributes.NoOptimization, value); }
		}

		bool GetFlagValue(MethodImplAttributes flag) {
			return (ImplAttributes & flag) != 0;
		}

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
					(MethodAttributes)((int)(Method.MethodAccess)MethodAccess.SelectedItem << 0) |
					(MethodAttributes)((int)(Method.VtableLayout)VtableLayout.SelectedItem << 8);
			}
			set {
				if (attributes != value) {
					bool oldStatic = Static;
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("Static");
					OnPropertyChanged("Final");
					OnPropertyChanged("Virtual");
					OnPropertyChanged("HideBySig");
					OnPropertyChanged("CheckAccessOnOverride");
					OnPropertyChanged("Abstract");
					OnPropertyChanged("SpecialName");
					OnPropertyChanged("PinvokeImpl");
					OnPropertyChanged("UnmanagedExport");
					OnPropertyChanged("RTSpecialName");
					OnPropertyChanged("HasSecurity");
					OnPropertyChanged("RequireSecObject");
					ImplMapVM.IsEnabled = PinvokeImpl;
					if (oldStatic != Static && MethodSigCreator.HasThis != !Static)
						MethodSigCreator.HasThis = !Static;
					HasErrorUpdated();
				}
			}
		}
		MethodAttributes attributes;

		public bool Static {
			get { return GetFlagValue(MethodAttributes.Static); }
			set { SetFlagValue(MethodAttributes.Static, value); }
		}

		public bool Final {
			get { return GetFlagValue(MethodAttributes.Final); }
			set { SetFlagValue(MethodAttributes.Final, value); }
		}

		public bool Virtual {
			get { return GetFlagValue(MethodAttributes.Virtual); }
			set { SetFlagValue(MethodAttributes.Virtual, value); }
		}

		public bool HideBySig {
			get { return GetFlagValue(MethodAttributes.HideBySig); }
			set { SetFlagValue(MethodAttributes.HideBySig, value); }
		}

		public bool CheckAccessOnOverride {
			get { return GetFlagValue(MethodAttributes.CheckAccessOnOverride); }
			set { SetFlagValue(MethodAttributes.CheckAccessOnOverride, value); }
		}

		public bool Abstract {
			get { return GetFlagValue(MethodAttributes.Abstract); }
			set { SetFlagValue(MethodAttributes.Abstract, value); }
		}

		public bool SpecialName {
			get { return GetFlagValue(MethodAttributes.SpecialName); }
			set { SetFlagValue(MethodAttributes.SpecialName, value); }
		}

		public bool PinvokeImpl {
			get { return GetFlagValue(MethodAttributes.PinvokeImpl); }
			set { SetFlagValue(MethodAttributes.PinvokeImpl, value); }
		}

		public bool UnmanagedExport {
			get { return GetFlagValue(MethodAttributes.UnmanagedExport); }
			set { SetFlagValue(MethodAttributes.UnmanagedExport, value); }
		}

		public bool RTSpecialName {
			get { return GetFlagValue(MethodAttributes.RTSpecialName); }
			set { SetFlagValue(MethodAttributes.RTSpecialName, value); }
		}

		public bool HasSecurity {
			get { return GetFlagValue(MethodAttributes.HasSecurity); }
			set { SetFlagValue(MethodAttributes.HasSecurity, value); }
		}

		public bool RequireSecObject {
			get { return GetFlagValue(MethodAttributes.RequireSecObject); }
			set { SetFlagValue(MethodAttributes.RequireSecObject, value); }
		}

		bool GetFlagValue(MethodAttributes flag) {
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(MethodAttributes flag, bool value) {
			if (value)
				Attributes |= flag;
			else
				Attributes &= ~flag;
		}

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		UTF8String name;

		public ImplMap ImplMap {
			get { return implMapVM.ImplMap; }
			set { implMapVM.ImplMap = value; }
		}

		public ImplMapVM ImplMapVM {
			get { return implMapVM; }
		}
		readonly ImplMapVM implMapVM;

		public MethodSig MethodSig {
			get { return MethodSigCreator.MethodSig; }
			set { MethodSigCreator.MethodSig = value; }
		}

		public string MethodSigHeader {
			get { return string.Format("MethodSig: {0}", MethodSigCreator.HasError ? "null" : MethodSigCreator.MethodSig.ToString()); }
		}

		public MethodSigCreatorVM MethodSigCreator {
			get { return methodSigCreator; }
		}
		readonly MethodSigCreatorVM methodSigCreator;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		public DeclSecuritiesVM DeclSecuritiesVM {
			get { return declSecuritiesVM; }
		}
		DeclSecuritiesVM declSecuritiesVM;

		public ParamDefsVM ParamDefsVM {
			get { return paramDefsVM; }
		}
		ParamDefsVM paramDefsVM;

		public GenericParamsVM GenericParamsVM {
			get { return genericParamsVM; }
		}
		GenericParamsVM genericParamsVM;

		public MethodOverridesVM MethodOverridesVM {
			get { return methodOverridesVM; }
		}
		MethodOverridesVM methodOverridesVM;

		readonly ModuleDef ownerModule;

		public MethodOptionsVM(MethodDefOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, language) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = ownerMethod == null || ownerMethod.GenericParameters.Count > 0,
				OwnerType = ownerType,
				OwnerMethod = ownerMethod,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;

			var methodSigCreatorOptions = new MethodSigCreatorOptions(typeSigCreatorOptions);
			methodSigCreatorOptions.IsPropertySig = false;
			this.methodSigCreator = new MethodSigCreatorVM(methodSigCreatorOptions);
			this.methodSigCreator.PropertyChanged += methodSigCreator_PropertyChanged;
			this.methodSigCreator.ParametersCreateTypeSigArray.PropertyChanged += methodSigCreator_PropertyChanged;
			this.methodSigCreator.ParametersCreateTypeSigArray.TypeSigCreator.ShowTypeFullName = true;
			this.methodSigCreator.ParametersCreateTypeSigArray.TypeSigCreator.CanAddFnPtr = false;

			this.customAttributesVM = new CustomAttributesVM(ownerModule, language, ownerType, ownerMethod);
			this.declSecuritiesVM = new DeclSecuritiesVM(ownerModule, language, ownerType, ownerMethod);
			this.paramDefsVM = new ParamDefsVM(ownerModule, language, ownerType, ownerMethod);
			this.genericParamsVM = new GenericParamsVM(ownerModule, language, ownerType, ownerMethod);
			this.methodOverridesVM = new MethodOverridesVM(ownerModule, language, ownerType, ownerMethod);

			this.origOptions = options;

			this.implMapVM = new ImplMapVM(ownerModule);
			ImplMapVM.PropertyChanged += implMapVM_PropertyChanged;

			ImplMapVM.IsEnabled = PinvokeImpl;
			Reinitialize();
		}

		void methodSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "HasThis") {
				if (Static != !MethodSigCreator.HasThis)
					Static = !MethodSigCreator.HasThis;
			}
			HasErrorUpdated();
			OnPropertyChanged("MethodSigHeader");
		}

		void implMapVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				PinvokeImpl = ImplMapVM.IsEnabled;
			HasErrorUpdated();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public MethodDefOptions CreateMethodDefOptions() {
			return CopyTo(new MethodDefOptions());
		}

		void InitializeFrom(MethodDefOptions options) {
			ImplAttributes = options.ImplAttributes;
			Attributes = options.Attributes;
			Name = options.Name;
			MethodSig = options.MethodSig;
			ImplMap = options.ImplMap;
			CodeType.SelectedItem = (Method.CodeType)((int)(options.ImplAttributes & MethodImplAttributes.CodeTypeMask) >> 0);
			ManagedType.SelectedItem = (Method.ManagedType)((int)(options.ImplAttributes & MethodImplAttributes.ManagedMask) >> 2);
			MethodAccess.SelectedItem = (Method.MethodAccess)((int)(options.Attributes & MethodAttributes.MemberAccessMask) >> 0);
			VtableLayout.SelectedItem = (Method.VtableLayout)((int)(options.Attributes & MethodAttributes.VtableLayoutMask) >> 8);
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
			options.MethodSig = MethodSig;
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
