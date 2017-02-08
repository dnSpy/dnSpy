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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Serialized image utilities
	/// </summary>
	public static class SerializedImageUtilities {
		/// <summary>
		/// Gets the image data
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="typeName">Name of type</param>
		/// <param name="serializedData">Serialized data</param>
		/// <param name="imageData">Updated with the image data</param>
		/// <returns></returns>
		public static bool GetImageData(ModuleDef module, string typeName, byte[] serializedData, out byte[] imageData) {
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

		static bool CouldBeBitmap(ModuleDef module, string name) => CheckType(module, name, SystemDrawingBitmap);
		static bool CouldBeIcon(ModuleDef module, string name) => CheckType(module, name, SystemDrawingIcon);

		/// <summary>
		/// Checks whether the type matches an expected type
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="name">Type name</param>
		/// <param name="expectedType">Expected type</param>
		/// <returns></returns>
		public static bool CheckType(ModuleDef module, string name, TypeRef expectedType) {
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

		/// <summary>
		/// Serializes the image
		/// </summary>
		/// <param name="resElem">Resource element</param>
		/// <returns></returns>
		public static ResourceElement Serialize(ResourceElement resElem) {
			var data = (byte[])((BuiltInResourceData)resElem.ResourceData).Data;
			bool isIcon = BitConverter.ToUInt32(data, 0) == 0x00010000;

			object obj;
			if (isIcon)
				obj = new System.Drawing.Icon(new MemoryStream(data));
			else
				obj = new System.Drawing.Bitmap(new MemoryStream(data));

			return new ResourceElement {
				Name = resElem.Name,
				ResourceData = new BinaryResourceData(new UserResourceType(obj.GetType().AssemblyQualifiedName, ResourceTypeCode.UserTypes), SerializationUtilities.Serialize(obj)),
			};
		}
	}
}
