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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	static class ContentTypeDefinitions {
#pragma warning disable CS0169
		[Export]
		[Name(ContentTypes.CompletionItemText)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? CompletionItemText;

		[Export]
		[Name(ContentTypes.CompletionDisplayText)]
		[BaseDefinition(ContentTypes.CompletionItemText)]
		static readonly ContentTypeDefinition? CompletionDisplayText;

		[Export]
		[Name(ContentTypes.CompletionSuffix)]
		[BaseDefinition(ContentTypes.CompletionItemText)]
		static readonly ContentTypeDefinition? CompletionSuffix;

		[Export]
		[Name(ContentTypes.CompletionToolTip)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? CompletionToolTip;
#pragma warning restore CS0169
	}
}
