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

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	enum SecAc
	{
		ActionNil			= SecurityAction.ActionNil,
		Request				= SecurityAction.Request,
		Demand				= SecurityAction.Demand,
		Assert				= SecurityAction.Assert,
		Deny				= SecurityAction.Deny,
		PermitOnly			= SecurityAction.PermitOnly,
		LinktimeCheck		= SecurityAction.LinktimeCheck,
		InheritanceCheck	= SecurityAction.InheritanceCheck,
		RequestMinimum		= SecurityAction.RequestMinimum,
		RequestOptional		= SecurityAction.RequestOptional,
		RequestRefuse		= SecurityAction.RequestRefuse,
		PrejitGrant			= SecurityAction.PrejitGrant,
		PrejitDenied		= SecurityAction.PrejitDenied,
		NonCasDemand		= SecurityAction.NonCasDemand,
		NonCasLinkDemand	= SecurityAction.NonCasLinkDemand,
		NonCasInheritance	= SecurityAction.NonCasInheritance,
	}

	enum DeclSecVer
	{
		V1,
		V2,
	}

	sealed class DeclSecurityVM : ViewModelBase
	{
		public IEditSecurityAttribute EditSecurityAttribute {
			set { editSecurityAttribute = value; }
		}
		IEditSecurityAttribute editSecurityAttribute;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public string FullName {
			get { return SecurityActionEnumList.SelectedItem.ToString(); }
		}

		public string V1XMLString {
			get { return v1XMLString; }
			set {
				if (v1XMLString != value) {
					v1XMLString = value;
					OnPropertyChanged("V1XMLString");
				}
			}
		}
		string v1XMLString = string.Empty;

		public bool IsV1 {
			get { return (DeclSecVer)DeclSecVerEnumList.SelectedItem == DeclSecVer.V1; }
		}

		public bool IsV2 {
			get { return (DeclSecVer)DeclSecVerEnumList.SelectedItem == DeclSecVer.V2; }
		}

		public EnumListVM DeclSecVerEnumList {
			get { return declSecVerEnumListVM; }
		}
		readonly EnumListVM declSecVerEnumListVM;
		static readonly EnumVM[] declSecVerList = EnumVM.Create(typeof(DeclSecVer));

		public EnumListVM SecurityActionEnumList {
			get { return securityActionEnumListVM; }
		}
		readonly EnumListVM securityActionEnumListVM;
		static readonly EnumVM[] secActList = EnumVM.Create(typeof(SecAc));

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		public MyObservableCollection<SecurityAttributeVM> SecurityAttributeCollection {
			get { return securityAttributeCollection; }
		}
		readonly MyObservableCollection<SecurityAttributeVM> securityAttributeCollection = new MyObservableCollection<SecurityAttributeVM>();

		readonly DeclSecurityOptions origOptions;
		readonly ModuleDef module;
		readonly Language language;

		public DeclSecurityVM(DeclSecurityOptions options, ModuleDef module, Language language)
		{
			this.module = module;
			this.language = language;
			this.origOptions = options;
			this.customAttributesVM = new CustomAttributesVM(module, language);
			CustomAttributesVM.PropertyChanged += CustomAttributesVM_PropertyChanged;
			this.declSecVerEnumListVM = new EnumListVM(declSecVerList, (a, b) => OnDeclSecVerChanged());
			this.securityActionEnumListVM = new EnumListVM(secActList, (a, b) => OnSecurityActionChanged());
			this.SecurityAttributeCollection.CollectionChanged += SecurityAttributeCollection_CollectionChanged;
			Reinitialize();
		}

		void SecurityAttributeCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			HasErrorUpdated();
		}

		void CustomAttributesVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			HasErrorUpdated();
		}

		void OnDeclSecVerChanged()
		{
			OnPropertyChanged("IsV1");
			OnPropertyChanged("IsV2");
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void OnSecurityActionChanged()
		{
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		public void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			if (editSecurityAttribute == null)
				throw new InvalidOperationException();
			int index = SecurityAttributeCollection.SelectedIndex;
			var caVm = editSecurityAttribute.Edit("Edit Security Attribute", new SecurityAttributeVM(SecurityAttributeCollection[index].CreateSecurityAttribute(), new TypeSigCreatorOptions(module, language)));
			if (caVm != null) {
				SecurityAttributeCollection[index] = caVm;
				SecurityAttributeCollection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return SecurityAttributeCollection.SelectedIndex >= 0 && SecurityAttributeCollection.SelectedIndex < SecurityAttributeCollection.Count;
		}

		void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			if (editSecurityAttribute == null)
				throw new InvalidOperationException();
			var caVm = editSecurityAttribute.Edit("Create Security Attribute", new SecurityAttributeVM(new SecurityAttribute(), new TypeSigCreatorOptions(module, language)));
			if (caVm != null) {
				SecurityAttributeCollection.Add(caVm);
				SecurityAttributeCollection.SelectedIndex = SecurityAttributeCollection.Count - 1;
			}
		}

		bool AddCurrentCanExecute()
		{
			return true;
		}

		void Reinitialize()
		{
			InitializeFrom(origOptions);
		}

		public DeclSecurityOptions CreateDeclSecurityOptions()
		{
			return CopyTo(new DeclSecurityOptions());
		}

		void InitializeFrom(DeclSecurityOptions options)
		{
			SecurityActionEnumList.SelectedItem = (SecAc)(options.Action & SecurityAction.ActionMask);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
			SecurityAttributeCollection.Clear();
			SecurityAttributeCollection.AddRange(options.SecurityAttributes.Select(a => new SecurityAttributeVM(a, new TypeSigCreatorOptions(module, language))));
			V1XMLString = options.V1XMLString;
			DeclSecVerEnumList.SelectedItem = options.V1XMLString == null ? DeclSecVer.V2 : DeclSecVer.V1;
		}

		DeclSecurityOptions CopyTo(DeclSecurityOptions options)
		{
			options.Action = (SecurityAction)(SecAc)SecurityActionEnumList.SelectedItem;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.CustomAttributeCollection.Select(a => a.CreateCustomAttributeOptions().Create()));
			options.SecurityAttributes.Clear();
			options.SecurityAttributes.AddRange(SecurityAttributeCollection.Select(a => a.CreateSecurityAttribute()));
			options.V1XMLString = IsV1 ? V1XMLString : null;
			return options;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get {
				return CustomAttributesVM.HasError ||
					SecurityAttributeCollection.Any(a => a.HasError);
			}
		}
	}
}
