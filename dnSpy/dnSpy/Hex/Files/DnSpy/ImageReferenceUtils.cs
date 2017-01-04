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

using System.Diagnostics;
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

		public static ImageReference GetImageReference(Table table) {
			switch (table) {
			case Table.Module:					return DsImages.ModulePublic;
			case Table.TypeRef:					return DsImages.ClassPublic;
			case Table.TypeDef:					return DsImages.ClassPublic;
			case Table.FieldPtr:				return DsImages.FieldPublic;
			case Table.Field:					return DsImages.FieldPublic;
			case Table.MethodPtr:				return DsImages.MethodPublic;
			case Table.Method:					return DsImages.MethodPublic;
			case Table.ParamPtr:				return DsImages.Parameter;
			case Table.Param:					return DsImages.Parameter;
			case Table.InterfaceImpl:			return DsImages.InterfacePublic;
			case Table.MemberRef:				return DsImages.Property;
			case Table.Constant:				return DsImages.ConstantPublic;
			case Table.CustomAttribute:			break;
			case Table.FieldMarshal:			break;
			case Table.DeclSecurity:			break;
			case Table.ClassLayout:				break;
			case Table.FieldLayout:				break;
			case Table.StandAloneSig:			return DsImages.LocalVariable;
			case Table.EventMap:				return DsImages.EventPublic;
			case Table.EventPtr:				return DsImages.EventPublic;
			case Table.Event:					return DsImages.EventPublic;
			case Table.PropertyMap:				return DsImages.Property;
			case Table.PropertyPtr:				return DsImages.Property;
			case Table.Property:				return DsImages.Property;
			case Table.MethodSemantics:			break;
			case Table.MethodImpl:				break;
			case Table.ModuleRef:				return DsImages.ModulePublic;
			case Table.TypeSpec:				return DsImages.Template;
			case Table.ImplMap:					break;
			case Table.FieldRVA:				break;
			case Table.ENCLog:					break;
			case Table.ENCMap:					break;
			case Table.Assembly:				return DsImages.Assembly;
			case Table.AssemblyProcessor:		return DsImages.Assembly;
			case Table.AssemblyOS:				return DsImages.Assembly;
			case Table.AssemblyRef:				return DsImages.Assembly;
			case Table.AssemblyRefProcessor:	return DsImages.Assembly;
			case Table.AssemblyRefOS:			return DsImages.Assembly;
			case Table.File:					return DsImages.ModuleFile;
			case Table.ExportedType:			return DsImages.ClassPublic;
			case Table.ManifestResource:		return DsImages.SourceFileGroup;
			case Table.NestedClass:				return DsImages.ClassPublic;
			case Table.GenericParam:			break;
			case Table.MethodSpec:				return DsImages.MethodPublic;
			case Table.GenericParamConstraint:	break;
			case Table.Document:				break;
			case Table.MethodDebugInformation:	break;
			case Table.LocalScope:				break;
			case Table.LocalVariable:			break;
			case Table.LocalConstant:			break;
			case Table.ImportScope:				break;
			case Table.StateMachineMethod:		break;
			case Table.CustomDebugInformation:	break;
			default:
				Debug.Fail($"Unknown table: {table}");
				break;
			}
			return DsImages.Metadata;
		}
	}
}
