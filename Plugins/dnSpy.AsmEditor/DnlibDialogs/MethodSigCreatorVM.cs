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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.Threading;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	enum MethodCallingConv {
		Default			= CallingConvention.Default,
		C				= CallingConvention.C,
		StdCall			= CallingConvention.StdCall,
		ThisCall		= CallingConvention.ThisCall,
		FastCall		= CallingConvention.FastCall,
		VarArg			= CallingConvention.VarArg,
		NativeVarArg	= CallingConvention.NativeVarArg,
	}

	sealed class MethodSigCreatorVM : ViewModelBase {
		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand AddReturnTypeCommand => new RelayCommand(a => AddReturnType());

		public PropertySig PropertySig {
			get { return CreateSig(new PropertySig()); }
			set { WriteSignature(value); }
		}

		public MethodSig MethodSig {
			get { return CreateSig(new MethodSig()); }
			set { WriteSignature(value); }
		}

		public MethodBaseSig MethodBaseSig {
			get { return IsPropertySig ? (MethodBaseSig)PropertySig : MethodSig; }
			set { WriteSignature(value); }
		}

		public bool ShowSignatureFullName => !options.DontShowSignatureFullName;
		public bool IsPropertySig => options.IsPropertySig;
		public bool IsMethodSig => !IsPropertySig;
		public bool CanHaveSentinel => options.CanHaveSentinel;

		public string SignatureFullName {
			get {
				var sig = MethodBaseSig;
				if (sig.GenParamCount > 100)
					sig.GenParamCount = 100;
				return FullNameCreator.MethodBaseSigFullName(null, null, sig, options.TypeSigCreatorOptions.OwnerMethod, null);
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

		bool GetFlags(dnlib.DotNet.CallingConvention flag) => (CallingConvention & flag) != 0;

		void SetFlags(dnlib.DotNet.CallingConvention flag, bool value) {
			if (value)
				CallingConvention |= flag;
			else
				CallingConvention &= ~flag;
		}

		public EnumListVM MethodCallingConv { get; }
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

		public UInt32VM GenericParameterCount { get; }

		public string Title {
			get {
				if (!string.IsNullOrEmpty(title))
					return title;
				return IsPropertySig ? dnSpy_AsmEditor_Resources.CreatePropertySignature : dnSpy_AsmEditor_Resources.CreateMethodSignature;
			}
		}
		string title;

		public CreateTypeSigArrayVM ParametersCreateTypeSigArray { get; }
		public CreateTypeSigArrayVM SentinelCreateTypeSigArray { get; }

		readonly MethodSigCreatorOptions options;

		public MethodSigCreatorVM(MethodSigCreatorOptions options) {
			this.options = options.Clone();
			this.title = options.TypeSigCreatorOptions.Title;
			this.ParametersCreateTypeSigArray = new CreateTypeSigArrayVM(options.TypeSigCreatorOptions.Clone(null), null);
			this.ParametersCreateTypeSigArray.TypeSigCollection.CollectionChanged += (s, e) => OnPropertyChanged("SignatureFullName");
			this.SentinelCreateTypeSigArray = new CreateTypeSigArrayVM(options.TypeSigCreatorOptions.Clone(null), null);
			this.SentinelCreateTypeSigArray.TypeSigCollection.CollectionChanged += (s, e) => OnPropertyChanged("SignatureFullName");
			this.SentinelCreateTypeSigArray.IsEnabled = CanHaveSentinel;
			this.GenericParameterCount = new UInt32VM(0, a => {
				HasErrorUpdated();
				OnPropertyChanged("SignatureFullName");
				if (GenericParameterCount != null && !GenericParameterCount.HasError)
					IsGeneric = GenericParameterCount.Value != 0;
			}) {
				Min = ModelUtils.COMPRESSED_UINT32_MIN,
				Max = ModelUtils.COMPRESSED_UINT32_MAX,
			};
			this.MethodCallingConv = new EnumListVM(methodCallingConvList, (a, b) => {
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
			ReturnType = options.TypeSigCreatorOptions.OwnerModule.CorLibTypes.Void;
		}

		T CreateSig<T>(T sig) where T : MethodBaseSig {
			sig.CallingConvention = CallingConvention;
			sig.RetType = ReturnType;
			sig.Params.AddRange(ParametersCreateTypeSigArray.TypeSigArray);
			sig.GenParamCount = GenericParameterCount.HasError ? 0 : GenericParameterCount.Value;
			var sentAry = SentinelCreateTypeSigArray.TypeSigArray;
			sig.ParamsAfterSentinel = sentAry.Length == 0 ? null : ThreadSafeListCreator.Create<TypeSig>(sentAry);
			return sig;
		}

		void WriteSignature(MethodBaseSig sig) {
			if (sig == null) {
				CallingConvention = 0;
				ReturnType = null;
				ParametersCreateTypeSigArray.TypeSigCollection.Clear();
				GenericParameterCount.Value = 0;
				SentinelCreateTypeSigArray.TypeSigCollection.Clear();
			}
			else {
				CallingConvention = sig.CallingConvention;
				ReturnType = sig.RetType;
				ParametersCreateTypeSigArray.TypeSigCollection.Clear();
				ParametersCreateTypeSigArray.TypeSigCollection.AddRange(sig.Params);
				GenericParameterCount.Value = sig.GenParamCount;
				SentinelCreateTypeSigArray.TypeSigCollection.Clear();
				if (sig.ParamsAfterSentinel != null)
					SentinelCreateTypeSigArray.TypeSigCollection.AddRange(sig.ParamsAfterSentinel);
			}
		}

		void AddReturnType() {
			if (typeSigCreator == null)
				throw new InvalidOperationException();

			bool canceled;
			var newTypeSig = typeSigCreator.Create(options.TypeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateReturnType), ReturnType, out canceled);
			if (newTypeSig != null)
				ReturnType = newTypeSig;
		}

		protected override string Verify(string columnName) {
			if (columnName == "ReturnType")
				return ReturnType != null ? string.Empty : dnSpy_AsmEditor_Resources.ReturnTypeRequired;
			return string.Empty;
		}

		public override bool HasError => GenericParameterCount.HasError || ReturnType == null;

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
