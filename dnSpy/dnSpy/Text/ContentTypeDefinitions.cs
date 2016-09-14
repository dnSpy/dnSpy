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
#pragma warning disable 0169
		[Export]
		[Name(ContentTypes.Any)]
		static readonly ContentTypeDefinition AnyContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Inert)]
		[BaseDefinition(ContentTypes.Any)]
		static readonly ContentTypeDefinition InertContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Text)]
		[BaseDefinition(ContentTypes.Any)]
		static readonly ContentTypeDefinition TextContentTypeDefinition;

		[Export]
		[Name(ContentTypes.PlainText)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition PlainTextContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Xml)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition XmlContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Xaml)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition XamlContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Baml)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition BamlContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Intellisense)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition IntellisenseContentTypeDefinition;

		[Export]
		[Name(ContentTypes.SignatureHelp)]
		[BaseDefinition(ContentTypes.Intellisense)]
		static readonly ContentTypeDefinition SignatureHelpContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Code)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition CodeContentTypeDefinition;

		[Export]
		[Name(ContentTypes.CSharp)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition CSharpContentTypeDefinition;

		[Export]
		[Name(ContentTypes.VisualBasic)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition VisualBasicContentTypeDefinition;

		[Export]
		[Name(ContentTypes.IL)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition ILContentTypeDefinition;

		[Export]
		[Name(ContentTypes.RoslynCode)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition RoslynCodeContentTypeDefinition;

		[Export]
		[Name(ContentTypes.CSharpRoslyn)]
		[BaseDefinition(ContentTypes.CSharp)]
		[BaseDefinition(ContentTypes.RoslynCode)]
		static readonly ContentTypeDefinition CSharpRoslynContentTypeDefinition;

		[Export]
		[Name(ContentTypes.VisualBasicRoslyn)]
		[BaseDefinition(ContentTypes.VisualBasic)]
		[BaseDefinition(ContentTypes.RoslynCode)]
		static readonly ContentTypeDefinition VisualBasicRoslynContentTypeDefinition;

		[Export]
		[Name(ContentTypes.DecompiledCode)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition DecompiledCodeContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Repl)]
		[BaseDefinition(ContentTypes.Code)]
		static readonly ContentTypeDefinition ReplContentTypeDefinition;

		[Export]
		[Name(ContentTypes.Output)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition OutputContentTypeDefinition;

		[Export]
		[Name(ContentTypes.AboutDnSpy)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition AboutDnSpyContentTypeDefinition;
#pragma warning restore 0169
	}
}
