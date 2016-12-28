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
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;

namespace dnSpy.Hex.Files.DnSpy {
	static class ImageReferenceUtils {
		public static ImageReference? GetImageReference(string filename) {
			var extension = GetExtension(filename);
			switch (extension.ToUpperInvariant()) {
			case "CS":
			case "CSX":
				return DsImages.CSFileNode;
			case "VB":
			case "VBX":
				return DsImages.VBFileNode;
			case "TXT":
				return DsImages.TextFile;
			case "XAML":
			case "BAML":
				return DsImages.WPFFile;
			case "XML":
				return DsImages.XMLFile;
			case "XSD":
				return DsImages.XMLSchema;
			case "XSLT":
				return DsImages.XSLTransform;
			case "PNG":
			case "BMP":
			case "JPG":
			case "JPEG":
			case "ICO":
				return DsImages.Image;
			case "CUR":
				return DsImages.Cursor;
			}
			return null;
		}

		static string GetExtension(string s) {
			int i = s.LastIndexOf('.');
			if (i < 0)
				return string.Empty;
			return s.Substring(i + 1);
		}

		public static ImageReference GetImageReference(ResourceTypeCode typeCode) {
			if (typeCode >= ResourceTypeCode.UserTypes)
				return DsImages.UserDefinedDataType;

			switch (typeCode) {
			case ResourceTypeCode.String:
				return DsImages.String;

			case ResourceTypeCode.Null:
			case ResourceTypeCode.Boolean:
			case ResourceTypeCode.Char:
			case ResourceTypeCode.Byte:
			case ResourceTypeCode.SByte:
			case ResourceTypeCode.Int16:
			case ResourceTypeCode.UInt16:
			case ResourceTypeCode.Int32:
			case ResourceTypeCode.UInt32:
			case ResourceTypeCode.Int64:
			case ResourceTypeCode.UInt64:
			case ResourceTypeCode.Single:
			case ResourceTypeCode.Double:
			case ResourceTypeCode.Decimal:
			case ResourceTypeCode.DateTime:
			case ResourceTypeCode.TimeSpan:
			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
			case ResourceTypeCode.UserTypes:
			default:
				return DsImages.Binary;
			}
		}
	}
}
