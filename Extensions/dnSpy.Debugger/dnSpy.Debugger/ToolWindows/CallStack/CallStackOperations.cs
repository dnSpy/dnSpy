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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	abstract class CallStackOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanGoToSourceCode { get; }
		public abstract void GoToSourceCode(bool newTab);
		public abstract bool ShowReturnTypes { get; set; }
		public abstract bool ShowParameterTypes { get; set; }
		public abstract bool ShowParameterNames { get; set; }
		public abstract bool ShowParameterValues { get; set; }
		public abstract bool ShowFunctionOffset { get; set; }
		public abstract bool ShowModuleNames { get; set; }
		public abstract bool ShowDeclaringTypes { get; set; }
		public abstract bool ShowNamespaces { get; set; }
		public abstract bool ShowIntrinsicTypeKeywords { get; set; }
		public abstract bool ShowTokens { get; set; }
	}

	[Export(typeof(CallStackOperations))]
	sealed class CallStackOperationsImpl : CallStackOperations {
		readonly ICallStackVM callStackVM;
		readonly CallStackDisplaySettings callStackDisplaySettings;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;

		ObservableCollection<StackFrameVM> AllItems => callStackVM.AllItems;
		ObservableCollection<StackFrameVM> SelectedItems => callStackVM.SelectedItems;
		IEnumerable<StackFrameVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Index);

		[ImportingConstructor]
		CallStackOperationsImpl(ICallStackVM callStackVM, CallStackDisplaySettings callStackDisplaySettings, Lazy<ReferenceNavigatorService> referenceNavigatorService) {
			this.callStackVM = callStackVM;
			this.callStackDisplaySettings = callStackDisplaySettings;
			this.referenceNavigatorService = referenceNavigatorService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteImage(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteName(output, vm);
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

		public override bool CanGoToSourceCode => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void GoToSourceCode(bool newTab) {
			if (!CanGoToSourceCode)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			var options = newTab ? new object[] { PredefinedReferenceNavigatorOptions.NewTab } : Array.Empty<object>();
			referenceNavigatorService.Value.GoTo(vm.Frame, options);
		}

		public override bool ShowReturnTypes {
			get => callStackDisplaySettings.ShowReturnTypes;
			set => callStackDisplaySettings.ShowReturnTypes = value;
		}

		public override bool ShowParameterTypes {
			get => callStackDisplaySettings.ShowParameterTypes;
			set => callStackDisplaySettings.ShowParameterTypes = value;
		}

		public override bool ShowParameterNames {
			get => callStackDisplaySettings.ShowParameterNames;
			set => callStackDisplaySettings.ShowParameterNames = value;
		}

		public override bool ShowParameterValues {
			get => callStackDisplaySettings.ShowParameterValues;
			set => callStackDisplaySettings.ShowParameterValues = value;
		}

		public override bool ShowFunctionOffset {
			get => callStackDisplaySettings.ShowFunctionOffset;
			set => callStackDisplaySettings.ShowFunctionOffset = value;
		}

		public override bool ShowModuleNames {
			get => callStackDisplaySettings.ShowModuleNames;
			set => callStackDisplaySettings.ShowModuleNames = value;
		}

		public override bool ShowDeclaringTypes {
			get => callStackDisplaySettings.ShowDeclaringTypes;
			set => callStackDisplaySettings.ShowDeclaringTypes = value;
		}

		public override bool ShowNamespaces {
			get => callStackDisplaySettings.ShowNamespaces;
			set => callStackDisplaySettings.ShowNamespaces = value;
		}

		public override bool ShowIntrinsicTypeKeywords {
			get => callStackDisplaySettings.ShowIntrinsicTypeKeywords;
			set => callStackDisplaySettings.ShowIntrinsicTypeKeywords = value;
		}

		public override bool ShowTokens {
			get => callStackDisplaySettings.ShowTokens;
			set => callStackDisplaySettings.ShowTokens = value;
		}
	}
}
