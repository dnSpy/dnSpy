/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Text;

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
	}

	[Export(typeof(ExceptionsOperations))]
	sealed class ExceptionsOperationsImpl : ExceptionsOperations {
		readonly IExceptionsVM exceptionsVM;
		readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;

		BulkObservableCollection<ExceptionVM> AllItems => exceptionsVM.AllItems;
		ObservableCollection<ExceptionVM> SelectedItems => exceptionsVM.SelectedItems;
		//TODO: This should be view order
		IEnumerable<ExceptionVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Order);

		[ImportingConstructor]
		ExceptionsOperationsImpl(IExceptionsVM exceptionsVM, Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService) {
			this.exceptionsVM = exceptionsVM;
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			var debugWriter = new DebugOutputWriterImpl(output);
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteName(output, debugWriter, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteGroup(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteConditions(output, vm);
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

		public override bool CanAddException => true;
		public override void AddException() {
			//TODO:
		}

		public override bool CanRemoveExceptions => SelectedItems.Count > 0;
		public override void RemoveExceptions() {
			var ids = SelectedItems.Select(a => a.Definition.Id).ToArray();
			dbgExceptionSettingsService.Value.Remove(ids);
		}

		public override bool CanEditConditions => SelectedItems.Count > 0;
		public override void EditConditions() {
			//TODO: All selected items should get the same edited conditions
		}

		public override bool CanRestoreSettings => true;
		public override void RestoreSettings() {
			dbgExceptionSettingsService.Value.Reset();
			ResetSearchSettings();
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => exceptionsVM.ResetSearchSettings();
	}
}
