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
using dnSpy.Languages.ILSpy.Core.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Languages.ILSpy {
	static class ContentTypeDefinitions {
#pragma warning disable 0169
		[Export]
		[Name(ContentTypesInternal.DECOMPILER_ILSPY)]
		[BaseDefinition(ContentTypes.DECOMPILED_CODE)]
		static readonly ContentTypeDefinition DecompilerILSpyContentTypeDefinition;

		[Export]
		[Name(ContentTypesInternal.CSHARP_ILSPY)]
		[BaseDefinition(ContentTypesInternal.DECOMPILER_ILSPY)]
		[BaseDefinition(ContentTypes.CSHARP)]
		static readonly ContentTypeDefinition CSharpILSpyContentTypeDefinition;

		[Export]
		[Name(ContentTypesInternal.VISUALBASIC_ILSPY)]
		[BaseDefinition(ContentTypesInternal.DECOMPILER_ILSPY)]
		[BaseDefinition(ContentTypes.VISUALBASIC)]
		static readonly ContentTypeDefinition VisualBasicILSpyContentTypeDefinition;

		[Export]
		[Name(ContentTypesInternal.IL_ILSPY)]
		[BaseDefinition(ContentTypesInternal.DECOMPILER_ILSPY)]
		[BaseDefinition(ContentTypes.IL)]
		static readonly ContentTypeDefinition ILILSpyContentTypeDefinition;

		[Export]
		[Name(ContentTypesInternal.ILAST_ILSPY)]
		[BaseDefinition(ContentTypesInternal.DECOMPILER_ILSPY)]
		static readonly ContentTypeDefinition ILAstILSpyContentTypeDefinition;
#pragma warning restore 0169
	}
}
