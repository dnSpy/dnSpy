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

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Baml decompiler options
	/// </summary>
	public sealed class BamlDecompilerOptions {
		/// <summary>
		/// x:ClassModifier value string when type is internal
		/// </summary>
		public string InternalClassModifier { get; set; }

		/// <summary>
		/// Creates a new instance
		/// </summary>
		/// <param name="language">Language</param>
		/// <returns></returns>
		public static BamlDecompilerOptions Create(ILanguage language) {
			if (language.GenericGuid == LanguageConstants.LANGUAGE_VISUALBASIC)
				return CreateVB();
			return CreateCSharp();
		}

		/// <summary>
		/// Creates a new instance with C# values
		/// </summary>
		/// <returns></returns>
		public static BamlDecompilerOptions CreateCSharp() {
			return new BamlDecompilerOptions {
				InternalClassModifier = "internal",
			};
		}

		/// <summary>
		/// Creates a new instance with VB values
		/// </summary>
		/// <returns></returns>
		public static BamlDecompilerOptions CreateVB() {
			return new BamlDecompilerOptions {
				InternalClassModifier = "Friend",
			};
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public BamlDecompilerOptions() {
		}
	}
}
