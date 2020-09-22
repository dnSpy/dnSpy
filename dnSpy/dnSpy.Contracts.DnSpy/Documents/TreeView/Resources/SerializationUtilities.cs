/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.DnSpy.Properties;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Serialization utilities
	/// </summary>
	public static class SerializationUtilities {
		/// <summary>
		/// Creates a serialized image
		/// </summary>
		/// <param name="filename">Filename of image</param>
		/// <returns></returns>
		public static ResourceElement CreateSerializedImage(string filename) {
			using (var stream = File.OpenRead(filename))
				return CreateSerializedImage(stream, filename);
		}

		static ResourceElement CreateSerializedImage(Stream stream, string filename) {
			object obj;
			string typeName;
			if (filename.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) {
				obj = new System.Drawing.Icon(stream);
				typeName = SerializedImageUtilities.SystemDrawingIcon.AssemblyQualifiedName;
			}
			else {
				obj = new System.Drawing.Bitmap(stream);
				typeName = SerializedImageUtilities.SystemDrawingBitmap.AssemblyQualifiedName;
			}
			var serializedData = Serialize(obj);

			var userType = new UserResourceType(typeName, ResourceTypeCode.UserTypes);
			var rsrcElem = new ResourceElement {
				Name = Path.GetFileName(filename),
				ResourceData = new BinaryResourceData(userType, serializedData),
			};

			return rsrcElem;
		}

		/// <summary>
		/// Serializes the object
		/// </summary>
		/// <param name="obj">Data</param>
		/// <returns></returns>
		public static byte[] Serialize(object obj) {
			//TODO: The asm names of the saved types are saved in the serialized data. If the current
			//		module is eg. a .NET 2.0 asm, you should replace the versions from 4.0.0.0 to 2.0.0.0.
			var formatter = new BinaryFormatter();
			var outStream = new MemoryStream();
#pragma warning disable SYSLIB0011
			formatter.Serialize(outStream, obj);
#pragma warning restore SYSLIB0011
			return outStream.ToArray();
		}

		/// <summary>
		/// Deserializes the data
		/// </summary>
		/// <param name="data">Serialized data</param>
		/// <param name="obj">Deserialized data</param>
		/// <returns></returns>
		public static string Deserialize(byte[] data, out object? obj) {
			try {
#pragma warning disable SYSLIB0011
				obj = new BinaryFormatter().Deserialize(new MemoryStream(data));
#pragma warning restore SYSLIB0011
				return string.Empty;
			}
			catch (Exception ex) {
				obj = null;
				return string.Format(dnSpy_Contracts_DnSpy_Resources.CouldNotDeserializeData, ex.Message);
			}
		}

		/// <summary>
		/// Creates an object from a string
		/// </summary>
		/// <param name="targetType">Target type</param>
		/// <param name="typeAsString">Data as a string</param>
		/// <param name="obj">Updated with the deserialized data</param>
		/// <returns></returns>
		public static string CreateObjectFromString(Type targetType, string typeAsString, out object? obj) {
			obj = null;
			try {
				var typeConverter = TypeDescriptor.GetConverter(targetType);
				if (typeConverter.CanConvertFrom(null, typeof(string))) {
					obj = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, typeAsString);
					return string.Empty;
				}
			}
			catch (Exception ex) {
				return string.Format(dnSpy_Contracts_DnSpy_Resources.CouldNotConvertFromString, ex.Message);
			}

			return string.Format(dnSpy_Contracts_DnSpy_Resources.NoTypeConverter, targetType);
		}

		/// <summary>
		/// Converts data to a string
		/// </summary>
		/// <param name="obj">Data</param>
		/// <returns></returns>
		public static string? ConvertObjectToString(object obj) {
			var objType = obj.GetType();

			try {
				var typeConverter = TypeDescriptor.GetConverter(objType);
				if (typeConverter.CanConvertTo(null, typeof(string))) {
					if (typeConverter.ConvertTo(null, CultureInfo.InvariantCulture, obj, typeof(string)) is string s)
						return s;
				}
			}
			catch {
			}

			return obj.ToString();
		}
	}
}
