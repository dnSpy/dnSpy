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

namespace dnSpy.Contracts.Hex.Text {
	/// <summary>
	/// <see cref="HexClassifiedText"/> tags
	/// </summary>
	public static class PredefinedClassifiedTextTags {
		/// <summary>Normal text</summary>
		public const string Text = nameof(Text);
		/// <summary>Error</summary>
		public const string Error = nameof(Error);
		/// <summary>Keyword</summary>
		public const string Keyword = nameof(Keyword);
		/// <summary>Number</summary>
		public const string Number = nameof(Number);
		/// <summary>String</summary>
		public const string String = nameof(String);
		/// <summary>Char</summary>
		public const string Char = nameof(Char);
		/// <summary>Operator</summary>
		public const string Operator = nameof(Operator);
		/// <summary>Punctuation</summary>
		public const string Punctuation = nameof(Punctuation);
		/// <summary>Array name</summary>
		public const string ArrayName = nameof(ArrayName);
		/// <summary>Structure name</summary>
		public const string StructureName = nameof(StructureName);
		/// <summary>Value type</summary>
		public const string ValueType = nameof(ValueType);
		/// <summary>Enum type</summary>
		public const string Enum = nameof(Enum);
		/// <summary>Enum field</summary>
		public const string EnumField = nameof(EnumField);
		/// <summary>Field</summary>
		public const string Field = nameof(Field);
		/// <summary>Path name</summary>
		public const string PathName = nameof(PathName);
		/// <summary>Path separator</summary>
		public const string PathSeparator = nameof(PathSeparator);
		/// <summary>Filename without extension</summary>
		public const string Filename = nameof(Filename);
		/// <summary>Dot between filename and extension</summary>
		public const string FileDot = nameof(FileDot);
		/// <summary>File extension</summary>
		public const string FileExtension = nameof(FileExtension);
	}
}
