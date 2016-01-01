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

using System.Windows.Input;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// An option added to the app settings dialog box, see <see cref="ISimpleAppOptionCheckBox"/>,
	/// <see cref="ISimpleAppOptionButton"/>, <see cref="ISimpleAppOptionTextBox"/>,
	/// <see cref="ISimpleAppOptionUserContent"/>
	/// </summary>
	public interface ISimpleAppOption {
		/// <summary>
		/// Gets the order
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings);
	}

	/// <summary>
	/// Adds a checkbox to the app settings dialog box
	/// </summary>
	public interface ISimpleAppOptionCheckBox : ISimpleAppOption {
		/// <summary>
		/// Gets the checkbox text
		/// </summary>
		object Text { get; }

		/// <summary>
		/// Gets the value
		/// </summary>
		bool? Value { get; set; }

		/// <summary>
		/// true if it's a three-state checkbox
		/// </summary>
		bool IsThreeState { get; }

		/// <summary>
		/// Gets the tooltip or null
		/// </summary>
		object ToolTip { get; }
	}

	/// <summary>
	/// Adds a button to the app settings dialog box
	/// </summary>
	public interface ISimpleAppOptionButton : ISimpleAppOption {
		/// <summary>
		/// Gets the button text
		/// </summary>
		object Text { get; }

		/// <summary>
		/// Gets the button command
		/// </summary>
		ICommand Command { get; }

		/// <summary>
		/// Gets the tooltip or null
		/// </summary>
		object ToolTip { get; }
	}

	/// <summary>
	/// Adds a textbox to the app settings dialog box
	/// </summary>
	public interface ISimpleAppOptionTextBox : ISimpleAppOption {
		/// <summary>
		/// Gets the label text
		/// </summary>
		object Text { get; }

		/// <summary>
		/// Gets the textbox text
		/// </summary>
		string Value { get; set; }

		/// <summary>
		/// Gets the tooltip or null
		/// </summary>
		object ToolTip { get; }
	}

	/// <summary>
	/// Adds user content to the app settings dialog box
	/// </summary>
	public interface ISimpleAppOptionUserContent : ISimpleAppOption {
		/// <summary>
		/// Gets the UI content
		/// </summary>
		object UIContent { get; }
	}
}
