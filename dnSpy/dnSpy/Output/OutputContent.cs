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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Themes;

namespace dnSpy.Output {
	interface IOutputContent : IUIObjectProvider {
		double ZoomLevel { get; }
	}

	[Export(typeof(IOutputContent))]
	sealed class OutputContent : IOutputContent {
		public object UIObject => OutputControl;
		public IInputElement FocusedElement => OutputService.FocusedElement;
		public FrameworkElement ScaleElement => OutputControl;
		public double ZoomLevel => vmOutput.Value.ZoomLevel;
		IOutputServiceInternal OutputService => vmOutput.Value;

		OutputControl OutputControl {
			get {
				if (outputControl.DataContext == null)
					outputControl.DataContext = OutputService;
				return outputControl;
			}
		}
		readonly OutputControl outputControl;

		readonly Lazy<IOutputServiceInternal> vmOutput;

		[ImportingConstructor]
		OutputContent(IWpfCommandService wpfCommandService, IThemeService themeService, Lazy<IOutputServiceInternal> outputVM) {
			this.outputControl = new OutputControl();
			this.vmOutput = outputVM;
			themeService.ThemeChanged += ThemeService_ThemeChanged;

			wpfCommandService.Add(ControlConstants.GUID_OUTPUT_CONTROL, outputControl);
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_OUTPUT_CONTROL);
			cmds.Add(ApplicationCommands.Save,
				(s, e) => OutputService.SaveText(),
				(s, e) => e.CanExecute = OutputService.CanSaveText);
			cmds.Add(OutputCommands.CopyCommand,
				(s, e) => OutputService.Copy(),
				(s, e) => e.CanExecute = OutputService.CanCopy);
			cmds.Add(OutputCommands.CopyCommand, ModifierKeys.Control, Key.C);
			cmds.Add(OutputCommands.ClearAllCommand,
				(s, e) => OutputService.ClearAll(),
				(s, e) => e.CanExecute = OutputService.CanClearAll);
			cmds.Add(OutputCommands.ClearAllCommand, ModifierKeys.Control, Key.L);
			cmds.Add(OutputCommands.ToggleWordWrapCommand,
				(s, e) => OutputService.WordWrap = !OutputService.WordWrap,
				(s, e) => e.CanExecute = true);
			cmds.Add(OutputCommands.ToggleShowLineNumbersCommand,
				(s, e) => OutputService.ShowLineNumbers = !OutputService.ShowLineNumbers,
				(s, e) => e.CanExecute = true);
			cmds.Add(OutputCommands.ToggleShowTimestampsCommand,
				(s, e) => OutputService.ShowTimestamps = !OutputService.ShowTimestamps,
				(s, e) => e.CanExecute = true);
			for (int i = 0; i < OutputCommands.SelectLogWindowCommands.Length; i++) {
				int tmpIndex = i;
				cmds.Add(OutputCommands.SelectLogWindowCommands[i],
					(s, e) => SelectLog(tmpIndex),
					(s, e) => e.CanExecute = OutputService.CanSelectLog(tmpIndex));
			}

			outputControl.PreviewKeyDown += OutputControl_PreviewKeyDown;
		}

		void OutputControl_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (!waitingForSecondKey && e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.E) {
				waitingForSecondKey = true;
				e.Handled = true;
				return;
			}
			if (waitingForSecondKey && (e.KeyboardDevice.Modifiers == ModifierKeys.Control || e.KeyboardDevice.Modifiers == ModifierKeys.None) && e.Key == Key.W) {
				waitingForSecondKey = false;
				e.Handled = true;
				OutputCommands.ToggleWordWrapCommand.Execute(null, outputControl);
				return;
			}

			waitingForSecondKey = false;
		}
		bool waitingForSecondKey;

		void SelectLog(int tmpIndex) {
			var vm = OutputService.SelectLog(tmpIndex);
			Debug.Assert(vm != null);
			if (vm == null)
				return;
			vm.FocusedElement?.Focus();
			// Must use Loaded prio or the normal text editor could get focus
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
				if (OutputService.SelectedOutputBufferVM == vm)
					vm.FocusedElement?.Focus();
			}));
		}

		void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e) => OutputService.RefreshThemeFields();
	}
}
