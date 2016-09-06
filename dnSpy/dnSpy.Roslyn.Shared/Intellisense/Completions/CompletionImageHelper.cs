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
using System.Reflection;
using dnSpy.Contracts.Images;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	static class CompletionImageHelper {
		static readonly Assembly imageAssembly = Assembly.Load("dnSpy");

		public static ImageReference? GetImageReference(ImmutableArray<string> tags) {
			var name = GetImageName(tags);
			if (name == null)
				return null;
			return new ImageReference(imageAssembly, name);
		}

		static string GetImageName(ImmutableArray<string> tags) {
			switch (tags.ToCompletionKind()) {
			case CompletionKind.Unknown:				return null;
			case CompletionKind.ClassProtected:			return "ClassProtected";
			case CompletionKind.ClassInternal:			return "ClassInternal";
			case CompletionKind.ClassPrivate:			return "ClassPrivate";
			case CompletionKind.Class:					return "Class";
			case CompletionKind.ConstantProtected:		return "LiteralProtected";
			case CompletionKind.ConstantInternal:		return "LiteralInternal";
			case CompletionKind.ConstantPrivate:		return "LiteralPrivate";
			case CompletionKind.Constant:				return "Literal";
			case CompletionKind.DelegateProtected:		return "DelegateProtected";
			case CompletionKind.DelegateInternal:		return "DelegateInternal";
			case CompletionKind.DelegatePrivate:		return "DelegatePrivate";
			case CompletionKind.Delegate:				return "Delegate";
			case CompletionKind.EnumProtected:			return "EnumProtected";
			case CompletionKind.EnumInternal:			return "EnumInternal";
			case CompletionKind.EnumPrivate:			return "EnumPrivate";
			case CompletionKind.Enum:					return "Enum";
			case CompletionKind.EventProtected:			return "EventProtected";
			case CompletionKind.EventInternal:			return "EventInternal";
			case CompletionKind.EventPrivate:			return "EventPrivate";
			case CompletionKind.Event:					return "Event";
			case CompletionKind.ExtensionMethodProtected:return "ExtensionMethod";
			case CompletionKind.ExtensionMethodInternal:return "ExtensionMethod";
			case CompletionKind.ExtensionMethodPrivate: return "ExtensionMethod";
			case CompletionKind.ExtensionMethod:		return "ExtensionMethod";
			case CompletionKind.FieldProtected:			return "FieldProtected";
			case CompletionKind.FieldInternal:			return "FieldInternal";
			case CompletionKind.FieldPrivate:			return "FieldPrivate";
			case CompletionKind.Field:					return "Field";
			case CompletionKind.InterfaceProtected:		return "InterfaceProtected";
			case CompletionKind.InterfaceInternal:		return "InterfaceInternal";
			case CompletionKind.InterfacePrivate:		return "InterfacePrivate";
			case CompletionKind.Interface:				return "Interface";
			case CompletionKind.MethodProtected:		return "MethodProtected";
			case CompletionKind.MethodInternal:			return "MethodInternal";
			case CompletionKind.MethodPrivate:			return "MethodPrivate";
			case CompletionKind.Method:					return "Method";
			case CompletionKind.ModuleProtected:		return "ModuleProtected";
			case CompletionKind.ModuleInternal:			return "ModuleInternal";
			case CompletionKind.ModulePrivate:			return "ModulePrivate";
			case CompletionKind.Module:					return "Module";
			case CompletionKind.OperatorProtected:		return "OperatorProtected";
			case CompletionKind.OperatorInternal:		return "OperatorInternal";
			case CompletionKind.OperatorPrivate:		return "OperatorPrivate";
			case CompletionKind.Operator:				return "Operator";
			case CompletionKind.PropertyProtected:		return "PropertyProtected";
			case CompletionKind.PropertyInternal:		return "PropertyInternal";
			case CompletionKind.PropertyPrivate:		return "PropertyPrivate";
			case CompletionKind.Property:				return "Property";
			case CompletionKind.StructureProtected:		return "StructProtected";
			case CompletionKind.StructureInternal:		return "StructInternal";
			case CompletionKind.StructurePrivate:		return "StructPrivate";
			case CompletionKind.Structure:				return "Struct";
			case CompletionKind.FileCSharp:				return "CSharpFile";
			case CompletionKind.FileVisualBasic:		return "VisualBasicFile";
			case CompletionKind.ProjectCSharp:			return "CSProjectNode";
			case CompletionKind.ProjectVisualBasic:		return "VBProjectNode";
			case CompletionKind.EnumMember:				return "EnumValue";
			case CompletionKind.Assembly:				return "Assembly";
			case CompletionKind.RangeVariable:			return "Local";
			case CompletionKind.Local:					return "Local";
			case CompletionKind.Parameter:				return "Local";// Same image as Local, just like what VS does
			case CompletionKind.Intrinsic:				return "Type";
			case CompletionKind.Keyword:				return "IntellisenseKeyword";
			case CompletionKind.Label:					return "Label";
			case CompletionKind.Namespace:				return "Namespace";
			case CompletionKind.Folder:					return "FolderClosed";
			case CompletionKind.Reference:				return "AssemblyReference";
			case CompletionKind.TypeParameter:			return "GenericParameter";
			case CompletionKind.Snippet:				return "Snippet";
			case CompletionKind.StatusError:			return "StatusError";
			case CompletionKind.StatusWarning:			return "StatusWarning";
			case CompletionKind.StatusInformation:		return "StatusInformation";
			default: return null;
			}
		}
	}
}
