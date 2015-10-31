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

using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class ParamDefVM : ViewModelBase {
		readonly ParamDefOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public string FullName {
			get {
				var sb = new StringBuilder();

				if (Sequence.HasError)
					sb.Append("???");
				else if (Sequence.Value == 0)
					sb.Append("param(return)");
				else
					sb.Append(string.Format("param({0})", Sequence.Value));

				sb.Append(' ');
				sb.Append(string.IsNullOrEmpty(Name) ? "<<no-name>>" : Name);

				return sb.ToString();
			}
		}

		public ParamAttributes Attributes {
			get { return attributes; }
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("In");
					OnPropertyChanged("Out");
					OnPropertyChanged("Optional");
					OnPropertyChanged("HasDefault");
					OnPropertyChanged("HasFieldMarshal");
					ConstantVM.IsEnabled = HasDefault;
					MarshalTypeVM.IsEnabled = HasFieldMarshal;
					HasErrorUpdated();
				}
			}
		}
		ParamAttributes attributes;

		public bool In {
			get { return GetFlagValue(ParamAttributes.In); }
			set { SetFlagValue(ParamAttributes.In, value); }
		}

		public bool Out {
			get { return GetFlagValue(ParamAttributes.Out); }
			set { SetFlagValue(ParamAttributes.Out, value); }
		}

		public bool Optional {
			get { return GetFlagValue(ParamAttributes.Optional); }
			set { SetFlagValue(ParamAttributes.Optional, value); }
		}

		public bool HasDefault {
			get { return GetFlagValue(ParamAttributes.HasDefault); }
			set { SetFlagValue(ParamAttributes.HasDefault, value); }
		}

		public bool HasFieldMarshal {
			get { return GetFlagValue(ParamAttributes.HasFieldMarshal); }
			set { SetFlagValue(ParamAttributes.HasFieldMarshal, value); }
		}

		bool GetFlagValue(ParamAttributes flag) {
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(ParamAttributes flag, bool value) {
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
					OnPropertyChanged("FullName");
				}
			}
		}
		UTF8String name;

		public UInt16VM Sequence {
			get { return sequence; }
		}
		UInt16VM sequence;

		public Constant Constant {
			get { return HasDefault ? ownerModule.UpdateRowId(new ConstantUser(ConstantVM.Value)) : null; }
		}

		public ConstantVM ConstantVM {
			get { return constantVM; }
		}
		readonly ConstantVM constantVM;

		public MarshalTypeVM MarshalTypeVM {
			get { return marshalTypeVM; }
		}
		readonly MarshalTypeVM marshalTypeVM;

		public string MarshalTypeString {
			get { return string.Format("Marshal Type: {0}", HasFieldMarshal ? MarshalTypeVM.TypeString : "nothing"); }
		}

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		readonly ModuleDef ownerModule;

		public ParamDefVM(ParamDefOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod) {
			this.ownerModule = ownerModule;
			this.origOptions = options;
			this.sequence = new UInt16VM(a => { OnPropertyChanged("FullName"); HasErrorUpdated(); });
			this.customAttributesVM = new CustomAttributesVM(ownerModule, language);
			this.constantVM = new ConstantVM(ownerModule, options.Constant == null ? null : options.Constant.Value, "Default value for this parameter");
			ConstantVM.PropertyChanged += constantVM_PropertyChanged;
			this.marshalTypeVM = new MarshalTypeVM(ownerModule, language, ownerType != null ? ownerType : ownerMethod == null ? null : ownerMethod.DeclaringType, ownerMethod);
			MarshalTypeVM.PropertyChanged += marshalTypeVM_PropertyChanged;

			ConstantVM.IsEnabled = HasDefault;
			MarshalTypeVM.IsEnabled = HasFieldMarshal;
			Reinitialize();
		}

		void constantVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				HasDefault = ConstantVM.IsEnabled;
			HasErrorUpdated();
		}

		void marshalTypeVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				HasFieldMarshal = MarshalTypeVM.IsEnabled;
			else if (e.PropertyName == "TypeString")
				OnPropertyChanged("MarshalTypeString");
			HasErrorUpdated();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public ParamDefOptions CreateParamDefOptions() {
			return CopyTo(new ParamDefOptions());
		}

		void InitializeFrom(ParamDefOptions options) {
			Name = options.Name;
			Sequence.Value = options.Sequence;
			Attributes = options.Attributes;
			if (options.Constant != null) {
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
