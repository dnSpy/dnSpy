/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.App;

namespace dnSpy.MainApp {
	[ExportAutoLoaded(Order = double.MinValue)]
	sealed class MessageBoxManagerLoader : IAutoLoaded {
		[ImportingConstructor]
		MessageBoxManagerLoader(MessageBoxManager messageBoxManager) {
			MsgBox.Instance = messageBoxManager;
		}
	}

	[Export, Export(typeof(IMessageBoxManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class MessageBoxManager : IMessageBoxManager {
		static readonly Guid SETTINGS_GUID = new Guid("686C5CFB-FF63-4AA5-8C92-E08607AE5146");
		const string IGNORED_SECTION = "Ignored";
		const string IGNORED_ATTR = "id";

		public bool CanEnableAllWarnings {
			get { return ignoredMessages.Count > 0; }
		}

		readonly IAppWindow appWindow;
		readonly ISettingsManager settingsManager;
		readonly HashSet<string> ignoredMessages;

		[ImportingConstructor]
		MessageBoxManager(IAppWindow appWindow, ISettingsManager settingsManager) {
			this.appWindow = appWindow;
			this.settingsManager = settingsManager;
			this.ignoredMessages = new HashSet<string>();
			ReadSettings();
		}

		void ReadSettings() {
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			foreach (var ignoredSect in sect.SectionsWithName(IGNORED_SECTION)) {
				var id = ignoredSect.Attribute<string>(IGNORED_ATTR);
				if (!string.IsNullOrEmpty(id))
					ignoredMessages.Add(id);
			}
		}

		public void EnableAllWarnings() {
			ignoredMessages.Clear();
			SaveSettings();
		}

		void SaveSettings() {
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			foreach (var id in ignoredMessages) {
				var ignoredSect = sect.CreateSection(IGNORED_SECTION);
				ignoredSect.Attribute(IGNORED_ATTR, id);
			}
		}

		public MsgBoxButton? ShowIgnorableMessage(string id, string message, MsgBoxButton buttons = MsgBoxButton.OK, Window ownerWindow = null) {
			if (ignoredMessages.Contains(id))
				return null;
			MsgBoxDlg win;
			MsgBoxVM vm;
			Create(message, buttons, true, ownerWindow, out win, out vm);
			win.ShowDialog();
			if (win.ClickedButton != MsgBoxButton.None && vm.DontShowAgain) {
				ignoredMessages.Add(id);
				SaveSettings();
			}
			return win.ClickedButton;
		}

		public MsgBoxButton Show(string message, MsgBoxButton buttons = MsgBoxButton.OK, Window ownerWindow = null) {
			MsgBoxDlg win;
			MsgBoxVM vm;
			Create(message, buttons, false, ownerWindow, out win, out vm);
			win.ShowDialog();
			return win.ClickedButton;
		}

		void Create(string message, MsgBoxButton buttons, bool hasDontShowAgain, Window ownerWindow, out MsgBoxDlg win, out MsgBoxVM vm) {
			win = new MsgBoxDlg();
			var winTmp = win;
			vm = new MsgBoxVM(message, button => winTmp.Close(button));
			vm.HasDontShowAgain = hasDontShowAgain;
			vm.HasOKButton = (buttons & MsgBoxButton.OK) != 0;
			vm.HasYesButton = (buttons & MsgBoxButton.Yes) != 0;
			vm.HasNoButton = (buttons & MsgBoxButton.No) != 0;
			vm.HasCancelButton = (buttons & MsgBoxButton.Cancel) != 0;
			win.DataContext = vm;
			win.Owner = ownerWindow ?? appWindow.MainWindow;
		}

		public T Ask<T>(string labelMessage, string defaultText = null, Func<string, T> converter = null, Func<string, string> verifier = null, Window ownerWindow = null) {
			var win = new AskDlg();
			if (converter == null)
				converter = CreateDefaultConverter<T>();
			if (verifier == null)
				verifier = CreateDefaultVerifier<T>();

			var vm = new AskVM(labelMessage, s => converter(s), verifier);
			vm.Text = defaultText ?? string.Empty;
			win.DataContext = vm;
			win.Owner = ownerWindow ?? appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return default(T);
			return (T)vm.Value;
		}

		Func<string, T> CreateDefaultConverter<T>() {
			var c = TypeDescriptor.GetConverter(typeof(T));
			return s => (T)c.ConvertFromInvariantString(s);
		}

		Func<string, string> CreateDefaultVerifier<T>() {
			var c = TypeDescriptor.GetConverter(typeof(T));
			return s => {
				if (c.IsValid(s))
					return string.Empty;
				return "The text is invalid";
			};
		}
	}
}
