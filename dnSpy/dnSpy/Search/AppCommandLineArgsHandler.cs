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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.App;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Search {
	[Export(typeof(IAppCommandLineArgsHandler))]
	sealed class AppCommandLineArgsHandler : IAppCommandLineArgsHandler {
		readonly IDsToolWindowService toolWindowService;
		readonly Lazy<ISearchService> searchService;

		[ImportingConstructor]
		AppCommandLineArgsHandler(IDsToolWindowService toolWindowService, Lazy<ISearchService> searchService) {
			this.toolWindowService = toolWindowService;
			this.searchService = searchService;
		}

		public double Order => 1000;

		public void OnNewArgs(IAppCommandLineArgs args) {
			bool show = false;

			var loc = GetSearchLocation(args.SearchIn);
			var typ = GetSearchType(args.SearchFor);

			if (!(loc is null)) {
				show = true;
				searchService.Value.SearchLocation = loc.Value;
			}

			if (!(typ is null)) {
				show = true;
				searchService.Value.SearchType = typ.Value;
			}

			if (!(args.SearchText is null)) {
				show = true;
				searchService.Value.SearchText = args.SearchText;
			}

			if (show)
				toolWindowService.Show(SearchToolWindowContent.THE_GUID);
		}

		SearchLocation? GetSearchLocation(string searchLocation) {
			if (string.IsNullOrEmpty(searchLocation))
				return null;
			switch (searchLocation) {
			case "a":
			case "all":
			case "all-files":
				return SearchLocation.AllFiles;
			case "sel":
			case "selected":
			case "selected-files":
				return SearchLocation.SelectedFiles;
			case "dir":
			case "folder":
			case "same-dir":
			case "same-folder":
				return SearchLocation.AllFilesInSameDir;
			case "t":
			case "type":
			case "sel-type":
			case "selected-type":
				return SearchLocation.SelectedType;
			}
			Debug.Fail($"Unknown search loc: {searchLocation}");
			return null;
		}

		SearchType? GetSearchType(string searchType) {
			if (string.IsNullOrEmpty(searchType))
				return null;
			switch (searchType) {
			case "asm":
			case "assembly":
				return SearchType.AssemblyDef;
			case "mod":
			case "module":
				return SearchType.ModuleDef;
			case "ns":
			case "namespace":
				return SearchType.Namespace;
			case "t":
			case "type":
				return SearchType.TypeDef;
			case "f":
			case "field":
				return SearchType.FieldDef;
			case "m":
			case "method":
				return SearchType.MethodDef;
			case "p":
			case "prop":
			case "property":
				return SearchType.PropertyDef;
			case "e":
			case "evt":
			case "event":
				return SearchType.EventDef;
			case "pm":
			case "param":
				return SearchType.ParamDef;
			case "loc":
			case "local":
				return SearchType.Local;
			case "pl":
			case "pmloc":
			case "pm-loc":
			case "paramlocal":
			case "param-local":
				return SearchType.ParamLocal;
			case "ar":
			case "asmref":
			case "asm-ref":
			case "assembly-ref":
				return SearchType.AssemblyRef;
			case "mr":
			case "modref":
			case "mod-ref":
			case "module-ref":
				return SearchType.ModuleRef;
			case "r":
			case "res":
			case "rsrc":
			case "resource":
				return SearchType.Resource;
			case "generic":
				return SearchType.GenericTypeDef;
			case "non-generic":
				return SearchType.NonGenericTypeDef;
			case "enum":
				return SearchType.EnumTypeDef;
			case "if":
			case "iface":
			case "interface":
				return SearchType.InterfaceTypeDef;
			case "cl":
			case "class":
				return SearchType.ClassTypeDef;
			case "st":
			case "struct":
				return SearchType.StructTypeDef;
			case "del":
			case "delegate":
				return SearchType.DelegateTypeDef;
			case "member":
				return SearchType.Member;
			case "any":
				return SearchType.Any;
			case "lit":
			case "literal":
			case "const":
			case "constant":
				return SearchType.Literal;
			}
			Debug.Fail($"Unknown search type: {searchType}");
			return null;
		}
	}
}
