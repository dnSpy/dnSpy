/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Roslyn.Intellisense.Completions {
	static class RoslynIntellisenseFilters {
		public static RoslynIntellisenseFilter[] CreateFilters(IImageMonikerService imageMonikerService) => new RoslynIntellisenseFilter[] {
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.LocalVariable, dnSpy_Roslyn_Resources.LocalsAndParametersToolTip, "L", WellKnownTags.Local, WellKnownTags.Parameter),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ConstantPublic, dnSpy_Roslyn_Resources.ConstantsToolTip, "O", WellKnownTags.Constant),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.Property, dnSpy_Roslyn_Resources.PropertiesToolTip, "P", WellKnownTags.Property),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.EventPublic, dnSpy_Roslyn_Resources.EventsToolTip, "V", WellKnownTags.Event),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.FieldPublic, dnSpy_Roslyn_Resources.FieldsToolTip, "F", WellKnownTags.Field),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.MethodPublic, dnSpy_Roslyn_Resources.MethodsToolTip, "M", WellKnownTags.Method),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ExtensionMethod, dnSpy_Roslyn_Resources.ExtensionMethodsToolTip, "X", WellKnownTags.ExtensionMethod),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.InterfacePublic, dnSpy_Roslyn_Resources.InterfacesToolTip, "I", WellKnownTags.Interface),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ClassPublic, dnSpy_Roslyn_Resources.ClassesToolTip, "C", WellKnownTags.Class),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ModulePublic, dnSpy_Roslyn_Resources.ModulesToolTip, "U", WellKnownTags.Module),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.StructurePublic, dnSpy_Roslyn_Resources.StructuresToolTip, "S", WellKnownTags.Structure),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.EnumerationPublic, dnSpy_Roslyn_Resources.EnumsToolTip, "E", WellKnownTags.Enum),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.DelegatePublic, dnSpy_Roslyn_Resources.DelegatesToolTip, "D", WellKnownTags.Delegate),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.Namespace, dnSpy_Roslyn_Resources.NamespacesToolTip, "N", WellKnownTags.Namespace),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.IntellisenseKeyword, dnSpy_Roslyn_Resources.KeywordsToolTip, "K", WellKnownTags.Keyword),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.Snippet, dnSpy_Roslyn_Resources.SnippetsToolTip, "T", WellKnownTags.Snippet),
		};
	}

	sealed class RoslynIntellisenseFilter : IntellisenseFilter {
		public string[] Tags { get; }

		public RoslynIntellisenseFilter(IImageMonikerService imageMonikerService, ImageReference imageReference, string toolTip, string accessKey, params string[] tags)
			: base(imageMonikerService.ToImageMoniker(imageReference), toolTip, accessKey, automationText: null, initialIsChecked: false, initialIsEnabled: true) {
			if (tags == null)
				throw new ArgumentNullException(nameof(tags));
			if (tags.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(tags));
			Tags = tags;
		}
	}
}
