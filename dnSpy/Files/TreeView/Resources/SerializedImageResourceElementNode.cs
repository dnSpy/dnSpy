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
using System.Windows.Media;
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
	[ExportResourceNodeCreator(Order = FileTVConstants.ORDER_RSRCCREATOR_SERIALIZED_IMAGE_RESOURCE_ELEMENT_NODE)]
	sealed class SerializedImageResourceElementNodeCreator : IResourceNodeCreator {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			return null;
		}

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			var serializedData = resourceElement.ResourceData as BinaryResourceData;
			if (serializedData == null)
				return null;

			byte[] imageData;
			if (GetImageData(module, serializedData.TypeName, serializedData.Data, out imageData))
				return new SerializedImageResourceElementNode(treeNodeGroup, resourceElement, imageData);

			return null;
		}

		internal static bool GetImageData(ModuleDef module, string typeName, byte[] serializedData, out byte[] imageData) {
			imageData = null;
			if (CouldBeBitmap(module, typeName)) {
				var dict = Deserializer.Deserialize(SystemDrawingBitmap.DefinitionAssembly.FullName, SystemDrawingBitmap.ReflectionFullName, serializedData);
				// Bitmap loops over every item looking for "Data" (case insensitive)
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

			if (CouldBeIcon(module, typeName)) {
				var dict = Deserializer.Deserialize(SystemDrawingIcon.DefinitionAssembly.FullName, SystemDrawingIcon.ReflectionFullName, serializedData);
				DeserializedDataInfo info;
				if (!dict.TryGetValue("IconData", out info))
					return false;
				imageData = info.Value as byte[];
				return imageData != null;
			}

			return false;
		}

		static bool CouldBeBitmap(ModuleDef module, string name) {
			return CheckType(module, name, SystemDrawingBitmap);
		}

		static bool CouldBeIcon(ModuleDef module, string name) {
			return CheckType(module, name, SystemDrawingIcon);
		}

		internal static bool CheckType(ModuleDef module, string name, TypeRef expectedType) {
			if (module == null)
				module = new ModuleDefUser();
			var tr = TypeNameParser.ParseReflection(module, name, null);
			if (tr == null)
				return false;

			var flags = AssemblyNameComparerFlags.All & ~AssemblyNameComparerFlags.Version;
			if (!new AssemblyNameComparer(flags).Equals(tr.DefinitionAssembly, expectedType.DefinitionAssembly))
				return false;

			if (!new SigComparer().Equals(tr, expectedType))
				return false;

			return true;
		}
		static readonly AssemblyRef SystemDrawingAsm = new AssemblyRefUser(new AssemblyNameInfo("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
		static readonly TypeRef SystemDrawingBitmap = new TypeRefUser(null, "System.Drawing", "Bitmap", SystemDrawingAsm);
		static readonly TypeRef SystemDrawingIcon = new TypeRefUser(null, "System.Drawing", "Icon", SystemDrawingAsm);
	}

	sealed class SerializedImageResourceElementNode : ResourceElementNode, ISerializedImageResourceElementNode {
		public ImageSource ImageSource {
			get { return imageSource; }
		}
		ImageSource imageSource;
		byte[] imageData;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.SERIALIZED_IMAGE_RESOURCE_ELEMENT_NODE); }
		}

		protected override ImageReference GetIcon() {
			return new ImageReference(GetType().Assembly, "ImageFile");
		}

		public SerializedImageResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement, byte[] imageData)
			: base(treeNodeGroup, resourceElement) {
			InitializeImageData(imageData);
		}

		void InitializeImageData(byte[] imageData) {
			this.imageData = imageData;
			this.imageSource = ImageResourceElementNode.CreateImageSource(imageData);
		}

		public override void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null) {
				smartOutput.AddUIElement(() => {
					return new System.Windows.Controls.Image {
						Source = ImageSource,
					};
				});
			}

			base.WriteShort(output, language, showOffset);
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var id = imageData;
			yield return new ResourceData(ResourceElement.Name, token => new MemoryStream(id));
		}

		internal ResourceElement GetAsRawImage() {
			return new ResourceElement {
				Name = ResourceElement.Name,
				ResourceData = new BuiltInResourceData(ResourceTypeCode.ByteArray, imageData),
			};
		}

		internal ResourceElement Serialize(ResourceElement resElem) {
			var data = (byte[])((BuiltInResourceData)resElem.ResourceData).Data;
			bool isIcon = BitConverter.ToUInt32(data, 0) == 0x00010000;

			object obj;
			if (isIcon)
				obj = new System.Drawing.Icon(new MemoryStream(data));
			else
				obj = new System.Drawing.Bitmap(new MemoryStream(data));

			return new ResourceElement {
				Name = resElem.Name,
				ResourceData = new BinaryResourceData(new UserResourceType(obj.GetType().AssemblyQualifiedName, ResourceTypeCode.UserTypes), SerializationUtils.Serialize(obj)),
			};
		}

		public override string CheckCanUpdateData(ResourceElement newResElem) {
			var res = base.CheckCanUpdateData(newResElem);
			if (!string.IsNullOrEmpty(res))
				return res;

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			if (!SerializedImageResourceElementNodeCreator.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out imageData))
				return "The new data is not an image.";

			try {
				ImageResourceElementNode.CreateImageSource(imageData);
			}
			catch {
				return "The new data is not an image.";
			}

			return string.Empty;
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);

			var binData = (BinaryResourceData)newResElem.ResourceData;
			byte[] imageData;
			SerializedImageResourceElementNodeCreator.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out imageData);
			InitializeImageData(imageData);
		}
	}
}
