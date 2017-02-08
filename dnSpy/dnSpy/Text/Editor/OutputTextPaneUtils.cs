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
using dnSpy.Contracts.Output;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	/// <summary>
	/// <see cref="IOutputTextPane"/> utils
	/// </summary>
	static class OutputTextPaneUtils {
		static readonly object Key = typeof(OutputTextPaneUtils);

		/// <summary>
		/// Adds the <see cref="IOutputTextPane"/> instance to the <see cref="ITextView"/> properties
		/// </summary>
		/// <param name="outputTextPane">Output text pane</param>
		/// <param name="textView">Log editor text view</param>
		public static void AddInstance(IOutputTextPane outputTextPane, ITextView textView) {
			if (outputTextPane == null)
				throw new ArgumentNullException(nameof(outputTextPane));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			textView.Properties.AddProperty(Key, outputTextPane);
		}

		/// <summary>
		/// Returns the <see cref="IOutputTextPane"/> instance if it has been added by <see cref="AddInstance(IOutputTextPane, ITextView)"/>
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		public static IOutputTextPane TryGetInstance(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			IOutputTextPane outputTextPane;
			textView.Properties.TryGetProperty(Key, out outputTextPane);
			return outputTextPane;
		}
	}
}
