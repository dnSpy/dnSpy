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

using System.Diagnostics;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Shared.Themes {
	public static class ThemeUtils {
		public static ITextColor GetTextColor(ITheme theme, object data) =>
			GetColor(data).ToTextColor(theme);

		public static Color GetColor(object data) {
			if (data is ITextColor)
				return new Color((ITextColor)data);

			if (data is TextTokenKind)
				return new Color(((TextTokenKind)data).ToColorType());

			if (data is ColorType)
				return new Color((ColorType)data);

			if (data is OutputColor)
				return new Color(((OutputColor)data).ToTextTokenKind().ToColorType());

			Debug.Fail($"Unknown color: '{data}'");
			return new Color(ColorType.Error);
		}
	}
}
