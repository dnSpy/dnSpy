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

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Roslyn.Text.Tagging {
	sealed class RoslynTaggerAsyncState {
		public RoslynTaggerAsyncState() { }

		public bool IsValid => SyntaxRoot is not null && SemanticModel is not null && Workspace is not null;
		public bool IsInitialized { get; private set; }
		public SyntaxNode? SyntaxRoot { get; private set; }
		public SemanticModel? SemanticModel { get; private set; }
		public Workspace? Workspace { get; private set; }
		public List<ITagSpan<IClassificationTag>> TagsList { get; } = new List<ITagSpan<IClassificationTag>>();

		public void Initialize(SyntaxNode syntaxRoot, SemanticModel semanticModel, Workspace workspace) {
			if (IsInitialized)
				return;

			SyntaxRoot = syntaxRoot;
			SemanticModel = semanticModel;
			Workspace = workspace;
			IsInitialized = true;
		}
	}
}
