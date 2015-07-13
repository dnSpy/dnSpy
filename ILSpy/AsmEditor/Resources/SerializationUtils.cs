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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using dnlib.DotNet.Resources;

namespace ICSharpCode.ILSpy.AsmEditor.Resources
{
	static class SerializationUtils
	{
		public static ResourceElement CreateSerializedImage(string filename)
		{
			using (var stream = File.OpenRead(filename))
				return CreateSerializedImage(stream, filename);
		}

		static ResourceElement CreateSerializedImage(Stream stream, string filename)
		{
			object obj;
			if (filename.EndsWith(".ico"))
				obj = new System.Drawing.Icon(stream);
			else
				obj = new System.Drawing.Bitmap(stream);
			var serializedData = Serialize(obj);

			var userType = new UserResourceType(obj.GetType().AssemblyQualifiedName, ResourceTypeCode.UserTypes);
			var rsrcElem = new ResourceElement {
				Name = Path.GetFileName(filename),
				ResourceData = new BinaryResourceData(userType, serializedData),
			};

			return rsrcElem;
		}

		public static byte[] Serialize(object obj)
		{
			//TODO: The asm names of the saved types are saved in the serialized data. If the current
			//		module is eg. a .NET 2.0 asm, you should replace the versions from 4.0.0.0 to 2.0.0.0.
			var formatter = new BinaryFormatter();
			var outStream = new MemoryStream();
			formatter.Serialize(outStream, obj);
			return outStream.ToArray();
		}

		public static string Deserialize(byte[] data, out object obj)
		{
			try {
				obj = new BinaryFormatter().Deserialize(new MemoryStream(data));
				return string.Empty;
			}
			catch (Exception ex) {
				obj = null;
				return string.Format("Could not deserialize data: {0}", ex.Message);
			}
		}

		public static string CreateObjectFromString(Type targetType, string typeAsString, out object obj)
		{
			obj = null;
			try {
				var typeConverter = TypeDescriptor.GetConverter(targetType);
				if (typeConverter.CanConvertFrom(null, typeof(string))) {
					obj = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, typeAsString);
					return string.Empty;
				}
			}
			catch (Exception ex) {
				return string.Format("Could not convert it from a string: {0}", ex.Message);
			}

			return string.Format("{0} does not have a TypeConverter and can't be converted from a string.", targetType);
		}

		public static string ConvertObjectToString(object obj)
		{
			var objType = obj.GetType();

			try {
				var typeConverter = TypeDescriptor.GetConverter(objType);
				if (typeConverter.CanConvertTo(null, typeof(string))) {
					var s = typeConverter.ConvertTo(null, CultureInfo.InvariantCulture, obj, typeof(string)) as string;
					if (s != null)
						return s;
				}
			}
			catch {
			}

			return obj.ToString();
		}
	}
}
