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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Text classifier tags
	/// </summary>
	public static class PredefinedTextClassifierTags {
		/// <summary>
		/// Method body editor
		/// </summary>
		public static readonly string MethodBodyEditor = nameof(MethodBodyEditor);

		/// <summary>
		/// List dialog column: Name
		/// </summary>
		public static readonly string DocListDialogName = "Name";

		/// <summary>
		/// List dialog column: Count
		/// </summary>
		public static readonly string DocListDialogCount = "DocumentCount";

		/// <summary>
		/// GAC dialog column: Name
		/// </summary>
		public static readonly string GacDialogName = "Name";

		/// <summary>
		/// GAC dialog column: Version
		/// </summary>
		public static readonly string GacDialogVersion = "Version";

		/// <summary>
		/// Windows dialog column: Name
		/// </summary>
		public static readonly string TabsDialogName = "Name";

		/// <summary>
		/// Windows dialog column: Module
		/// </summary>
		public static readonly string TabsDialogModule = "Module";

		/// <summary>
		/// Windows dialog column: Path
		/// </summary>
		public static readonly string TabsDialogPath = "Path";

		/// <summary>
		/// Bookmarks window column: Name
		/// </summary>
		public static readonly string BookmarksWindowName = "Name";

		/// <summary>
		/// Bookmarks window column: Labels
		/// </summary>
		public static readonly string BookmarksWindowLabels = "Labels";

		/// <summary>
		/// Bookmarks window column: Location
		/// </summary>
		public static readonly string BookmarksWindowLocation = "Location";

		/// <summary>
		/// Bookmarks window column: Module
		/// </summary>
		public static readonly string BookmarksWindowModule = "Module";

		/// <summary>
		/// Code breakpoints window column: Name
		/// </summary>
		public static readonly string CodeBreakpointsWindowName = "Name";

		/// <summary>
		/// Code breakpoints window column: Labels
		/// </summary>
		public static readonly string CodeBreakpointsWindowLabels = "Labels";

		/// <summary>
		/// Code breakpoints window column: Condition
		/// </summary>
		public static readonly string CodeBreakpointsWindowCondition = "Condition";

		/// <summary>
		/// Code breakpoints window column: Hit Count
		/// </summary>
		public static readonly string CodeBreakpointsWindowHitCount = "HitCount";

		/// <summary>
		/// Code breakpoints window column: Filter
		/// </summary>
		public static readonly string CodeBreakpointsWindowFilter = "Filter";

		/// <summary>
		/// Code breakpoints window column: When Hit
		/// </summary>
		public static readonly string CodeBreakpointsWindowWhenHit = "WhenHit";

		/// <summary>
		/// Code breakpoints window column: Module
		/// </summary>
		public static readonly string CodeBreakpointsWindowModule = "Module";

		/// <summary>
		/// Module Breakpoints window column: Module Name
		/// </summary>
		public static readonly string ModuleBreakpointsWindowModuleName = "ModuleName";

		/// <summary>
		/// Module Breakpoints window column: Order
		/// </summary>
		public static readonly string ModuleBreakpointsWindowOrder = "Order";

		/// <summary>
		/// Module Breakpoints window column: AppDomain Name
		/// </summary>
		public static readonly string ModuleBreakpointsWindowModuleAppDomainName = "AppDomainName";

		/// <summary>
		/// Module Breakpoints window column: Process Name
		/// </summary>
		public static readonly string ModuleBreakpointsWindowProcessName = "ProcessName";

		/// <summary>
		/// Call Stack window column: Name
		/// </summary>
		public static readonly string CallStackWindowName = "Name";

		/// <summary>
		/// Attach to Process window column: Process
		/// </summary>
		public static readonly string AttachToProcessWindowProcess = "Process";

		/// <summary>
		/// Attach to Process window column: PID
		/// </summary>
		public static readonly string AttachToProcessWindowPid = "PID";

		/// <summary>
		/// Attach to Process window column: Title
		/// </summary>
		public static readonly string AttachToProcessWindowTitle = "Title";

		/// <summary>
		/// Attach to Process window column: Type
		/// </summary>
		public static readonly string AttachToProcessWindowType = "Type";

		/// <summary>
		/// Attach to Process window column: Machine
		/// </summary>
		public static readonly string AttachToProcessWindowMachine = "Machine";

		/// <summary>
		/// Attach to Process window column: Path
		/// </summary>
		public static readonly string AttachToProcessWindowFullPath = "Path";

		/// <summary>
		/// Attach to Process window column: Command Line
		/// </summary>
		public static readonly string AttachToProcessWindowCommandLine = "CommandLine";

		/// <summary>
		/// Exception Settings window column: Break When Thrown
		/// </summary>
		public static readonly string ExceptionSettingsWindowName = "Name";

		/// <summary>
		/// Exception Settings window column: Category
		/// </summary>
		public static readonly string ExceptionSettingsWindowCategory = "Category";

		/// <summary>
		/// Exception Settings window column: Conditions
		/// </summary>
		public static readonly string ExceptionSettingsWindowConditions = "Conditions";

		/// <summary>
		/// Locals window column: Name
		/// </summary>
		public static readonly string LocalsWindowName = "Name";

		/// <summary>
		/// Locals window column: Value
		/// </summary>
		public static readonly string LocalsWindowValue = "Value";

		/// <summary>
		/// Locals window column: Type
		/// </summary>
		public static readonly string LocalsWindowType = "Type";

		/// <summary>
		/// Autos window column: Name
		/// </summary>
		public static readonly string AutosWindowName = "Name";

		/// <summary>
		/// Autos window column: Value
		/// </summary>
		public static readonly string AutosWindowValue = "Value";

		/// <summary>
		/// Autos window column: Type
		/// </summary>
		public static readonly string AutosWindowType = "Type";

		/// <summary>
		/// Watch window column: Name
		/// </summary>
		public static readonly string WatchWindowName = "Name";

		/// <summary>
		/// Watch window column: Value
		/// </summary>
		public static readonly string WatchWindowValue = "Value";

		/// <summary>
		/// Watch window column: Type
		/// </summary>
		public static readonly string WatchWindowType = "Type";

		/// <summary>
		/// Modules window column: Name
		/// </summary>
		public static readonly string ModulesWindowName = "Name";

		/// <summary>
		/// Modules window column: Path
		/// </summary>
		public static readonly string ModulesWindowPath = "Path";

		/// <summary>
		/// Modules window column: Optimized
		/// </summary>
		public static readonly string ModulesWindowOptimized = "Optimized";

		/// <summary>
		/// Modules window column: Dynamic
		/// </summary>
		public static readonly string ModulesWindowDynamic = "Dynamic";

		/// <summary>
		/// Modules window column: InMemory
		/// </summary>
		public static readonly string ModulesWindowInMemory = "InMemory";

		/// <summary>
		/// Modules window column: Order
		/// </summary>
		public static readonly string ModulesWindowOrder = "Order";

		/// <summary>
		/// Modules window column: Version
		/// </summary>
		public static readonly string ModulesWindowVersion = "Version";

		/// <summary>
		/// Modules window column: Timestamp
		/// </summary>
		public static readonly string ModulesWindowTimestamp = "Timestamp";

		/// <summary>
		/// Modules window column: Address
		/// </summary>
		public static readonly string ModulesWindowAddress = "Address";

		/// <summary>
		/// Modules window column: Process
		/// </summary>
		public static readonly string ModulesWindowProcess = "Process";

		/// <summary>
		/// Modules window column: AppDomain
		/// </summary>
		public static readonly string ModulesWindowAppDomain = "AppDomain";

		/// <summary>
		/// Threads window column: Id
		/// </summary>
		public static readonly string ThreadsWindowId = "ID";

		/// <summary>
		/// Threads window column: ManagedId
		/// </summary>
		public static readonly string ThreadsWindowManagedId = "ManagedId";

		/// <summary>
		/// Threads window column: CategoryText
		/// </summary>
		public static readonly string ThreadsWindowCategoryText = "CategoryText";

		/// <summary>
		/// Threads window column: Name
		/// </summary>
		public static readonly string ThreadsWindowName = "Name";

		/// <summary>
		/// Threads window column: Location
		/// </summary>
		public static readonly string ThreadsWindowLocation = "Location";

		/// <summary>
		/// Threads window column: Priority
		/// </summary>
		public static readonly string ThreadsWindowPriority = "Priority";

		/// <summary>
		/// Threads window column: AffinityMask
		/// </summary>
		public static readonly string ThreadsWindowAffinityMask = "AffinityMask";

		/// <summary>
		/// Threads window column: Suspended Count
		/// </summary>
		public static readonly string ThreadsWindowSuspended = "SuspendedCount";

		/// <summary>
		/// Threads window column: Process Name
		/// </summary>
		public static readonly string ThreadsWindowProcess = "ProcessName";

		/// <summary>
		/// Threads window column: AppDomain
		/// </summary>
		public static readonly string ThreadsWindowAppDomain = "AppDomain";

		/// <summary>
		/// Threads window column: State
		/// </summary>
		public static readonly string ThreadsWindowUserState = "State";

		/// <summary>
		/// Processes window column: Name
		/// </summary>
		public static readonly string ProcessesWindowName = "Name";

		/// <summary>
		/// Processes window column: ID
		/// </summary>
		public static readonly string ProcessesWindowId = "ID";

		/// <summary>
		/// Processes window column: Title
		/// </summary>
		public static readonly string ProcessesWindowTitle = "Title";

		/// <summary>
		/// Processes window column: State
		/// </summary>
		public static readonly string ProcessesWindowState = "State";

		/// <summary>
		/// Processes window column: Debugging
		/// </summary>
		public static readonly string ProcessesWindowDebugging = "Debugging";

		/// <summary>
		/// Processes window column: Architecture
		/// </summary>
		public static readonly string ProcessesWindowArchitecture = "Architecture";

		/// <summary>
		/// Processes window column: Path
		/// </summary>
		public static readonly string ProcessesWindowPath = "Path";

		/// <summary>
		/// Options dialog text
		/// </summary>
		public static readonly string OptionsDialogText = "Text";
	}
}
