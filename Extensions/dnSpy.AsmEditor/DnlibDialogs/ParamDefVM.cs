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

using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class ParamDefVM : ViewModelBase {
		readonly ParamDefOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		public string FullName {
			get {
				var sb = new StringBuilder();

				if (Sequence.HasError)
					sb.Append("???");
				else if (Sequence.Value == 0)
					sb.Append(dnSpy_AsmEditor_Resources.ParameterReturn);
				else
					sb.Append(string.Format(dnSpy_AsmEditor_Resources.ParameterNumber, Sequence.Value));

				sb.Append(' ');
				sb.Append(string.IsNullOrEmpty(Name) ? dnSpy_AsmEditor_Resources.NoName : Name);

				return sb.ToString();
			}
		}

		public ParamAttributes Attributes {
			get => attributes;
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(In));
					OnPropertyChanged(nameof(Out));
					OnPropertyChanged(nameof(Lcid));
					OnPropertyChanged(nameof(Retval));
					OnPropertyChanged(nameof(Optional));
					OnPropertyChanged(nameof(HasDefault));
					OnPropertyChanged(nameof(HasFieldMarshal));
					ConstantVM.IsEnabled = HasDefault;
					MarshalTypeVM.IsEnabled = HasFieldMarshal;
					HasErrorUpdated();
				}
			}
		}
		ParamAttributes attributes;

		public bool In {
			get => GetFlagValue(ParamAttributes.In);
			set => SetFlagValue(ParamAttributes.In, value);
		}

		public bool Out {
			get => GetFlagValue(ParamAttributes.Out);
			set => SetFlagValue(ParamAttributes.Out, value);
		}

		public bool Lcid {
			get => GetFlagValue(ParamAttributes.Lcid);
			set => SetFlagValue(ParamAttributes.Lcid, value);
		}

		public bool Retval {
			get => GetFlagValue(ParamAttributes.Retval);
			set => SetFlagValue(ParamAttributes.Retval, value);
		}

		public bool Optional {
			get => GetFlagValue(ParamAttributes.Optional);
			set => SetFlagValue(ParamAttributes.Optional, value);
		}

		public bool HasDefault {
			get => GetFlagValue(ParamAttributes.HasDefault);
			set => SetFlagValue(ParamAttributes.HasDefault, value);
		}

		public bool HasFieldMarshal {
			get => GetFlagValue(ParamAttributes.HasFieldMarshal);
			set => SetFlagValue(ParamAttributes.HasFieldMarshal, value);
		}

		bool GetFlagValue(ParamAttributes flag) => (Attributes & flag) != 0;

		void SetFlagValue(ParamAttributes flag, bool value) {
			if (value)
				Attributes |= flag;
			else
				Attributes &= ~flag;
		}

		public string? Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
					OnPropertyChanged(nameof(FullName));
				}
			}
		}
		UTF8String? name;

		public UInt16VM Sequence { get; }
		public Constant? Constant => HasDefault ? ownerModule.UpdateRowId(new ConstantUser(ConstantVM.Value)) : null;
		public ConstantVM ConstantVM { get; }
		public MarshalTypeVM MarshalTypeVM { get; }
		public string MarshalTypeString => string.Format(dnSpy_AsmEditor_Resources.MarshalType, HasFieldMarshal ? MarshalTypeVM.TypeString : dnSpy_AsmEditor_Resources.MarshalType_Nothing);
		public CustomAttributesVM CustomAttributesVM { get; }

		readonly ModuleDef ownerModule;

		public ParamDefVM(ParamDefOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef? ownerType, MethodDef? ownerMethod) {
			this.ownerModule = ownerModule;
			origOptions = options;
			Sequence = new UInt16VM(a => { OnPropertyChanged(nameof(FullName)); HasErrorUpdated(); });
			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);
			ConstantVM = new ConstantVM(ownerModule, options.Constant?.Value, dnSpy_AsmEditor_Resources.Parameter_DefaultValueInfo);
			ConstantVM.PropertyChanged += constantVM_PropertyChanged;
			MarshalTypeVM = new MarshalTypeVM(ownerModule, decompilerService, ownerType ?? ownerMethod?.DeclaringType, ownerMethod);
			MarshalTypeVM.PropertyChanged += marshalTypeVM_PropertyChanged;

			ConstantVM.IsEnabled = HasDefault;
			MarshalTypeVM.IsEnabled = HasFieldMarshal;
			Reinitialize();
		}

		void constantVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ConstantVM.IsEnabled))
				HasDefault = ConstantVM.IsEnabled;
			HasErrorUpdated();
		}

		void marshalTypeVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(MarshalTypeVM.IsEnabled))
				HasFieldMarshal = MarshalTypeVM.IsEnabled;
			else if (e.PropertyName == nameof(MarshalTypeVM.TypeString))
				OnPropertyChanged(nameof(MarshalTypeString));
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public ParamDefOptions CreateParamDefOptions() => CopyTo(new ParamDefOptions());

		void InitializeFrom(ParamDefOptions options) {
			Name = options.Name;
			Sequence.Value = options.Sequence;
			Attributes = options.Attributes;
			if (!(options.Constant is null)) {
				HasDefault = true;
				ConstantVM.Value = options.Constant.Value;
			}
			else {
				HasDefault = false;
				ConstantVM.Value = null;
			}
			MarshalTypeVM.Type = options.MarshalType;
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		ParamDefOptions CopyTo(ParamDefOptions options) {
			options.Name = Name;
			options.Sequence = Sequence.Value;
			options.Attributes = Attributes;
			options.Constant = HasDefault ? Constant : null;
			options.MarshalType = HasFieldMarshal ? MarshalTypeVM.Type : null;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		public override bool HasError {
			get {
				return (HasDefault && ConstantVM.HasError) ||
					(HasFieldMarshal && MarshalTypeVM.HasError) ||
					Sequence.HasError;
			}
		}
	}
}
