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
using System.Collections.Generic;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class TypeSigCreatorVM : ViewModelBase {
		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public IShowWarningMessage ShowWarningMessage {
			set { showWarningMessage = value; }
		}
		IShowWarningMessage showWarningMessage;

		public ICreateTypeSigArray CreateTypeSigArray {
			set { createTypeSigArray = value; }
		}
		ICreateTypeSigArray createTypeSigArray;

		public ICreateMethodPropertySig CreateMethodPropertySig {
			set { createMethodPropertySig = value; }
		}
		ICreateMethodPropertySig createMethodPropertySig;

		public string Title {
			get {
				if (!string.IsNullOrEmpty(options.Title))
					return options.Title;
				return dnSpy_AsmEditor_Resources.CreateTypeSig;
			}
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
					OnPropertyChanged(nameof(CanAddLeafTypeSig));
					OnPropertyChanged(nameof(CanAddNonLeafTypeSig));
					OnPropertyChanged(nameof(CanAddGeneric));
				}
			}
		}
		bool isEnabled = true;

		public TypeSig TypeSig {
			get { return typeSig; }
			set {
				if (typeSig != value) {
					bool nullChange = typeSig == null || value == null;
					typeSig = value;
					OnPropertyChanged(nameof(TypeSig));
					OnPropertyChanged(nameof(TypeSigDnlibFullName));
					OnPropertyChanged(nameof(TypeSigLanguageFullName));
					OnPropertyChanged(nameof(IsValidTypeSig));
					OnPropertyChanged(nameof(CanAddLeafTypeSig));
					OnPropertyChanged(nameof(CanAddNonLeafTypeSig));
					OnPropertyChanged(nameof(CanShowTypeFullName));
					if (nullChange) {
						OnPropertyChanged(nameof(AddingLeafTypeSig));
						OnPropertyChanged(nameof(AddingNonLeafTypeSig));
					}
					HasErrorUpdated();
				}
			}
		}
		TypeSig typeSig;

		public bool CanShowTypeFullName => ShowTypeFullName && IsValidTypeSig;

		public bool ShowTypeFullName {
			get { return showTypeFullName; }
			set {
				if (showTypeFullName != value) {
					showTypeFullName = value;
					OnPropertyChanged(nameof(ShowTypeFullName));
					OnPropertyChanged(nameof(CanShowTypeFullName));
				}
			}
		}
		bool showTypeFullName = true;

		public bool CanAddGeneric => IsEnabled && (options.CanAddGenericTypeVar || options.CanAddGenericMethodVar);
		public bool IsValidTypeSig => options.NullTypeSigAllowed || TypeSig != null;
		public bool CanAddLeafTypeSig => IsEnabled && TypeSig == null;
		public bool AddingLeafTypeSig => TypeSig == null;
		public bool CanAddNonLeafTypeSig => IsEnabled && !(TypeSig is PinnedSig) && TypeSig != null;
		public bool AddingNonLeafTypeSig => TypeSig != null;
		public string TypeSigDnlibFullName => TypeSig == null ? "null" : TypeSig.FullName;

		public string TypeSigLanguageFullName {
			get {
				if (TypeSig == null)
					return "null";
				var output = new StringBuilderTextColorOutput();
				Language.WriteType(output, TypeSig.ToTypeDefOrRef(), true);
				return output.ToString();
			}
		}

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand ClearTypeSigCommand => new RelayCommand(a => TypeSig = null, a => IsEnabled && TypeSig != null);
		public ICommand RemoveLastTypeSigCommand => new RelayCommand(a => RemoveLastTypeSig(), a => IsEnabled && TypeSig != null);
		public ICommand AddTypeDefOrRefCommand => new RelayCommand(a => AddTypeDefOrRef(), a => AddTypeDefOrRefCanExecute());
		public ICommand AddGenericVarCommand => new RelayCommand(a => AddGenericVar(), a => AddGenericVarCanExecute());
		public ICommand AddGenericMVarCommand => new RelayCommand(a => AddGenericMVar(), a => AddGenericMVarCanExecute());
		public ICommand AddFnPtrSigCommand => new RelayCommand(a => AddFnPtrSig(), a => AddFnPtrSigCanExecute());
		public ICommand AddGenericInstSigCommand => new RelayCommand(a => AddGenericInstSig(), a => AddGenericInstSigCanExecute());
		public ICommand AddPtrSigCommand => new RelayCommand(a => AddPtrSig(), a => AddPtrSigCanExecute());
		public ICommand AddByRefSigCommand => new RelayCommand(a => AddByRefSig(), a => AddByRefSigCanExecute());
		public ICommand AddSZArraySigCommand => new RelayCommand(a => AddSZArraySig(), a => AddSZArraySigCanExecute());
		public ICommand AddArraySigCommand => new RelayCommand(a => AddArraySig(), a => AddArraySigCanExecute());
		public ICommand AddCModReqdSigCommand => new RelayCommand(a => AddCModReqdSig(), a => AddCModReqdSigCanExecute());
		public ICommand AddCModOptSigCommand => new RelayCommand(a => AddCModOptSig(), a => AddCModOptSigCanExecute());
		public ICommand AddPinnedSigCommand => new RelayCommand(a => AddPinnedSig(), a => AddPinnedSigCanExecute());
		public IEnumerable<ILanguage> AllLanguages => options.LanguageManager.AllLanguages;

		public ILanguage Language {
			get { return options.Language; }
			set {
				if (options.Language != value) {
					options.Language = value;
					OnPropertyChanged(nameof(Language));
					OnPropertyChanged(nameof(TypeSigLanguageFullName));
				}
			}
		}

		public UInt32VM GenericVariableNumber { get; }

		readonly TypeSigCreatorOptions options;
		readonly TypeSig defaultTypeSig;

		public TypeSigCreatorVM(TypeSigCreatorOptions options, TypeSig defaultTypeSig = null) {
			this.options = options.Clone();
			this.defaultTypeSig = defaultTypeSig;
			this.ArrayRank = new UInt32VM(2, a => { });
			this.ArraySizes = new UInt32ListDataFieldVM(a => { }) {
				Min = ModelUtils.COMPRESSED_UINT32_MIN,
				Max = ModelUtils.COMPRESSED_UINT32_MAX,
			};
			this.ArrayLowerBounds = new Int32ListDataFieldVM(a => { }) {
				Min = ModelUtils.COMPRESSED_INT32_MIN,
				Max = ModelUtils.COMPRESSED_INT32_MAX,
			};
			this.GenericVariableNumber = new UInt32VM(0, a => { });

			Reinitialize();
		}

		void Reinitialize() => this.TypeSig = defaultTypeSig;

		void ShowWarning(Guid? guid, string msg) {
			if (showWarningMessage == null)
				throw new InvalidOperationException();
			showWarningMessage.Show(guid, msg);
		}

		void RemoveLastTypeSig() {
			if (TypeSig != null)
				TypeSig = TypeSig.Next;
		}

		TypeDefOrRefSig GetTypeSig() => GetTypeSig(dnSpy_AsmEditor_Resources.Pick_Type, VisibleMembersFlags.TypeDef);

		TypeDefOrRefSig GetTypeSig(string title, VisibleMembersFlags flags) {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();

			var type = dnlibTypePicker.GetDnlibType<ITypeDefOrRef>(title, new FlagsFileTreeNodeFilter(flags), null, options.OwnerModule);
			if (type == null)
				return null;

			var corLibSig = options.OwnerModule.CorLibTypes.GetCorLibTypeSig(type);
			if (corLibSig != null)
				return corLibSig;
			else {
				var td = type.ResolveTypeDef();
				bool isValueType;
				if (td == null)
					isValueType = false;    // Most types aren't value types
				else
					isValueType = td.IsValueType;

				if (isValueType)
					return new ValueTypeSig(type);
				else
					return new ClassSig(type);
			}
		}

		void AddTypeDefOrRef() => TypeSig = GetTypeSig();
		bool AddTypeDefOrRefCanExecute() => CanAddLeafTypeSig;
		void AddGenericVar() => TypeSig = new GenericVar(GenericVariableNumber.Value, options.OwnerType);
		bool AddGenericVarCanExecute() => !GenericVariableNumber.HasError && options.CanAddGenericTypeVar && CanAddLeafTypeSig;
		void AddGenericMVar() => TypeSig = new GenericMVar(GenericVariableNumber.Value, options.OwnerMethod);
		bool AddGenericMVarCanExecute() => !GenericVariableNumber.HasError && options.CanAddGenericMethodVar && CanAddLeafTypeSig;

		void AddFnPtrSig() {
			if (createMethodPropertySig == null)
				throw new InvalidOperationException();

			var createOptions = new MethodSigCreatorOptions(options.Clone(dnSpy_AsmEditor_Resources.CreateFnPtrMethodSignature));
			createOptions.IsPropertySig = false;
			createOptions.CanHaveSentinel = true;

			var fnPtrSig = TypeSig as FnPtrSig;
			var msig = fnPtrSig == null ? null : fnPtrSig.MethodSig;
			var sig = createMethodPropertySig.Create(createOptions, msig);
			if (sig == null)
				return;

			TypeSig = new FnPtrSig(sig);
		}

		bool AddFnPtrSigCanExecute() => CanAddFnPtr && CanAddLeafTypeSig;

		public bool CanAddFnPtr {
			get { return canAddFnPtr; }
			set {
				if (canAddFnPtr != value) {
					canAddFnPtr = value;
					OnPropertyChanged(nameof(CanAddFnPtr));
				}
			}
		}
		bool canAddFnPtr = true;

		void AddGenericInstSig() {
			var origType = GetTypeSig(dnSpy_AsmEditor_Resources.Pick_GenericType, VisibleMembersFlags.GenericTypeDef);
			if (origType == null)
				return;
			var type = origType as ClassOrValueTypeSig;
			if (type == null) {
				ShowWarning(null, dnSpy_AsmEditor_Resources.TypeMustBeGeneric);
				return;
			}
			var genericType = type.TypeDefOrRef.ResolveTypeDef();
			if (genericType == null) {
				ShowWarning(null, dnSpy_AsmEditor_Resources.CouldNotResolveType);
				return;
			}
			if (genericType.GenericParameters.Count == 0) {
				ShowWarning(null, string.Format(dnSpy_AsmEditor_Resources.NotGenericType, genericType.FullName));
				return;
			}

			var genArgs = createTypeSigArray.Create(options.Clone(dnSpy_AsmEditor_Resources.CreateGenericInstanceTypeArguments), genericType.GenericParameters.Count, null);
			if (genArgs == null)
				return;

			TypeSig = new GenericInstSig(type, genArgs);
		}

		bool AddGenericInstSigCanExecute() => CanAddLeafTypeSig;
		void AddPtrSig() => TypeSig = new PtrSig(TypeSig);
		bool AddPtrSigCanExecute() => CanAddNonLeafTypeSig;
		void AddByRefSig() => TypeSig = new ByRefSig(TypeSig);
		bool AddByRefSigCanExecute() => CanAddNonLeafTypeSig;
		void AddSZArraySig() => TypeSig = new SZArraySig(TypeSig);
		bool AddSZArraySigCanExecute() => CanAddNonLeafTypeSig;

		public UInt32VM ArrayRank { get; }
		public UInt32ListDataFieldVM ArraySizes { get; }
		public Int32ListDataFieldVM ArrayLowerBounds { get; }

		void AddArraySig() => TypeSig = new ArraySig(TypeSig, ArrayRank.Value, ArraySizes.Value, ArrayLowerBounds.Value);

		bool AddArraySigCanExecute() =>
			CanAddNonLeafTypeSig &&
			!ArrayRank.HasError &&
			!ArraySizes.HasError &&
			!ArrayLowerBounds.HasError;

		void AddCModReqdSig() {
			var type = GetTypeSig();
			if (type != null)
				TypeSig = new CModReqdSig(type.ToTypeDefOrRef(), TypeSig);
		}

		bool AddCModReqdSigCanExecute() => CanAddNonLeafTypeSig;

		void AddCModOptSig() {
			var type = GetTypeSig();
			if (type != null)
				TypeSig = new CModOptSig(type.ToTypeDefOrRef(), TypeSig);
		}

		bool AddCModOptSigCanExecute() => CanAddNonLeafTypeSig;
		void AddPinnedSig() => TypeSig = new PinnedSig(TypeSig);
		bool AddPinnedSigCanExecute() => options.IsLocal && CanAddNonLeafTypeSig;
		public override bool HasError => !IsValidTypeSig;
	}
}
