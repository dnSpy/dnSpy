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

using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Application command line arguments
	/// </summary>
	public interface IAppCommandLineArgs {
		/// <summary>Settings filename</summary>
		string? SettingsFilename { get; }

		/// <summary>Filenames to load</summary>
		IEnumerable<string> Filenames { get; }

		/// <summary>true if single-instance</summary>
		bool SingleInstance { get; }

		/// <summary>true to activate the window</summary>
		bool Activate { get; }

		/// <summary>Language, either human readable or a language guid
		/// (<see cref="IDecompiler.GenericGuid"/> or <see cref="IDecompiler.UniqueGuid"/>)</summary>
		string Language { get; }

		/// <summary>Culture</summary>
		string Culture { get; }

		/// <summary>Member to select, either an MD token or an XML doc name</summary>
		string SelectMember { get; }

		/// <summary>Show the file in a new tab</summary>
		bool NewTab { get; }

		/// <summary>Search string or null if none</summary>
		string? SearchText { get; }

		/// <summary>Search type</summary>
		string SearchFor { get; }

		/// <summary>Search location</summary>
		string SearchIn { get; }

		/// <summary>Theme name (<see cref="ITheme.Guid"/> or <see cref="ITheme.Name"/>)</summary>
		string Theme { get; }

		/// <summary>true to load all saved files at startup</summary>
		bool LoadFiles { get; }

		/// <summary>Full screen</summary>
		bool? FullScreen { get; }

		/// <summary>Tool windows to show</summary>
		string ShowToolWindow { get; }

		/// <summary>Tool windows to hide</summary>
		string HideToolWindow { get; }

		/// <summary>Show start up time</summary>
		bool ShowStartupTime { get; }

		/// <summary>Attach to this process, unless it's 0</summary>
		int DebugAttachPid { get; }

		/// <summary>Event handle duplicated into the postmortem debugger process</summary>
		uint DebugEvent { get; }

		/// <summary>Address of a JIT_DEBUG_INFO structure allocated in the target process' address space (https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/-jdinfo--use-jit-debug-info-) </summary>
		ulong JitDebugInfo { get; }

		/// <summary>Attach to this process name, unless it's empty. Can contain wildcards.</summary>
		string DebugAttachProcess { get; }

		/// <summary>Additional directory to check for extensions.</summary>
		string ExtraExtensionDirectory { get; }

		/// <summary>
		/// Returns true if the argument is present
		/// </summary>
		/// <param name="argName">Argument name, eg. <c>--my-arg</c></param>
		/// <returns></returns>
		bool HasArgument(string argName);

		/// <summary>
		/// Gets the argument value or null if the argument isn't present
		/// </summary>
		/// <param name="argName">Argument name, eg. <c>--my-arg</c></param>
		/// <returns></returns>
		string? GetArgumentValue(string argName);

		/// <summary>
		/// Gets all user arguments and values
		/// </summary>
		/// <returns></returns>
		IEnumerable<(string argument, string value)> GetArguments();
	}
}
