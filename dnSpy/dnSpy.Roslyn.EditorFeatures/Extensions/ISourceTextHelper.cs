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

// Some methods are declared in dnSpy.Roslyn.Shared but this asm can't reference them (and I don't
// want to create a new assembly with just one class in it that can be referenced by this asm and
// dnSpy.Roslyn.Shared). This gets initialized at startup.

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.EditorFeatures.Extensions {
	interface ISourceTextHelper {
		SourceTextContainer AsTextContainer(ITextBuffer buffer);
		ITextBuffer TryGetTextBuffer(SourceTextContainer textContainer);
		ITextSnapshot FindCorrespondingEditorTextSnapshot(SourceText text);
		SourceText AsText(ITextSnapshot textSnapshot);
	}

	static class SourceTextHelper {
		public static ISourceTextHelper Instance {
			get {
				if (sourceTextHelper == null)
					throw new InvalidOperationException($"{nameof(SourceTextHelper)}.{nameof(Instance)} hasn't been initialized yet");
				return sourceTextHelper;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (sourceTextHelper != null)
					throw new InvalidOperationException();
				sourceTextHelper = value;
			}
		}
		static ISourceTextHelper sourceTextHelper;
	}
}
