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
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.TreeView.Resources {
	[ExportResourceNodeCreator(Order = FileTVConstants.ORDER_RSRCCREATOR_SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE)]
	sealed class SerializedImageListStreamerResourceElementNodeCreator : IResourceNodeCreator {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) => null;

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			var serializedData = resourceElement.ResourceData as BinaryResourceData;
			if (serializedData == null)
				return null;

			byte[] imageData;
			if (SerializedImageListStreamerUtilities.GetImageData(module, serializedData.TypeName, serializedData.Data, out imageData))
				return new SerializedImageListStreamerResourceElementNode(treeNodeGroup, resourceElement, imageData);

			return null;
		}
	}

	sealed class SerializedImageListStreamerResourceElementNode : ResourceElementNode, ISerializedImageListStreamerResourceElementNode {
		ImageListOptions imageListOptions;
		byte[] imageData;

		public override Guid Guid => new Guid(FileTVConstants.SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE_GUID);
		public ImageListOptions ImageListOptions => new ImageListOptions(imageListOptions) { Name = Name };
		protected override ImageReference GetIcon() => new ImageReference(GetType().Assembly, "ImageFile");

		public SerializedImageListStreamerResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement, byte[] imageData)
			: base(treeNodeGroup, resourceElement) {
			InitializeImageData(imageData);
		}

		void InitializeImageData(byte[] imageData) {
			this.imageListOptions = SerializedImageListStreamerUtilities.ReadImageData(imageData);
			this.imageData = imageData;
		}

		public override void WriteShort(IDecompilerOutput output, ILanguage language, bool showOffset) {
			var documentViewerOutput = output as IDocumentViewerOutput;
			if (documentViewerOutput != null) {
				for (int i = 0; i < imageListOptions.ImageSources.Count; i++) {
					if (i > 0)
						output.Write(" ", BoxedTextColor.Text);
					var imageSource = imageListOptions.ImageSources[i];
					documentViewerOutput.AddUIElement(() => {
						return new System.Windows.Controls.Image {
							Source = imageSource,
						};
					});
				}
			}

			base.WriteShort(output, language, showOffset);
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var id = imageData;
			yield return new ResourceData(ResourceElement.Name, token => new MemoryStream(id));
		}

		public override string CheckCanUpdateData(ResourceElement newResElem) {
			var res = base.CheckCanUpdateData(newResElem);
			if (!string.IsNullOrEmpty(res))
				return res;
			return SerializedImageListStreamerUtilities.CheckCanUpdateData(this.GetModule(), newResElem);
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			SerializedImageListStreamerUtilities.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out imageData);
			InitializeImageData(imageData);
		}
	}
}
