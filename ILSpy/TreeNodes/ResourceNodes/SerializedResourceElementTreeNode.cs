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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Input;
using dnlib.DotNet.Resources;
using dnSpy.AsmEditor;
using dnSpy.AsmEditor.Resources;
using dnSpy.Options;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Base class of serialized resources
	/// </summary>
	public abstract class SerializedResourceElementTreeNode : ResourceElementTreeNode
	{
		public ICommand DeserializeCommand {
			get { return new RelayCommand(a => Deserialize(), a => DeserializeCanExecute()); }
		}

		protected object DeserializedData {
			get { return deserializedData; }
		}
		object deserializedData;

		public string DeserializedStringValue {
			get { return deserializedData == null ? null : deserializedData.ToString(); }
		}

		public bool IsSerialized {
			get { return deserializedData == null; }
		}

		public override string ValueString {
			get {
				if (deserializedData == null)
					return base.ValueString;
				return ConvertObjectToString(deserializedData);
			}
		}

		public override string IconName {
			get { return "UserDefinedDataType"; }
		}

		protected SerializedResourceElementTreeNode(ResourceElement resElem)
			: base(resElem)
		{
			Debug.Assert(resElem.ResourceData is BinaryResourceData);
			DeserializeIfPossible();
		}

		void DeserializeIfPossible()
		{
			if (OtherSettings.Instance.DeserializeResources)
				Deserialize();
		}

		protected override IEnumerable<ResourceData> GetDeserialized()
		{
			if (deserializedData != null)
				yield return new ResourceData(resElem.Name, () => ResourceUtils.StringToStream(ConvertObjectToString(deserializedData)));
			else
				yield return new ResourceData(resElem.Name, () => new MemoryStream(((BinaryResourceData)resElem.ResourceData).Data));
		}

		protected virtual void OnDeserialized()
		{
		}

		public void Deserialize()
		{
			if (!DeserializeCanExecute())
				return;

			var serializedData = ((BinaryResourceData)resElem.ResourceData).Data;
			var formatter = new BinaryFormatter();
			try {
				deserializedData = formatter.Deserialize(new MemoryStream(serializedData));
			}
			catch {
				return;
			}
			if (deserializedData == null)
				return;

			try {
				OnDeserialized();
			}
			catch {
				deserializedData = null;
			}
		}

		public bool DeserializeCanExecute()
		{
			return IsSerialized;
		}

		static string ConvertObjectToString(object obj)
		{
			if (obj == null)
				return null;
			if (!OtherSettings.Instance.DeserializeResources)
				return obj.ToString();

			return SerializationUtils.ConvertObjectToString(obj);
		}

		public override void UpdateData(ResourceElement newResElem)
		{
			base.UpdateData(newResElem);
			deserializedData = null;
			DeserializeIfPossible();
		}

		public override string GetStringContents()
		{
			if (IsSerialized)
				return null;
			return DeserializedStringValue;
		}
	}
}
