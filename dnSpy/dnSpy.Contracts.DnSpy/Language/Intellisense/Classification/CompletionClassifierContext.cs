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

namespace dnSpy.Contracts.Language.Intellisense.Classification {
	/// <summary>
	/// <see cref="ICompletionClassifier"/> context
	/// </summary>
	public sealed class CompletionClassifierContext {
		/// <summary>
		/// Gets the completion to classify
		/// </summary>
		public Completion Completion { get; }

		/// <summary>
		/// Gets the current user input text
		/// </summary>
		public string InputText { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="completion">Completion to classify</param>
		/// <param name="inputText">Current user input text</param>
		public CompletionClassifierContext(Completion completion, string inputText) {
			if (completion == null)
				throw new ArgumentNullException(nameof(completion));
			if (inputText == null)
				throw new ArgumentNullException(nameof(inputText));
			Completion = completion;
			InputText = inputText;
		}
	}
}
