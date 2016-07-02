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
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	static class ContentTypeDefinitions {
#pragma warning disable CS0169
		[Export]
		[Name(ContentTypes.ANY)]
		static readonly ContentTypeDefinition AnyContentTypeDefinition;

		[Export]
		[Name(ContentTypes.INERT)]
		[BaseDefinition(ContentTypes.ANY)]
		static readonly ContentTypeDefinition InertContentTypeDefinition;

		[Export]
		[Name(ContentTypes.TEXT)]
		[BaseDefinition(ContentTypes.ANY)]
		static readonly ContentTypeDefinition TextContentTypeDefinition;

		[Export]
		[Name(ContentTypes.PLAIN_TEXT)]
		[BaseDefinition(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition PlainTextContentTypeDefinition;

		[Export]
		[Name(ContentTypes.XML)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition XMLContentTypeDefinition;

		[Export]
		[Name(ContentTypes.XAML)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition XAMLContentTypeDefinition;

		[Export]
		[Name(ContentTypes.BAML)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition BAMLContentTypeDefinition;

		[Export]
		[Name(ContentTypes.CODE)]
		[BaseDefinition(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition CodeContentTypeDefinition;

		[Export]
		[Name(ContentTypes.CSHARP)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition CSharpContentTypeDefinition;

		[Export]
		[Name(ContentTypes.VISUALBASIC)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition VisualBasicContentTypeDefinition;

		[Export]
		[Name(ContentTypes.IL)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition ILContentTypeDefinition;

		[Export]
		[Name(ContentTypes.ROSLYN_CODE)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition RoslynCodeContentTypeDefinition;

		[Export]
		[Name(ContentTypes.CSHARP_ROSLYN)]
		[BaseDefinition(ContentTypes.CSHARP)]
		[BaseDefinition(ContentTypes.ROSLYN_CODE)]
		static readonly ContentTypeDefinition CSharpRoslynContentTypeDefinition;

		[Export]
		[Name(ContentTypes.VISUALBASIC_ROSLYN)]
		[BaseDefinition(ContentTypes.VISUALBASIC)]
		[BaseDefinition(ContentTypes.ROSLYN_CODE)]
		static readonly ContentTypeDefinition VisualBasicRoslynContentTypeDefinition;

		[Export]
		[Name(ContentTypes.DECOMPILED_CODE)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition DecompiledCodeContentTypeDefinition;

		[Export]
		[Name(ContentTypes.REPL)]
		[BaseDefinition(ContentTypes.CODE)]
		static readonly ContentTypeDefinition ReplContentTypeDefinition;

		[Export]
		[Name(ContentTypes.OUTPUT)]
		[BaseDefinition(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition OutputContentTypeDefinition;

		[Export]
		[Name(ContentTypes.ABOUT_DNSPY)]
		[BaseDefinition(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition AboutDnSpyContentTypeDefinition;
#pragma warning restore CS0169
	}
}
