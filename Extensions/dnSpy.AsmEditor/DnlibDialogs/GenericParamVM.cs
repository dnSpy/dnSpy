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

using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	enum GPVariance {
		NonVariant		= (int)GenericParamAttributes.NonVariant >> 0,
		Covariant		= (int)GenericParamAttributes.Covariant >> 0,
		Contravariant	= (int)GenericParamAttributes.Contravariant >> 0,
	}

	sealed class GenericParamVM : ViewModelBase {
		readonly GenericParamOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		public string FullName {
			get {
				var sb = new StringBuilder();

				if (Number.HasError)
					sb.Append("???");
				else
					sb.Append(string.Format(dnSpy_AsmEditor_Resources.GenericParameterNumber, Number.Value));

				sb.Append(' ');
				sb.Append(string.IsNullOrEmpty(Name) ? dnSpy_AsmEditor_Resources.NoName : Name);

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
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(ReferenceTypeConstraint));
					OnPropertyChanged(nameof(NotNullableValueTypeConstraint));
					OnPropertyChanged(nameof(DefaultConstructorConstraint));
				}
			}
		}
		GenericParamAttributes attributes;

		public EnumListVM GPVarianceVM { get; }

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

		bool GetFlagValue(GenericParamAttributes flag) => (Attributes & flag) != 0;

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
					OnPropertyChanged(nameof(Name));
					OnPropertyChanged(nameof(FullName));
				}
			}
		}
		UTF8String name;

		public UInt16VM Number { get; }
		public TypeDefOrRefAndCAsVM<GenericParamConstraint> TypeDefOrRefAndCAsVM { get; }
		public CustomAttributesVM CustomAttributesVM { get; }
		public TypeSigCreatorVM TypeSigCreator { get; }

		readonly ModuleDef ownerModule;

		public GenericParamVM(GenericParamOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod) {
			this.ownerModule = ownerModule;
			origOptions = options;
			Number = new UInt16VM(a => { OnPropertyChanged(nameof(FullName)); HasErrorUpdated(); });
			TypeDefOrRefAndCAsVM = new TypeDefOrRefAndCAsVM<GenericParamConstraint>(dnSpy_AsmEditor_Resources.EditGenericParameterConstraint, dnSpy_AsmEditor_Resources.CreateGenericParameterConstraint, ownerModule, decompilerService, ownerType, ownerMethod);
			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);
			GPVarianceVM = new EnumListVM(EnumVM.Create(typeof(GPVariance)));

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

			Reinitialize();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public GenericParamOptions CreateGenericParamOptions() => CopyTo(new GenericParamOptions());

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

		public override bool HasError => Number.HasError;
	}
}
