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
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes {
	[Export(typeof(IResourceFactory<Resource, ResourceTreeNode>))]
	sealed class ImageResourceTreeNodeFactory : IResourceFactory<Resource, ResourceTreeNode> {
		public int Priority {
			get { return 0; }
		}

		public ResourceTreeNode Create(ModuleDef module, Resource resInput) {
			var er = resInput as EmbeddedResource;
			if (er == null)
				return null;

			er.Data.Position = 0;
			if (!ImageResourceElementTreeNodeFactory.CouldBeImage(er.Name, er.Data))
				return null;

			return new ImageResourceTreeNode(er);
		}
	}

	[Export(typeof(IResourceFactory<ResourceElement, ResourceElementTreeNode>))]
	sealed class ImageResourceElementTreeNodeFactory : IResourceFactory<ResourceElement, ResourceElementTreeNode> {
		public int Priority {
			get { return 0; }
		}

		public ResourceElementTreeNode Create(ModuleDef module, ResourceElement resInput) {
			if (resInput.ResourceData.Code != ResourceTypeCode.ByteArray && resInput.ResourceData.Code != ResourceTypeCode.Stream)
				return null;

			var data = (byte[])((BuiltInResourceData)resInput.ResourceData).Data;
			var stream = MemoryImageStream.Create(data);
			if (!CouldBeImage(resInput.Name, stream))
				return null;

			return new ImageResourceElementTreeNode(resInput);
		}

		internal static bool CouldBeImage(string name, IBinaryReader reader) {
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

	public sealed class ImageResourceElementTreeNode : ResourceElementTreeNode {
		ImageSource imageSource;
		byte[] imageData;

		public ImageSource ImageSource {
			get { return imageSource; }
		}

		public override string IconName {
			get { return "ImageFile"; }
		}

		public ImageResourceElementTreeNode(ResourceElement resElem)
			: base(resElem) {
			InitializeImageData();
		}

		void InitializeImageData() {
			this.imageData = (byte[])((BuiltInResourceData)resElem.ResourceData).Data;
			this.imageSource = CreateImageSource(this.imageData);
		}

		internal static ImageSource CreateImageSource(byte[] data) {
			// Check if CUR
			if (data.Length >= 4 && BitConverter.ToUInt32(data, 0) == 0x00020000) {
				try {
					data[2] = 1;
					return CreateImageSource2(data);
				}
				finally {
					data[2] = 2;
				}
			}

			return CreateImageSource2(data);
		}

		static ImageSource CreateImageSource2(byte[] data) {
			var bimg = new BitmapImage();
			bimg.BeginInit();
			bimg.StreamSource = new MemoryStream(data);
			bimg.EndInit();
			return bimg;
		}

		public override void Decompile(Language language, ITextOutput output) {
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null) {
				language.WriteComment(output, string.Empty);
				output.WriteOffsetComment(this);
				smartOutput.AddUIElement(() => {
					return new Image {
						Source = ImageSource,
					};
				});
				output.Write(" = ", TextTokenType.Comment);
				output.WriteDefinition(UIUtils.CleanUpName(Name), this, TextTokenType.Comment);
				output.WriteLine();
				return;
			}

			base.Decompile(language, output);
		}

		protected override IEnumerable<ResourceData> GetDeserialized() {
			yield return new ResourceData(resElem.Name, () => new MemoryStream(imageData));
		}

		public override string CheckCanUpdateData(ResourceElement newResElem) {
			var res = base.CheckCanUpdateData(newResElem);
			if (!string.IsNullOrEmpty(res))
				return res;

			try {
				CreateImageSource((byte[])((BuiltInResourceData)newResElem.ResourceData).Data);
			}
			catch {
				return "The new data is not an image.";
			}

			return null;
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);
			InitializeImageData();
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("imgresel", UIUtils.CleanUpName(resElem.Name)); }
		}
	}

	sealed class ImageResourceTreeNode : ResourceTreeNode {
		readonly ImageSource imageSource;
		readonly byte[] imageData;

		public ImageSource ImageSource {
			get { return imageSource; }
		}

		public override string IconName {
			get { return "ImageFile"; }
		}

		public ImageResourceTreeNode(EmbeddedResource er)
			: base(er) {
			this.imageData = er.GetResourceData();
			this.imageSource = ImageResourceElementTreeNode.CreateImageSource(this.imageData);
		}

		public override void Decompile(Language language, ITextOutput output) {
			var so = output as ISmartTextOutput;
			if (so != null) {
				so.AddUIElement(() => {
					return new Image {
						Source = ImageSource,
					};
				});
			}

			base.Decompile(language, output);
			if (so != null) {
				so.AddButton(null, "Save", (s, e) => Save());
				so.WriteLine();
				so.WriteLine();
			}
		}

		protected override IEnumerable<ResourceData> GetDeserialized() {
			yield return new ResourceData(r.Name, () => new MemoryStream(imageData));
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("imgres", UIUtils.CleanUpName(r.Name)); }
		}
	}
}
