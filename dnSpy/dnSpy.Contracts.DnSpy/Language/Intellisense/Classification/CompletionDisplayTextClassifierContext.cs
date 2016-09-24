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
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Contracts.Language.Intellisense.Classification {
	/// <summary>
	/// Context needed to classify <see cref="Completion.DisplayText"/>
	/// </summary>
	public sealed class CompletionDisplayTextClassifierContext : CompletionClassifierContext {
		/// <summary>
		/// Returns <see cref="CompletionClassifierKind.DisplayText"/>
		/// </summary>
		public override CompletionClassifierKind Kind => CompletionClassifierKind.DisplayText;

		/// <summary>
		/// Gets the current user input text
		/// </summary>
		public string InputText { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="completionSet">Completion set</param>
		/// <param name="completion">Completion to classify</param>
		/// <param name="displayText">Text to classify</param>
		/// <param name="inputText">Current user input text</param>
		public CompletionDisplayTextClassifierContext(CompletionSet completionSet, Completion completion, string displayText, string inputText)
			: base(completionSet, completion, displayText) {
			if (inputText == null)
				throw new ArgumentNullException(nameof(inputText));
			InputText = inputText;
		}
	}
}
