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

namespace dnSpy.Debugger.UI {
	static class ContentTypeDefinitions {
#pragma warning disable CS0169
		[Export]
		[Name(ContentTypes.CodeBreakpointsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? CodeBreakpointsWindow;

		[Export]
		[Name(ContentTypes.CodeBreakpointsWindowLabels)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? CodeBreakpointsWindowLabels;

		[Export]
		[Name(ContentTypes.ModuleBreakpointsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ModuleBreakpointsWindow;

		[Export]
		[Name(ContentTypes.ModuleBreakpointsWindowModuleName)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ModuleBreakpointsWindowModuleName;

		[Export]
		[Name(ContentTypes.ModuleBreakpointsWindowOrder)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ModuleBreakpointsWindowOrder;

		[Export]
		[Name(ContentTypes.ModuleBreakpointsWindowAppDomainName)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ModuleBreakpointsWindowAppDomainName;

		[Export]
		[Name(ContentTypes.ModuleBreakpointsWindowProcessName)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ModuleBreakpointsWindowProcessName;

		[Export]
		[Name(ContentTypes.CallStackWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? CallStackWindow;

		[Export]
		[Name(ContentTypes.AttachToProcessWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? AttachToProcessWindow;

		[Export]
		[Name(ContentTypes.ExceptionSettingsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ExceptionSettingsWindow;

		[Export]
		[Name(ContentTypes.VariablesWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? VariablesWindow;

		[Export]
		[Name(ContentTypes.LocalsWindow)]
		[BaseDefinition(ContentTypes.VariablesWindow)]
		static readonly ContentTypeDefinition? LocalsWindow;

		[Export]
		[Name(ContentTypes.AutosWindow)]
		[BaseDefinition(ContentTypes.VariablesWindow)]
		static readonly ContentTypeDefinition? AutosWindow;

		[Export]
		[Name(ContentTypes.WatchWindow)]
		[BaseDefinition(ContentTypes.VariablesWindow)]
		static readonly ContentTypeDefinition? WatchWindow;

		[Export]
		[Name(ContentTypes.ModulesWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ModulesWindow;

		[Export]
		[Name(ContentTypes.ProcessesWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ProcessesWindow;

		[Export]
		[Name(ContentTypes.ThreadsWindow)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ThreadsWindow;

		[Export]
		[Name(ContentTypes.ThreadsWindowName)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition? ThreadsWindowName;
#pragma warning restore CS0169
	}
}
