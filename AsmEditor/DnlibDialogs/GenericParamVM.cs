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

using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.DnlibDialogs {
	enum GPVariance {
		NonVariant		= (int)GenericParamAttributes.NonVariant >> 0,
		Covariant		= (int)GenericParamAttributes.Covariant >> 0,
		Contravariant	= (int)GenericParamAttributes.Contravariant >> 0,
	}

	sealed class GenericParamVM : ViewModelBase {
		readonly GenericParamOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public string FullName {
			get {
				var sb = new StringBuilder();

				if (Number.HasError)
					sb.Append("???");
				else
					sb.Append(string.Format("gparam({0})", Number.Value));

				sb.Append(' ');
				sb.Append(string.IsNullOrEmpty(Name) ? "<<no-name>>" : Name);

				return sb.ToString();
			}
		}

		public GenericParamAttributes Attributes {
			get {
				var mask = GenericParamAttributes.VarianceMask;
				return (attributes & ~mask) |
					((GenericParamAttributes)((int)(GPVariance)GPVarianceVM.SelectedItem << 0) & GenericParamAttributes.VarianceMask);
			}
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("ReferenceTypeConstraint");
					OnPropertyChanged("NotNullableValueTypeConstraint");
					OnPropertyChanged("DefaultConstructorConstraint");
				}
			}
		}
		GenericParamAttributes attributes;

		public EnumListVM GPVarianceVM {
			get { return gpVarianceVM; }
		}
		readonly EnumListVM gpVarianceVM;

		public bool ReferenceTypeConstraint {
			get { return GetFlagValue(GenericParamAttributes.ReferenceTypeConstraint); }
			set { SetFlagValue(GenericParamAttributes.ReferenceTypeConstraint, value); }
		}

		public bool NotNullableValueTypeConstraint {
			get { return GetFlagValue(GenericParamAttributes.NotNullableValueTypeConstraint); }
			set { SetFlagValue(GenericParamAttributes.NotNullableValueTypeConstraint, value); }
		}

		public bool DefaultConstructorConstraint {
			get { return GetFlagValue(GenericParamAttributes.DefaultConstructorConstraint); }
			set { SetFlagValue(GenericParamAttributes.DefaultConstructorConstraint, value); }
		}

		bool GetFlagValue(GenericParamAttributes flag) {
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(GenericParamAttributes flag, bool value) {
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
					OnPropertyChanged("FullName");
				}
			}
		}
		UTF8String name;

		public UInt16VM Number {
			get { return number; }
		}
		UInt16VM number;

		public TypeDefOrRefAndCAsVM<GenericParamConstraint> TypeDefOrRefAndCAsVM {
			get { return typeDefOrRefAndCAsVM; }
		}
		TypeDefOrRefAndCAsVM<GenericParamConstraint> typeDefOrRefAndCAsVM;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		public TypeSigCreatorVM TypeSigCreator {
			get { return typeSigCreator; }
		}
		TypeSigCreatorVM typeSigCreator;

		readonly ModuleDef ownerModule;

		public GenericParamVM(GenericParamOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod) {
			this.ownerModule = ownerModule;
			this.origOptions = options;
			this.number = new UInt16VM(a => { OnPropertyChanged("FullName"); HasErrorUpdated(); });
			this.typeDefOrRefAndCAsVM = new TypeDefOrRefAndCAsVM<GenericParamConstraint>("Edit Generic Parameter Constraint", "Create Generic Parameter Constraint", ownerModule, language, ownerType, ownerMethod);
			this.customAttributesVM = new CustomAttributesVM(ownerModule, language);
			this.gpVarianceVM = new EnumListVM(EnumVM.Create(typeof(GPVariance)));

			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, language) {
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
			this.typeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);

			Reinitialize();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public GenericParamOptions CreateGenericParamOptions() {
			return CopyTo(new GenericParamOptions());
		}

		void InitializeFrom(GenericParamOptions options) {
			Number.Value = options.Number;
			Attributes = options.Flags;
			Name = options.Name;
			GPVarianceVM.SelectedItem = (GPVariance)((int)(options.Flags & GenericParamAttributes.VarianceMask) >> 0);
			TypeSigCreator.TypeSig = options.Kind.ToTypeSig();
			TypeDefOrRefAndCAsVM.InitializeFrom(options.GenericParamConstraints);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		GenericParamOptions CopyTo(GenericParamOptions options) {
			options.Number = Number.Value;
			options.Flags = Attributes;
			options.Name = Name;
			options.Kind = TypeSigCreator.TypeSig.ToTypeDefOrRef();
			options.GenericParamConstraints.Clear();
			options.GenericParamConstraints.AddRange(TypeDefOrRefAndCAsVM.Collection.Select(a => a.CreateTypeDefOrRefAndCAOptions().CreateGenericParamConstraint(ownerModule)));
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError {
			get { return Number.HasError; }
		}
	}
}
