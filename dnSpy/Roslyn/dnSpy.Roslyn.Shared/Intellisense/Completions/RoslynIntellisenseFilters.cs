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

using System;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Roslyn.Shared.Properties;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	static class RoslynIntellisenseFilters {
		public static RoslynIntellisenseFilter[] CreateFilters(IImageMonikerService imageMonikerService) => new RoslynIntellisenseFilter[] {
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.LocalVariable, dnSpy_Roslyn_Shared_Resources.LocalsAndParametersToolTip, "L", CompletionTags.Local, CompletionTags.Parameter),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ConstantPublic, dnSpy_Roslyn_Shared_Resources.ConstantsToolTip, "O", CompletionTags.Constant),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.Property, dnSpy_Roslyn_Shared_Resources.PropertiesToolTip, "P", CompletionTags.Property),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.EventPublic, dnSpy_Roslyn_Shared_Resources.EventsToolTip, "V", CompletionTags.Event),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.FieldPublic, dnSpy_Roslyn_Shared_Resources.FieldsToolTip, "F", CompletionTags.Field),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.MethodPublic, dnSpy_Roslyn_Shared_Resources.MethodsToolTip, "M", CompletionTags.Method),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ExtensionMethod, dnSpy_Roslyn_Shared_Resources.ExtensionMethodsToolTip, "X", CompletionTags.ExtensionMethod),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.InterfacePublic, dnSpy_Roslyn_Shared_Resources.InterfacesToolTip, "I", CompletionTags.Interface),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ClassPublic, dnSpy_Roslyn_Shared_Resources.ClassesToolTip, "C", CompletionTags.Class),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.ModulePublic, dnSpy_Roslyn_Shared_Resources.ModulesToolTip, "U", CompletionTags.Module),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.StructurePublic, dnSpy_Roslyn_Shared_Resources.StructuresToolTip, "S", CompletionTags.Structure),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.EnumerationPublic, dnSpy_Roslyn_Shared_Resources.EnumsToolTip, "E", CompletionTags.Enum),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.DelegatePublic, dnSpy_Roslyn_Shared_Resources.DelegatesToolTip, "D", CompletionTags.Delegate),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.Namespace, dnSpy_Roslyn_Shared_Resources.NamespacesToolTip, "N", CompletionTags.Namespace),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.IntellisenseKeyword, dnSpy_Roslyn_Shared_Resources.KeywordsToolTip, "K", CompletionTags.Keyword),
			new RoslynIntellisenseFilter(imageMonikerService, DsImages.Snippet, dnSpy_Roslyn_Shared_Resources.SnippetsToolTip, "T", CompletionTags.Snippet),
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
