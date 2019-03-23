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

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Global debugger settings. This class is thread safe. Listeners will be notified
	/// on a random thread.
	/// </summary>
	public abstract class DebuggerSettings : INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that got changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// true to use hexadecimal numbers, false to use decimal numbers
		/// </summary>
		public abstract bool UseHexadecimal { get; set; }

		/// <summary>
		/// true to use digit separators
		/// </summary>
		public abstract bool UseDigitSeparators { get; set; }

		/// <summary>
		/// true to colorize debugger tool windows and other debugger UI objects
		/// </summary>
		public abstract bool SyntaxHighlight { get; set; }

		/// <summary>
		/// true to auto open the locals window when the debugger starts
		/// </summary>
		public abstract bool AutoOpenLocalsWindow { get; set; }

		/// <summary>
		/// Use modules loaded from memory instead of files. Useful if a module gets decrypted at runtime.
		/// </summary>
		public abstract bool UseMemoryModules { get; set; }

		/// <summary>
		/// true if properties and methods can be executed (used by locals / watch windows)
		/// </summary>
		public abstract bool PropertyEvalAndFunctionCalls { get; set; }

		/// <summary>
		/// Use ToString() or similar method to get a string representation of an object (used by locals / watch windows)
		/// </summary>
		public abstract bool UseStringConversionFunction { get; set; }

		/// <summary>
		/// true to prevent detection of managed debuggers
		/// </summary>
		public abstract bool PreventManagedDebuggerDetection { get; set; }

		/// <summary>
		/// true to patch IsDebuggerPresent() so it can't be used to detect native debuggers
		/// </summary>
		public abstract bool AntiIsDebuggerPresent { get; set; }

		/// <summary>
		/// true to patch CheckRemoteDebuggerPresent() so it can't be used to detect native debuggers
		/// </summary>
		public abstract bool AntiCheckRemoteDebuggerPresent { get; set; }

		/// <summary>
		/// true to ignore break instructions and <see cref="System.Diagnostics.Debugger.Break"/> method calls
		/// </summary>
		public abstract bool IgnoreBreakInstructions { get; set; }

		/// <summary>
		/// true to break all processes when one process breaks
		/// </summary>
		public abstract bool BreakAllProcesses { get; set; }

		/// <summary>
		/// true to enable Managed Debugging Assistants (MDA)
		/// </summary>
		public abstract bool EnableManagedDebuggingAssistants { get; set; }

		/// <summary>
		/// Highlights the value of a variable that has changed in variables windows
		/// </summary>
		public abstract bool HighlightChangedVariables { get; set; }

		/// <summary>
		/// Shows raw structure of objects in variables windows
		/// </summary>
		public abstract bool ShowRawStructureOfObjects { get; set; }

		/// <summary>
		/// Sort parameters (locals window)
		/// </summary>
		public abstract bool SortParameters { get; set; }

		/// <summary>
		/// Sort locals (locals window)
		/// </summary>
		public abstract bool SortLocals { get; set; }

		/// <summary>
		/// Group parameters and locals together (locals window)
		/// </summary>
		public abstract bool GroupParametersAndLocalsTogether { get; set; }

		/// <summary>
		/// Show compiler generated variables (locals/autos window)
		/// </summary>
		public abstract bool ShowCompilerGeneratedVariables { get; set; }

		/// <summary>
		/// Show decompiler generated variables (locals/autos window)
		/// </summary>
		public abstract bool ShowDecompilerGeneratedVariables { get; set; }

		/// <summary>
		/// Hide compiler generated members in variables windows (respect debugger attributes, eg. <see cref="CompilerGeneratedAttribute"/>)
		/// </summary>
		public abstract bool HideCompilerGeneratedMembers { get; set; }

		/// <summary>
		/// Respect attributes that can hide a member, eg. <see cref="DebuggerBrowsableAttribute"/> and <see cref="DebuggerBrowsableState.Never"/>
		/// </summary>
		public abstract bool RespectHideMemberAttributes { get; set; }

		/// <summary>
		/// Hide deprecated members
		/// </summary>
		public abstract bool HideDeprecatedError { get; set; }

		/// <summary>
		/// Suppress JIT optimization on module load (system modules). If false, the code will be optimized and
		/// much more difficult to debug (it will be like when attaching to a process).
		/// System modules are all non-program modules (eg. GAC assemblies).
		/// </summary>
		public abstract bool SuppressJITOptimization_SystemModules { get; set; }

		/// <summary>
		/// Suppress JIT optimization on module load (program modules). If false, the code will be optimized and
		/// much more difficult to debug (it will be like when attaching to a process).
		/// All modules in the same folder, or sub folder, as the main executable are considered program modules.
		/// </summary>
		public abstract bool SuppressJITOptimization_ProgramModules { get; set; }

		/// <summary>
		/// Give focus to the active process
		/// </summary>
		public abstract bool FocusActiveProcess { get; set; }

		/// <summary>
		/// Show return values in Locals window
		/// </summary>
		public abstract bool ShowReturnValues { get; set; }

		/// <summary>
		/// Redirect GUI applications' console output to the Output window
		/// </summary>
		public abstract bool RedirectGuiConsoleOutput { get; set; }

		/// <summary>
		/// Show only public members in variables windows
		/// </summary>
		public abstract bool ShowOnlyPublicMembers { get; set; }

		/// <summary>
		/// Show all locals. Captured variables aren't shown, their display classes are shown instead.
		/// </summary>
		public abstract bool ShowRawLocals { get; set; }

		/// <summary>
		/// Async debugging (step over await statements, step out of async methods)
		/// </summary>
		public abstract bool AsyncDebugging { get; set; }

		/// <summary>
		/// Step over properties and operators
		/// </summary>
		public abstract bool StepOverPropertiesAndOperators { get; set; }

		/// <summary>
		/// Ignore unhandled exceptions
		/// </summary>
		public abstract bool IgnoreUnhandledExceptions { get; set; }

		/// <summary>
		/// Show the full string value even if it's a very long string
		/// </summary>
		public abstract bool FullString { get; set; }
	}
}
