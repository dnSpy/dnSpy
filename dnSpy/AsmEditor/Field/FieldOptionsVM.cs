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
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.Field {
	enum FieldAccess {
		PrivateScope	= (int)FieldAttributes.PrivateScope >> 0,
		Private			= (int)FieldAttributes.Private >> 0,
		FamANDAssem		= (int)FieldAttributes.FamANDAssem >> 0,
		Assembly		= (int)FieldAttributes.Assembly >> 0,
		Family			= (int)FieldAttributes.Family >> 0,
		FamORAssem		= (int)FieldAttributes.FamORAssem >> 0,
		Public			= (int)FieldAttributes.Public >> 0,
	}

	sealed class FieldOptionsVM : ViewModelBase {
		readonly FieldDefOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		static readonly EnumVM[] fieldAccessList = new EnumVM[] {
			new EnumVM(Field.FieldAccess.PrivateScope, "PrivateScope"),
			new EnumVM(Field.FieldAccess.Private, "Private"),
			new EnumVM(Field.FieldAccess.FamANDAssem, "Family and Assembly"),
			new EnumVM(Field.FieldAccess.Assembly, "Assembly"),
			new EnumVM(Field.FieldAccess.Family, "Family"),
			new EnumVM(Field.FieldAccess.FamORAssem, "Family or Assembly"),
			new EnumVM(Field.FieldAccess.Public, "Public"),
		};
		public EnumListVM FieldAccess {
			get { return fieldAccessVM; }
		}
		readonly EnumListVM fieldAccessVM = new EnumListVM(fieldAccessList);

		public FieldAttributes Attributes {
			get {
				return (attributes & ~FieldAttributes.FieldAccessMask) |
					(FieldAttributes)((int)(Field.FieldAccess)FieldAccess.SelectedItem << 0);
			}
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("Static");
					OnPropertyChanged("InitOnly");
					OnPropertyChanged("Literal");
					OnPropertyChanged("NotSerialized");
					OnPropertyChanged("SpecialName");
					OnPropertyChanged("PinvokeImpl");
					OnPropertyChanged("RTSpecialName");
					OnPropertyChanged("HasFieldMarshal");
					OnPropertyChanged("HasDefault");
					OnPropertyChanged("HasFieldRVA");
					OnPropertyChanged("MarshalTypeString");
					ConstantVM.IsEnabled = HasDefault;
					MarshalTypeVM.IsEnabled = HasFieldMarshal;
					ImplMapVM.IsEnabled = PinvokeImpl;
					HasErrorUpdated();
				}
			}
		}
		FieldAttributes attributes;

		public bool Static {
			get { return GetFlagValue(FieldAttributes.Static); }
			set { SetFlagValue(FieldAttributes.Static, value); }
		}

		public bool InitOnly {
			get { return GetFlagValue(FieldAttributes.InitOnly); }
			set { SetFlagValue(FieldAttributes.InitOnly, value); }
		}

		public bool Literal {
			get { return GetFlagValue(FieldAttributes.Literal); }
			set { SetFlagValue(FieldAttributes.Literal, value); }
		}

		public bool NotSerialized {
			get { return GetFlagValue(FieldAttributes.NotSerialized); }
			set { SetFlagValue(FieldAttributes.NotSerialized, value); }
		}

		public bool SpecialName {
			get { return GetFlagValue(FieldAttributes.SpecialName); }
			set { SetFlagValue(FieldAttributes.SpecialName, value); }
		}

		public bool PinvokeImpl {
			get { return GetFlagValue(FieldAttributes.PinvokeImpl); }
			set { SetFlagValue(FieldAttributes.PinvokeImpl, value); }
		}

		public bool RTSpecialName {
			get { return GetFlagValue(FieldAttributes.RTSpecialName); }
			set { SetFlagValue(FieldAttributes.RTSpecialName, value); }
		}

		public bool HasFieldMarshal {
			get { return GetFlagValue(FieldAttributes.HasFieldMarshal); }
			set { SetFlagValue(FieldAttributes.HasFieldMarshal, value); }
		}

		public bool HasDefault {
			get { return GetFlagValue(FieldAttributes.HasDefault); }
			set { SetFlagValue(FieldAttributes.HasDefault, value); }
		}

		public bool HasFieldRVA {
			get { return GetFlagValue(FieldAttributes.HasFieldRVA); }
			set { SetFlagValue(FieldAttributes.HasFieldRVA, value); }
		}

		bool GetFlagValue(FieldAttributes flag) {
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(FieldAttributes flag, bool value) {
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

		public TypeSig FieldTypeSig {
			get { return typeSigCreator.TypeSig; }
			set { typeSigCreator.TypeSig = value; }
		}

		public string FieldTypeHeader {
			get { return string.Format("Field Type: {0}", typeSigCreator.TypeSigDnlibFullName); }
		}

		public TypeSigCreatorVM TypeSigCreator {
			get { return typeSigCreator; }
		}
		readonly TypeSigCreatorVM typeSigCreator;

		public Constant Constant {
			get { return HasDefault ? ownerModule.UpdateRowId(new ConstantUser(ConstantVM.Value)) : null; }
		}

		public ConstantVM ConstantVM {
			get { return constantVM; }
		}
		readonly ConstantVM constantVM;

		public MarshalTypeVM MarshalTypeVM {
			get { return marshalTypeVM; }
		}
		readonly MarshalTypeVM marshalTypeVM;

		public NullableUInt32VM FieldOffset {
			get { return fieldOffset; }
		}
		readonly NullableUInt32VM fieldOffset;

		public HexStringVM InitialValue {
			get { return initialValue; }
		}
		readonly HexStringVM initialValue;

		public UInt32VM RVA {
			get { return rva; }
		}
		readonly UInt32VM rva;

		public ImplMap ImplMap {
			get { return implMapVM.ImplMap; }
			set { implMapVM.ImplMap = value; }
		}

		public ImplMapVM ImplMapVM {
			get { return implMapVM; }
		}
		readonly ImplMapVM implMapVM;

		public string MarshalTypeString {
			get { return string.Format("Marshal Type: {0}", HasFieldMarshal ? MarshalTypeVM.TypeString : "nothing"); }
		}

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		readonly ModuleDef ownerModule;

		public FieldOptionsVM(FieldDefOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, language) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = false,
				OwnerType = ownerType,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			this.typeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			TypeSigCreator.PropertyChanged += typeSigCreator_PropertyChanged;

			this.customAttributesVM = new CustomAttributesVM(ownerModule, language);
			this.origOptions = options;

			this.constantVM = new ConstantVM(ownerModule, options.Constant == null ? null : options.Constant.Value, "Default value for this field");
			ConstantVM.PropertyChanged += constantVM_PropertyChanged;
			this.marshalTypeVM = new MarshalTypeVM(ownerModule, language, ownerType, null);
			MarshalTypeVM.PropertyChanged += marshalTypeVM_PropertyChanged;
			this.fieldOffset = new NullableUInt32VM(a => HasErrorUpdated());
			this.initialValue = new HexStringVM(a => HasErrorUpdated());
			this.rva = new UInt32VM(a => HasErrorUpdated());
			this.implMapVM = new ImplMapVM(ownerModule);
			ImplMapVM.PropertyChanged += implMapVM_PropertyChanged;

			this.typeSigCreator.CanAddFnPtr = false;
			ConstantVM.IsEnabled = HasDefault;
			MarshalTypeVM.IsEnabled = HasFieldMarshal;
			ImplMapVM.IsEnabled = PinvokeImpl;
			Reinitialize();
		}

		void constantVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				HasDefault = ConstantVM.IsEnabled;
			HasErrorUpdated();
		}

		void marshalTypeVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				HasFieldMarshal = MarshalTypeVM.IsEnabled;
			else if (e.PropertyName == "TypeString")
				OnPropertyChanged("MarshalTypeString");
			HasErrorUpdated();
		}

		void implMapVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				PinvokeImpl = ImplMapVM.IsEnabled;
			HasErrorUpdated();
		}

		void typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "TypeSigDnlibFullName")
				OnPropertyChanged("FieldTypeHeader");
			HasErrorUpdated();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public FieldDefOptions CreateFieldDefOptions() {
			return CopyTo(new FieldDefOptions());
		}

		void InitializeFrom(FieldDefOptions options) {
			Attributes = options.Attributes;
			Name = options.Name;
			FieldTypeSig = options.FieldSig == null ? null : options.FieldSig.Type;
			FieldOffset.Value = options.FieldOffset;
			MarshalTypeVM.Type = options.MarshalType;
			RVA.Value = (uint)options.RVA;
			InitialValue.Value = options.InitialValue;
			ImplMap = options.ImplMap;
			if (options.Constant != null) {
				HasDefault = true;
				ConstantVM.Value = options.Constant.Value;
			}
			else {
				HasDefault = false;
				ConstantVM.Value = null;
			}
			FieldAccess.SelectedItem = (Field.FieldAccess)((int)(options.Attributes & FieldAttributes.FieldAccessMask) >> 0);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		FieldDefOptions CopyTo(FieldDefOptions options) {
			options.Attributes = Attributes;
			options.Name = Name;
			var typeSig = FieldTypeSig;
			options.FieldSig = typeSig == null ? null : new FieldSig(typeSig);
			options.FieldOffset = FieldOffset.Value;
			options.MarshalType = HasFieldMarshal ? MarshalTypeVM.Type : null;
			options.RVA = (dnlib.PE.RVA)RVA.Value;
			options.InitialValue = HasFieldRVA ? InitialValue.Value.ToArray() : null;
			options.ImplMap = PinvokeImpl ? ImplMap : null;
			options.Constant = HasDefault ? Constant : null;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError {
			get {
				return (HasDefault && ConstantVM.HasError) ||
					(HasFieldMarshal && MarshalTypeVM.HasError) ||
					(HasFieldRVA && InitialValue.HasError) ||
					(PinvokeImpl && ImplMapVM.HasError) ||
					RVA.HasError ||
					FieldOffset.HasError ||
					TypeSigCreator.HasError;
			}
		}
	}
}
