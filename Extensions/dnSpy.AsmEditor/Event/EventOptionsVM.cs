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

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;

namespace dnSpy.AsmEditor.Event {
	sealed class EventOptionsVM : ViewModelBase {
		readonly EventDefOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand PickAddMethodCommand => new RelayCommand(a => PickAddMethod());
		public ICommand PickInvokeMethodCommand => new RelayCommand(a => PickInvokeMethod());
		public ICommand PickRemoveMethodCommand => new RelayCommand(a => PickRemoveMethod());
		public ICommand ClearAddMethodCommand => new RelayCommand(a => AddMethod = null, a => AddMethod != null);
		public ICommand ClearInvokeMethodCommand => new RelayCommand(a => InvokeMethod = null, a => InvokeMethod != null);
		public ICommand ClearRemoveMethodCommand => new RelayCommand(a => RemoveMethod = null, a => RemoveMethod != null);

		public EventAttributes Attributes {
			get => attributes;
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(SpecialName));
					OnPropertyChanged(nameof(RTSpecialName));
				}
			}
		}
		EventAttributes attributes;

		public bool SpecialName {
			get => GetFlagValue(EventAttributes.SpecialName);
			set => SetFlagValue(EventAttributes.SpecialName, value);
		}

		public bool RTSpecialName {
			get => GetFlagValue(EventAttributes.RTSpecialName);
			set => SetFlagValue(EventAttributes.RTSpecialName, value);
		}

		bool GetFlagValue(EventAttributes flag) => (Attributes & flag) != 0;

		void SetFlagValue(EventAttributes flag, bool value) {
			if (value)
				Attributes |= flag;
			else
				Attributes &= ~flag;
		}

		public string Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		UTF8String name;

		public TypeSig EventTypeSig {
			get => TypeSigCreator.TypeSig;
			set => TypeSigCreator.TypeSig = value;
		}

		public string EventTypeHeader => string.Format(dnSpy_AsmEditor_Resources.EventType, TypeSigCreator.TypeSigDnlibFullName);
		public TypeSigCreatorVM TypeSigCreator { get; }
		public string AddMethodFullName => GetFullName(AddMethod);
		public string InvokeMethodFullName => GetFullName(InvokeMethod);
		public string RemoveMethodFullName => GetFullName(RemoveMethod);
		static string GetFullName(MethodDef md) => md == null ? "null" : md.FullName;

		public MethodDef AddMethod {
			get => addMethod;
			set {
				if (addMethod != value) {
					addMethod = value;
					OnPropertyChanged(nameof(AddMethod));
					OnPropertyChanged(nameof(AddMethodFullName));
				}
			}
		}
		MethodDef addMethod;

		public MethodDef InvokeMethod {
			get => invokeMethod;
			set {
				if (invokeMethod != value) {
					invokeMethod = value;
					OnPropertyChanged(nameof(InvokeMethod));
					OnPropertyChanged(nameof(InvokeMethodFullName));
				}
			}
		}
		MethodDef invokeMethod;

		public MethodDef RemoveMethod {
			get => removeMethod;
			set {
				if (removeMethod != value) {
					removeMethod = value;
					OnPropertyChanged(nameof(RemoveMethod));
					OnPropertyChanged(nameof(RemoveMethodFullName));
				}
			}
		}
		MethodDef removeMethod;

		public MethodDefsVM OtherMethodsVM { get; }
		public CustomAttributesVM CustomAttributesVM { get; }

		readonly ModuleDef ownerModule;

		public EventOptionsVM(EventDefOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType) {
			this.ownerModule = ownerModule;
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = true,
				OwnerType = ownerType,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			TypeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			TypeSigCreator.PropertyChanged += typeSigCreator_PropertyChanged;

			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);
			OtherMethodsVM = new MethodDefsVM(ownerModule, decompilerService);

			origOptions = options;

			TypeSigCreator.CanAddFnPtr = false;
			Reinitialize();
		}

		void typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(TypeSigCreator.TypeSigDnlibFullName))
				OnPropertyChanged(nameof(EventTypeHeader));
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);

		MethodDef PickMethod(MethodDef origMethod) {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			return dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Method, new SameModuleDocumentTreeNodeFilter(ownerModule, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.MethodDef)), origMethod, ownerModule);
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

		public EventDefOptions CreateEventDefOptions() => CopyTo(new EventDefOptions());

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
