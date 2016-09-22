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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Completion session
	/// </summary>
	interface ICompletionSession : IIntellisenseSession {
		/// <summary>
		/// Adds the selected <see cref="Completion"/> to the text buffer
		/// </summary>
		void Commit();

		/// <summary>
		/// Raised when it's been committed
		/// </summary>
		event EventHandler Committed;

		/// <summary>
		/// true if <see cref="IIntellisenseSession.Start"/> has been called
		/// </summary>
		bool IsStarted { get; }

		/// <summary>
		/// Filters the completion list and should be called whenever the user has modified the document
		/// </summary>
		void Filter();

		/// <summary>
		/// Gets the completion collections
		/// </summary>
		ReadOnlyObservableCollection<CompletionCollection> CompletionSets { get; }

		/// <summary>
		/// Gets/sets the selected <see cref="CompletionCollection"/>
		/// </summary>
		CompletionCollection SelectedCompletionSet { get; set; }

		/// <summary>
		/// Raised when <see cref="SelectedCompletionSet"/> is changed
		/// </summary>
		event EventHandler<ValueChangedEventArgs<CompletionCollection>> SelectedCompletionSetChanged;
	}
}
