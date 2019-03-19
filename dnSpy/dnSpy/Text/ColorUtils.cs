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

using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text {
	static class ColorUtils {
		public static IClassificationType GetClassificationType(IClassificationTypeRegistryService classificationTypeRegistryService, IThemeClassificationTypeService themeClassificationTypeService, object color) {
			if (color is TextColor)
				return themeClassificationTypeService.GetClassificationType((TextColor)color);
			if (color is IClassificationType ct)
				return ct;
			if (color is string classificationTypeName)
				return classificationTypeRegistryService.GetClassificationType(classificationTypeName) ?? themeClassificationTypeService.GetClassificationType(TextColor.Text);
			return themeClassificationTypeService.GetClassificationType(TextColor.Text);
		}
	}
}
