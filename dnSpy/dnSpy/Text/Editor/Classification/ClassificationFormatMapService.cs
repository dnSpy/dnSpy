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

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Classification;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Editor.Classification {
	[Export(typeof(IClassificationFormatMapService))]
	sealed class ClassificationFormatMapService : IClassificationFormatMapService {
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		ClassificationFormatMapService(IThemeManager themeManager) {
			this.themeManager = themeManager;
		}

		public IClassificationFormatMap GetClassificationFormatMap(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(typeof(ClassificationFormatMap), () => new ClassificationFormatMap(themeManager, textView));
		}

		public IClassificationFormatMap GetClassificationFormatMap(string category) {
			throw new System.NotImplementedException();//TODO:
		}
	}
}
