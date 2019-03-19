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

using System.Diagnostics;
using dnSpy.Contracts.Images;
using dnSpy.Roslyn.Internal;

namespace dnSpy.Roslyn.Glyphs {
	static class GlyphHelper {
		public static ImageReference? GetImageReference(this Glyph glyph) {
			switch (glyph) {
			case Glyph.None:					return null;
			case Glyph.Assembly:				return DsImages.Assembly;
			case Glyph.BasicFile:				return DsImages.VBFileNode;
			case Glyph.BasicProject:			return DsImages.VBProjectNode;
			case Glyph.ClassPublic:				return DsImages.ClassPublic;
			case Glyph.ClassProtected:			return DsImages.ClassProtected;
			case Glyph.ClassPrivate:			return DsImages.ClassPrivate;
			case Glyph.ClassInternal:			return DsImages.ClassInternal;
			case Glyph.CSharpFile:				return DsImages.CSFileNode;
			case Glyph.CSharpProject:			return DsImages.CSProjectNode;
			case Glyph.ConstantPublic:			return DsImages.ConstantPublic;
			case Glyph.ConstantProtected:		return DsImages.ConstantProtected;
			case Glyph.ConstantPrivate:			return DsImages.ConstantPrivate;
			case Glyph.ConstantInternal:		return DsImages.ConstantInternal;
			case Glyph.DelegatePublic:			return DsImages.DelegatePublic;
			case Glyph.DelegateProtected:		return DsImages.DelegateProtected;
			case Glyph.DelegatePrivate:			return DsImages.DelegatePrivate;
			case Glyph.DelegateInternal:		return DsImages.DelegateInternal;
			case Glyph.EnumPublic:				return DsImages.EnumerationPublic;
			case Glyph.EnumProtected:			return DsImages.EnumerationProtected;
			case Glyph.EnumPrivate:				return DsImages.EnumerationPrivate;
			case Glyph.EnumInternal:			return DsImages.EnumerationInternal;
			case Glyph.EnumMemberPublic:		return DsImages.EnumerationItemPublic;
			case Glyph.EnumMemberProtected:		return DsImages.EnumerationItemProtected;
			case Glyph.EnumMemberPrivate:		return DsImages.EnumerationItemPrivate;
			case Glyph.EnumMemberInternal:		return DsImages.EnumerationItemInternal;
			case Glyph.Error:					return DsImages.StatusError;
			case Glyph.StatusInformation:		return DsImages.StatusInformation;
			case Glyph.EventPublic:				return DsImages.EventPublic;
			case Glyph.EventProtected:			return DsImages.EventProtected;
			case Glyph.EventPrivate:			return DsImages.EventPrivate;
			case Glyph.EventInternal:			return DsImages.EventInternal;
			case Glyph.ExtensionMethodPublic:	return DsImages.ExtensionMethod;
			case Glyph.ExtensionMethodProtected:return DsImages.ExtensionMethod;
			case Glyph.ExtensionMethodPrivate:	return DsImages.ExtensionMethod;
			case Glyph.ExtensionMethodInternal:	return DsImages.ExtensionMethod;
			case Glyph.FieldPublic:				return DsImages.FieldPublic;
			case Glyph.FieldProtected:			return DsImages.FieldProtected;
			case Glyph.FieldPrivate:			return DsImages.FieldPrivate;
			case Glyph.FieldInternal:			return DsImages.FieldInternal;
			case Glyph.InterfacePublic:			return DsImages.InterfacePublic;
			case Glyph.InterfaceProtected:		return DsImages.InterfaceProtected;
			case Glyph.InterfacePrivate:		return DsImages.InterfacePrivate;
			case Glyph.InterfaceInternal:		return DsImages.InterfaceInternal;
			case Glyph.Intrinsic:				return DsImages.Type;
			case Glyph.Keyword:					return DsImages.IntellisenseKeyword;
			case Glyph.Label:					return DsImages.Label;
			case Glyph.Local:					return DsImages.LocalVariable;
			case Glyph.Namespace:				return DsImages.Namespace;
			case Glyph.MethodPublic:			return DsImages.MethodPublic;
			case Glyph.MethodProtected:			return DsImages.MethodProtected;
			case Glyph.MethodPrivate:			return DsImages.MethodPrivate;
			case Glyph.MethodInternal:			return DsImages.MethodInternal;
			case Glyph.ModulePublic:			return DsImages.ModulePublic;
			case Glyph.ModuleProtected:			return DsImages.ModuleProtected;
			case Glyph.ModulePrivate:			return DsImages.ModulePrivate;
			case Glyph.ModuleInternal:			return DsImages.ModuleInternal;
			case Glyph.OpenFolder:				return DsImages.FolderOpened;
			case Glyph.Operator:				return DsImages.OperatorPublic;
			case Glyph.Parameter:				return DsImages.LocalVariable;// Same image as Local, just like what VS does
			case Glyph.PropertyPublic:			return DsImages.Property;
			case Glyph.PropertyProtected:		return DsImages.PropertyProtected;
			case Glyph.PropertyPrivate:			return DsImages.PropertyPrivate;
			case Glyph.PropertyInternal:		return DsImages.PropertyInternal;
			case Glyph.RangeVariable:			return DsImages.LocalVariable;
			case Glyph.Reference:				return DsImages.Reference;
			case Glyph.StructurePublic:			return DsImages.StructurePublic;
			case Glyph.StructureProtected:		return DsImages.StructureProtected;
			case Glyph.StructurePrivate:		return DsImages.StructurePrivate;
			case Glyph.StructureInternal:		return DsImages.StructureInternal;
			case Glyph.TypeParameter:			return DsImages.Type;
			case Glyph.Snippet:					return DsImages.Snippet;
			case Glyph.CompletionWarning:		return DsImages.StatusWarning;
			case Glyph.AddReference:			return DsImages.AddReference;
			case Glyph.NuGet:					return DsImages.NuGet;
			default:
				Debug.Fail($"Unknown {nameof(Glyph)}: {glyph}");
				return null;
			}
		}
	}
}
