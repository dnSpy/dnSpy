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
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.DnlibDialogs;

namespace ICSharpCode.ILSpy.AsmEditor.Event
{
	sealed class EventOptionsVM : ViewModelBase
	{
		readonly EventDefOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public EventAttributes Attributes {
			get { return attributes; }
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged("Attributes");
					OnPropertyChanged("SpecialName");
					OnPropertyChanged("RTSpecialName");
				}
			}
		}
		EventAttributes attributes;

		public bool SpecialName {
			get { return GetFlagValue(EventAttributes.SpecialName); }
			set { SetFlagValue(EventAttributes.SpecialName, value); }
		}

		public bool RTSpecialName {
			get { return GetFlagValue(EventAttributes.RTSpecialName); }
			set { SetFlagValue(EventAttributes.RTSpecialName, value); }
		}

		bool GetFlagValue(EventAttributes flag)
		{
			return (Attributes & flag) != 0;
		}

		void SetFlagValue(EventAttributes flag, bool value)
		{
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
				}
			}
		}
		UTF8String name;

		public TypeSig EventTypeSig {
			get { return typeSigCreator.TypeSig; }
			set { typeSigCreator.TypeSig = value; }
		}

		public string EventTypeHeader {
			get { return string.Format("Event Type: {0}", typeSigCreator.TypeSigDnlibFullName); }
		}

		public TypeSigCreatorVM TypeSigCreator {
			get { return typeSigCreator; }
		}
		readonly TypeSigCreatorVM typeSigCreator;

		public CustomAttributesVM CustomAttributesVM {
			get { return customAttributesVM; }
		}
		CustomAttributesVM customAttributesVM;

		public EventOptionsVM(EventDefOptions options, ModuleDef module, Language language, TypeDef ownerType)
		{
			var typeSigCreatorOptions = new TypeSigCreatorOptions(module, language) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = true,
				OwnerType = ownerType,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			this.typeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			this.typeSigCreator.PropertyChanged += typeSigCreator_PropertyChanged;

			this.customAttributesVM = new CustomAttributesVM(module, language);

			this.origOptions = options;

			this.typeSigCreator.CanAddFnPtr = false;
			Reinitialize();
		}

		void typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "TypeSigDnlibFullName")
				OnPropertyChanged("EventTypeHeader");
			HasErrorUpdated();
		}

		void Reinitialize()
		{
			InitializeFrom(origOptions);
		}

		public EventDefOptions CreateEventDefOptions()
		{
			return CopyTo(new EventDefOptions());
		}

		void InitializeFrom(EventDefOptions options)
		{
			Attributes = options.Attributes;
			Name = options.Name;
			EventTypeSig = options.EventType.ToTypeSig();
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		EventDefOptions CopyTo(EventDefOptions options)
		{
			options.Attributes = Attributes;
			options.Name = Name;
			options.EventType = EventTypeSig.ToTypeDefOrRef();
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			return options;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get { return false; }
		}
	}
}
