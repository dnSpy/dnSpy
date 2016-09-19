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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Extension;
using dnSpy.Roslyn.EditorFeatures.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Shared.Text {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions)]
	sealed class FirstUseOptimizationLoader : IAutoLoaded {
		[ImportingConstructor]
		FirstUseOptimizationLoader() {
			SourceTextHelper.Instance = new SharedSourceTextHelper();
		}
	}

	sealed class SharedSourceTextHelper : ISourceTextHelper {
		SourceTextContainer ISourceTextHelper.AsTextContainer(ITextBuffer buffer) => buffer.AsTextContainer();
		ITextBuffer ISourceTextHelper.TryGetTextBuffer(SourceTextContainer textContainer) => textContainer.TryGetTextBuffer();
		ITextSnapshot ISourceTextHelper.FindCorrespondingEditorTextSnapshot(SourceText text) => text.TryGetTextSnapshot();
		SourceText ISourceTextHelper.AsText(ITextSnapshot textSnapshot) => textSnapshot.AsText();
	}
}
