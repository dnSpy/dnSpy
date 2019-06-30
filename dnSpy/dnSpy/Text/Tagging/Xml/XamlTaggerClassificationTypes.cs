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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging.Xml {
	[Export(typeof(XamlTaggerClassificationTypes))]
	sealed class XamlTaggerClassificationTypes : TaggerClassificationTypes {
		public ClassificationTag MarkupExtensionClass { get; }
		public ClassificationTag MarkupExtensionParameterName { get; }
		public ClassificationTag MarkupExtensionParameterValue { get; }

		[ImportingConstructor]
		XamlTaggerClassificationTypes(IClassificationTypeRegistryService classificationTypeRegistryService) {
			Attribute = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlAttribute));
			AttributeQuotes = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlAttributeQuotes));
			AttributeValue = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlAttributeValue));
			AttributeValueXaml = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlAttributeValue));
			CDataSection = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlCDataSection));
			Comment = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlComment));
			Delimiter = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlDelimiter));
			Keyword = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlKeyword));
			MarkupExtensionClass = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlMarkupExtensionClass));
			MarkupExtensionParameterName = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlMarkupExtensionParameterName));
			MarkupExtensionParameterValue = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlMarkupExtensionParameterValue));
			Name = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlName));
			ProcessingInstruction = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlProcessingInstruction));
			Text = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XamlText));
		}
	}
}
