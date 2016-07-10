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
using System.Windows.Input;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Impements <see cref="ISimpleAppOptionCheckBox"/>
	/// </summary>
	public sealed class SimpleAppOptionCheckBox : ISimpleAppOptionCheckBox {
		/// <summary>
		/// Gets/sets the order
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// true if it's a three-state checkbox
		/// </summary>
		public bool IsThreeState { get; set; }

		/// <summary>
		/// Gets/sets the checkbox text
		/// </summary>
		public object Text { get; set; }

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		public bool? Value { get; set; }

		/// <summary>
		/// Gets/sets the tooltip or null
		/// </summary>
		public object ToolTip { get; set; }

		readonly Action<bool, IAppRefreshSettings, bool?> onClosed;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="currentValue">Current value (<see cref="Value"/>)</param>
		/// <param name="onClosed">Called when the dialog box has closed, see
		/// <see cref="ISimpleAppOption.OnClosed(bool, IAppRefreshSettings)"/>. The 3rd argument is
		/// the current value (<see cref="Value"/>)</param>
		public SimpleAppOptionCheckBox(bool? currentValue, Action<bool, IAppRefreshSettings, bool?> onClosed) {
			if (onClosed == null)
				throw new ArgumentNullException(nameof(onClosed));
			this.onClosed = onClosed;
			this.Value = currentValue;
		}

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) =>
			onClosed(saveSettings, appRefreshSettings, Value);
	}

	/// <summary>
	/// Impements <see cref="ISimpleAppOptionButton"/>
	/// </summary>
	public sealed class SimpleAppOptionButton : ISimpleAppOptionButton {
		/// <summary>
		/// Gets/sets the order
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Gets/sets the button command
		/// </summary>
		public ICommand Command { get; set; }

		/// <summary>
		/// Gets/sets the button text
		/// </summary>
		public object Text { get; set; }

		/// <summary>
		/// Gets/sets the tooltip or null
		/// </summary>
		public object ToolTip { get; set; }

		readonly Action<bool, IAppRefreshSettings> onClosed;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onClosed">Called when the dialog box has closed, see
		/// <see cref="ISimpleAppOption.OnClosed(bool, IAppRefreshSettings)"/></param>
		public SimpleAppOptionButton(Action<bool, IAppRefreshSettings> onClosed = null) {
			this.onClosed = onClosed ?? new Action<bool, IAppRefreshSettings>((a, b) => { });
		}

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) =>
			onClosed(saveSettings, appRefreshSettings);
	}

	/// <summary>
	/// Impements <see cref="ISimpleAppOptionTextBox"/>
	/// </summary>
	public sealed class SimpleAppOptionTextBox : ISimpleAppOptionTextBox {
		/// <summary>
		/// Gets/sets the order
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Gets/sets the label text
		/// </summary>
		public object Text { get; set; }

		/// <summary>
		/// Gets/sets the textbox text
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Gets/sets the tooltip or null
		/// </summary>
		public object ToolTip { get; set; }

		readonly Action<bool, IAppRefreshSettings, string> onClosed;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="currentValue">Current value (<see cref="Value"/>)</param>
		/// <param name="onClosed">Called when the dialog box has closed, see
		/// <see cref="ISimpleAppOption.OnClosed(bool, IAppRefreshSettings)"/>. The 3rd argument is
		/// the current value (<see cref="Value"/>)</param>
		public SimpleAppOptionTextBox(string currentValue, Action<bool, IAppRefreshSettings, string> onClosed) {
			if (onClosed == null)
				throw new ArgumentNullException(nameof(onClosed));
			this.onClosed = onClosed;
			this.Value = currentValue;
		}

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) =>
			onClosed(saveSettings, appRefreshSettings, Value);
	}

	/// <summary>
	/// Impements <see cref="ISimpleAppOptionUserContent"/>
	/// </summary>
	/// <typeparam name="TUIContent">UI content type</typeparam>
	public sealed class SimpleAppOptionUserContent<TUIContent> : ISimpleAppOptionUserContent {
		/// <summary>
		/// Gets/sets the order
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Gets/sets the UI content
		/// </summary>
		public object UIContent => uiContent;
		readonly TUIContent uiContent;

		readonly Action<bool, IAppRefreshSettings, TUIContent> onClosed;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="uiContent">UI content</param>
		/// <param name="onClosed">Called when the dialog box has closed, see
		/// <see cref="ISimpleAppOption.OnClosed(bool, IAppRefreshSettings)"/>. The 3rd argument is
		/// the UI content (<paramref name="uiContent"/>)</param>
		public SimpleAppOptionUserContent(TUIContent uiContent, Action<bool, IAppRefreshSettings, TUIContent> onClosed) {
			if (onClosed == null)
				throw new ArgumentNullException(nameof(onClosed));
			this.onClosed = onClosed;
			this.uiContent = uiContent;
		}

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) =>
			onClosed(saveSettings, appRefreshSettings, uiContent);
	}
}
