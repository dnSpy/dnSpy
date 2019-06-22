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

using System;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Roslyn.Properties;
using Microsoft.CodeAnalysis.Tags;

namespace dnSpy.Roslyn.Intellisense.Completions {
	static class RoslynIntellisenseFilters {
		public static RoslynIntellisenseFilter[] CreateFilters() => new RoslynIntellisenseFilter[] {
			new RoslynIntellisenseFilter(DsImages.LocalVariable, dnSpy_Roslyn_Resources.LocalsAndParametersToolTip, "L", WellKnownTags.Local, WellKnownTags.Parameter),
			new RoslynIntellisenseFilter(DsImages.ConstantPublic, dnSpy_Roslyn_Resources.ConstantsToolTip, "O", WellKnownTags.Constant),
			new RoslynIntellisenseFilter(DsImages.Property, dnSpy_Roslyn_Resources.PropertiesToolTip, "P", WellKnownTags.Property),
			new RoslynIntellisenseFilter(DsImages.EventPublic, dnSpy_Roslyn_Resources.EventsToolTip, "V", WellKnownTags.Event),
			new RoslynIntellisenseFilter(DsImages.FieldPublic, dnSpy_Roslyn_Resources.FieldsToolTip, "F", WellKnownTags.Field),
			new RoslynIntellisenseFilter(DsImages.MethodPublic, dnSpy_Roslyn_Resources.MethodsToolTip, "M", WellKnownTags.Method),
			new RoslynIntellisenseFilter(DsImages.ExtensionMethod, dnSpy_Roslyn_Resources.ExtensionMethodsToolTip, "X", WellKnownTags.ExtensionMethod),
			new RoslynIntellisenseFilter(DsImages.InterfacePublic, dnSpy_Roslyn_Resources.InterfacesToolTip, "I", WellKnownTags.Interface),
			new RoslynIntellisenseFilter(DsImages.ClassPublic, dnSpy_Roslyn_Resources.ClassesToolTip, "C", WellKnownTags.Class),
			new RoslynIntellisenseFilter(DsImages.ModulePublic, dnSpy_Roslyn_Resources.ModulesToolTip, "U", WellKnownTags.Module),
			new RoslynIntellisenseFilter(DsImages.StructurePublic, dnSpy_Roslyn_Resources.StructuresToolTip, "S", WellKnownTags.Structure),
			new RoslynIntellisenseFilter(DsImages.EnumerationPublic, dnSpy_Roslyn_Resources.EnumsToolTip, "E", WellKnownTags.Enum),
			new RoslynIntellisenseFilter(DsImages.DelegatePublic, dnSpy_Roslyn_Resources.DelegatesToolTip, "D", WellKnownTags.Delegate),
			new RoslynIntellisenseFilter(DsImages.Namespace, dnSpy_Roslyn_Resources.NamespacesToolTip, "N", WellKnownTags.Namespace),
			new RoslynIntellisenseFilter(DsImages.IntellisenseKeyword, dnSpy_Roslyn_Resources.KeywordsToolTip, "K", WellKnownTags.Keyword),
			new RoslynIntellisenseFilter(DsImages.Snippet, dnSpy_Roslyn_Resources.SnippetsToolTip, "T", WellKnownTags.Snippet),
		};
	}

	sealed class RoslynIntellisenseFilter : DsIntellisenseFilter {
		public string[] Tags { get; }

		public RoslynIntellisenseFilter(ImageReference imageReference, string toolTip, string accessKey, params string[] tags)
			: base(imageReference, toolTip, accessKey, false, true) {
			if (tags is null)
				throw new ArgumentNullException(nameof(tags));
			if (tags.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(tags));
			Tags = tags;
		}
	}
}
