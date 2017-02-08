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
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Types {
	enum TypeKind {
		Unknown,
		Class,
		StaticClass,
		Interface,
		Struct,
		Enum,
		Delegate,
	}

	enum TypeVisibility {
		NotPublic			= (int)TypeAttributes.NotPublic >> 0,
		Public				= (int)TypeAttributes.Public >> 0,
		NestedPublic		= (int)TypeAttributes.NestedPublic >> 0,
		NestedPrivate		= (int)TypeAttributes.NestedPrivate >> 0,
		NestedFamily		= (int)TypeAttributes.NestedFamily >> 0,
		NestedAssembly		= (int)TypeAttributes.NestedAssembly >> 0,
		NestedFamANDAssem	= (int)TypeAttributes.NestedFamANDAssem >> 0,
		NestedFamORAssem	= (int)TypeAttributes.NestedFamORAssem >> 0,
	}

	enum TypeLayout {
		AutoLayout			= (int)TypeAttributes.AutoLayout >> 3,
		SequentialLayout	= (int)TypeAttributes.SequentialLayout >> 3,
		ExplicitLayout		= (int)TypeAttributes.ExplicitLayout >> 3,
	}

	enum TypeSemantics {
		Class				= (int)TypeAttributes.Class >> 5,
		Interface			= (int)TypeAttributes.Interface >> 5,
	}

	enum TypeStringFormat {
		AnsiClass			= (int)TypeAttributes.AnsiClass >> 16,
		UnicodeClass		= (int)TypeAttributes.UnicodeClass >> 16,
		AutoClass			= (int)TypeAttributes.AutoClass >> 16,
		CustomFormatClass	= (int)TypeAttributes.CustomFormatClass >> 16,
	}

	enum TypeCustomFormat {
		Value0,
		Value1,
		Value2,
		Value3,
	}

	sealed class TypeOptionsVM : ViewModelBase {
		readonly TypeDefOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public bool IsNonNestedType => !IsNestedType;
		public bool IsNestedType { get; }
		public string VisibilityAccessbilityText => IsNestedType ? dnSpy_AsmEditor_Resources.Type_Flags_Accessibility : dnSpy_AsmEditor_Resources.Type_Flags_Visibility;

		static readonly EnumVM[] typeKindList = EnumVM.Create(typeof(TypeKind));
		public EnumListVM TypeKind { get; }

		static readonly EnumVM[] typeVisibilityList = new EnumVM[] {
			new EnumVM(Types.TypeVisibility.NotPublic, "NotPublic"),
			new EnumVM(Types.TypeVisibility.Public, "Public"),
			new EnumVM(Types.TypeVisibility.NestedPublic, "Public"),
			new EnumVM(Types.TypeVisibility.NestedPrivate, "Private"),
			new EnumVM(Types.TypeVisibility.NestedFamily, "Family"),
			new EnumVM(Types.TypeVisibility.NestedAssembly, "Assembly"),
			new EnumVM(Types.TypeVisibility.NestedFamANDAssem, "Family and Assembly"),
			new EnumVM(Types.TypeVisibility.NestedFamORAssem, "Family or Assembly"),
		};
		public EnumListVM TypeVisibility { get; } = new EnumListVM(typeVisibilityList);

		static readonly EnumVM[] typeLayoutList = new EnumVM[] {
			new EnumVM(Types.TypeLayout.AutoLayout, "Auto"),
			new EnumVM(Types.TypeLayout.SequentialLayout, "Sequential"),
			new EnumVM(Types.TypeLayout.ExplicitLayout, "Explicit"),
		};
		public EnumListVM TypeLayout { get; }

		static readonly EnumVM[] typeSemanticsList = EnumVM.Create(typeof(TypeSemantics));
		public EnumListVM TypeSemantics { get; }

		static readonly EnumVM[] typeStringFormatList = new EnumVM[] {
			new EnumVM(Types.TypeStringFormat.AnsiClass, "Ansi"),
			new EnumVM(Types.TypeStringFormat.UnicodeClass, "Unicode"),
			new EnumVM(Types.TypeStringFormat.AutoClass, "Auto"),
			new EnumVM(Types.TypeStringFormat.CustomFormatClass, "CustomFormat"),
		};
		public EnumListVM TypeStringFormat { get; } = new EnumListVM(typeStringFormatList);

		static readonly EnumVM[] typeCustomFormatList = EnumVM.Create(typeof(TypeCustomFormat));
		public EnumListVM TypeCustomFormat { get; } = new EnumListVM(typeCustomFormatList);

		public TypeAttributes Attributes {
			get {
				var mask = TypeAttributes.VisibilityMask |
							TypeAttributes.LayoutMask |
							TypeAttributes.ClassSemanticsMask |
							TypeAttributes.StringFormatMask |
							TypeAttributes.CustomFormatMask;
				return (attributes & ~mask) |
					((TypeAttributes)((int)(Types.TypeVisibility)TypeVisibility.SelectedItem << 0) & TypeAttributes.VisibilityMask) |
					((TypeAttributes)((int)(Types.TypeLayout)TypeLayout.SelectedItem << 3) & TypeAttributes.LayoutMask) |
					((TypeAttributes)((int)(Types.TypeSemantics)TypeSemantics.SelectedItem << 5) & TypeAttributes.ClassSemanticsMask) |
					((TypeAttributes)((int)(Types.TypeStringFormat)TypeStringFormat.SelectedItem << 16) & TypeAttributes.StringFormatMask) |
					((TypeAttributes)((int)(Types.TypeCustomFormat)TypeCustomFormat.SelectedItem << 22) & TypeAttributes.CustomFormatMask);
			}
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(Abstract));
					OnPropertyChanged(nameof(Sealed));
					OnPropertyChanged(nameof(SpecialName));
					OnPropertyChanged(nameof(Import));
					OnPropertyChanged(nameof(Serializable));
					OnPropertyChanged(nameof(WindowsRuntime));
					OnPropertyChanged(nameof(BeforeFieldInit));
					OnPropertyChanged(nameof(Forwarder));
					OnPropertyChanged(nameof(RTSpecialName));
					OnPropertyChanged(nameof(HasSecurity));
					InitializeTypeKind();
				}
			}
		}
		TypeAttributes attributes;

		public bool Abstract {
			get { return GetFlagValue(TypeAttributes.Abstract); }
			set { SetFlagValue(TypeAttributes.Abstract, value); }
		}

		public bool Sealed {
			get { return GetFlagValue(TypeAttributes.Sealed); }
			set { SetFlagValue(TypeAttributes.Sealed, value); }
		}

		public bool SpecialName {
			get { return GetFlagValue(TypeAttributes.SpecialName); }
			set { SetFlagValue(TypeAttributes.SpecialName, value); }
		}

		public bool Import {
			get { return GetFlagValue(TypeAttributes.Import); }
			set { SetFlagValue(TypeAttributes.Import, value); }
		}

		public bool Serializable {
			get { return GetFlagValue(TypeAttributes.Serializable); }
			set { SetFlagValue(TypeAttributes.Serializable, value); }
		}

		public bool WindowsRuntime {
			get { return GetFlagValue(TypeAttributes.WindowsRuntime); }
			set { SetFlagValue(TypeAttributes.WindowsRuntime, value); }
		}

		public bool BeforeFieldInit {
			get { return GetFlagValue(TypeAttributes.BeforeFieldInit); }
			set { SetFlagValue(TypeAttributes.BeforeFieldInit, value); }
		}

		public bool Forwarder {
			get { return GetFlagValue(TypeAttributes.Forwarder); }
			set { SetFlagValue(TypeAttributes.Forwarder, value); }
		}

		public bool RTSpecialName {
			get { return GetFlagValue(TypeAttributes.RTSpecialName); }
			set { SetFlagValue(TypeAttributes.RTSpecialName, value); }
		}

		public bool HasSecurity {
			get { return GetFlagValue(TypeAttributes.HasSecurity); }
			set { SetFlagValue(TypeAttributes.HasSecurity, value); }
		}

		bool GetFlagValue(TypeAttributes flag) => (Attributes & flag) != 0;

		void SetFlagValue(TypeAttributes flag, bool value) {
			if (value)
				Attributes |= flag;
			else
				Attributes &= ~flag;
		}

		public string Namespace {
			get { return ns; }
			set {
				if (ns != value) {
					ns = value;
					OnPropertyChanged(nameof(Namespace));
				}
			}
		}
		UTF8String ns;

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		UTF8String name;

		public NullableUInt16VM PackingSize { get; }
		public NullableUInt32VM ClassSize { get; }

		public TypeSig BaseTypeSig {
			get { return TypeSigCreator.TypeSig; }
			set { TypeSigCreator.TypeSig = value; }
		}

		public string BaseTypeHeader => string.Format(dnSpy_AsmEditor_Resources.BaseTypeX, TypeSigCreator.TypeSigDnlibFullName);
		public TypeSigCreatorVM TypeSigCreator { get; }
		public CustomAttributesVM CustomAttributesVM { get; }
		public DeclSecuritiesVM DeclSecuritiesVM { get; }
		public GenericParamsVM GenericParamsVM { get; }
		public TypeDefOrRefAndCAsVM<InterfaceImpl> InterfaceImplsVM { get; }

		readonly ModuleDef ownerModule;

		public TypeOptionsVM(TypeDefOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = false,
				OwnerType = ownerType,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			TypeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			TypeSigCreator.PropertyChanged += typeSigCreator_PropertyChanged;

			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService, ownerType, null);
			DeclSecuritiesVM = new DeclSecuritiesVM(ownerModule, decompilerService, ownerType, null);
			GenericParamsVM = new GenericParamsVM(ownerModule, decompilerService, ownerType, null);
			InterfaceImplsVM = new TypeDefOrRefAndCAsVM<InterfaceImpl>(dnSpy_AsmEditor_Resources.EditInterfaceImpl, dnSpy_AsmEditor_Resources.CreateInterfaceImpl, ownerModule, decompilerService, ownerType, null);

			origOptions = options;
			IsNestedType = (options.Attributes & TypeAttributes.VisibilityMask) > TypeAttributes.Public;
			TypeKind = new EnumListVM(typeKindList, (a, b) => OnTypeKindChanged());
			TypeLayout = new EnumListVM(typeLayoutList, (a, b) => InitializeTypeKind());
			TypeSemantics = new EnumListVM(typeSemanticsList, (a, b) => InitializeTypeKind());
			PackingSize = new NullableUInt16VM(a => HasErrorUpdated());
			ClassSize = new NullableUInt32VM(a => HasErrorUpdated());

			Types.TypeVisibility start, end;
			if (!IsNestedType) {
				start = Types.TypeVisibility.NotPublic;
				end = Types.TypeVisibility.Public;
			}
			else {
				start = Types.TypeVisibility.NestedPublic;
				end = Types.TypeVisibility.NestedFamORAssem;
			}
			for (var t = Types.TypeVisibility.NotPublic; t <= Types.TypeVisibility.NestedFamORAssem; t++) {
				if (t < start || t > end)
					TypeVisibility.Items.RemoveAt(TypeVisibility.GetIndex(t));
			}

			InitializeTypeKind();
			TypeSigCreator.CanAddFnPtr = false;
			Reinitialize();
		}

		void typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			InitializeTypeKind();
			if (e.PropertyName == nameof(TypeSigCreator.TypeSigDnlibFullName))
				OnPropertyChanged(nameof(BaseTypeHeader));
			HasErrorUpdated();
		}

		bool IsSystemValueType(IType type) =>
			new SigComparer().Equals(type, ownerModule.CorLibTypes.GetTypeRef("System", "ValueType")) &&
			type.DefinitionAssembly.IsCorLib();

		bool IsSystemEnum(IType type) =>
			new SigComparer().Equals(type, ownerModule.CorLibTypes.GetTypeRef("System", "Enum")) &&
			type.DefinitionAssembly.IsCorLib();

		void InitializeTypeKind() {
			if (OnTypeKindChanged_called)
				return;
			if (IsStaticClass())
				TypeKind.SelectedItem = Types.TypeKind.StaticClass;
			else if (IsInterface())
				TypeKind.SelectedItem = Types.TypeKind.Interface;
			else if (IsStruct())
				TypeKind.SelectedItem = Types.TypeKind.Struct;
			else if (IsEnum())
				TypeKind.SelectedItem = Types.TypeKind.Enum;
			else if (IsDelegate())
				TypeKind.SelectedItem = Types.TypeKind.Delegate;
			else if (IsClass())
				TypeKind.SelectedItem = Types.TypeKind.Class;
			else
				TypeKind.SelectedItem = Types.TypeKind.Unknown;
		}

		bool IsClass() {
			return IsClassBaseType(BaseTypeSig) &&
				(Types.TypeSemantics)TypeSemantics.SelectedItem == Types.TypeSemantics.Class;
		}

		bool IsStaticClass() {
			return new SigComparer().Equals(BaseTypeSig, ownerModule.CorLibTypes.Object.TypeDefOrRef) &&
				BaseTypeSig.DefinitionAssembly.IsCorLib() &&
				(Types.TypeLayout)TypeLayout.SelectedItem == Types.TypeLayout.AutoLayout &&
				(Types.TypeSemantics)TypeSemantics.SelectedItem == Types.TypeSemantics.Class &&
				Abstract &&
				Sealed;
		}

		bool IsInterface() {
			return BaseTypeSig == null &&
				(Types.TypeLayout)TypeLayout.SelectedItem == Types.TypeLayout.AutoLayout &&
				(Types.TypeSemantics)TypeSemantics.SelectedItem == Types.TypeSemantics.Interface &&
				Abstract &&
				!Sealed;
		}

		bool IsStruct() {
			return IsSystemValueType(BaseTypeSig) &&
				(Types.TypeSemantics)TypeSemantics.SelectedItem == Types.TypeSemantics.Class &&
				!Abstract &&
				Sealed;
		}

		bool IsEnum() {
			return IsSystemEnum(BaseTypeSig) &&
				(Types.TypeLayout)TypeLayout.SelectedItem == Types.TypeLayout.AutoLayout &&
				(Types.TypeSemantics)TypeSemantics.SelectedItem == Types.TypeSemantics.Class &&
				!Abstract &&
				Sealed;
		}

		bool IsDelegate() {
			return new SigComparer().Equals(BaseTypeSig, ownerModule.CorLibTypes.GetTypeRef("System", "MulticastDelegate")) &&
				BaseTypeSig.DefinitionAssembly.IsCorLib() &&
				(Types.TypeLayout)TypeLayout.SelectedItem == Types.TypeLayout.AutoLayout &&
				(Types.TypeSemantics)TypeSemantics.SelectedItem == Types.TypeSemantics.Class &&
				!Abstract &&
				Sealed;
		}

		bool OnTypeKindChanged_called;
		void OnTypeKindChanged() {
			if (OnTypeKindChanged_called)
				return;
			OnTypeKindChanged_called = true;
			try {
				OnTypeKindChanged2();
			}
			finally {
				OnTypeKindChanged_called = false;
			}
		}

		void OnTypeKindChanged2() {
			switch ((Types.TypeKind)TypeKind.SelectedItem) {
			case Types.TypeKind.Unknown:
				break;

			case Types.TypeKind.Class:
				if (!IsClassBaseType(BaseTypeSig))
					BaseTypeSig = ownerModule.CorLibTypes.Object;
				TypeSemantics.SelectedItem = Types.TypeSemantics.Class;
				break;

			case Types.TypeKind.StaticClass:
				BaseTypeSig = ownerModule.CorLibTypes.Object;
				TypeLayout.SelectedItem = Types.TypeLayout.AutoLayout;
				TypeSemantics.SelectedItem = Types.TypeSemantics.Class;
				Abstract = true;
				Sealed = true;
				break;

			case Types.TypeKind.Interface:
				BaseTypeSig = null;
				TypeLayout.SelectedItem = Types.TypeLayout.AutoLayout;
				TypeSemantics.SelectedItem = Types.TypeSemantics.Interface;
				Abstract = true;
				Sealed = false;
				break;

			case Types.TypeKind.Struct:
				BaseTypeSig = new ClassSig(ownerModule.CorLibTypes.GetTypeRef("System", "ValueType"));
				TypeSemantics.SelectedItem = Types.TypeSemantics.Class;
				Abstract = false;
				Sealed = true;
				break;

			case Types.TypeKind.Enum:
				BaseTypeSig = new ClassSig(ownerModule.CorLibTypes.GetTypeRef("System", "Enum"));
				TypeLayout.SelectedItem = Types.TypeLayout.AutoLayout;
				TypeSemantics.SelectedItem = Types.TypeSemantics.Class;
				Abstract = false;
				Sealed = true;
				break;

			case Types.TypeKind.Delegate:
				BaseTypeSig = new ClassSig(ownerModule.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
				TypeLayout.SelectedItem = Types.TypeLayout.AutoLayout;
				TypeSemantics.SelectedItem = Types.TypeSemantics.Class;
				Abstract = false;
				Sealed = true;
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		bool IsClassBaseType(IType type) {
			return type != null &&
				!IsSystemEnum(type) &&
				!IsSystemValueType(type);
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public TypeDefOptions CreateTypeDefOptions() => CopyTo(new TypeDefOptions());

		void InitializeFrom(TypeDefOptions options) {
			Attributes = options.Attributes;
			Namespace = options.Namespace;
			Name = options.Name;
			PackingSize.Value = options.PackingSize;
			ClassSize.Value = options.ClassSize;
			BaseTypeSig = options.BaseType.ToTypeSig();
			TypeVisibility.SelectedItem = (Types.TypeVisibility)((int)(options.Attributes & TypeAttributes.VisibilityMask) >> 0);
			TypeLayout.SelectedItem = (Types.TypeLayout)(((int)options.Attributes >> 3) & 3);
			TypeSemantics.SelectedItem = (Types.TypeSemantics)(((int)options.Attributes >> 5) & 1);
			TypeStringFormat.SelectedItem = (Types.TypeStringFormat)(((int)options.Attributes >> 16) & 3);
			TypeCustomFormat.SelectedItem = (Types.TypeCustomFormat)(((int)options.Attributes >> 22) & 3);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
			DeclSecuritiesVM.InitializeFrom(options.DeclSecurities);
			GenericParamsVM.InitializeFrom(options.GenericParameters);
			InterfaceImplsVM.InitializeFrom(options.Interfaces);
		}

		TypeDefOptions CopyTo(TypeDefOptions options) {
			options.Attributes = Attributes;
			options.Namespace = Namespace;
			options.Name = Name;
			options.PackingSize = PackingSize.Value;
			options.ClassSize = ClassSize.Value;
			options.BaseType = BaseTypeSig.ToTypeDefOrRef();
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			options.DeclSecurities.Clear();
			options.DeclSecurities.AddRange(DeclSecuritiesVM.Collection.Select(a => a.CreateDeclSecurityOptions().Create(ownerModule)));
			options.GenericParameters.Clear();
			options.GenericParameters.AddRange(GenericParamsVM.Collection.Select(a => a.CreateGenericParamOptions().Create(ownerModule)));
			options.Interfaces.Clear();
			options.Interfaces.AddRange(InterfaceImplsVM.Collection.Select(a => a.CreateTypeDefOrRefAndCAOptions().CreateInterfaceImpl(ownerModule)));
			if (ModelUtils.GetHasSecurityBit(options.DeclSecurities, options.CustomAttributes))
				options.Attributes |= TypeAttributes.HasSecurity;
			else
				options.Attributes &= ~TypeAttributes.HasSecurity;
			return options;
		}

		public override bool HasError => PackingSize.HasError || ClassSize.HasError;
	}
}
