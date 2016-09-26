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

using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// <see cref="Completion"/> extensions
	/// </summary>
	static class CompletionExtensions {
		/// <summary>
		/// Gets the filter text or null. This is <see cref="IDsCompletion.FilterText"/> or <see cref="Completion.DisplayText"/>
		/// if <paramref name="completion"/> is not a <see cref="IDsCompletion"/>
		/// </summary>
		/// <param name="completion">Completion</param>
		/// <returns></returns>
		public static string TryGetFilterText(this Completion completion) => (completion as IDsCompletion)?.FilterText ?? completion.DisplayText;
	}
}
