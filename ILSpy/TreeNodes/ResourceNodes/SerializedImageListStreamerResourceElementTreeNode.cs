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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AsmEditor.Resources;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[Export(typeof(IResourceFactory<ResourceElement, ResourceElementTreeNode>))]
	sealed class SerializedImageListStreamerResourceElementTreeNodeFactory : IResourceFactory<ResourceElement, ResourceElementTreeNode>
	{
		public int Priority {
			get { return 100; }
		}

		public ResourceElementTreeNode Create(ModuleDef module, ResourceElement resInput)
		{
			var serializedData = resInput.ResourceData as BinaryResourceData;
			if (serializedData == null)
				return null;

			byte[] imageData;
			if (GetImageData(module, serializedData.TypeName, serializedData.Data, out imageData))
				return new SerializedImageListStreamerResourceElementTreeNode(resInput, imageData);

			return null;
		}

		internal static bool GetImageData(ModuleDef module, string typeName, byte[] serializedData, out byte[] imageData)
		{
			imageData = null;
			if (!SerializedImageResourceElementTreeNodeFactory.CheckType(module, typeName, SystemWindowsFormsImageListStreamer))
				return false;

			var dict = Deserializer.Deserialize(SystemWindowsFormsImageListStreamer.DefinitionAssembly.FullName, SystemWindowsFormsImageListStreamer.ReflectionFullName, serializedData);
			// ImageListStreamer loops over every item looking for "Data" (case insensitive)
			foreach (var v in dict.Values) {
				var d = v.Value as byte[];
				if (d == null)
					continue;
				if ("Data".Equals(v.Name, StringComparison.OrdinalIgnoreCase)) {
					imageData = d;
					return true;
				}
			}

			return false;
		}

		static readonly AssemblyRef SystemWindowsForms = new AssemblyRefUser(new AssemblyNameInfo("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
		static readonly TypeRef SystemWindowsFormsImageListStreamer = new TypeRefUser(null, "System.Windows.Forms", "ImageListStreamer", SystemWindowsForms);
	}

	sealed class SerializedImageListStreamerResourceElementTreeNode : ResourceElementTreeNode
	{
		ImageListOptions imageListOptions;
		byte[] imageData;

		public ImageListOptions ImageListOptions {
			get { return new ImageListOptions(imageListOptions) { Name = Name }; }
		}

		public override string IconName {
			get { return "ImageFile"; }
		}

		public SerializedImageListStreamerResourceElementTreeNode(ResourceElement resElem, byte[] imageData)
			: base(resElem)
		{
			InitializeImageData(imageData);
		}

		static ImageListOptions ReadImageData(byte[] imageData)
		{
			var imageList = new ImageList();
			var info = new SerializationInfo(typeof(ImageListStreamer), new FormatterConverter());
			info.AddValue("Data", imageData);
			var ctor = typeof(ImageListStreamer).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
			var streamer = (ImageListStreamer)ctor.Invoke(new object[] { info, new StreamingContext(StreamingContextStates.All) });
			imageList.ImageStream = streamer;

			var opts = new ImageListOptions();
			opts.ColorDepth = imageList.ColorDepth;
			opts.ImageSize = imageList.ImageSize;
			opts.TransparentColor = imageList.TransparentColor;

			for (int i = 0; i < imageList.Images.Count; i++) {
				var bitmap = imageList.Images[i];
				var stream = new MemoryStream();
				bitmap.Save(stream, ImageFormat.Bmp);
				opts.ImageSources.Add(ImageResourceElementTreeNode.CreateImageSource(stream.ToArray()));
			}

			return opts;
		}

		void InitializeImageData(byte[] imageData)
		{
			this.imageListOptions = ReadImageData(imageData);
			this.imageData = imageData;
		}

		public override void Decompile(Language language, ITextOutput output)
		{
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null) {
				for (int i = 0; i < imageListOptions.ImageSources.Count; i++) {
					if (i > 0)
						output.WriteSpace();
					var imageSource = imageListOptions.ImageSources[i];
					smartOutput.AddUIElement(() => {
						return new Image {
							Source = imageSource,
						};
					});
				}
			}

			base.Decompile(language, output);
		}

		protected override IEnumerable<ResourceData> GetDeserialized()
		{
			yield return new ResourceData(resElem.Name, () => new MemoryStream(imageData));
		}

		internal ResourceElement Serialize(IList<ImageSource> imageSources)
		{
			return Serialize(ImageListOptions);
		}

		internal static ResourceElement Serialize(ImageListOptions opts)
		{
			var imgList = new ImageList();
			imgList.ColorDepth = opts.ColorDepth;
			imgList.ImageSize = opts.ImageSize;
			imgList.TransparentColor = opts.TransparentColor;

			foreach (var imageSource in opts.ImageSources) {
				var bitmapSource = imageSource as BitmapSource;
				if (bitmapSource == null)
					throw new InvalidOperationException("Only BitmapSources can be used");
				var encoder = new BmpBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
				var outStream = new MemoryStream();
				encoder.Save(outStream);
				outStream.Position = 0;
				var wfBmp = new System.Drawing.Bitmap(outStream);
				imgList.Images.Add(wfBmp);
			}

			var obj = imgList.ImageStream;
			return new ResourceElement {
				Name = opts.Name,
				ResourceData = new BinaryResourceData(new UserResourceType(obj.GetType().AssemblyQualifiedName, ResourceTypeCode.UserTypes), AsmEditor.Resources.SerializationUtils.Serialize(obj)),
			};
		}

		public override string CheckCanUpdateData(ResourceElement newResElem)
		{
			var res = base.CheckCanUpdateData(newResElem);
			if (!string.IsNullOrEmpty(res))
				return res;
			return CheckCanUpdateData(GetModule(this), newResElem);
		}

		internal static string CheckCanUpdateData(ModuleDef module, ResourceElement newResElem)
		{
			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			if (!SerializedImageListStreamerResourceElementTreeNodeFactory.GetImageData(module, binData.TypeName, binData.Data, out imageData))
				return "The new data is not an image list.";

			try {
				ReadImageData(imageData);
			}
			catch {
				return "The new data is not an image list.";
			}

			return string.Empty;
		}

		public override void UpdateData(ResourceElement newResElem)
		{
			base.UpdateData(newResElem);

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			SerializedImageListStreamerResourceElementTreeNodeFactory.GetImageData(GetModule(this), binData.TypeName, binData.Data, out imageData);
			InitializeImageData(imageData);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("serimgrelistsel", UIUtils.CleanUpName(resElem.Name)); }
		}
	}
}
