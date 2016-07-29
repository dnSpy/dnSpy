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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Scripting.Roslyn {
	/// <summary>
	/// The script's global class
	/// </summary>
	public interface IScriptGlobals : ITextPrinter {
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

		// NOTE: We have to add the same ITextPrinter methods and props here because Roslyn only
		// allows global access to everything defined in this interface, and not every public
		// method and property the global object defines.

		/// <summary>
		/// Print options
		/// </summary>
		new IPrintOptions PrintOptions { get; }

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="text">Text</param>
		new void PrintError(string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void PrintError(string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="text">Text or null</param>
		new void PrintLineError(string text);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void PrintLineError(string fmt, params object[] args);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		new void Print(object color, string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		new void Print(TextColor color, string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="text">Text</param>
		new void Print(string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void Print(object color, string fmt, params object[] args);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void Print(TextColor color, string fmt, params object[] args);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void Print(string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text or null</param>
		new void PrintLine(object color, string text);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text or null</param>
		new void PrintLine(TextColor color, string text);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="text">Text or null</param>
		new void PrintLine(string text = null);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void PrintLine(object color, string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void PrintLine(TextColor color, string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		new void PrintLine(string fmt, params object[] args);

		/// <summary>
		/// Formats and prints a value to the screen
		/// </summary>
		/// <param name="value">Value, can be null</param>
		/// <param name="color">Color</param>
		new void Print(object value, object color);

		/// <summary>
		/// Formats and prints a value to the screen
		/// </summary>
		/// <param name="value">Value, can be null</param>
		/// <param name="color">Color</param>
		new void Print(object value, TextColor color = TextColor.ReplScriptOutputText);

		/// <summary>
		/// Formats and prints a value followed by a new line to the screen
		/// </summary>
		/// <param name="value">Value or null</param>
		/// <param name="color">Color</param>
		new void PrintLine(object value, object color);

		/// <summary>
		/// Formats and prints a value followed by a new line to the screen
		/// </summary>
		/// <param name="value">Value or null</param>
		/// <param name="color">Color</param>
		new void PrintLine(object value, TextColor color = TextColor.ReplScriptOutputText);

		/// <summary>
		/// Formats and prints an exception to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		new void Print(Exception ex, object color);

		/// <summary>
		/// Formats and prints an exception to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		new void Print(Exception ex, TextColor color = TextColor.Error);

		/// <summary>
		/// Formats and prints an exception followed by a new line to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		new void PrintLine(Exception ex, object color);

		/// <summary>
		/// Formats and prints an exception followed by a new line to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		new void PrintLine(Exception ex, TextColor color = TextColor.Error);

		/// <summary>
		/// UI thread dispatcher
		/// </summary>
		Dispatcher UIDispatcher { get; }

		/// <summary>
		/// Creates a new <see cref="ICachedWriter"/> instance. Useful if your script runs in the
		/// background and prints text.
		/// </summary>
		/// <returns></returns>
		ICachedWriter CreateWriter();

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
