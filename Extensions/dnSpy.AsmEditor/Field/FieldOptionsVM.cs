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

namespace dnSpy.AsmEditor.Field {
	enum FieldAccess {
		PrivateScope	= (int)FieldAttributes.PrivateScope,
		Private			= (int)FieldAttributes.Private,
		FamANDAssem		= (int)FieldAttributes.FamANDAssem,
		Assembly		= (int)FieldAttributes.Assembly,
		Family			= (int)FieldAttributes.Family,
		FamORAssem		= (int)FieldAttributes.FamORAssem,
		Public			= (int)FieldAttributes.Public,
	}

	sealed class FieldOptionsVM : ViewModelBase {
		readonly FieldDefOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		static readonly EnumVM[] fieldAccessList = EnumVM.Create(typeof(FieldAccess));
		public EnumListVM FieldAccess { get; } = new EnumListVM(fieldAccessList);

		public FieldAttributes Attributes {
			get => (attributes & ~FieldAttributes.FieldAccessMask) | (FieldAttributes)(FieldAccess)FieldAccess.SelectedItem!;
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(Static));
					OnPropertyChanged(nameof(InitOnly));
					OnPropertyChanged(nameof(Literal));
					OnPropertyChanged(nameof(NotSerialized));
					OnPropertyChanged(nameof(SpecialName));
					OnPropertyChanged(nameof(PinvokeImpl));
					OnPropertyChanged(nameof(RTSpecialName));
					OnPropertyChanged(nameof(HasFieldMarshal));
					OnPropertyChanged(nameof(HasDefault));
					OnPropertyChanged(nameof(HasFieldRVA));
					OnPropertyChanged(nameof(MarshalTypeString));
					ConstantVM.IsEnabled = HasDefault;
					MarshalTypeVM.IsEnabled = HasFieldMarshal;
					ImplMapVM.IsEnabled = PinvokeImpl;
					HasErrorUpdated();
				}
			}
		}
		FieldAttributes attributes;

		public bool Static {
			get => GetFlagValue(FieldAttributes.Static);
			set => SetFlagValue(FieldAttributes.Static, value);
		}

		public bool InitOnly {
			get => GetFlagValue(FieldAttributes.InitOnly);
			set => SetFlagValue(FieldAttributes.InitOnly, value);
		}

		public bool Literal {
			get => GetFlagValue(FieldAttributes.Literal);
			set => SetFlagValue(FieldAttributes.Literal, value);
		}

		public bool NotSerialized {
			get => GetFlagValue(FieldAttributes.NotSerialized);
			set => SetFlagValue(FieldAttributes.NotSerialized, value);
		}

		public bool SpecialName {
			get => GetFlagValue(FieldAttributes.SpecialName);
			set => SetFlagValue(FieldAttributes.SpecialName, value);
		}

		public bool PinvokeImpl {
			get => GetFlagValue(FieldAttributes.PinvokeImpl);
			set => SetFlagValue(FieldAttributes.PinvokeImpl, value);
		}

		public bool RTSpecialName {
			get => GetFlagValue(FieldAttributes.RTSpecialName);
			set => SetFlagValue(FieldAttributes.RTSpecialName, value);
		}

		public bool HasFieldMarshal {
			get => GetFlagValue(FieldAttributes.HasFieldMarshal);
			set => SetFlagValue(FieldAttributes.HasFieldMarshal, value);
		}

		public bool HasDefault {
			get => GetFlagValue(FieldAttributes.HasDefault);
			set => SetFlagValue(FieldAttributes.HasDefault, value);
		}

		public bool HasFieldRVA {
			get => GetFlagValue(FieldAttributes.HasFieldRVA);
			set => SetFlagValue(FieldAttributes.HasFieldRVA, value);
		}

		bool GetFlagValue(FieldAttributes flag) => (Attributes & flag) != 0;

		void SetFlagValue(FieldAttributes flag, bool value) {
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

		public TypeSig? FieldTypeSig {
			get => TypeSigCreator.TypeSig;
			set => TypeSigCreator.TypeSig = value;
		}

		public string FieldTypeHeader => string.Format(dnSpy_AsmEditor_Resources.FieldType, TypeSigCreator.TypeSigDnlibFullName);

		public TypeSigCreatorVM TypeSigCreator { get; }
		public Constant? Constant => HasDefault ? ownerModule.UpdateRowId(new ConstantUser(ConstantVM.Value)) : null;
		public ConstantVM ConstantVM { get; }
		public MarshalTypeVM MarshalTypeVM { get; }
		public NullableUInt32VM FieldOffset { get; }
		public HexStringVM InitialValue { get; }
		public UInt32VM RVA { get; }

		public ImplMap? ImplMap {
			get => ImplMapVM.ImplMap;
			set => ImplMapVM.ImplMap = value;
		}

		public ImplMapVM ImplMapVM { get; }
		public string MarshalTypeString =>
			string.Format(dnSpy_AsmEditor_Resources.MarshalType, HasFieldMarshal ? MarshalTypeVM.TypeString : dnSpy_AsmEditor_Resources.MarshalType_Nothing);
		public CustomAttributesVM CustomAttributesVM { get; }

		readonly ModuleDef ownerModule;

		public FieldOptionsVM(FieldDefOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = false,
				OwnerType = ownerType,
			};
			if (!(ownerType is null) && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			TypeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			TypeSigCreator.PropertyChanged += typeSigCreator_PropertyChanged;

			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);
			origOptions = options;

			ConstantVM = new ConstantVM(ownerModule, options.Constant?.Value, dnSpy_AsmEditor_Resources.Field_DefaultValueInfo);
			ConstantVM.PropertyChanged += constantVM_PropertyChanged;
			MarshalTypeVM = new MarshalTypeVM(ownerModule, decompilerService, ownerType, null);
			MarshalTypeVM.PropertyChanged += marshalTypeVM_PropertyChanged;
			FieldOffset = new NullableUInt32VM(a => HasErrorUpdated());
			InitialValue = new HexStringVM(a => HasErrorUpdated());
			RVA = new UInt32VM(a => HasErrorUpdated());
			ImplMapVM = new ImplMapVM(ownerModule);
			ImplMapVM.PropertyChanged += implMapVM_PropertyChanged;

			TypeSigCreator.CanAddFnPtr = false;
			ConstantVM.IsEnabled = HasDefault;
			MarshalTypeVM.IsEnabled = HasFieldMarshal;
			ImplMapVM.IsEnabled = PinvokeImpl;
			Reinitialize();
		}

		void constantVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ConstantVM.IsEnabled))
				HasDefault = ConstantVM.IsEnabled;
			HasErrorUpdated();
		}

		void marshalTypeVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(MarshalTypeVM.IsEnabled))
				HasFieldMarshal = MarshalTypeVM.IsEnabled;
			else if (e.PropertyName == nameof(MarshalTypeVM.TypeString))
				OnPropertyChanged(nameof(MarshalTypeString));
			HasErrorUpdated();
		}

		void implMapVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ImplMapVM.IsEnabled))
				PinvokeImpl = ImplMapVM.IsEnabled;
			HasErrorUpdated();
		}

		void typeSigCreator_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(TypeSigCreator.TypeSigDnlibFullName))
				OnPropertyChanged(nameof(FieldTypeHeader));
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);

		public FieldDefOptions CreateFieldDefOptions() => CopyTo(new FieldDefOptions());

		void InitializeFrom(FieldDefOptions options) {
			Attributes = options.Attributes;
			Name = options.Name;
			FieldTypeSig = options.FieldSig?.Type;
			FieldOffset.Value = options.FieldOffset;
			MarshalTypeVM.Type = options.MarshalType;
			RVA.Value = (uint)options.RVA;
			InitialValue.Value = options.InitialValue!;
			ImplMap = options.ImplMap;
			if (!(options.Constant is null)) {
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
			options.FieldSig = typeSig is null ? null : new FieldSig(typeSig);
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
