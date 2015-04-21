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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.Threading;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	enum MethodCallingConv
	{
		Default = CallingConvention.Default,
		C = CallingConvention.C,
		StdCall = CallingConvention.StdCall,
		ThisCall = CallingConvention.ThisCall,
		FastCall = CallingConvention.FastCall,
		VarArg = CallingConvention.VarArg,
		NativeVarArg = CallingConvention.NativeVarArg,
	}

	sealed class MethodSigCreatorVM : ViewModelBase
	{
		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand AddReturnTypeCommand {
			get { return new RelayCommand(a => AddReturnType()); }
		}

		public PropertySig PropertySig {
			get { return CreateSig(new PropertySig()); }
		}

		public MethodSig MethodSig {
			get { return CreateSig(new MethodSig()); }
		}

		public MethodBaseSig MethodBaseSig {
			get { return IsPropertySig ? (MethodBaseSig)PropertySig : MethodSig; }
		}

		public bool IsPropertySig {
			get { return options.IsPropertySig; }
		}

		public bool IsMethodSig {
			get { return !IsPropertySig; }
		}

		public bool CanHaveSentinel {
			get { return options.CanHaveSentinel; }
		}

		public string SignatureFullName {
			get {
				var sig = MethodBaseSig;
				if (sig.GenParamCount > 100)
					sig.GenParamCount = 100;
				return FullNameCreator.MethodBaseSigFullName(null, null, sig, options.TypeSigCreatorOptions.OwnerMethod);
			}
		}

		public CallingConvention CallingConvention {
			get { return callingConvention; }
			set {
				if (callingConvention != value) {
					callingConvention = value;
					OnPropertyChanged("CallingConvention");
					OnPropertyChanged("SignatureFullName");
					OnPropertyChanged("IsGeneric");
					OnPropertyChanged("HasThis");
					OnPropertyChanged("ExplicitThis");
				}
			}
		}
		CallingConvention callingConvention;

		public bool IsGeneric {
			get { return GetFlags(dnlib.DotNet.CallingConvention.Generic); }
			set { SetFlags(dnlib.DotNet.CallingConvention.Generic, value); }
		}

		public bool HasThis {
			get { return GetFlags(dnlib.DotNet.CallingConvention.HasThis); }
			set { SetFlags(dnlib.DotNet.CallingConvention.HasThis, value); }
		}

		public bool ExplicitThis {
			get { return GetFlags(dnlib.DotNet.CallingConvention.ExplicitThis); }
			set { SetFlags(dnlib.DotNet.CallingConvention.ExplicitThis, value); }
		}

		bool GetFlags(dnlib.DotNet.CallingConvention flag)
		{
			return (CallingConvention & flag) != 0;
		}

		void SetFlags(dnlib.DotNet.CallingConvention flag, bool value)
		{
			if (value)
				CallingConvention |= flag;
			else
				CallingConvention &= ~flag;
		}

		public EnumListVM MethodCallingConv {
			get { return methodCallingConvVM; }
		}
		readonly EnumListVM methodCallingConvVM;
		internal static readonly EnumVM[] methodCallingConvList = EnumVM.Create(typeof(MethodCallingConv));

		public TypeSig ReturnType {
			get { return retType; }
			set {
				if (retType != value) {
					retType = value;
					OnPropertyChanged("ReturnType");
					OnPropertyChanged("SignatureFullName");
					OnPropertyChanged("ErrorText");
					HasErrorUpdated();
				}
			}
		}
		TypeSig retType;

		public UInt32VM GenericParameterCount {
			get { return genericParameterCount; }
		}
		UInt32VM genericParameterCount;

		public string Title {
			get {
				if (!string.IsNullOrEmpty(title))
					return title;
				return IsPropertySig ? "Create Property Signature" : "Create Method Signature";
			}
		}
		string title;

		public CreateTypeSigArrayVM ParametersCreateTypeSigArray {
			get { return parametersCreateTypeSigArray; }
		}
		CreateTypeSigArrayVM parametersCreateTypeSigArray;

		public CreateTypeSigArrayVM SentinelCreateTypeSigArray {
			get { return sentinelCreateTypeSigArray; }
		}
		CreateTypeSigArrayVM sentinelCreateTypeSigArray;

		readonly MethodSigCreatorOptions options;

		public MethodSigCreatorVM(MethodSigCreatorOptions options)
		{
			this.options = options.Clone();
			this.title = options.TypeSigCreatorOptions.Title;
			this.parametersCreateTypeSigArray = new CreateTypeSigArrayVM(options.TypeSigCreatorOptions.Clone(null), null);
			this.ParametersCreateTypeSigArray.TypeSigCollection.CollectionChanged += (s, e) => OnPropertyChanged("SignatureFullName");
			this.sentinelCreateTypeSigArray = new CreateTypeSigArrayVM(options.TypeSigCreatorOptions.Clone(null), null);
			this.SentinelCreateTypeSigArray.TypeSigCollection.CollectionChanged += (s, e) => OnPropertyChanged("SignatureFullName");
			this.sentinelCreateTypeSigArray.IsEnabled = CanHaveSentinel;
			this.genericParameterCount = new UInt32VM(0, a => {
				HasErrorUpdated();
				OnPropertyChanged("SignatureFullName");
				if (GenericParameterCount != null && !GenericParameterCount.HasError)
					IsGeneric = GenericParameterCount.Value != 0;
			});
			this.methodCallingConvVM = new EnumListVM(methodCallingConvList, () => {
				if (!IsMethodSig)
					throw new InvalidOperationException();
				CallingConvention = (CallingConvention & ~dnlib.DotNet.CallingConvention.Mask) |
					(dnlib.DotNet.CallingConvention)(MethodCallingConv)MethodCallingConv.SelectedItem;
			});
			if (!CanHaveSentinel) {
				MethodCallingConv.Items.RemoveAt(MethodCallingConv.GetIndex(DnlibDialogs.MethodCallingConv.VarArg));
				MethodCallingConv.Items.RemoveAt(MethodCallingConv.GetIndex(DnlibDialogs.MethodCallingConv.NativeVarArg));
			}
			if (IsMethodSig)
				MethodCallingConv.SelectedItem = DnlibDialogs.MethodCallingConv.Default;
			else
				CallingConvention = (CallingConvention & ~dnlib.DotNet.CallingConvention.Mask) | dnlib.DotNet.CallingConvention.Property;
			ReturnType = options.TypeSigCreatorOptions.Module.CorLibTypes.Void;
		}

		T CreateSig<T>(T sig) where T : MethodBaseSig
		{
			sig.CallingConvention = CallingConvention;
			sig.RetType = ReturnType;
			sig.Params.AddRange(ParametersCreateTypeSigArray.TypeSigArray);
			sig.GenParamCount = GenericParameterCount.HasError ? 0 : GenericParameterCount.Value;
			var sentAry = SentinelCreateTypeSigArray.TypeSigArray;
			sig.ParamsAfterSentinel = sentAry.Length == 0 ? null : ThreadSafeListCreator.Create<TypeSig>(sentAry);
			return sig;
		}

		void AddReturnType()
		{
			if (typeSigCreator == null)
				throw new InvalidOperationException();

			var newTypeSig = typeSigCreator.Create(options.TypeSigCreatorOptions.Clone("Create Return Type"), ReturnType);
			if (newTypeSig != null)
				ReturnType = newTypeSig;
		}

		protected override string Verify(string columnName)
		{
			if (columnName == "ReturnType")
				return ReturnType != null ? string.Empty : "A return type is required";
			return string.Empty;
		}

		public override bool HasError {
			get {
				return GenericParameterCount.HasError ||
					ReturnType == null;
			}
		}

		public string ErrorText {
			get {
				string err;
				if (!string.IsNullOrEmpty(err = Verify("ReturnType")))
					return err;

				return string.Empty;
			}
		}
	}
}
