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
using System.Windows;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Shows message boxes
	/// </summary>
	public interface IMessageBoxService {
		/// <summary>
		/// Shows a message box unless the user has disabled showing this particular message. null
		/// is returned if the message was ignored and no message box was shown. Otherwise, the
		/// return value is the same as <see cref="Show(string, MsgBoxButton, Window)"/>.
		/// </summary>
		/// <param name="guid">Unique guid for this message</param>
		/// <param name="message">Message to show</param>
		/// <param name="buttons">Buttons that should be present</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton? ShowIgnorableMessage(Guid guid, string message, MsgBoxButton buttons = MsgBoxButton.OK, Window? ownerWindow = null);

		/// <summary>
		/// Shows a message box
		/// </summary>
		/// <param name="message">Message to show</param>
		/// <param name="buttons">Buttons that should be present</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		/// <returns></returns>
		MsgBoxButton Show(string message, MsgBoxButton buttons = MsgBoxButton.OK, Window? ownerWindow = null);

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
		T Ask<T>(string labelMessage, string? defaultText = null, string? title = null, Func<string, T>? converter = null, Func<string, string?>? verifier = null, Window? ownerWindow = null);

		/// <summary>
		/// Shows an exception message
		/// </summary>
		/// <param name="exception">Exception</param>
		/// <param name="msg">Message to show or null</param>
		/// <param name="ownerWindow">Owner window or null to use the main window</param>
		void Show(Exception exception, string? msg = null, Window? ownerWindow = null);
	}
}
