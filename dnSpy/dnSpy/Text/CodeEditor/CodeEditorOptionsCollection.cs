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
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.CodeEditor {
	sealed class CodeEditorOptionsCollection {
		public IEnumerable<CodeEditorOptions> Options {
			get {
				foreach (var o in codeEditorOptions)
					yield return o;
			}
		}

		readonly CodeEditorOptions[] codeEditorOptions;

		public CodeEditorOptionsCollection(CodeEditorOptions[] codeEditorOptions) {
			if (codeEditorOptions == null)
				throw new ArgumentNullException(nameof(codeEditorOptions));
			this.codeEditorOptions = codeEditorOptions;
		}

		public CodeEditorOptions Find(Guid guid) {
			foreach (var options in codeEditorOptions) {
				if (options.Guid == guid)
					return options;
			}
			return null;
		}

		public CodeEditorOptions Find(IContentType contentType) {
			foreach (var options in codeEditorOptions) {
				if (options.ContentType == contentType)
					return options;
			}
			return null;
		}
	}
}
