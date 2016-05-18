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
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Creates <see cref="ILanguageCompiler"/> instances
	/// </summary>
	public interface ILanguageCompilerCreator {
		/// <summary>
		/// Order of this creator
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Gets the icon shown in menus or null
		/// </summary>
		ImageReference? Icon { get; }

		/// <summary>
		/// Language it supports, eg. <see cref="dnSpy.Contracts.Languages.LanguageConstants.LANGUAGE_CSHARP"/>.
		/// This property is compared against <see cref="dnSpy.Contracts.Languages.ILanguage.GenericGuid"/>.
		/// </summary>
		Guid Language { get; }

		/// <summary>
		/// Creates a new <see cref="ILanguageCompiler"/> instance
		/// </summary>
		/// <returns></returns>
		ILanguageCompiler Create();
	}
}
