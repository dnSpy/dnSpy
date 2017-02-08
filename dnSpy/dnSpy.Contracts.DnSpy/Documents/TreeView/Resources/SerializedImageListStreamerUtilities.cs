/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Properties;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Serialized image list streamer utilities
	/// </summary>
	public static class SerializedImageListStreamerUtilities {
		/// <summary>
		/// Gets the image data
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="typeName">Name of type</param>
		/// <param name="serializedData">Serialized data</param>
		/// <param name="imageData">Updated with image data</param>
		/// <returns></returns>
		public static bool GetImageData(ModuleDef module, string typeName, byte[] serializedData, out byte[] imageData) {
			imageData = null;
			if (!SerializedImageUtilities.CheckType(module, typeName, SystemWindowsFormsImageListStreamer))
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

		/// <summary>
		/// Serialize an image list
		/// </summary>
		/// <param name="opts">Options</param>
		/// <returns></returns>
		public static ResourceElement Serialize(ImageListOptions opts) {
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
				ResourceData = new BinaryResourceData(new UserResourceType(obj.GetType().AssemblyQualifiedName, ResourceTypeCode.UserTypes), SerializationUtilities.Serialize(obj)),
			};
		}

		/// <summary>
		/// Checks whether the data can be updated
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="newResElem">New data</param>
		/// <returns></returns>
		public static string CheckCanUpdateData(ModuleDef module, ResourceElement newResElem) {
			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			if (!GetImageData(module, binData.TypeName, binData.Data, out imageData))
				return dnSpy_Contracts_DnSpy_Resources.NewDataNotImageList;

			try {
				ReadImageData(imageData);
			}
			catch {
				return dnSpy_Contracts_DnSpy_Resources.NewDataNotImageList;
			}

			return string.Empty;
		}

		/// <summary>
		/// Reads an image list
		/// </summary>
		/// <param name="imageData">Serialized image list</param>
		/// <returns></returns>
		public static ImageListOptions ReadImageData(byte[] imageData) {
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
				opts.ImageSources.Add(ImageResourceUtilities.CreateImageSource(stream.ToArray()));
			}

			return opts;
		}
	}
}
