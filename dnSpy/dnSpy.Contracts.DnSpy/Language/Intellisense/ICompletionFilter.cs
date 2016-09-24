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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Filters <see cref="Completion"/>s
	/// </summary>
	public interface ICompletionFilter {
		/// <summary>
		/// Returns true if the search text matches this <see cref="Completion"/>
		/// </summary>
		/// <param name="completion">Completion</param>
		/// <returns></returns>
		bool IsMatch(Completion completion);

		/// <summary>
		/// Returns spans matching the search text
		/// </summary>
		/// <param name="completionText">Source text to match, eg. <see cref="Completion.DisplayText"/></param>
		/// <returns></returns>
		Span[] GetMatchSpans(string completionText);
	}
}
