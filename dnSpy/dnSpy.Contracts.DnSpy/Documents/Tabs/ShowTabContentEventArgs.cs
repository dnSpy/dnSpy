/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Result
	/// </summary>
	public enum ShowTabContentResult {
		/// <summary>
		/// The content failed to be shown, eg. an exception occurred
		/// </summary>
		Failed,

		/// <summary>
		/// Content was shown
		/// </summary>
		ShowedContent,

		/// <summary>
		/// A <see cref="IReferenceHandler"/> handled it and no new content was shown
		/// </summary>
		ReferenceHandler,
	}

	/// <summary>
	/// Show tab content event args
	/// </summary>
	public sealed class ShowTabContentEventArgs : EventArgs {
		/// <summary>
		/// Gets the result
		/// </summary>
		public ShowTabContentResult Result { get; }

		/// <summary>
		/// true if the content was shown
		/// </summary>
		public bool Success => Result == ShowTabContentResult.ShowedContent;

		/// <summary>
		/// Set to true if the caret has been moved by a previous handler
		/// </summary>
		public bool HasMovedCaret { get; set; }

		/// <summary>
		/// Gets the tab
		/// </summary>
		public IDocumentTab Tab { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="result">Result></param>
		/// <param name="tab">Tab</param>
		public ShowTabContentEventArgs(ShowTabContentResult result, IDocumentTab tab) {
			Result = result;
			HasMovedCaret = false;
			Tab = tab;
		}
	}
}
