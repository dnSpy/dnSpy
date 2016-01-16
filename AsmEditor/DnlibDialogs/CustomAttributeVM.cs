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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class CustomAttributeVM : ViewModelBase {
		readonly CustomAttributeOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand PickConstructorCommand {
			get { return new RelayCommand(a => PickConstructor()); }
		}

		public string TypeFullName {
			get {
				var mrCtor = Constructor as MemberRef;
				if (mrCtor != null)
					return mrCtor.GetDeclaringTypeFullName() ?? string.Empty;

				var mdCtor = Constructor as MethodDef;
				if (mdCtor != null) {
					var declType = mdCtor.DeclaringType;
					if (declType != null)
						return declType.FullName;
				}

				return string.Empty;
			}
		}

		public string FullName {
			get {
				if (IsRawData)
					return dnSpy_AsmEditor_Resources.RawCustomAttributeBlob2;
				var sb = new StringBuilder();
				sb.Append(TypeFullName);
				sb.Append('(');
				bool first = true;
				foreach (var arg in ConstructorArguments) {
					if (!first)
						sb.Append(", ");
					first = false;
					sb.Append(arg.ToString());
				}
				foreach (var namedArg in CANamedArgumentsVM.Collection) {
					if (!first)
						sb.Append(", ");
					first = false;
					sb.Append(namedArg.ToString());
				}
				sb.Append(')');
				return sb.ToString();
			}
		}

		public bool IsRawData {
			get { return isRawData; }
			set {
				if (isRawData != value) {
					isRawData = value;
					ConstructorArguments.IsEnabled = !value;
					CANamedArgumentsVM.Collection.IsEnabled = !value;
					OnPropertyChanged("IsRawData");
					OnPropertyChanged("IsNotRawData");
					OnPropertyChanged("FullName");
					HasErrorUpdated();
				}
			}
		}
		bool isRawData;

		public bool IsNotRawData {
			get { return !IsRawData; }
			set { IsRawData = !value; }
		}

		public HexStringVM RawData {
			get { return rawData; }
		}
		HexStringVM rawData;

		public ICustomAttributeType Constructor {
			get { return constructor; }
			set {
				if (constructor != value) {
					constructor = value;
					OnPropertyChanged("Constructor");
					ConstructorArguments.Clear();
					CreateArguments();
					OnPropertyChanged("TypeFullName");
					OnPropertyChanged("FullName");
					HasErrorUpdated();
				}
			}
		}
		ICustomAttributeType constructor;

		public MyObservableCollection<CAArgumentVM> ConstructorArguments {
			get { return constructorArguments; }
		}
		readonly MyObservableCollection<CAArgumentVM> constructorArguments = new MyObservableCollection<CAArgumentVM>();

		public CANamedArgumentsVM CANamedArgumentsVM {
			get { return caNamedArgumentsVM; }
		}
		CANamedArgumentsVM caNamedArgumentsVM;

		readonly ModuleDef ownerModule;
		readonly ILanguageManager languageManager;
		readonly TypeDef ownerType;
		readonly MethodDef ownerMethod;

		public CustomAttributeVM(CustomAttributeOptions options, ModuleDef ownerModule, ILanguageManager languageManager, TypeDef ownerType, MethodDef ownerMethod) {
			this.origOptions = options;
			this.ownerModule = ownerModule;
			this.languageManager = languageManager;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;

			this.rawData = new HexStringVM(a => HasErrorUpdated());
			this.caNamedArgumentsVM = new CANamedArgumentsVM(ownerModule, languageManager, ownerType, ownerMethod, a => !IsRawData && a.Collection.Count < ushort.MaxValue);
			ConstructorArguments.CollectionChanged += Args_CollectionChanged;
			CANamedArgumentsVM.Collection.CollectionChanged += Args_CollectionChanged;

			Reinitialize();
		}

		void Args_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			Hook(e);
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void Hook(NotifyCollectionChangedEventArgs e) {
			if (e.OldItems != null) {
				foreach (INotifyPropertyChanged i in e.OldItems)
					i.PropertyChanged -= arg_PropertyChanged;
			}
			if (e.NewItems != null) {
				foreach (INotifyPropertyChanged i in e.NewItems)
					i.PropertyChanged += arg_PropertyChanged;
			}
		}

		void arg_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void CreateArguments() {
			int count = Constructor == null ? 0 : Constructor.MethodSig.GetParamCount();
			while (ConstructorArguments.Count > count)
				ConstructorArguments.RemoveAt(ConstructorArguments.Count - 1);
			while (ConstructorArguments.Count < count) {
				var type = Constructor.MethodSig.Params[ConstructorArguments.Count];
				ConstructorArguments.Add(new CAArgumentVM(ownerModule, CreateCAArgument(type), new TypeSigCreatorOptions(ownerModule, languageManager), type));
			}
		}

		static CAArgument CreateCAArgument(TypeSig type) {
			return new CAArgument(type, ModelUtils.GetDefaultValue(type, true));
		}

		void PickConstructor() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newCtor = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Constructor, new FlagsFileTreeNodeFilter(VisibleMembersFlags.InstanceConstructor), Constructor, ownerModule);
			if (newCtor != null)
				Constructor = newCtor;
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public CustomAttributeOptions CreateCustomAttributeOptions() {
			return CopyTo(new CustomAttributeOptions());
		}

		void InitializeFrom(CustomAttributeOptions options) {
			IsRawData = options.RawData != null;
			RawData.Value = options.RawData;
			Constructor = options.Constructor;
			ConstructorArguments.Clear();
			var sig = Constructor == null ? null : Constructor.MethodSig;
			for (int i = 0; i < options.ConstructorArguments.Count; i++) {
				TypeSig type = null;
				if (sig != null && i < sig.Params.Count)
					type = sig.Params[i];
				ConstructorArguments.Add(new CAArgumentVM(ownerModule, options.ConstructorArguments[i], new TypeSigCreatorOptions(ownerModule, languageManager), type));
			}
			CANamedArgumentsVM.InitializeFrom(options.NamedArguments);
			CreateArguments();
		}

		CustomAttributeOptions CopyTo(CustomAttributeOptions options) {
			options.Constructor = Constructor;
			options.ConstructorArguments.Clear();
			options.NamedArguments.Clear();
			if (IsRawData)
				options.RawData = RawData.Value.ToArray();
			else {
				options.RawData = null;
				int count = Constructor == null ? 0 : Constructor.MethodSig.GetParamCount();
				for (int i = 0; i < count; i++)
					options.ConstructorArguments.Add(ConstructorArguments[i].CreateCAArgument(Constructor.MethodSig.Params[i]));
				options.NamedArguments.AddRange(CANamedArgumentsVM.Collection.Select(a => a.CreateCANamedArgument()));
			}
			return options;
		}

		public override bool HasError {
			get {
				return Constructor == null ||
					(IsRawData && rawData.HasError) ||
					(!IsRawData &&
						(ConstructorArguments.Any(a => a.HasError) ||
						CANamedArgumentsVM.Collection.Any(a => a.HasError)));
			}
		}
	}
}
