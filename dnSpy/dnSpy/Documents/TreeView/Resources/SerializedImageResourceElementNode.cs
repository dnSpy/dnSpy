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
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView.Resources {
	[ExportResourceNodeProvider(Order = DocumentTreeViewConstants.ORDER_RSRCPROVIDER_SERIALIZED_IMAGE_RESOURCE_ELEMENT_NODE)]
	sealed class SerializedImageResourceElementNodeProvider : IResourceNodeProvider {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) => null;

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			var serializedData = resourceElement.ResourceData as BinaryResourceData;
			if (serializedData == null)
				return null;

			byte[] imageData;
			if (SerializedImageUtilities.GetImageData(module, serializedData.TypeName, serializedData.Data, out imageData))
				return new SerializedImageResourceElementNode(treeNodeGroup, resourceElement, imageData);

			return null;
		}
	}

	sealed class SerializedImageResourceElementNode : ResourceElementNode, ISerializedImageResourceElementNode {
		public ImageSource ImageSource => imageSource;
		ImageSource imageSource;
		byte[] imageData;

		public override Guid Guid => new Guid(DocumentTreeViewConstants.SERIALIZED_IMAGE_RESOURCE_ELEMENT_NODE);
		protected override ImageReference GetIcon() => new ImageReference(GetType().Assembly, "ImageFile");

		public SerializedImageResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement, byte[] imageData)
			: base(treeNodeGroup, resourceElement) {
			InitializeImageData(imageData);
		}

		void InitializeImageData(byte[] imageData) {
			this.imageData = imageData;
			this.imageSource = ImageResourceUtilities.CreateImageSource(imageData);
		}

		public override void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset) {
			var documentViewerOutput = output as IDocumentViewerOutput;
			if (documentViewerOutput != null) {
				documentViewerOutput.AddUIElement(() => {
					return new System.Windows.Controls.Image {
						Source = ImageSource,
					};
				});
			}

			base.WriteShort(output, decompiler, showOffset);
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var id = imageData;
			yield return new ResourceData(ResourceElement.Name, token => new MemoryStream(id));
		}

		public ResourceElement GetAsRawImage() => new ResourceElement {
			Name = ResourceElement.Name,
			ResourceData = new BuiltInResourceData(ResourceTypeCode.ByteArray, imageData),
		};

		public override string CheckCanUpdateData(ResourceElement newResElem) {
			var res = base.CheckCanUpdateData(newResElem);
			if (!string.IsNullOrEmpty(res))
				return res;

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			if (!SerializedImageUtilities.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out imageData))
				return dnSpy_Resources.NewDataIsNotAnImage;

			try {
				ImageResourceUtilities.CreateImageSource(imageData);
			}
			catch {
				return dnSpy_Resources.NewDataIsNotAnImage;
			}

			return string.Empty;
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			SerializedImageUtilities.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out imageData);
			InitializeImageData(imageData);
		}
	}
}
