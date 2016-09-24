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
using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	/// <summary>
	/// Creates <see cref="Completion"/> text UI elements
	/// </summary>
	interface ICompletionTextElementProvider : IDisposable {
		/// <summary>
		/// Creates a UI element of the text part (<see cref="Completion.DisplayText"/>) that is shown
		/// in the completion listbox.
		/// </summary>
		/// <param name="completionSet">Owner collection</param>
		/// <param name="completion">Completion</param>
		/// <returns></returns>
		FrameworkElement Create(CompletionSet completionSet, Completion completion);
	}
}
