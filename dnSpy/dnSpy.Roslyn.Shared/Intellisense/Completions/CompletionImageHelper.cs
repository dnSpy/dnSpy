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

using System.Collections.Immutable;
using dnSpy.Contracts.Images;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	static class CompletionImageHelper {
		public static ImageReference? GetImageReference(ImmutableArray<string> tags) {
			switch (tags.ToCompletionKind()) {
			case CompletionKind.Unknown:				return null;
			case CompletionKind.ClassProtected:			return DsImages.ClassProtected;
			case CompletionKind.ClassInternal:			return DsImages.ClassInternal;
			case CompletionKind.ClassPrivate:			return DsImages.ClassPrivate;
			case CompletionKind.Class:					return DsImages.ClassPublic;
			case CompletionKind.ConstantProtected:		return DsImages.ConstantProtected;
			case CompletionKind.ConstantInternal:		return DsImages.ConstantInternal;
			case CompletionKind.ConstantPrivate:		return DsImages.ConstantPrivate;
			case CompletionKind.Constant:				return DsImages.ConstantPublic;
			case CompletionKind.DelegateProtected:		return DsImages.DelegateProtected;
			case CompletionKind.DelegateInternal:		return DsImages.DelegateInternal;
			case CompletionKind.DelegatePrivate:		return DsImages.DelegatePrivate;
			case CompletionKind.Delegate:				return DsImages.DelegatePublic;
			case CompletionKind.EnumProtected:			return DsImages.EnumerationProtected;
			case CompletionKind.EnumInternal:			return DsImages.EnumerationInternal;
			case CompletionKind.EnumPrivate:			return DsImages.EnumerationPrivate;
			case CompletionKind.Enum:					return DsImages.EnumerationPublic;
			case CompletionKind.EventProtected:			return DsImages.EventProtected;
			case CompletionKind.EventInternal:			return DsImages.EventInternal;
			case CompletionKind.EventPrivate:			return DsImages.EventPrivate;
			case CompletionKind.Event:					return DsImages.EventPublic;
			case CompletionKind.ExtensionMethodProtected:return DsImages.ExtensionMethod;
			case CompletionKind.ExtensionMethodInternal:return DsImages.ExtensionMethod;
			case CompletionKind.ExtensionMethodPrivate: return DsImages.ExtensionMethod;
			case CompletionKind.ExtensionMethod:		return DsImages.ExtensionMethod;
			case CompletionKind.FieldProtected:			return DsImages.FieldProtected;
			case CompletionKind.FieldInternal:			return DsImages.FieldInternal;
			case CompletionKind.FieldPrivate:			return DsImages.FieldPrivate;
			case CompletionKind.Field:					return DsImages.FieldPublic;
			case CompletionKind.InterfaceProtected:		return DsImages.InterfaceProtected;
			case CompletionKind.InterfaceInternal:		return DsImages.InterfaceInternal;
			case CompletionKind.InterfacePrivate:		return DsImages.InterfacePrivate;
			case CompletionKind.Interface:				return DsImages.InterfacePublic;
			case CompletionKind.MethodProtected:		return DsImages.MethodProtected;
			case CompletionKind.MethodInternal:			return DsImages.MethodInternal;
			case CompletionKind.MethodPrivate:			return DsImages.MethodPrivate;
			case CompletionKind.Method:					return DsImages.MethodPublic;
			case CompletionKind.ModuleProtected:		return DsImages.ModuleProtected;
			case CompletionKind.ModuleInternal:			return DsImages.ModuleInternal;
			case CompletionKind.ModulePrivate:			return DsImages.ModulePrivate;
			case CompletionKind.Module:					return DsImages.ModulePublic;
			case CompletionKind.OperatorProtected:		return DsImages.OperatorProtected;
			case CompletionKind.OperatorInternal:		return DsImages.OperatorInternal;
			case CompletionKind.OperatorPrivate:		return DsImages.OperatorPrivate;
			case CompletionKind.Operator:				return DsImages.OperatorPublic;
			case CompletionKind.PropertyProtected:		return DsImages.PropertyProtected;
			case CompletionKind.PropertyInternal:		return DsImages.PropertyInternal;
			case CompletionKind.PropertyPrivate:		return DsImages.PropertyPrivate;
			case CompletionKind.Property:				return DsImages.Property;
			case CompletionKind.StructureProtected:		return DsImages.StructureProtected;
			case CompletionKind.StructureInternal:		return DsImages.StructureInternal;
			case CompletionKind.StructurePrivate:		return DsImages.StructurePrivate;
			case CompletionKind.Structure:				return DsImages.StructurePublic;
			case CompletionKind.FileCSharp:				return DsImages.CSFileNode;
			case CompletionKind.FileVisualBasic:		return DsImages.VBFileNode;
			case CompletionKind.ProjectCSharp:			return DsImages.CSProjectNode;
			case CompletionKind.ProjectVisualBasic:		return DsImages.VBProjectNode;
			case CompletionKind.EnumMember:				return DsImages.EnumerationItemPublic;
			case CompletionKind.Assembly:				return DsImages.Assembly;
			case CompletionKind.RangeVariable:			return DsImages.LocalVariable;
			case CompletionKind.Local:					return DsImages.LocalVariable;
			case CompletionKind.Parameter:				return DsImages.LocalVariable;// Same image as Local, just like what VS does
			case CompletionKind.Intrinsic:				return DsImages.Type;
			case CompletionKind.Keyword:				return DsImages.IntellisenseKeyword;
			case CompletionKind.Label:					return DsImages.Label;
			case CompletionKind.Namespace:				return DsImages.Namespace;
			case CompletionKind.Folder:					return DsImages.FolderOpened;
			case CompletionKind.Reference:				return DsImages.Reference;
			case CompletionKind.TypeParameter:			return DsImages.Type;
			case CompletionKind.Snippet:				return DsImages.Snippet;
			case CompletionKind.StatusError:			return DsImages.StatusError;
			case CompletionKind.StatusWarning:			return DsImages.StatusWarning;
			case CompletionKind.StatusInformation:		return DsImages.StatusInformation;
			default: return null;
			}
		}
	}
}
