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
using ICSharpCode.ILSpy.AsmEditor.DnlibDialogs;

namespace ICSharpCode.ILSpy.AsmEditor.Property
{
	sealed class PropertyOptionsVM : ViewModelBase
	{
		readonly PropertyDefOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public PropertyAttributes Attributes {
			get { return attributes; }
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("SpecialName");
					OnPropertyChanged("RTSpecialName");
					OnPropertyChanged("HasDefault");
					ConstantVM.IsEnabled = HasDefault;
					HasErrorUpdated();
				}
			}
		}
		PropertyAttributes attributes;

		public bool SpecialName {
			get { return GetFlagValue(PropertyAttributes.SpecialName); }
			set { SetFlagValue(PropertyAttributes.SpecialName, value); }
		}

		public bool RTSpecialName {
			get { return GetFlagValue(PropertyAttributes.RTSpecialName); }
			set { SetFlagValue(PropertyAttributes.RTSpecialName, value); }
		}

		public bool HasDefault {
			get { return GetFlagValue(PropertyAttributes.HasDefault); }
			set { SetFlagValue(PropertyAttributes.HasDefault, value); }
		}

		bool GetFlagValue(PropertyAttributes flag)
		{
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(PropertyAttributes flag, bool value)
		{
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

		public PropertySig PropertySig {
			get { return MethodSigCreator.PropertySig; }
			set { MethodSigCreator.PropertySig = value; }
		}

		public string PropertySigHeader {
			get { return string.Format("Property: {0}", MethodSigCreator.HasError ? "null" : MethodSigCreator.PropertySig.ToString()); }
		}

		public MethodSigCreatorVM MethodSigCreator {
			get { return methodSigCreator; }
		}
		readonly MethodSigCreatorVM methodSigCreator;

		public Constant Constant {
			get { return HasDefault ? ownerModule.UpdateRowId(new ConstantUser(constantVM.Value)) : null; }
		}

		public ConstantVM ConstantVM {
			get { return constantVM; }
		}
		readonly ConstantVM constantVM;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		readonly ModuleDef ownerModule;

		public PropertyOptionsVM(PropertyDefOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType)
		{
			this.ownerModule = ownerModule;
			this.origOptions = options;

			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, language) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = true,
				OwnerType = ownerType,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			var methodSigCreatorOptions = new MethodSigCreatorOptions(typeSigCreatorOptions);
			methodSigCreatorOptions.IsPropertySig = true;
			this.methodSigCreator = new MethodSigCreatorVM(methodSigCreatorOptions);
			this.methodSigCreator.PropertyChanged += methodSigCreator_PropertyChanged;
			this.methodSigCreator.ParametersCreateTypeSigArray.PropertyChanged += methodSigCreator_PropertyChanged;
			this.methodSigCreator.ParametersCreateTypeSigArray.TypeSigCreator.CanAddFnPtr = false;
			this.customAttributesVM = new CustomAttributesVM(ownerModule, language);
			this.constantVM = new ConstantVM(ownerModule, options.Constant == null ? null : options.Constant.Value, "Default value for this property");
			this.constantVM.PropertyChanged += constantVM_PropertyChanged;

			ConstantVM.IsEnabled = HasDefault;
			Reinitialize();
		}

		void constantVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsEnabled")
				HasDefault = ConstantVM.IsEnabled;
			HasErrorUpdated();
		}

		void methodSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			HasErrorUpdated();
			OnPropertyChanged("PropertySigHeader");
		}

		void Reinitialize()
		{
			InitializeFrom(origOptions);
		}

		public PropertyDefOptions CreatePropertyDefOptions()
		{
			return CopyTo(new PropertyDefOptions());
		}

		void InitializeFrom(PropertyDefOptions options)
		{
			Attributes = options.Attributes;
			Name = options.Name;
			PropertySig = options.PropertySig;
			if (options.Constant != null) {
				HasDefault = true;
				ConstantVM.Value = options.Constant.Value;
			}
			else {
				HasDefault = false;
				ConstantVM.Value = null;
			}
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		PropertyDefOptions CopyTo(PropertyDefOptions options)
		{
			options.Attributes = Attributes;
			options.Name = Name;
			options.PropertySig = PropertySig;
			options.Constant = HasDefault ? Constant : null;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get {
				return MethodSigCreator.HasError ||
					(HasDefault && ConstantVM.HasError);
			}
		}
	}
}
