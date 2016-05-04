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
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Files;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MemberRefVM : ViewModelBase {
		readonly MemberRefOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand InitializeFromFieldCommand => new RelayCommand(a => InitializeFromField());
		public ICommand InitializeFromMethodCommand => new RelayCommand(a => InitializeFromMethod());
		public ICommand PickTypeCommand => new RelayCommand(a => PickType());
		public ICommand PickTypeSpecCommand => new RelayCommand(a => PickTypeSpec());
		public ICommand PickMethodDefCommand => new RelayCommand(a => PickMethodDef());
		public ICommand PickModuleRefCommand => new RelayCommand(a => PickModuleRef());

		public IMemberRefParent Class {
			get { return @class; }
			set {
				if (@class != value) {
					@class = value;
					OnPropertyChanged(nameof(Class));
					OnPropertyChanged(nameof(ClassFullName));
					HasErrorUpdated();
				}
			}
		}
		IMemberRefParent @class;

		public string ClassFullName {
			get {
				if (Class == null)
					return "null";
				return Class.ToString();
			}
		}

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		string name;

		public TypeSigCreatorVM TypeSigCreatorVM { get; }
		public MethodSigCreatorVM MethodSigCreatorVM { get; }
		public CustomAttributesVM CustomAttributesVM { get; }
		public bool IsField { get; }
		public bool IsMethod => !IsField;

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		public MemberRefVM(MemberRefOptions options, TypeSigCreatorOptions typeSigCreatorOptions, bool isField) {
			this.IsField = isField;
			this.typeSigCreatorOptions = typeSigCreatorOptions.Clone();
			this.origOptions = options;
			this.CustomAttributesVM = new CustomAttributesVM(typeSigCreatorOptions.OwnerModule, typeSigCreatorOptions.LanguageManager);

			this.typeSigCreatorOptions.CanAddGenericMethodVar = true;
			this.typeSigCreatorOptions.CanAddGenericTypeVar = true;
			this.typeSigCreatorOptions.IsLocal = false;
			this.typeSigCreatorOptions.NullTypeSigAllowed = false;

			this.TypeSigCreatorVM = new TypeSigCreatorVM(this.typeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateFieldTypeSig));
			TypeSigCreatorVM.PropertyChanged += (s, e) => HasErrorUpdated();

			var mopts = new MethodSigCreatorOptions(this.typeSigCreatorOptions.Clone());
			mopts.CanHaveSentinel = true;
			this.MethodSigCreatorVM = new MethodSigCreatorVM(mopts);
			MethodSigCreatorVM.PropertyChanged += (s, e) => HasErrorUpdated();

			Reinitialize();
		}

		void InitializeFromField() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newField = dnlibTypePicker.GetDnlibType<IField>(dnSpy_AsmEditor_Resources.Pick_Field, new FlagsFileTreeNodeFilter(VisibleMembersFlags.FieldDef), null, typeSigCreatorOptions.OwnerModule);
			if (newField != null)
				InitializeFrom(new MemberRefOptions(typeSigCreatorOptions.OwnerModule.Import(newField)));
		}

		void InitializeFromMethod() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newMethod = dnlibTypePicker.GetDnlibType<IMethod>(dnSpy_AsmEditor_Resources.Pick_Method, new FlagsFileTreeNodeFilter(VisibleMembersFlags.MethodDef), null, typeSigCreatorOptions.OwnerModule);
			if (newMethod != null) {
				var mr = typeSigCreatorOptions.OwnerModule.Import(newMethod) as MemberRef;
				if (mr != null)
					InitializeFrom(new MemberRefOptions(mr));
			}
		}

		void PickType() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newType = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Type, new FlagsFileTreeNodeFilter(VisibleMembersFlags.TypeDef), Class as ITypeDefOrRef, typeSigCreatorOptions.OwnerModule);
			if (newType != null)
				Class = newType;
		}

		void PickTypeSpec() {
			if (typeSigCreator == null)
				throw new InvalidOperationException();
			bool canceled;
			var newType = typeSigCreator.Create(typeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateTypeSpec), (Class as ITypeDefOrRef).ToTypeSig(), out canceled);
			if (!canceled)
				Class = newType.ToTypeDefOrRef();
		}

		void PickMethodDef() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newMethod = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Method, new SameAssemblyFileTreeNodeFilter(typeSigCreatorOptions.OwnerModule, new FlagsFileTreeNodeFilter(VisibleMembersFlags.MethodDef)), Class as IMethod, typeSigCreatorOptions.OwnerModule);
			if (newMethod != null) {
				var md = newMethod as MethodDef;
				Debug.Assert(md != null);
				if (md != null)
					Class = md;
			}
		}

		void PickModuleRef() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var file = dnlibTypePicker.GetDnlibType<IDnSpyFile>(dnSpy_AsmEditor_Resources.Pick_Module, new SameAssemblyFileTreeNodeFilter(typeSigCreatorOptions.OwnerModule, new FlagsFileTreeNodeFilter(VisibleMembersFlags.ModuleDef)), null, typeSigCreatorOptions.OwnerModule);
			if (file != null) {
				var module = file.ModuleDef;
				if (module != null) {
					var modRef = new ModuleRefUser(typeSigCreatorOptions.OwnerModule, module.Name);
					Class = typeSigCreatorOptions.OwnerModule.UpdateRowId(modRef);
				}
			}
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public MemberRefOptions CreateMemberRefOptions() => CopyTo(new MemberRefOptions());

		void InitializeFrom(MemberRefOptions options) {
			this.Class = options.Class;
			this.Name = options.Name;
			if (IsField) {
				var fs = options.Signature as FieldSig;
				TypeSigCreatorVM.TypeSig = fs?.Type;
			}
			else
				MethodSigCreatorVM.MethodSig = options.Signature as MethodSig;
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		MemberRefOptions CopyTo(MemberRefOptions options) {
			options.Class = this.Class;
			options.Name = this.Name;
			if (IsField)
				options.Signature = new FieldSig(TypeSigCreatorVM.TypeSig);
			else
				options.Signature = MethodSigCreatorVM.MethodSig;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError {
			get {
				return
					Class == null ||
					(IsField && TypeSigCreatorVM.HasError) ||
					(IsMethod && MethodSigCreatorVM.HasError);
			}
		}
	}
}
