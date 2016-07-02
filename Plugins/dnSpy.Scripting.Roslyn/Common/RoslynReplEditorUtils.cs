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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Scripting.Roslyn.Common {
	static class RoslynReplEditorUtils {
		static readonly object Key = typeof(RoslynReplEditorUtils);

		/// <summary>
		/// Adds the <see cref="ScriptControlVM"/> instance to the <see cref="ITextView"/> properties
		/// </summary>
		/// <param name="vm">Script control</param>
		/// <param name="textView">REPL editor text view</param>
		public static void AddInstance(ScriptControlVM vm, ITextView textView) {
			if (vm == null)
				throw new ArgumentNullException(nameof(vm));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			textView.Properties.AddProperty(Key, vm);
		}

		/// <summary>
		/// Returns the <see cref="ScriptControlVM"/> instance if it has been added by <see cref="AddInstance(ScriptControlVM, ITextView)"/>
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		public static ScriptControlVM TryGetInstance(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			ScriptControlVM vm;
			textView.Properties.TryGetProperty(Key, out vm);
			return vm;
		}
	}
}
