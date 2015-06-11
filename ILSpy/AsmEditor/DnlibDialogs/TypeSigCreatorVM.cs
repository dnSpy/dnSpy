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
using System.Collections.Generic;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;
using ICSharpCode.ILSpy.TreeNodes.Filters;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class TypeSigCreatorVM : ViewModelBase
	{
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
				return "Create TypeSig";
			}
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged("IsEnabled");
					OnPropertyChanged("CanAddLeafTypeSig");
					OnPropertyChanged("CanAddNonLeafTypeSig");
					OnPropertyChanged("CanAddGeneric");
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
					OnPropertyChanged("TypeSig");
					OnPropertyChanged("TypeSigDnlibFullName");
					OnPropertyChanged("TypeSigLanguageFullName");
					OnPropertyChanged("IsValidTypeSig");
					OnPropertyChanged("CanAddLeafTypeSig");
					OnPropertyChanged("CanAddNonLeafTypeSig");
					OnPropertyChanged("CanShowTypeFullName");
					if (nullChange) {
						OnPropertyChanged("AddingLeafTypeSig");
						OnPropertyChanged("AddingNonLeafTypeSig");
					}
					HasErrorUpdated();
				}
			}
		}
		TypeSig typeSig;

		public bool CanShowTypeFullName {
			get { return ShowTypeFullName && IsValidTypeSig; }
		}

		public bool ShowTypeFullName {
			get { return showTypeFullName; }
			set {
				if (showTypeFullName != value) {
					showTypeFullName = value;
					OnPropertyChanged("ShowTypeFullName");
					OnPropertyChanged("CanShowTypeFullName");
				}
			}
		}
		bool showTypeFullName = true;

		public bool CanAddGeneric {
			get { return IsEnabled && (options.CanAddGenericTypeVar || options.CanAddGenericMethodVar); }
		}

		public bool IsValidTypeSig {
			get { return options.NullTypeSigAllowed || TypeSig != null; }
		}

		public bool CanAddLeafTypeSig {
			get { return IsEnabled && TypeSig == null; }
		}

		public bool AddingLeafTypeSig {
			get { return TypeSig == null; }
		}

		public bool CanAddNonLeafTypeSig {
			get { return IsEnabled && !(TypeSig is PinnedSig) && TypeSig != null; }
		}

		public bool AddingNonLeafTypeSig {
			get { return TypeSig != null; }
		}

		public string TypeSigDnlibFullName {
			get { return TypeSig == null ? "null" : TypeSig.FullName; }
		}

		public string TypeSigLanguageFullName {
			get {
				if (TypeSig == null)
					return string.Empty;
				return Language.TypeToString(TypeSig.ToTypeDefOrRef(), true);
			}
		}

		public ICommand ClearTypeSigCommand {
			get { return new RelayCommand(a => TypeSig = null, a => IsEnabled && TypeSig != null); }
		}

		public ICommand RemoveLastTypeSigCommand {
			get { return new RelayCommand(a => RemoveLastTypeSig(), a => IsEnabled && TypeSig != null); }
		}

		public ICommand AddTypeDefOrRefCommand {
			get { return new RelayCommand(a => AddTypeDefOrRef(), a => AddTypeDefOrRefCanExecute()); }
		}

		public ICommand AddGenericVarCommand {
			get { return new RelayCommand(a => AddGenericVar(), a => AddGenericVarCanExecute()); }
		}

		public ICommand AddGenericMVarCommand {
			get { return new RelayCommand(a => AddGenericMVar(), a => AddGenericMVarCanExecute()); }
		}

		public ICommand AddFnPtrSigCommand {
			get { return new RelayCommand(a => AddFnPtrSig(), a => AddFnPtrSigCanExecute()); }
		}

		public ICommand AddGenericInstSigCommand {
			get { return new RelayCommand(a => AddGenericInstSig(), a => AddGenericInstSigCanExecute()); }
		}

		public ICommand AddPtrSigCommand {
			get { return new RelayCommand(a => AddPtrSig(), a => AddPtrSigCanExecute()); }
		}

		public ICommand AddByRefSigCommand {
			get { return new RelayCommand(a => AddByRefSig(), a => AddByRefSigCanExecute()); }
		}

		public ICommand AddSZArraySigCommand {
			get { return new RelayCommand(a => AddSZArraySig(), a => AddSZArraySigCanExecute()); }
		}

		public ICommand AddArraySigCommand {
			get { return new RelayCommand(a => AddArraySig(), a => AddArraySigCanExecute()); }
		}

		public ICommand AddCModReqdSigCommand {
			get { return new RelayCommand(a => AddCModReqdSig(), a => AddCModReqdSigCanExecute()); }
		}

		public ICommand AddCModOptSigCommand {
			get { return new RelayCommand(a => AddCModOptSig(), a => AddCModOptSigCanExecute()); }
		}

		public ICommand AddPinnedSigCommand {
			get { return new RelayCommand(a => AddPinnedSig(), a => AddPinnedSigCanExecute()); }
		}

		public IEnumerable<Language> AllLanguages {
			get { return Languages.AllLanguages; }
		}

		public Language Language {
			get { return options.Language; }
			set {
				if (options.Language != value) {
					options.Language = value;
					OnPropertyChanged("Language");
					OnPropertyChanged("TypeSigLanguageFullName");
				}
			}
		}

		public UInt32VM GenericVariableNumber {
			get { return genericVariableNumber; }
		}
		readonly UInt32VM genericVariableNumber;

		readonly TypeSigCreatorOptions options;

		public TypeSigCreatorVM(TypeSigCreatorOptions options)
		{
			this.options = options.Clone();
			this.arrayRank = new UInt32VM(2, a => { });
			this.arraySizes = new UInt32ListDataFieldVM(a => { }) {
				Min = ModelUtils.COMPRESSED_UINT32_MIN,
				Max = ModelUtils.COMPRESSED_UINT32_MAX,
			};
			this.arrayLowerBounds = new Int32ListDataFieldVM(a => { }) {
				Min = ModelUtils.COMPRESSED_INT32_MIN,
				Max = ModelUtils.COMPRESSED_INT32_MAX,
			};
			this.genericVariableNumber = new UInt32VM(0, a => { });
		}

		void ShowWarning(string key, string msg)
		{
			if (showWarningMessage == null)
				throw new InvalidOperationException();
			showWarningMessage.Show(key, msg);
		}

		void RemoveLastTypeSig()
		{
			if (TypeSig != null)
				TypeSig = TypeSig.Next;
		}

		TypeDefOrRefSig GetTypeSig()
		{
			return GetTypeSig(VisibleMembersFlags.TypeDef);
		}

		TypeDefOrRefSig GetTypeSig(VisibleMembersFlags flags)
		{
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();

			var type = dnlibTypePicker.GetDnlibType<ITypeDefOrRef>(new FlagsTreeViewNodeFilter(flags), null, options.OwnerModule);
			if (type == null)
				return null;

			var corLibSig = options.OwnerModule.CorLibTypes.GetCorLibTypeSig(type);
			if (corLibSig != null)
				return corLibSig;
			else {
				var td = type.ResolveTypeDef();
				bool isValueType;
				if (td == null)
					isValueType = false;	// Most types aren't value types
				else
					isValueType = td.IsValueType;

				if (isValueType)
					return new ValueTypeSig(type);
				else
					return new ClassSig(type);
			}
		}

		void AddTypeDefOrRef()
		{
			TypeSig = GetTypeSig();
		}

		bool AddTypeDefOrRefCanExecute()
		{
			return CanAddLeafTypeSig;
		}

		void AddGenericVar()
		{
			TypeSig = new GenericVar(genericVariableNumber.Value, options.OwnerType);
		}

		bool AddGenericVarCanExecute()
		{
			return !genericVariableNumber.HasError && options.CanAddGenericTypeVar && CanAddLeafTypeSig;
		}

		void AddGenericMVar()
		{
			TypeSig = new GenericMVar(genericVariableNumber.Value, options.OwnerMethod);
		}

		bool AddGenericMVarCanExecute()
		{
			return !genericVariableNumber.HasError && options.CanAddGenericMethodVar && CanAddLeafTypeSig;
		}

		void AddFnPtrSig()
		{
			if (createMethodPropertySig == null)
				throw new InvalidOperationException();

			var createOptions = new MethodSigCreatorOptions(options.Clone("Create FnPtr Method Signature"));
			createOptions.IsPropertySig = false;
			createOptions.CanHaveSentinel = true;
			var sig = createMethodPropertySig.Create(createOptions);
			if (sig == null)
				return;

			TypeSig = new FnPtrSig(sig);
		}

		bool AddFnPtrSigCanExecute()
		{
			return CanAddFnPtr && CanAddLeafTypeSig;
		}

		public bool CanAddFnPtr {
			get { return canAddFnPtr; }
			set {
				if (canAddFnPtr != value) {
					canAddFnPtr = value;
					OnPropertyChanged("CanAddFnPtr");
				}
			}
		}
		bool canAddFnPtr = true;

		void AddGenericInstSig()
		{
			var origType = GetTypeSig(VisibleMembersFlags.GenericTypeDef);
			if (origType == null)
				return;
			var type = origType as ClassOrValueTypeSig;
			if (type == null) {
				ShowWarning("typesig: invalid type", "The type must be a generic type");
				return;
			}
			var genericType = type.TypeDefOrRef.ResolveTypeDef();
			if (genericType == null) {
				ShowWarning("typesig: couldn't resolve type", "Could not resolve the type. Make sure that its assembly has already been loaded.");
				return;
			}
			if (genericType.GenericParameters.Count == 0) {
				ShowWarning("typesig: not a generic type", string.Format("{0} is not a generic type", genericType.FullName));
				return;
			}

			var genArgs = createTypeSigArray.Create(options.Clone("Create Generic Instance Type Arguments"), genericType.GenericParameters.Count, null);
			if (genArgs == null)
				return;

			TypeSig = new GenericInstSig(type, genArgs);
		}

		bool AddGenericInstSigCanExecute()
		{
			return CanAddLeafTypeSig;
		}

		void AddPtrSig()
		{
			TypeSig = new PtrSig(TypeSig);
		}

		bool AddPtrSigCanExecute()
		{
			return CanAddNonLeafTypeSig;
		}

		void AddByRefSig()
		{
			TypeSig = new ByRefSig(TypeSig);
		}

		bool AddByRefSigCanExecute()
		{
			return CanAddNonLeafTypeSig;
		}

		void AddSZArraySig()
		{
			TypeSig = new SZArraySig(TypeSig);
		}

		bool AddSZArraySigCanExecute()
		{
			return CanAddNonLeafTypeSig;
		}

		public UInt32VM ArrayRank {
			get { return arrayRank; }
		}
		UInt32VM arrayRank;

		public UInt32ListDataFieldVM ArraySizes {
			get { return arraySizes; }
		}
		UInt32ListDataFieldVM arraySizes;

		public Int32ListDataFieldVM ArrayLowerBounds {
			get { return arrayLowerBounds; }
		}
		Int32ListDataFieldVM arrayLowerBounds;

		void AddArraySig()
		{
			TypeSig = new ArraySig(TypeSig, arrayRank.Value, arraySizes.Value, arrayLowerBounds.Value);
		}

		bool AddArraySigCanExecute()
		{
			return CanAddNonLeafTypeSig &&
				!arrayRank.HasError &&
				!arraySizes.HasError &&
				!arrayLowerBounds.HasError;
		}

		void AddCModReqdSig()
		{
			var type = GetTypeSig();
			if (type != null)
				TypeSig = new CModReqdSig(type.ToTypeDefOrRef(), TypeSig);
		}

		bool AddCModReqdSigCanExecute()
		{
			return CanAddNonLeafTypeSig;
		}

		void AddCModOptSig()
		{
			var type = GetTypeSig();
			if (type != null)
				TypeSig = new CModOptSig(type.ToTypeDefOrRef(), TypeSig);
		}

		bool AddCModOptSigCanExecute()
		{
			return CanAddNonLeafTypeSig;
		}

		void AddPinnedSig()
		{
			TypeSig = new PinnedSig(TypeSig);
		}

		bool AddPinnedSigCanExecute()
		{
			return options.IsLocal && CanAddNonLeafTypeSig;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get { return !IsValidTypeSig; }
		}
	}
}
