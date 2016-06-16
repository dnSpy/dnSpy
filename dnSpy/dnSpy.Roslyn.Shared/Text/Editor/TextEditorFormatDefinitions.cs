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

using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Roslyn.Shared.Text.Editor {
	[ExportTextEditorFormatDefinition(RoslynAppearanceCategoryConstants.CodeEditor_CSharp, AppearanceCategoryConstants.CodeEditor)]
	sealed class CodeEditorCSharpTextEditorFormatDefinition : TextEditorFormatDefinition {
		public override string DisplayName => "Code Editor (C#)";
	}

	[ExportTextEditorFormatDefinition(RoslynAppearanceCategoryConstants.CodeEditor_VisualBasic, AppearanceCategoryConstants.CodeEditor)]
	sealed class CodeEditorVisualBasicTextEditorFormatDefinition : TextEditorFormatDefinition {
		public override string DisplayName => "Code Editor (Visual Basic)";
	}
}
