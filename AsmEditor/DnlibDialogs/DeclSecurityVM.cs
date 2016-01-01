/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
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

	sealed class DeclSecurityVM : ViewModelBase {
		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
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

		public SecurityAttributesVM SecurityAttributesVM {
			get { return securityAttributesVM; }
		}
		SecurityAttributesVM securityAttributesVM;

		readonly DeclSecurityOptions origOptions;
		readonly ModuleDef ownerModule;
		readonly ILanguageManager languageManager;
		readonly TypeDef ownerType;
		readonly MethodDef ownerMethod;

		public DeclSecurityVM(DeclSecurityOptions options, ModuleDef ownerModule, ILanguageManager languageManager, TypeDef ownerType, MethodDef ownerMethod) {
			this.ownerModule = ownerModule;
			this.languageManager = languageManager;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;
			this.origOptions = options;
			this.customAttributesVM = new CustomAttributesVM(ownerModule, languageManager);
			CustomAttributesVM.PropertyChanged += CustomAttributesVM_PropertyChanged;
			this.declSecVerEnumListVM = new EnumListVM(declSecVerList, (a, b) => OnDeclSecVerChanged());
			this.securityActionEnumListVM = new EnumListVM(secActList, (a, b) => OnSecurityActionChanged());
			this.securityAttributesVM = new SecurityAttributesVM(ownerModule, languageManager, ownerType, ownerMethod);
			this.SecurityAttributesVM.Collection.CollectionChanged += SecurityAttributesVM_CollectionChanged;
			Reinitialize();
		}

		void SecurityAttributesVM_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			HasErrorUpdated();
		}

		void CustomAttributesVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			HasErrorUpdated();
		}

		void OnDeclSecVerChanged() {
			OnPropertyChanged("IsV1");
			OnPropertyChanged("IsV2");
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void OnSecurityActionChanged() {
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public DeclSecurityOptions CreateDeclSecurityOptions() {
			return CopyTo(new DeclSecurityOptions());
		}

		void InitializeFrom(DeclSecurityOptions options) {
			SecurityActionEnumList.SelectedItem = (SecAc)options.Action;
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
			SecurityAttributesVM.Collection.Clear();
			SecurityAttributesVM.Collection.AddRange(options.SecurityAttributes.Select(a => new SecurityAttributeVM(a, ownerModule, languageManager, ownerType, ownerMethod)));
			V1XMLString = options.V1XMLString;
			DeclSecVerEnumList.SelectedItem = options.V1XMLString == null ? DeclSecVer.V2 : DeclSecVer.V1;
		}

		DeclSecurityOptions CopyTo(DeclSecurityOptions options) {
			options.Action = (SecurityAction)(SecAc)SecurityActionEnumList.SelectedItem;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			options.SecurityAttributes.Clear();
			options.SecurityAttributes.AddRange(SecurityAttributesVM.Collection.Select(a => a.CreateSecurityAttribute()));
			options.V1XMLString = IsV1 ? V1XMLString : null;
			return options;
		}

		public override bool HasError {
			get {
				return CustomAttributesVM.HasError ||
					SecurityAttributesVM.Collection.Any(a => a.HasError);
			}
		}
	}
}
