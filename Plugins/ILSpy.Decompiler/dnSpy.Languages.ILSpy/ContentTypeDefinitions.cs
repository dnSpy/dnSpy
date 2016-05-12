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

namespace dnSpy.Languages.ILSpy {
	static class ContentTypeDefinitions {
#pragma warning disable CS0169
		[ExportContentTypeDefinition(ContentTypes.DECOMPILER_ILSPY)]
		[DisplayName("Decompiler - ILSpy")]
		[BaseContentType(ContentTypes.DECOMPILED_CODE)]
		static readonly ContentTypeDefinition DecompilerILSpyContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.CSHARP_ILSPY)]
		[DisplayName("C# - ILSpy")]
		[BaseContentType(ContentTypes.DECOMPILER_ILSPY)]
		[BaseContentType(ContentTypes.CSHARP)]
		static readonly ContentTypeDefinition CSharpILSpyContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.VISUALBASIC_ILSPY)]
		[DisplayName("Visual Basic - ILSpy")]
		[BaseContentType(ContentTypes.DECOMPILER_ILSPY)]
		[BaseContentType(ContentTypes.VISUALBASIC)]
		static readonly ContentTypeDefinition VisualBasicILSpyContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.IL_ILSPY)]
		[DisplayName("IL - ILSpy")]
		[BaseContentType(ContentTypes.DECOMPILER_ILSPY)]
		[BaseContentType(ContentTypes.IL)]
		static readonly ContentTypeDefinition ILILSpyContentTypeDefinition;

		[ExportContentTypeDefinition(ContentTypes.ILAST_ILSPY)]
		[DisplayName("ILAst - ILSpy")]
		[BaseContentType(ContentTypes.DECOMPILER_ILSPY)]
		static readonly ContentTypeDefinition ILAstILSpyContentTypeDefinition;
#pragma warning restore CS0169
	}
}
