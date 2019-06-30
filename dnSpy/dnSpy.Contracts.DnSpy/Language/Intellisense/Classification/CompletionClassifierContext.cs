/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Contracts.Language.Intellisense.Classification {
	/// <summary>
	/// Completion classifier context
	/// </summary>
	public abstract class CompletionClassifierContext : TextClassifierContext {
		/// <summary>
		/// Context kind
		/// </summary>
		public abstract CompletionClassifierKind Kind { get; }

		/// <summary>
		/// Gets the collection
		/// </summary>
		public CompletionSet CompletionSet { get; }

		/// <summary>
		/// Gets the completion to classify
		/// </summary>
		public Completion Completion { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="completionSet">Completion set</param>
		/// <param name="completion">Completion to classify</param>
		/// <param name="text">Text to classify</param>
		/// <param name="colorize">true if it should be colorized</param>
		protected CompletionClassifierContext(CompletionSet completionSet, Completion completion, string text, bool colorize)
			: base(text, string.Empty, colorize) {
			CompletionSet = completionSet ?? throw new ArgumentNullException(nameof(completionSet));
			Completion = completion ?? throw new ArgumentNullException(nameof(completion));
		}
	}
}
