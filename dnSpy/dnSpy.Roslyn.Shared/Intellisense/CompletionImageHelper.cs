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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace dnSpy.Roslyn.Shared.Intellisense {
	static class CompletionImageHelper {
		static readonly Assembly imageAssembly = Assembly.Load("dnSpy");

		public static ImageReference? GetImageReference(ImmutableArray<string> tags) {
			var name = GetImageName(tags);
			return name == null ? (ImageReference?)null : new ImageReference(imageAssembly, name);
		}

		static string GetImageName(ImmutableArray<string> tags) {
			foreach (var tag in tags) {
				switch (tag) {
				case CompletionTags.Class:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "ClassProtected";
					case Access.Internal:	return "ClassInternal";
					case Access.Private:	return "ClassPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Class";
					}
				case CompletionTags.Constant:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "LiteralProtected";
					case Access.Internal:	return "LiteralInternal";
					case Access.Private:	return "LiteralPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Literal";
					}
				case CompletionTags.Delegate:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "DelegateProtected";
					case Access.Internal:	return "DelegateInternal";
					case Access.Private:	return "DelegatePrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Delegate";
					}
				case CompletionTags.Enum:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "EnumProtected";
					case Access.Internal:	return "EnumInternal";
					case Access.Private:	return "EnumPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Enum";
					}
				case CompletionTags.Event:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "EventProtected";
					case Access.Internal:	return "EventInternal";
					case Access.Private:	return "EventPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Event";
					}
				case CompletionTags.ExtensionMethod:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "ExtensionMethodProtected";
					case Access.Internal:	return "ExtensionMethodInternal";
					case Access.Private:	return "ExtensionMethodPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "ExtensionMethod";
					}
				case CompletionTags.Field:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "FieldProtected";
					case Access.Internal:	return "FieldInternal";
					case Access.Private:	return "FieldPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Field";
					}
				case CompletionTags.Interface:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "InterfaceProtected";
					case Access.Internal:	return "InterfaceInternal";
					case Access.Private:	return "InterfacePrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Interface";
					}
				case CompletionTags.Method:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "MethodProtected";
					case Access.Internal:	return "MethodInternal";
					case Access.Private:	return "MethodPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Method";
					}
				case CompletionTags.Module:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "ModuleProtected";
					case Access.Internal:	return "ModuleInternal";
					case Access.Private:	return "ModulePrivate";
					case Access.Public:
					case Access.None:
					default:
						return "AssemblyModule";
					}
				case CompletionTags.Operator:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "OperatorProtected";
					case Access.Internal:	return "OperatorInternal";
					case Access.Private:	return "OperatorPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Operator";
					}
				case CompletionTags.Property:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "PropertyProtected";
					case Access.Internal:	return "PropertyInternal";
					case Access.Private:	return "PropertyPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Property";
					}
				case CompletionTags.Structure:
					switch (GetAccess(tags)) {
					case Access.Protected:	return "StructProtected";
					case Access.Internal:	return "StructInternal";
					case Access.Private:	return "StructPrivate";
					case Access.Public:
					case Access.None:
					default:
						return "Struct";
					}

				case CompletionTags.File:
					switch (GetLanguage(tags)) {
					case Language.CSharp:		return "CSharpFile";
					case Language.VisualBasic:	return "VisualBasicFile";
					case Language.None:
					default:
						return null;
					}

				case CompletionTags.Project:
					switch (GetLanguage(tags)) {
					case Language.CSharp:		return "CSProjectNode";
					case Language.VisualBasic:	return "VBProjectNode";
					case Language.None:
					default:
						return null;
					}

				case CompletionTags.Assembly:	return "Assembly";
				case CompletionTags.Parameter:	return "Local";// Same as local
				case CompletionTags.RangeVariable:return "Local";
				case CompletionTags.Intrinsic:	return "Type";
				case CompletionTags.Keyword:	return "IntellisenseKeyword";
				case CompletionTags.Label:		return "Label";
				case CompletionTags.Local:		return "Local";
				case CompletionTags.Namespace:	return "Namespace";
				case CompletionTags.Folder:		return "FolderClosed";
				case CompletionTags.Reference:	return "AssemblyReference";
				case CompletionTags.TypeParameter:return "GenericParameter";
				case CompletionTags.Snippet:	return "Snippet";
				case CompletionTags.Error:		return "StatusError";
				case CompletionTags.Warning:	return "StatusWarning";
				case "StatusInformation":		return "StatusInformation";
				}
			}
			return null;
		}

		static Access GetAccess(ImmutableArray<string> tags) {
			if (tags.Contains(CompletionTags.Public))
				return Access.Public;
			if (tags.Contains(CompletionTags.Protected))
				return Access.Protected;
			if (tags.Contains(CompletionTags.Internal))
				return Access.Internal;
			if (tags.Contains(CompletionTags.Private))
				return Access.Private;
			return Access.None;
		}

		static Language GetLanguage(ImmutableArray<string> tags) {
			if (tags.Contains(LanguageNames.CSharp))
				return Language.CSharp;
			if (tags.Contains(LanguageNames.VisualBasic))
				return Language.VisualBasic;
			return Language.None;
		}

		enum Access {
			None,
			Public,
			Protected,
			Internal,
			Private,
		}

		enum Language {
			None,
			CSharp,
			VisualBasic,
		}
	}
}
