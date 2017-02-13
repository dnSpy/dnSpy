/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger {
	static class ContentTypeDefinitions {
#pragma warning disable 0169
		//[Export]
		[Name(ContentTypes.BreakpointsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition BreakpointsWindow;

		//[Export]
		[Name(ContentTypes.CallStackWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition CallStackWindow;

		//[Export]
		[Name(ContentTypes.AttachToProcessWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition AttachToProcessWindow;

		//[Export]
		[Name(ContentTypes.ExceptionSettingsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition ExceptionSettingsWindow;

		//[Export]
		[Name(ContentTypes.LocalsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition LocalsWindow;

		//[Export]
		[Name(ContentTypes.ModulesWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition ModulesWindow;

		//[Export]
		[Name(ContentTypes.ThreadsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition ThreadsWindow;
#pragma warning restore 0169
	}
}
