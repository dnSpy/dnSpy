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
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.Search;

namespace dnSpy.AsmEditor.Event {
	sealed class EventOptionsVM : ViewModelBase {
		readonly EventDefOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand PickAddMethodCommand {
			get { return new RelayCommand(a => PickAddMethod()); }
		}

		public ICommand PickInvokeMethodCommand {
			get { return new RelayCommand(a => PickInvokeMethod()); }
		}

		public ICommand PickRemoveMethodCommand {
			get { return new RelayCommand(a => PickRemoveMethod()); }
		}

		public ICommand ClearAddMethodCommand {
			get { return new RelayCommand(a => AddMethod = null, a => AddMethod != null); }
		}

		public ICommand ClearInvokeMethodCommand {
			get { return new RelayCommand(a => InvokeMethod = null, a => InvokeMethod != null); }
		}

		public ICommand ClearRemoveMethodCommand {
			get { return new RelayCommand(a => RemoveMethod = null, a => RemoveMethod != null); }
		}

		public EventAttributes Attributes {
			get { return attributes; }
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("SpecialName");
					OnPropertyChanged("RTSpecialName");
				}
			}
		}
		EventAttributes attributes;

		public bool SpecialName {
			get { return GetFlagValue(EventAttributes.SpecialName); }
			set { SetFlagValue(EventAttributes.SpecialName, value); }
		}

		public bool RTSpecialName {
			get { return GetFlagValue(EventAttributes.RTSpecialName); }
			set { SetFlagValue(EventAttributes.RTSpecialName, value); }
		}

		bool GetFlagValue(EventAttributes flag) {
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(EventAttributes flag, bool value) {
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

		public TypeSig EventTypeSig {
			get { return typeSigCreator.TypeSig; }
			set { typeSigCreator.TypeSig = value; }
		}

		public string EventTypeHeader {
			get { return string.Format("Event Type: {0}", typeSigCreator.TypeSigDnlibFullName); }
		}

		public TypeSigCreatorVM TypeSigCreator {
			get { return typeSigCreator; }
		}
		readonly TypeSigCreatorVM typeSigCreator;

		public string AddMethodFullName {
			get { return GetFullName(AddMethod); }
		}

		public string InvokeMethodFullName {
			get { return GetFullName(InvokeMethod); }
		}

		public string RemoveMethodFullName {
			get { return GetFullName(RemoveMethod); }
		}

		static string GetFullName(MethodDef md) {
			return md == null ? "null" : md.FullName;
		}

		public MethodDef AddMethod {
			get { return addMethod; }
			set {
				if (addMethod != value) {
					addMethod = value;
					OnPropertyChanged("AddMethod");
					OnPropertyChanged("AddMethodFullName");
				}
			}
		}
		MethodDef addMethod;

		public MethodDef InvokeMethod {
			get { return invokeMethod; }
			set {
				if (invokeMethod != value) {
					invokeMethod = value;
					OnPropertyChanged("InvokeMethod");
					OnPropertyChanged("InvokeMethodFullName");
				}
			}
		}
		MethodDef invokeMethod;

		public MethodDef RemoveMethod {
			get { return removeMethod; }
			set {
				if (removeMethod != value) {
					removeMethod = value;
					OnPropertyChanged("RemoveMethod");
					OnPropertyChanged("RemoveMethodFullName");
				}
			}
		}
		MethodDef removeMethod;

		public MethodDefsVM OtherMethodsVM {
			get { return otherMethodsVM; }
		}
		MethodDefsVM otherMethodsVM;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		readonly ModuleDef ownerModule;

		public EventOptionsVM(EventDefOptions options, ModuleDef ownerModule, ILanguageManager languageManager, TypeDef ownerType) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, languageManager) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = true,
				OwnerType = ownerType,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			this.typeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			this.typeSigCreator.PropertyChanged += typeSigCreator_PropertyChanged;

			this.customAttributesVM = new CustomAttributesVM(ownerModule, languageManager);
			this.otherMethodsVM = new MethodDefsVM(ownerModule, languageManager);

			this.origOptions = options;

			this.typeSigCreator.CanAddFnPtr = false;
			Reinitialize();
		}

		void typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "TypeSigDnlibFullName")
				OnPropertyChanged("EventTypeHeader");
			HasErrorUpdated();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		MethodDef PickMethod(MethodDef origMethod) {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			return dnlibTypePicker.GetDnlibType(new SameModuleFileTreeNodeFilter(ownerModule, new FlagsFileTreeNodeFilter(VisibleMembersFlags.MethodDef)), origMethod, ownerModule);
		}

		void PickAddMethod() {
			var method = PickMethod(AddMethod);
			if (method != null)
				AddMethod = method;
		}

		void PickInvokeMethod() {
			var method = PickMethod(InvokeMethod);
			if (method != null)
				InvokeMethod = method;
		}

		void PickRemoveMethod() {
			var method = PickMethod(RemoveMethod);
			if (method != null)
				RemoveMethod = method;
		}

		public EventDefOptions CreateEventDefOptions() {
			return CopyTo(new EventDefOptions());
		}

		void InitializeFrom(EventDefOptions options) {
			Attributes = options.Attributes;
			Name = options.Name;
			EventTypeSig = options.EventType.ToTypeSig();
			AddMethod = options.AddMethod;
			InvokeMethod = options.InvokeMethod;
			RemoveMethod = options.RemoveMethod;
			OtherMethodsVM.InitializeFrom(options.OtherMethods);
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		EventDefOptions CopyTo(EventDefOptions options) {
			options.Attributes = Attributes;
			options.Name = Name;
			options.EventType = EventTypeSig.ToTypeDefOrRef();
			options.AddMethod = AddMethod;
			options.InvokeMethod = InvokeMethod;
			options.RemoveMethod = RemoveMethod;
			options.OtherMethods.Clear();
			options.OtherMethods.AddRange(OtherMethodsVM.Collection.Select(a => a.Method));
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}
	}
}
