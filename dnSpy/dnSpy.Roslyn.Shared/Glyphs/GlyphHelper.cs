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
using System.Reflection;
using dnSpy.Contracts.Images;
using dnSpy.Roslyn.Internal;

namespace dnSpy.Roslyn.Shared.Glyphs {
	static class GlyphHelper {
		static readonly Assembly imageAssembly = Assembly.Load("dnSpy");

		public static ImageReference? GetImageReference(this Glyph glyph) {
			var name = GetImageName(glyph);
			if (name == null)
				return null;
			return new ImageReference(imageAssembly, name);
		}

		static string GetImageName(Glyph glyph) {
			switch (glyph) {
			case Glyph.Assembly:				return "Assembly";
			case Glyph.BasicFile:				return "VisualBasicFile";
			case Glyph.BasicProject:			return "VBProjectNode";
			case Glyph.ClassPublic:				return "Class";
			case Glyph.ClassProtected:			return "ClassProtected";
			case Glyph.ClassPrivate:			return "ClassPrivate";
			case Glyph.ClassInternal:			return "ClassInternal";
			case Glyph.CSharpFile:				return "CSharpFile";
			case Glyph.CSharpProject:			return "CSProjectNode";
			case Glyph.ConstantPublic:			return "Literal";
			case Glyph.ConstantProtected:		return "LiteralProtected";
			case Glyph.ConstantPrivate:			return "LiteralPrivate";
			case Glyph.ConstantInternal:		return "LiteralInternal";
			case Glyph.DelegatePublic:			return "Delegate";
			case Glyph.DelegateProtected:		return "DelegateProtected";
			case Glyph.DelegatePrivate:			return "DelegatePrivate";
			case Glyph.DelegateInternal:		return "DelegateInternal";
			case Glyph.EnumPublic:				return "Enum";
			case Glyph.EnumProtected:			return "EnumProtected";
			case Glyph.EnumPrivate:				return "EnumPrivate";
			case Glyph.EnumInternal:			return "EnumInternal";
			case Glyph.EnumMember:				return "EnumValue";
			case Glyph.Error:					return "StatusError";
			case Glyph.EventPublic:				return "Event";
			case Glyph.EventProtected:			return "EventProtected";
			case Glyph.EventPrivate:			return "EventPrivate";
			case Glyph.EventInternal:			return "EventInternal";
			case Glyph.ExtensionMethodPublic:	return "ExtensionMethod";
			case Glyph.ExtensionMethodProtected:return "ExtensionMethod";
			case Glyph.ExtensionMethodPrivate:	return "ExtensionMethod";
			case Glyph.ExtensionMethodInternal:	return "ExtensionMethod";
			case Glyph.FieldPublic:				return "Field";
			case Glyph.FieldProtected:			return "FieldProtected";
			case Glyph.FieldPrivate:			return "FieldPrivate";
			case Glyph.FieldInternal:			return "FieldInternal";
			case Glyph.InterfacePublic:			return "Interface";
			case Glyph.InterfaceProtected:		return "InterfaceProtected";
			case Glyph.InterfacePrivate:		return "InterfacePrivate";
			case Glyph.InterfaceInternal:		return "InterfaceInternal";
			case Glyph.Intrinsic:				return "Type";
			case Glyph.Keyword:					return "IntellisenseKeyword";
			case Glyph.Label:					return "Label";
			case Glyph.Local:					return "Local";
			case Glyph.Namespace:				return "Namespace";
			case Glyph.MethodPublic:			return "Method";
			case Glyph.MethodProtected:			return "MethodProtected";
			case Glyph.MethodPrivate:			return "MethodPrivate";
			case Glyph.MethodInternal:			return "MethodInternal";
			case Glyph.ModulePublic:			return "Module";
			case Glyph.ModuleProtected:			return "ModuleProtected";
			case Glyph.ModulePrivate:			return "ModulePrivate";
			case Glyph.ModuleInternal:			return "ModuleInternal";
			case Glyph.OpenFolder:				return "FolderOpened";
			case Glyph.Operator:				return "Operator";
			case Glyph.Parameter:				return "Local";// Same image as Local, just like what VS does
			case Glyph.PropertyPublic:			return "Property";
			case Glyph.PropertyProtected:		return "PropertyProtected";
			case Glyph.PropertyPrivate:			return "PropertyPrivate";
			case Glyph.PropertyInternal:		return "PropertyInternal";
			case Glyph.RangeVariable:			return "Local";
			case Glyph.Reference:				return "AssemblyReference";
			case Glyph.StructurePublic:			return "Struct";
			case Glyph.StructureProtected:		return "StructProtected";
			case Glyph.StructurePrivate:		return "StructPrivate";
			case Glyph.StructureInternal:		return "StructInternal";
			case Glyph.TypeParameter:			return "GenericParameter";
			case Glyph.Snippet:					return "Snippet";
			case Glyph.CompletionWarning:		return "StatusWarning";
			case Glyph.AddReference:			return "AddReference";
			case Glyph.NuGet:					return "NuGet";
			default:
				Debug.Fail($"Unknown {nameof(Glyph)}: {glyph}");
				return null;
			}
		}
	}
}
