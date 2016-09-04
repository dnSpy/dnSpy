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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace dnSpy.Roslyn.Shared.Intellisense {
	enum CompletionKind {
		Unknown,
		ClassProtected,
		ClassInternal,
		ClassPrivate,
		Class,
		ConstantProtected,
		ConstantInternal,
		ConstantPrivate,
		Constant,
		DelegateProtected,
		DelegateInternal,
		DelegatePrivate,
		Delegate,
		EnumProtected,
		EnumInternal,
		EnumPrivate,
		Enum,
		EventProtected,
		EventInternal,
		EventPrivate,
		Event,
		ExtensionMethodProtected,
		ExtensionMethodInternal,
		ExtensionMethodPrivate,
		ExtensionMethod,
		FieldProtected,
		FieldInternal,
		FieldPrivate,
		Field,
		InterfaceProtected,
		InterfaceInternal,
		InterfacePrivate,
		Interface,
		MethodProtected,
		MethodInternal,
		MethodPrivate,
		Method,
		ModuleProtected,
		ModuleInternal,
		ModulePrivate,
		Module,
		OperatorProtected,
		OperatorInternal,
		OperatorPrivate,
		Operator,
		PropertyProtected,
		PropertyInternal,
		PropertyPrivate,
		Property,
		StructureProtected,
		StructureInternal,
		StructurePrivate,
		Structure,
		FileCSharp,
		FileVisualBasic,
		ProjectCSharp,
		ProjectVisualBasic,
		EnumMember,
		Assembly,
		RangeVariable,
		Local,
		Parameter,
		Intrinsic,
		Keyword,
		Label,
		Namespace,
		Folder,
		Reference,
		TypeParameter,
		Snippet,
		StatusError,
		StatusWarning,
		StatusInformation,
	}

	static class CompletionKindUtilities {
		public static CompletionKind ToCompletionKind(this ImmutableArray<string> tags) {
			foreach (var tag in tags) {
				switch (tag) {
				case CompletionTags.Class:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.ClassProtected;
					case Access.Internal:	return CompletionKind.ClassInternal;
					case Access.Private:	return CompletionKind.ClassPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Class;
					}

				case CompletionTags.Constant:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.ConstantProtected;
					case Access.Internal:	return CompletionKind.ConstantInternal;
					case Access.Private:	return CompletionKind.ConstantPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Constant;
					}

				case CompletionTags.Delegate:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.DelegateProtected;
					case Access.Internal:	return CompletionKind.DelegateInternal;
					case Access.Private:	return CompletionKind.DelegatePrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Delegate;
					}

				case CompletionTags.Enum:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.EnumProtected;
					case Access.Internal:	return CompletionKind.EnumInternal;
					case Access.Private:	return CompletionKind.EnumPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Enum;
					}

				case CompletionTags.Event:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.EventProtected;
					case Access.Internal:	return CompletionKind.EventInternal;
					case Access.Private:	return CompletionKind.EventPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Event;
					}

				case CompletionTags.ExtensionMethod:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.ExtensionMethodProtected;
					case Access.Internal:	return CompletionKind.ExtensionMethodInternal;
					case Access.Private:	return CompletionKind.ExtensionMethodPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.ExtensionMethod;
					}

				case CompletionTags.Field:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.FieldProtected;
					case Access.Internal:	return CompletionKind.FieldInternal;
					case Access.Private:	return CompletionKind.FieldPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Field;
					}

				case CompletionTags.Interface:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.InterfaceProtected;
					case Access.Internal:	return CompletionKind.InterfaceInternal;
					case Access.Private:	return CompletionKind.InterfacePrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Interface;
					}

				case CompletionTags.Method:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.MethodProtected;
					case Access.Internal:	return CompletionKind.MethodInternal;
					case Access.Private:	return CompletionKind.MethodPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Method;
					}

				case CompletionTags.Module:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.ModuleProtected;
					case Access.Internal:	return CompletionKind.ModuleInternal;
					case Access.Private:	return CompletionKind.ModulePrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Module;
					}

				case CompletionTags.Operator:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.OperatorProtected;
					case Access.Internal:	return CompletionKind.OperatorInternal;
					case Access.Private:	return CompletionKind.OperatorPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Operator;
					}

				case CompletionTags.Property:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.PropertyProtected;
					case Access.Internal:	return CompletionKind.PropertyInternal;
					case Access.Private:	return CompletionKind.PropertyPrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Property;
					}

				case CompletionTags.Structure:
					switch (GetAccess(tags)) {
					case Access.Protected:	return CompletionKind.StructureProtected;
					case Access.Internal:	return CompletionKind.StructureInternal;
					case Access.Private:	return CompletionKind.StructurePrivate;
					case Access.Public:
					case Access.None:
					default:
						return CompletionKind.Structure;
					}

				case CompletionTags.File:
					switch (GetLanguage(tags)) {
					case Language.CSharp:		return CompletionKind.FileCSharp;
					case Language.VisualBasic:	return CompletionKind.FileVisualBasic;
					case Language.None:
					default:
						return CompletionKind.Unknown;
					}

				case CompletionTags.Project:
					switch (GetLanguage(tags)) {
					case Language.CSharp:		return CompletionKind.ProjectCSharp;
					case Language.VisualBasic:	return CompletionKind.ProjectVisualBasic;
					case Language.None:
					default:
						return CompletionKind.Unknown;
					}

				case CompletionTags.EnumMember:	return CompletionKind.EnumMember;
				case CompletionTags.Assembly:	return CompletionKind.Assembly;
				case CompletionTags.Parameter:	return CompletionKind.Parameter;
				case CompletionTags.RangeVariable:return CompletionKind.RangeVariable;
				case CompletionTags.Intrinsic:	return CompletionKind.Intrinsic;
				case CompletionTags.Keyword:	return CompletionKind.Keyword;
				case CompletionTags.Label:		return CompletionKind.Label;
				case CompletionTags.Local:		return CompletionKind.Local;
				case CompletionTags.Namespace:	return CompletionKind.Namespace;
				case CompletionTags.Folder:		return CompletionKind.Folder;
				case CompletionTags.Reference:	return CompletionKind.Reference;
				case CompletionTags.TypeParameter:return CompletionKind.TypeParameter;
				case CompletionTags.Snippet:	return CompletionKind.Snippet;
				case CompletionTags.Error:		return CompletionKind.StatusError;
				case CompletionTags.Warning:	return CompletionKind.StatusWarning;
				case "StatusInformation":		return CompletionKind.StatusInformation;
				}
			}
			return CompletionKind.Unknown;
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
