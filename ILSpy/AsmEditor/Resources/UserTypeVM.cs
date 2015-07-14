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
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;
using ICSharpCode.ILSpy.TreeNodes.Filters;

namespace ICSharpCode.ILSpy.AsmEditor.Resources
{
	sealed class UserTypeVM : ViewModelBase
	{
		readonly bool canDeserialize;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand PickTypeCommand {
			get { return new RelayCommand(a => PickType()); }
		}

		public string TypeFullName {
			get { return typeFullName; }
			set {
				if (typeFullName != value) {
					typeFullName = value;
					OnPropertyChanged("TypeFullName");
					OnPropertyChanged("StringValue");
					HasErrorUpdated();
				}
			}
		}
		string typeFullName = string.Empty;

		public string StringValue {
			get { return stringValue; }
			set {
				if (stringValue != value) {
					stringValue = value;
					OnPropertyChanged("StringValue");
				}
			}
		}
		string stringValue = string.Empty;

		readonly ModuleDef ownerModule;

		public UserTypeVM(ModuleDef ownerModule, bool canDeserialize)
		{
			this.ownerModule = ownerModule;
			this.canDeserialize = canDeserialize;
		}

		void PickType()
		{
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newType = dnlibTypePicker.GetDnlibType(new FlagsTreeViewNodeFilter(VisibleMembersFlags.TypeDef), GetTypeRef(), ownerModule);
			if (newType != null)
				TypeFullName = newType.AssemblyQualifiedName;
		}

		public void SetData(byte[] data)
		{
			StringValue = GetString(data);
		}

		public byte[] GetSerializedData()
		{
			object obj;
			if (!string.IsNullOrEmpty(GetSerializedData(out obj)))
				return null;
			return SerializationUtils.Serialize(obj);
		}

		string GetString(byte[] data)
		{
			if (!canDeserialize)
				return string.Empty;

			if (data == null)
				return string.Empty;

			object obj;
			if (!string.IsNullOrEmpty(SerializationUtils.Deserialize(data, out obj)))
				return string.Empty;

			return SerializationUtils.ConvertObjectToString(obj);
		}

		string GetSerializedData(out object obj)
		{
			obj = null;
			Type type;
			var error = LoadType(out type);
			if (!string.IsNullOrEmpty(error))
				return error;

			return SerializationUtils.CreateObjectFromString(type, StringValue, out obj);
		}

		const string SERIALIZATION_DISABLED_ERROR = "(De)serialization is disabled in the settings.";
		string LoadType(out Type type)
		{
			if (!canDeserialize) {
				type = null;
				return SERIALIZATION_DISABLED_ERROR;
			}

			try {
				type = Type.GetType(TypeFullName);
				if (type == null)
					return "Could not find the type or its assembly.";
				return string.Empty;
			}
			catch (Exception ex) {
				type = null;
				return string.Format("Could not load type '{0}': {1}", typeFullName, ex.Message);
			}
		}

		ITypeDefOrRef GetTypeRef()
		{
			return TypeNameParser.ParseReflection(ownerModule, typeFullName, null);
		}

		protected override string Verify(string columnName)
		{
			if (columnName == "TypeFullName") {
				Type type;
				var error = LoadType(out type);
				if (!string.IsNullOrEmpty(error))
					return error;
				return string.Empty;
			}

			if (columnName == "StringValue") {
				object obj;
				return GetSerializedData(out obj);
			}

			return string.Empty;
		}

		public override bool HasError {
			get {
				return !string.IsNullOrEmpty(Verify("TypeFullName")) ||
					!string.IsNullOrEmpty(Verify("StringValue"));
			}
		}
	}
}
