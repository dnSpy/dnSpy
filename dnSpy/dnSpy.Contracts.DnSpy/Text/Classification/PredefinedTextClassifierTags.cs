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
		/// Breakpoints window column: Name
		/// </summary>
		public static readonly string BreakpointsWindowName = "Name";

		/// <summary>
		/// Breakpoints window column: Assembly
		/// </summary>
		public static readonly string BreakpointsWindowAssembly = "Assembly";

		/// <summary>
		/// Breakpoints window column: Module
		/// </summary>
		public static readonly string BreakpointsWindowModule = "Module";

		/// <summary>
		/// Breakpoints window column: File
		/// </summary>
		public static readonly string BreakpointsWindowFile = "File";

		/// <summary>
		/// Call Stack window column: Name
		/// </summary>
		public static readonly string CallStackWindowName = "Name";

		/// <summary>
		/// Attach to Process window column: FullPath
		/// </summary>
		public static readonly string AttachToProcessWindowFullPath = "FullPath";

		/// <summary>
		/// Attach to Process window column: Filename
		/// </summary>
		public static readonly string AttachToProcessWindowFilename = "Filename";

		/// <summary>
		/// Attach to Process window column: PID
		/// </summary>
		public static readonly string AttachToProcessWindowPid = "PID";

		/// <summary>
		/// Attach to Process window column: CLRVersion
		/// </summary>
		public static readonly string AttachToProcessWindowClrVersion = "CLRVersion";

		/// <summary>
		/// Attach to Process window column: Type
		/// </summary>
		public static readonly string AttachToProcessWindowType = "Type";

		/// <summary>
		/// Attach to Process window column: Machine
		/// </summary>
		public static readonly string AttachToProcessWindowMachine = "Machine";

		/// <summary>
		/// Attach to Process window column: Title
		/// </summary>
		public static readonly string AttachToProcessWindowTitle = "Title";

		/// <summary>
		/// Exception Settings window column: Name
		/// </summary>
		public static readonly string ExceptionSettingsWindowName = "Name";

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
		public static readonly string ThreadsWindowId = "Id";

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
		/// Threads window column: Suspended
		/// </summary>
		public static readonly string ThreadsWindowSuspended = "Suspended";

		/// <summary>
		/// Threads window column: Process
		/// </summary>
		public static readonly string ThreadsWindowProcess = "Process";

		/// <summary>
		/// Threads window column: AppDomain
		/// </summary>
		public static readonly string ThreadsWindowAppDomain = "AppDomain";

		/// <summary>
		/// Threads window column: UserState
		/// </summary>
		public static readonly string ThreadsWindowUserState = "UserState";

		/// <summary>
		/// Options dialog text
		/// </summary>
		public static readonly string OptionsDialogText = "Text";
	}
}
