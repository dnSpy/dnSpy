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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Shared.UI.Files.TreeView.Resources {
	public abstract class SerializedResourceElementNode : ResourceElementNode, ISerializedResourceElementNode {
		object deserializedData;

		string DeserializedStringValue {
			get { return deserializedData == null ? null : deserializedData.ToString(); }
		}

		bool IsSerialized {
			get { return deserializedData == null; }
		}

		protected override string ValueString {
			get {
				if (deserializedData == null)
					return base.ValueString;
				return ConvertObjectToString(deserializedData);
			}
		}

		protected override ImageReference GetIcon() {
			return new ImageReference(Context.FileTreeView.GetType().Assembly, "UserDefinedDataType");
		}

		protected SerializedResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement)
			: base(treeNodeGroup, resourceElement) {
			Debug.Assert(resourceElement.ResourceData is BinaryResourceData);
		}

		public override void Initialize() {
			DeserializeIfPossible();
		}

		void DeserializeIfPossible() {
			if (Context.DeserializeResources)
				Deserialize();
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var dd = deserializedData;
			var re = ResourceElement;
			if (dd != null)
				yield return new ResourceData(re.Name, token => ResourceUtils.StringToStream(ConvertObjectToString(dd)));
			else
				yield return new ResourceData(re.Name, token => new MemoryStream(((BinaryResourceData)re.ResourceData).Data));
		}

		protected virtual void OnDeserialized() {
		}

		public bool CanDeserialize {
			get { return IsSerialized; }
		}

		public void Deserialize() {
			if (!CanDeserialize)
				return;

			var serializedData = ((BinaryResourceData)ResourceElement.ResourceData).Data;
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

		string ConvertObjectToString(object obj) {
			if (obj == null)
				return null;
			if (!Context.DeserializeResources)
				return obj.ToString();

			return SerializationUtils.ConvertObjectToString(obj);
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);
			deserializedData = null;
			DeserializeIfPossible();
		}

		public override string ToString(CancellationToken token, bool canDecompile) {
			if (IsSerialized)
				return null;
			return DeserializedStringValue;
		}
	}
}
