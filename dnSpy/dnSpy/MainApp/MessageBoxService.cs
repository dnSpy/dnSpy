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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Settings;
using dnSpy.Properties;

namespace dnSpy.MainApp {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions, Order = double.MinValue)]
	sealed class MessageBoxServiceLoader : IAutoLoaded {
		[ImportingConstructor]
		MessageBoxServiceLoader(IMessageBoxService messageBoxService) => MsgBox.Instance = messageBoxService;
	}

	[Export, Export(typeof(IMessageBoxService))]
	sealed class MessageBoxService : IMessageBoxService {
		static readonly Guid SETTINGS_GUID = new Guid("686C5CFB-FF63-4AA5-8C92-E08607AE5146");
		const string IGNORED_SECTION = "Ignored";
		const string IGNORED_ATTR = "id";

		public bool CanEnableAllWarnings => ignoredMessages.Count > 0;

		readonly IAppWindow appWindow;
		readonly ISettingsService settingsService;
		readonly HashSet<Guid> ignoredMessages;

		[ImportingConstructor]
		MessageBoxService(IAppWindow appWindow, ISettingsService settingsService) {
			this.appWindow = appWindow;
			this.settingsService = settingsService;
			ignoredMessages = new HashSet<Guid>();
			ReadSettings();
		}

		void ReadSettings() {
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			foreach (var ignoredSect in sect.SectionsWithName(IGNORED_SECTION)) {
				var id = ignoredSect.Attribute<string>(IGNORED_ATTR);
				if (!Guid.TryParse(id, out var guid))
					continue;
				ignoredMessages.Add(guid);
			}
		}

		public void EnableAllWarnings() {
			ignoredMessages.Clear();
			SaveSettings();
		}

		void SaveSettings() {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var id in ignoredMessages) {
				var ignoredSect = sect.CreateSection(IGNORED_SECTION);
				ignoredSect.Attribute(IGNORED_ATTR, id);
			}
		}

		public MsgBoxButton? ShowIgnorableMessage(Guid guid, string message, MsgBoxButton buttons, Window? ownerWindow) {
			if (ignoredMessages.Contains(guid))
				return null;
			Create(message, buttons, true, ownerWindow, out var win, out var vm);
			win.ShowDialog();
			if (win.ClickedButton != MsgBoxButton.None && vm.DontShowAgain) {
				ignoredMessages.Add(guid);
				SaveSettings();
			}
			return win.ClickedButton;
		}

		public MsgBoxButton Show(string message, MsgBoxButton buttons, Window? ownerWindow) {
			Create(message, buttons, false, ownerWindow, out var win, out var vm);
			win.ShowDialog();
			return win.ClickedButton;
		}

		public void Show(Exception exception, string? msg, Window? ownerWindow) {
			string msgToShow;
			if (exception is not null) {
				msgToShow = $"{msg ?? dnSpy_Resources.ExceptionMessage}\n\n{exception.ToString()}";
				const int MAX_LEN = 2048;
				if (msgToShow.Length > MAX_LEN)
					msgToShow = msgToShow.Substring(0, MAX_LEN) + "[...]";
			}
			else
				msgToShow = msg ?? dnSpy_Resources.UnknownError;
			Show(msgToShow, MsgBoxButton.OK, ownerWindow);
		}

		void Create(string message, MsgBoxButton buttons, bool hasDontShowAgain, Window? ownerWindow, out MsgBoxDlg win, out MsgBoxVM vm) {
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
			var vmTmp = vm;
			win.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) => CopyText(vmTmp)));
		}

		static void CopyText(MsgBoxVM vm) {
			try {
				Clipboard.SetText(vm.Message);
			}
			catch (ExternalException) { }
		}

		public T Ask<T>(string labelMessage, string? defaultText, string? title, Func<string, T>? converter, Func<string, string?>? verifier, Window? ownerWindow) {
			var win = new AskDlg();
			if (converter is null)
				converter = CreateDefaultConverter<T>();
			if (verifier is null)
				verifier = CreateDefaultVerifier<T>();

			var vm = new AskVM(labelMessage, s => converter(s), verifier);
			vm.Text = defaultText ?? string.Empty;
			win.DataContext = vm;
			win.Owner = ownerWindow ?? appWindow.MainWindow;
			if (!string.IsNullOrWhiteSpace(title))
				win.Title = title;
			if (win.ShowDialog() != true)
				return default!;
			return (T)vm.Value!;
		}

		Func<string, T> CreateDefaultConverter<T>() {
			var c = TypeDescriptor.GetConverter(typeof(T));
			return s => (T)c.ConvertFromInvariantString(s);
		}

		Func<string, string?> CreateDefaultVerifier<T>() {
			var c = TypeDescriptor.GetConverter(typeof(T));
			return s => {
				if (c.IsValid(s))
					return string.Empty;
				return dnSpy_Resources.InvalidInputTextMessageBox;
			};
		}
	}
}
