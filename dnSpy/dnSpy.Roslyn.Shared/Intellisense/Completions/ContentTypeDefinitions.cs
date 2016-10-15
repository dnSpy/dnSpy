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
using dnSpy.Contracts.Text;
using dnSpy.Roslyn.Shared.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	static class ContentTypeDefinitions {
#pragma warning disable 0169
		[Export]
		[Name(RoslynContentTypes.CompletionItemTextRoslyn)]
		[BaseDefinition(ContentTypes.CompletionItemText)]
		static readonly ContentTypeDefinition CompletionItemTextRoslyn;

		[Export]
		[Name(RoslynContentTypes.CompletionDisplayTextRoslyn)]
		[BaseDefinition(ContentTypes.CompletionDisplayText)]
		[BaseDefinition(RoslynContentTypes.CompletionItemTextRoslyn)]
		static readonly ContentTypeDefinition CompletionDisplayTextRoslyn;

		[Export]
		[Name(RoslynContentTypes.CompletionSuffixRoslyn)]
		[BaseDefinition(ContentTypes.CompletionSuffix)]
		[BaseDefinition(RoslynContentTypes.CompletionItemTextRoslyn)]
		static readonly ContentTypeDefinition CompletionSuffixRoslyn;

		[Export]
		[Name(RoslynContentTypes.CompletionToolTipRoslyn)]
		[BaseDefinition(ContentTypes.CompletionToolTip)]
		[BaseDefinition(RoslynContentTypes.RoslynTaggedText)]
		static readonly ContentTypeDefinition CompletionToolTipRoslyn;
#pragma warning restore 0169
	}
}
