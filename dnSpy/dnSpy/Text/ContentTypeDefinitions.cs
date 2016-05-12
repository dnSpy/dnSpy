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

using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	static class ContentTypeDefinitions {
#pragma warning disable CS0169
		[ExportContentTypeDefinition(ContentTypes.ANY)]
		[DisplayName("any")]
		static readonly ContentTypeDefinition AnyContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.INERT)]
		[DisplayName("inert")]
		[BaseContentType(ContentTypes.ANY)]
		static readonly ContentTypeDefinition InertContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.TEXT)]
		[DisplayName("text")]
		[BaseContentType(ContentTypes.ANY)]
		static readonly ContentTypeDefinition TextContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.PLAIN_TEXT)]
		[DisplayName("plaintext")]
		[BaseContentType(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition PlainTextContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.XML)]
		[DisplayName("xml")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition XMLContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.XAML)]
		[DisplayName("XAML")]
		[BaseContentType(ContentTypes.CODE)]// It doesn't derive from XML in VS
		static readonly ContentTypeDefinition XAMLContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.BAML)]
		[DisplayName("BAML")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition BAMLContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.CODE)]
		[DisplayName("Code")]
		[BaseContentType(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition CodeContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.CSHARP)]
		[DisplayName("C#")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition CSharpContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.VISUALBASIC)]
		[DisplayName("Visual Basic")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition VisualBasicContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.IL)]
		[DisplayName("IL")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition ILContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.ROSLYN_CODE)]
		[DisplayName("Roslyn Code")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition RoslynCodeContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.CSHARP_ROSLYN)]
		[DisplayName("C# Roslyn")]
		[BaseContentType(ContentTypes.CSHARP)]
		[BaseContentType(ContentTypes.ROSLYN_CODE)]
		static readonly ContentTypeDefinition CSharpRoslynContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.VISUALBASIC_ROSLYN)]
		[DisplayName("Visual Basic Roslyn")]
		[BaseContentType(ContentTypes.VISUALBASIC)]
		[BaseContentType(ContentTypes.ROSLYN_CODE)]
		static readonly ContentTypeDefinition VisualBasicRoslynContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.DECOMPILED_CODE)]
		[DisplayName("Decompiled Code")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition DecompiledCodeContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.REPL)]
		[DisplayName("REPL")]
		[BaseContentType(ContentTypes.CODE)]
		static readonly ContentTypeDefinition ReplContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.OUTPUT)]
		[DisplayName("Output")]
		[BaseContentType(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition OutputContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.ABOUT_DNSPY)]
		[DisplayName("About - dnSpy")]
		[BaseContentType(ContentTypes.TEXT)]
		static readonly ContentTypeDefinition AboutDnSpyContentTypeDefinition;
#pragma warning restore CS0169
	}
}
