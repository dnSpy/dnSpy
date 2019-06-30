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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Search {
	sealed class SearchResultContext {
		public bool SyntaxHighlight { get; set; }
		public IDecompiler Decompiler { get; set; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }

		public SearchResultContext(IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider, IDecompiler decompiler) {
			SyntaxHighlight = true;
			ClassificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
			TextElementProvider = textElementProvider ?? throw new ArgumentNullException(nameof(textElementProvider));
			Decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
		}
	}
}
