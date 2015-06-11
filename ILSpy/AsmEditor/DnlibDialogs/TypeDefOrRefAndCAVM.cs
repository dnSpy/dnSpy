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

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class TypeDefOrRefAndCAVM : ViewModelBase
	{
		readonly TypeDefOrRefAndCAOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public string FullName {
			get { return typeSigCreator.TypeSigDnlibFullName; }
		}

		public TypeSigCreatorVM TypeSigCreator {
			get { return typeSigCreator; }
		}
		TypeSigCreatorVM typeSigCreator;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		public TypeDefOrRefAndCAVM(TypeDefOrRefAndCAOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod)
		{
			this.origOptions = options;

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
			TypeSigCreator.PropertyChanged += TypeSigCreator_PropertyChanged;
			this.customAttributesVM = new CustomAttributesVM(ownerModule, language);

			Reinitialize();
		}

		void TypeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "TypeSigDnlibFullName")
				OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void Reinitialize()
		{
			InitializeFrom(origOptions);
		}

		public TypeDefOrRefAndCAOptions CreateTypeDefOrRefAndCAOptions()
		{
			return CopyTo(new TypeDefOrRefAndCAOptions());
		}

		void InitializeFrom(TypeDefOrRefAndCAOptions options)
		{
			TypeSigCreator.TypeSig = options.TypeDefOrRef.ToTypeSig();
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		TypeDefOrRefAndCAOptions CopyTo(TypeDefOrRefAndCAOptions options)
		{
			options.TypeDefOrRef = TypeSigCreator.TypeSig.ToTypeDefOrRef();
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get { return typeSigCreator.HasError; }
		}
	}
}
