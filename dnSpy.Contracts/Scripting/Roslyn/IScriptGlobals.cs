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

using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;

namespace dnSpy.Contracts.Scripting.Roslyn {
	/// <summary>
	/// The script's global class
	/// </summary>
	public interface IScriptGlobals {
		/// <summary>
		/// Returns itself so it can be passed into classes that can't access the globals
		/// </summary>
		IScriptGlobals Instance { get; }

		/// <summary>
		/// Raised when the script gets reset. Can be used to unregister from events to prevent
		/// memory leaks. Raised on the UI thread.
		/// </summary>
		event EventHandler ScriptReset;

		/// <summary>
		/// Cancellation token that gets signalled when the script gets reset
		/// </summary>
		CancellationToken Token { get; }

		/// <summary>
		/// Print options
		/// </summary>
		IPrintOptions PrintOptions { get; }

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="text">Text</param>
		void Print(string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void Print(string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="text">Text or null</param>
		void PrintLine(string text = null);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void PrintLine(string fmt, params object[] args);

		/// <summary>
		/// Formats and prints a value to the screen
		/// </summary>
		/// <param name="value">Value, can be null</param>
		void Print(object value);

		/// <summary>
		/// Formats and prints a value followed by a new line to the screen
		/// </summary>
		/// <param name="value">Value or null</param>
		void PrintLine(object value);

		/// <summary>
		/// Formats and prints an exception to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		void Print(Exception ex);

		/// <summary>
		/// Formats and prints an exception followed by a new line to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		void PrintLine(Exception ex);

		/// <summary>
		/// UI thread dispatcher
		/// </summary>
		Dispatcher UIDispatcher { get; }

		/// <summary>
		/// Executes <paramref name="action"/> in the UI thread
		/// </summary>
		/// <param name="action">Code</param>
		void UI(Action action);

		/// <summary>
		/// Executes <paramref name="func"/> in the UI thread
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="func">Code</param>
		/// <returns></returns>
		T UI<T>(Func<T> func);

		/// <summary>
		/// Calls <see cref="System.Diagnostics.Debugger.Break"/>. Use dnSpy to debug itself
		/// (dnSpy --multiple) and then call this method from your script in the debugged dnSpy process.
		/// </summary>
		void Break();

		/// <summary>
		/// Resolves a service, and throws if it wasn't found
		/// </summary>
		/// <typeparam name="T">Type of service</typeparam>
		/// <returns></returns>
		T Resolve<T>();

		/// <summary>
		/// Resolves a service or returns null if not found
		/// </summary>
		/// <typeparam name="T">Type of service</typeparam>
		/// <returns></returns>
		T TryResolve<T>();

		/// <summary>
		/// Shows a message box
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="buttons">Buttons that should be present</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton Show(string message, MsgBoxButton buttons = MsgBoxButton.OK, Window ownerWindow = null);

		/// <summary>
		/// Shows a message box with buttons OK and Cancel
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton ShowOKCancel(string message, Window ownerWindow = null);

		/// <summary>
		/// Shows a message box with buttons OK and Cancel
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton ShowOC(string message, Window ownerWindow = null);

		/// <summary>
		/// Shows a message box with buttons Yes and No
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton ShowYesNo(string message, Window ownerWindow = null);

		/// <summary>
		/// Shows a message box with buttons Yes and No
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton ShowYN(string message, Window ownerWindow = null);

		/// <summary>
		/// Shows a message box with buttons Yes, No and Cancel
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton ShowYesNoCancel(string message, Window ownerWindow = null);

		/// <summary>
		/// Shows a message box with buttons Yes, No and Cancel
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton ShowYNC(string message, Window ownerWindow = null);

		/// <summary>
		/// Asks the user for a value and returns it or the default value (eg. null or 0) if the
		/// user canceled the dialog box.
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <param name="labelMessage">Label</param>
		/// <param name="defaultText">Default text to write to the textbox or null</param>
		/// <param name="title">Title or null</param>
		/// <param name="converter">Converts a string to the type, or null to use the default
		/// converter.</param>
		/// <param name="verifier">Verifies the typed message. Returns null or an empty string if
		/// it's a valid value, else an error message to show to the user.</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		T Ask<T>(string labelMessage, string defaultText = null, string title = null, Func<string, T> converter = null, Func<string, string> verifier = null, Window ownerWindow = null);

		/// <summary>
		/// Shows an exception message
		/// </summary>
		/// <param name="exception">Exception</param>
		/// <param name="msg">Message to show or null</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		void Show(Exception exception, string msg = null, Window ownerWindow = null);
	}
}
