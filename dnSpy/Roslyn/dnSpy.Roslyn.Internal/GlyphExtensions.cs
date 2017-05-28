/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using MSCA = Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Internal {
	static class GlyphExtensions {
		public static Glyph ToOurGlyph(this MSCA.Glyph glyph) {
			switch (glyph) {
			case MSCA.Glyph.Assembly: return Glyph.Assembly;
			case MSCA.Glyph.BasicFile: return Glyph.BasicFile;
			case MSCA.Glyph.BasicProject: return Glyph.BasicProject;
			case MSCA.Glyph.ClassPublic: return Glyph.ClassPublic;
			case MSCA.Glyph.ClassProtected: return Glyph.ClassProtected;
			case MSCA.Glyph.ClassPrivate: return Glyph.ClassPrivate;
			case MSCA.Glyph.ClassInternal: return Glyph.ClassInternal;
			case MSCA.Glyph.CSharpFile: return Glyph.CSharpFile;
			case MSCA.Glyph.CSharpProject: return Glyph.CSharpProject;
			case MSCA.Glyph.ConstantPublic: return Glyph.ConstantPublic;
			case MSCA.Glyph.ConstantProtected: return Glyph.ConstantProtected;
			case MSCA.Glyph.ConstantPrivate: return Glyph.ConstantPrivate;
			case MSCA.Glyph.ConstantInternal: return Glyph.ConstantInternal;
			case MSCA.Glyph.DelegatePublic: return Glyph.DelegatePublic;
			case MSCA.Glyph.DelegateProtected: return Glyph.DelegateProtected;
			case MSCA.Glyph.DelegatePrivate: return Glyph.DelegatePrivate;
			case MSCA.Glyph.DelegateInternal: return Glyph.DelegateInternal;
			case MSCA.Glyph.EnumPublic: return Glyph.EnumPublic;
			case MSCA.Glyph.EnumProtected: return Glyph.EnumProtected;
			case MSCA.Glyph.EnumPrivate: return Glyph.EnumPrivate;
			case MSCA.Glyph.EnumInternal: return Glyph.EnumInternal;
			case MSCA.Glyph.EnumMember: return Glyph.EnumMember;
			case MSCA.Glyph.Error: return Glyph.Error;
			case MSCA.Glyph.EventPublic: return Glyph.EventPublic;
			case MSCA.Glyph.EventProtected: return Glyph.EventProtected;
			case MSCA.Glyph.EventPrivate: return Glyph.EventPrivate;
			case MSCA.Glyph.EventInternal: return Glyph.EventInternal;
			case MSCA.Glyph.ExtensionMethodPublic: return Glyph.ExtensionMethodPublic;
			case MSCA.Glyph.ExtensionMethodProtected: return Glyph.ExtensionMethodProtected;
			case MSCA.Glyph.ExtensionMethodPrivate: return Glyph.ExtensionMethodPrivate;
			case MSCA.Glyph.ExtensionMethodInternal: return Glyph.ExtensionMethodInternal;
			case MSCA.Glyph.FieldPublic: return Glyph.FieldPublic;
			case MSCA.Glyph.FieldProtected: return Glyph.FieldProtected;
			case MSCA.Glyph.FieldPrivate: return Glyph.FieldPrivate;
			case MSCA.Glyph.FieldInternal: return Glyph.FieldInternal;
			case MSCA.Glyph.InterfacePublic: return Glyph.InterfacePublic;
			case MSCA.Glyph.InterfaceProtected: return Glyph.InterfaceProtected;
			case MSCA.Glyph.InterfacePrivate: return Glyph.InterfacePrivate;
			case MSCA.Glyph.InterfaceInternal: return Glyph.InterfaceInternal;
			case MSCA.Glyph.Intrinsic: return Glyph.Intrinsic;
			case MSCA.Glyph.Keyword: return Glyph.Keyword;
			case MSCA.Glyph.Label: return Glyph.Label;
			case MSCA.Glyph.Local: return Glyph.Local;
			case MSCA.Glyph.Namespace: return Glyph.Namespace;
			case MSCA.Glyph.MethodPublic: return Glyph.MethodPublic;
			case MSCA.Glyph.MethodProtected: return Glyph.MethodProtected;
			case MSCA.Glyph.MethodPrivate: return Glyph.MethodPrivate;
			case MSCA.Glyph.MethodInternal: return Glyph.MethodInternal;
			case MSCA.Glyph.ModulePublic: return Glyph.ModulePublic;
			case MSCA.Glyph.ModuleProtected: return Glyph.ModuleProtected;
			case MSCA.Glyph.ModulePrivate: return Glyph.ModulePrivate;
			case MSCA.Glyph.ModuleInternal: return Glyph.ModuleInternal;
			case MSCA.Glyph.OpenFolder: return Glyph.OpenFolder;
			case MSCA.Glyph.Operator: return Glyph.Operator;
			case MSCA.Glyph.Parameter: return Glyph.Parameter;
			case MSCA.Glyph.PropertyPublic: return Glyph.PropertyPublic;
			case MSCA.Glyph.PropertyProtected: return Glyph.PropertyProtected;
			case MSCA.Glyph.PropertyPrivate: return Glyph.PropertyPrivate;
			case MSCA.Glyph.PropertyInternal: return Glyph.PropertyInternal;
			case MSCA.Glyph.RangeVariable: return Glyph.RangeVariable;
			case MSCA.Glyph.Reference: return Glyph.Reference;
			case MSCA.Glyph.StructurePublic: return Glyph.StructurePublic;
			case MSCA.Glyph.StructureProtected: return Glyph.StructureProtected;
			case MSCA.Glyph.StructurePrivate: return Glyph.StructurePrivate;
			case MSCA.Glyph.StructureInternal: return Glyph.StructureInternal;
			case MSCA.Glyph.TypeParameter: return Glyph.TypeParameter;
			case MSCA.Glyph.Snippet: return Glyph.Snippet;
			case MSCA.Glyph.CompletionWarning: return Glyph.CompletionWarning;
			case MSCA.Glyph.AddReference: return Glyph.AddReference;
			case MSCA.Glyph.NuGet: return Glyph.NuGet;
			default:
				Debug.Fail($"New Glyph: {glyph}");
				return Glyph.Error;
			}
		}
	}
}
