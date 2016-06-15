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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// <see cref="EditorFormatDefinition"/> priority constants
	/// </summary>
	public static class EditorFormatDefinitionPriority {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const double BeforeLow = Low - 100000;
		public const double Low = -1000000;
		public const double AfterLow = Low + 100000;

		public const double BeforeDefault = Default - 100000;
		public const double Default = 0;
		public const double AfterDefault = Default + 100000;

		public const double BeforeHigh = High - 100000;
		public const double High = 1000000;
		public const double AfterHigh = High + 100000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
