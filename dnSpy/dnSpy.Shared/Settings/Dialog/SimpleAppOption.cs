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
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Shared.Settings.Dialog {
	public sealed class SimpleAppOptionCheckBox : ISimpleAppOptionCheckBox {
		public double Order { get; set; }
		public bool IsThreeState { get; set; }
		public object Text { get; set; }
		public bool? Value { get; set; }
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
				throw new ArgumentNullException();
			this.onClosed = onClosed;
			this.Value = currentValue;
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			onClosed(saveSettings, appRefreshSettings, Value);
		}
	}

	public sealed class SimpleAppOptionButton : ISimpleAppOptionButton {
		public double Order { get; set; }
		public ICommand Command { get; set; }
		public object Text { get; set; }
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

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			onClosed(saveSettings, appRefreshSettings);
		}
	}

	public sealed class SimpleAppOptionTextBox : ISimpleAppOptionTextBox {
		public double Order { get; set; }
		public object Text { get; set; }
		public string Value { get; set; }
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
				throw new ArgumentNullException();
			this.onClosed = onClosed;
			this.Value = currentValue;
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			onClosed(saveSettings, appRefreshSettings, Value);
		}
	}

	public sealed class SimpleAppOptionUserContent<TUIContent> : ISimpleAppOptionUserContent {
		public double Order { get; set; }
		public object UIContent {
			get { return uiContent; }
		}
		readonly TUIContent uiContent;

		readonly Action<bool, IAppRefreshSettings, TUIContent> onClosed;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="uiContent">UI content</param>
		/// <param name="onClosed">Called when the dialog box has closed, see
		/// <see cref="ISimpleAppOption.OnClosed(bool, IAppRefreshSettings)"/>. The 3rd argument is
		/// the UI content (<param name="uiContent")</param>
		public SimpleAppOptionUserContent(TUIContent uiContent, Action<bool, IAppRefreshSettings, TUIContent> onClosed) {
			if (onClosed == null)
				throw new ArgumentNullException();
			this.onClosed = onClosed;
			this.uiContent = uiContent;
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			onClosed(saveSettings, appRefreshSettings, uiContent);
		}
	}
}
