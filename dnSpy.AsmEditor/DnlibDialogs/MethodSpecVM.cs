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

using System;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MethodSpecVM : ViewModelBase {
		readonly MethodSpecOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand PickMethodCommand {
			get { return new RelayCommand(a => PickMethod()); }
		}

		public IMethodDefOrRef Method {
			get { return method; }
			set {
				if (method != value) {
					method = value;
					OnPropertyChanged("Method");
					OnPropertyChanged("MethodFullName");
					HasErrorUpdated();
				}
			}
		}
		IMethodDefOrRef method;

		public string MethodFullName {
			get {
				if (Method == null)
					return "null";
				return Method.FullName;
			}
		}

		public CreateTypeSigArrayVM CreateTypeSigArrayVM {
			get { return createTypeSigArrayVM; }
		}
		CreateTypeSigArrayVM createTypeSigArrayVM;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		public MethodSpecVM(MethodSpecOptions options, TypeSigCreatorOptions typeSigCreatorOptions) {
			this.origOptions = options;
			this.typeSigCreatorOptions = typeSigCreatorOptions;
			this.customAttributesVM = new CustomAttributesVM(typeSigCreatorOptions.OwnerModule, typeSigCreatorOptions.LanguageManager);

			this.typeSigCreatorOptions.CanAddGenericMethodVar = true;
			this.typeSigCreatorOptions.CanAddGenericTypeVar = true;
			this.typeSigCreatorOptions.IsLocal = false;
			this.typeSigCreatorOptions.NullTypeSigAllowed = false;

			this.createTypeSigArrayVM = new CreateTypeSigArrayVM(typeSigCreatorOptions, null);

			Reinitialize();
		}

		void PickMethod() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newMethod = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Method, new FlagsFileTreeNodeFilter(VisibleMembersFlags.MethodDef), Method, typeSigCreatorOptions.OwnerModule);
			if (newMethod != null)
				Method = newMethod;
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public MethodSpecOptions CreateMethodSpecOptions() {
			return CopyTo(new MethodSpecOptions());
		}

		void InitializeFrom(MethodSpecOptions options) {
			this.Method = options.Method;
			var gim = options.Instantiation as GenericInstMethodSig;
			CreateTypeSigArrayVM.TypeSigCollection.Clear();
			if (gim != null)
				CreateTypeSigArrayVM.TypeSigCollection.AddRange(gim.GenericArguments);
			this.CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		MethodSpecOptions CopyTo(MethodSpecOptions options) {
			options.Method = this.Method;
			options.Instantiation = new GenericInstMethodSig(CreateTypeSigArrayVM.TypeSigCollection);
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError {
			get { return Method == null; }
		}
	}
}
