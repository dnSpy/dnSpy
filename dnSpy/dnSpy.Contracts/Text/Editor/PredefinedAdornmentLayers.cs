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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Predefined adornment layers
	/// </summary>
	public static class PredefinedAdornmentLayers {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const string BraceCompletion = "9A229D96-53F4-4CD6-B4AD-979A15F26669";
		public const string CurrentLineHighlighter = "A3ED195F-5730-4660-BBB4-3A3EF1CDF4F0";
		public const string TextMarker = "0CE8935E-9B81-40BC-8F57-D296D2C7203A";
		public const string Selection = "4B8364EE-5907-4431-AA67-13FAD79C3988";
		public const string InterLine = "484C019A-14D4-4067-A628-BD9C60D9CE7F";
		public const string Squiggle = "4D03A40A-A65B-460A-9E81-D75251916EFE";
		public const string Text = "78E1A96E-7739-484B-8E41-4547C5C39D2A";
		public const string IntraText = "AEFF2895-81C9-433A-83A3-36B60DCE60D4";
		public const string VisibleWhitespace = "D31A4D58-93C9-41B1-B7D4-801B4C493405";
		public const string Caret = "C137716A-E25B-48B7-AD40-DDDD97D371AF";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
