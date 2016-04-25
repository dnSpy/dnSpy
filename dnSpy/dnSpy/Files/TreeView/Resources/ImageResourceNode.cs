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
using dnlib.IO;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Properties;
using dnSpy.Shared.Decompiler;
using dnSpy.Shared.Files.TreeView.Resources;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Files.TreeView.Resources {
	[ExportResourceNodeCreator(Order = FileTVConstants.ORDER_RSRCCREATOR_IMAGE_RESOURCE_NODE)]
	sealed class ImageResourceNodeCreator : IResourceNodeCreator {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			var er = resource as EmbeddedResource;
			if (er == null)
				return null;

			er.Data.Position = 0;
			if (!CouldBeImage(er.Name, er.Data))
				return null;

			return new ImageResourceNode(treeNodeGroup, er);
		}

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			if (resourceElement.ResourceData.Code != ResourceTypeCode.ByteArray && resourceElement.ResourceData.Code != ResourceTypeCode.Stream)
				return null;

			var data = (byte[])((BuiltInResourceData)resourceElement.ResourceData).Data;
			var stream = MemoryImageStream.Create(data);
			if (!CouldBeImage(resourceElement.Name, stream))
				return null;

			return new ImageResourceElementNode(treeNodeGroup, resourceElement);
		}

		static bool CouldBeImage(string name, IBinaryReader reader) {
			return CouldBeImage(name) || CouldBeImage(reader);
		}

		static readonly string[] fileExtensions = {
			".png",
			".gif",
			".bmp", ".dib",
			".jpg", ".jpeg", ".jpe", ".jif", ".jfif", ".jfi",
			".ico", ".cur",
		};
		static bool CouldBeImage(string name) {
			foreach (var ext in fileExtensions) {
				if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		static bool CouldBeImage(IBinaryReader reader) {
			reader.Position = 0;
			if (reader.Length < 0x16)
				return false;

			uint d = reader.ReadUInt32();
			if (unchecked((ushort)d) == 0x4D42) {
				// Possible BMP image
				reader.Position -= 2;
				uint size = reader.ReadUInt32();
				if (size > reader.Length)
					return false;
				reader.Position += 4;
				uint offs = reader.ReadUInt32();
				return offs < size;
			}

			// Check if GIF87a or GIF89a
			if (d == 0x38464947)
				return (d = reader.ReadUInt16()) == 0x6139 || d == 0x6137;

			// Check if PNG
			if (d == 0x474E5089)
				return reader.ReadUInt32() == 0x0A1A0A0D;

			// Check if ICO or CUR
			if (d == 0x00010000 || d == 0x00020000) {
				int num = reader.ReadUInt16();
				if (num <= 0)
					return false;

				reader.Position += 8;
				uint size = reader.ReadUInt32();
				uint offs = reader.ReadUInt32();
				uint end = unchecked(offs + size);
				return offs <= end && end <= reader.Length;
			}

			return false;
		}
	}

	sealed class ImageResourceNode : ResourceNode, IImageResourceNode {
		readonly ImageSource imageSource;
		readonly byte[] imageData;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.IMAGE_RESOURCE_NODE_GUID); }
		}

		protected override ImageReference GetIcon() {
			return new ImageReference(GetType().Assembly, "ImageFile");
		}

		public ImageResourceNode(ITreeNodeGroup treeNodeGroup, EmbeddedResource resource)
			: base(treeNodeGroup, resource) {
			this.imageData = resource.GetResourceData();
			this.imageSource = ImageResourceUtils.CreateImageSource(this.imageData);
		}

		public override void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			var so = output as ISmartTextOutput;
			if (so != null) {
				so.AddUIElement(() => {
					return new System.Windows.Controls.Image {
						Source = imageSource,
					};
				});
			}

			base.WriteShort(output, language, showOffset);
			if (so != null) {
				so.AddButton(dnSpy_Resources.SaveResourceButton, (s, e) => Save());
				so.WriteLine();
				so.WriteLine();
			}
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var id = imageData;
			yield return new ResourceData(Resource.Name, token => new MemoryStream(id));
		}
	}

	sealed class ImageResourceElementNode : ResourceElementNode, IImageResourceElementNode {
		ImageSource imageSource;
		byte[] imageData;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.IMAGE_RESOURCE_ELEMENT_NODE_GUID); }
		}

		protected override ImageReference GetIcon() {
			return new ImageReference(GetType().Assembly, "ImageFile");
		}

		public ImageResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement)
			: base(treeNodeGroup, resourceElement) {
			InitializeImageData();
		}

		void InitializeImageData() {
			this.imageData = (byte[])((BuiltInResourceData)ResourceElement.ResourceData).Data;
			this.imageSource = ImageResourceUtils.CreateImageSource(this.imageData);
		}

		public override void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null) {
				language.WriteCommentBegin(output, true);
				output.WriteOffsetComment(this, showOffset);
				smartOutput.AddUIElement(() => {
					return new System.Windows.Controls.Image {
						Source = imageSource,
					};
				});
				output.Write(" = ", TextTokenKind.Comment);
				const string LTR = "\u200E";
				output.WriteDefinition(NameUtils.CleanName(Name) + LTR, this, TextTokenKind.Comment);
				language.WriteCommentEnd(output, true);
				output.WriteLine();
				return;
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

			try {
				ImageResourceUtils.CreateImageSource((byte[])((BuiltInResourceData)newResElem.ResourceData).Data);
			}
			catch {
				return dnSpy_Resources.NewDataIsNotAnImage;
			}

			return null;
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);
			InitializeImageData();
		}
	}
}
