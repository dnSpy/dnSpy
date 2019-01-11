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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	abstract class ExceptionsOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanAddException { get; }
		public abstract void AddException();
		public abstract bool CanRemoveExceptions { get; }
		public abstract void RemoveExceptions();
		public abstract bool CanEditConditions { get; }
		public abstract void EditConditions();
		public abstract bool CanRestoreSettings { get; }
		public abstract void RestoreSettings();
		public abstract bool CanResetSearchSettings { get; }
		public abstract void ResetSearchSettings();
		public abstract bool CanToggleMatchingExceptions { get; }
		public abstract void ToggleMatchingExceptions();
		public abstract bool CanToggleBreakWhenThrown { get; }
		public abstract void ToggleBreakWhenThrown();
	}

	[Export(typeof(ExceptionsOperations))]
	sealed class ExceptionsOperationsImpl : ExceptionsOperations {
		readonly IAppWindow appWindow;
		readonly IExceptionsVM exceptionsVM;
		readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;

		BulkObservableCollection<ExceptionVM> AllItems => exceptionsVM.AllItems;
		ObservableCollection<ExceptionVM> SelectedItems => exceptionsVM.SelectedItems;
		IEnumerable<ExceptionVM> SortedSelectedItems => exceptionsVM.Sort(SelectedItems);

		[ImportingConstructor]
		ExceptionsOperationsImpl(IAppWindow appWindow, IExceptionsVM exceptionsVM, Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService) {
			this.appWindow = appWindow;
			this.exceptionsVM = exceptionsVM;
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new DbgStringBuilderTextWriter();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				bool needTab = false;
				foreach (var column in exceptionsVM.Descs.Columns) {
					if (!column.IsVisible)
						continue;
					if (column.Name == string.Empty)
						continue;

					if (needTab)
						output.Write(DbgTextColor.Text, "\t");
					switch (column.Id) {
					case ExceptionsWindowColumnIds.BreakWhenThrown:
						formatter.WriteName(output, vm);
						break;

					case ExceptionsWindowColumnIds.Category:
						formatter.WriteCategory(output, vm);
						break;

					case ExceptionsWindowColumnIds.Conditions:
						formatter.WriteConditions(output, vm);
						break;

					default:
						throw new InvalidOperationException();
					}

					needTab = true;
				}
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanSelectAll => SelectedItems.Count != AllItems.Count;
		public override void SelectAll() {
			SelectedItems.Clear();
			foreach (var vm in AllItems)
				SelectedItems.Add(vm);
		}

		public override bool CanAddException => dbgExceptionSettingsService.Value.CategoryDefinitions.Count > 0;
		public override void AddException() => exceptionsVM.IsAddingExceptions = !exceptionsVM.IsAddingExceptions;

		public override bool CanRemoveExceptions => SelectedItems.Count > 0;
		public override void RemoveExceptions() {
			var ids = SelectedItems.Select(a => a.Definition.Id).ToArray();
			dbgExceptionSettingsService.Value.Remove(ids);
		}

		public override bool CanEditConditions => SelectedItems.Count > 0;
		public override void EditConditions() {
			if (SelectedItems.Count == 0)
				return;

			var dlg = new EditExceptionConditionsDlg();
			var vm = new EditExceptionConditionsVM(SortedSelectedItems.First().Settings.Conditions);
			dlg.DataContext = vm;
			dlg.Owner = appWindow.MainWindow;
			var res = dlg.ShowDialog();
			if (res != true)
				return;

			var newConditions = vm.GetConditions();
			var newSettings = new DbgExceptionIdAndSettings[SelectedItems.Count];
			for (int i = 0; i < newSettings.Length; i++) {
				var item = SelectedItems[i];
				var flags = item.Settings.Flags | DbgExceptionDefinitionFlags.StopFirstChance;
				var settings = new DbgExceptionSettings(flags, newConditions);
				newSettings[i] = new DbgExceptionIdAndSettings(item.Definition.Id, settings);
			}
			dbgExceptionSettingsService.Value.Modify(newSettings);
		}

		public override bool CanRestoreSettings => true;
		public override void RestoreSettings() {
			dbgExceptionSettingsService.Value.Reset();
			ResetSearchSettings();
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => exceptionsVM.ResetSearchSettings();

		public override bool CanToggleMatchingExceptions => AllItems.Count > 0;
		public override void ToggleMatchingExceptions() => ToggleBreakWhenThrown(AllItems);

		public override bool CanToggleBreakWhenThrown => SelectedItems.Count > 0;
		public override void ToggleBreakWhenThrown() => ToggleBreakWhenThrown(SelectedItems);

		void ToggleBreakWhenThrown(IList<ExceptionVM> exceptions) {
			bool allSet = exceptions.All(a => a.BreakWhenThrown);
			var newSettings = new DbgExceptionIdAndSettings[exceptions.Count];
			for (int i = 0; i < newSettings.Length; i++) {
				var vm = exceptions[i];
				var flags = vm.Settings.Flags;
				if (allSet)
					flags &= ~DbgExceptionDefinitionFlags.StopFirstChance;
				else
					flags |= DbgExceptionDefinitionFlags.StopFirstChance;
				var settings = new DbgExceptionSettings(flags, vm.Settings.Conditions);
				newSettings[i] = new DbgExceptionIdAndSettings(vm.Definition.Id, settings);
			}
			dbgExceptionSettingsService.Value.Modify(newSettings);
		}
	}
}
