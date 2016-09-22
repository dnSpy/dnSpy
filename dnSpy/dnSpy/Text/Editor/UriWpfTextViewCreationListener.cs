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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewCreationListener))]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[ContentType(ContentTypes.Text)]
	sealed class UriWpfTextViewCreationListener : IWpfTextViewCreationListener {
		static readonly object textViewKey = new object();

		public void TextViewCreated(IWpfTextView textView) {
			textView.TextBuffer.Properties.AddProperty(textViewKey, textView);
		}

		public static IWpfTextView TryGetTextView(ITextBuffer textBuffer) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			IWpfTextView wpfTextView;
			if (textBuffer.Properties.TryGetProperty(textViewKey, out wpfTextView))
				return wpfTextView;
			return null;
		}
	}
}
