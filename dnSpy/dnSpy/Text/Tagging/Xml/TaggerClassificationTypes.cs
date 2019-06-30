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

using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging.Xml {
	abstract class TaggerClassificationTypes {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		public ClassificationTag Attribute { get; protected set; }
		public ClassificationTag AttributeQuotes { get; protected set; }
		public ClassificationTag AttributeValue { get; protected set; }
		// Optimization so the XAML tagger doesn't have to check every string for markup extension strings (attribute value strings beginning with '{')
		public ClassificationTag AttributeValueXaml { get; protected set; }
		public ClassificationTag CDataSection { get; protected set; }
		public ClassificationTag Comment { get; protected set; }
		public ClassificationTag Delimiter { get; protected set; }
		public ClassificationTag Keyword { get; protected set; }
		public ClassificationTag Name { get; protected set; }
		public ClassificationTag ProcessingInstruction { get; protected set; }
		public ClassificationTag Text { get; protected set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
	}
}
