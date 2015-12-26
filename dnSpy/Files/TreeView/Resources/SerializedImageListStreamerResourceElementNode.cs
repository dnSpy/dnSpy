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
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Decompiler;
using dnSpy.Shared.UI.Files.TreeView.Resources;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.TreeView.Resources {
	[ExportResourceNodeCreator(Order = FileTVConstants.ORDER_RSRCCREATOR_SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE)]
	sealed class SerializedImageListStreamerResourceElementNodeCreator : IResourceNodeCreator {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			return null;
		}

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			var serializedData = resourceElement.ResourceData as BinaryResourceData;
			if (serializedData == null)
				return null;

			byte[] imageData;
			if (SerializedImageListStreamerUtils.GetImageData(module, serializedData.TypeName, serializedData.Data, out imageData))
				return new SerializedImageListStreamerResourceElementNode(treeNodeGroup, resourceElement, imageData);

			return null;
		}
	}

	sealed class SerializedImageListStreamerResourceElementNode : ResourceElementNode, ISerializedImageListStreamerResourceElementNode {
		ImageListOptions imageListOptions;
		byte[] imageData;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE_GUID); }
		}

		public ImageListOptions ImageListOptions {
			get { return new ImageListOptions(imageListOptions) { Name = Name }; }
		}

		protected override ImageReference GetIcon() {
			return new ImageReference(GetType().Assembly, "ImageFile");
		}

		public SerializedImageListStreamerResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement, byte[] imageData)
			: base(treeNodeGroup, resourceElement) {
			InitializeImageData(imageData);
		}

		void InitializeImageData(byte[] imageData) {
			this.imageListOptions = SerializedImageListStreamerUtils.ReadImageData(imageData);
			this.imageData = imageData;
		}

		public override void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null) {
				for (int i = 0; i < imageListOptions.ImageSources.Count; i++) {
					if (i > 0)
						output.WriteSpace();
					var imageSource = imageListOptions.ImageSources[i];
					smartOutput.AddUIElement(() => {
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
			return SerializedImageListStreamerUtils.CheckCanUpdateData(this.GetModule(), newResElem);
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			SerializedImageListStreamerUtils.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out imageData);
			InitializeImageData(imageData);
		}
	}
}
