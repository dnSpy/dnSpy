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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging.Xml {
	[Export(typeof(XmlTaggerClassificationTypes))]
	sealed class XmlTaggerClassificationTypes : TaggerClassificationTypes {
		[ImportingConstructor]
		XmlTaggerClassificationTypes(IClassificationTypeRegistryService classificationTypeRegistryService) {
			Attribute = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlAttribute));
			AttributeQuotes = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlAttributeQuotes));
			AttributeValue = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlAttributeValue));
			CDataSection = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlCDataSection));
			Comment = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlComment));
			Delimiter = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDelimiter));
			Keyword = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlKeyword));
			Name = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlName));
			ProcessingInstruction = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlProcessingInstruction));
			Text = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlText));
		}
	}
}
