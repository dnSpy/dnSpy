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
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Tagging.Xml {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.Xml)]
	sealed class XmlTaggerProvider : ITaggerProvider {
		readonly XmlTaggerClassificationTypes xmlTaggerClassificationTypes;

		[ImportingConstructor]
		XmlTaggerProvider(XmlTaggerClassificationTypes xmlTaggerClassificationTypes) => this.xmlTaggerClassificationTypes = xmlTaggerClassificationTypes;

		public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag => new XmlTagger(xmlTaggerClassificationTypes) as ITagger<T>;
	}

	sealed class XmlTagger : XmlTaggerBase {
		public XmlTagger(XmlTaggerClassificationTypes xmlTaggerClassificationTypes)
			: base(xmlTaggerClassificationTypes) {
		}
	}
}
