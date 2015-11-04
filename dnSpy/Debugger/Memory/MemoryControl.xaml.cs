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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Memory {
	[Export(typeof(IPaneCreator))]
	sealed class MemoryControlCreator : IPaneCreator {
		// Four should be enough, but if more are required, it's as simple as changing this constant.
		// The first 10 will have keyboard shortcuts (Ctrl+N or Ctrl+Alt+N) depending on the command.
		public const int NUMBER_OF_MEMORY_WINDOWS = 4;

		MemoryControlCreator() {
		}

		public IPane Create(string name) {
			for (int i = 0; i < NUMBER_OF_MEMORY_WINDOWS; i++) {
				var pname = MemoryControl.IndexToPaneName(i);
				if (name == pname)
					return GetMemoryControlInstance(i);
			}
			return null;
		}

		internal static string GetHeaderText(int i) {
			if (i == 9)
				return "Memory 1_0";
			if (0 <= i && i <= 8)
				return string.Format("Memory _{0}", (i + 1) % 10);
			return string.Format("Memory {0}", i + 1);
		}

		internal static string GetCtrlInputGestureText(int i) {
			if (0 <= i && i <= 9)
				return string.Format("Ctrl+{0}", (i + 1) % 10);
			return string.Empty;
		}

		internal static MemoryControl GetMemoryControlInstance(int index) {
			var mc = memoryControls[index];
			if (mc == null) {
				memoryControls[index] = mc = new MemoryControl(index);
				mc.DataContext = new MemoryVM(mc.RefreshLines);
				mc.hexBox.StartOffset = 0;
				mc.hexBox.EndOffset = IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue;
			}
			return mc;
		}

		static MemoryControl[] memoryControls = new MemoryControl[NUMBER_OF_MEMORY_WINDOWS];
	}

	public partial class MemoryControl : UserControl, IPane {
		const string BASE_PANE_TYPE_NAME = "memory window";

		internal static string IndexToPaneName(int index) {
			return BASE_PANE_TYPE_NAME + (index + 1).ToString();
		}

		public int Index {
			get { return index; }
		}
		readonly int index;

		public MemoryControl(int index) {
			this.index = index;
			InitializeComponent();
		}

		public ICommand ShowCommand {
			get { return new RelayCommand(a => Show(), a => CanShow); }
		}

		string IPane.PaneName {
			get { return IndexToPaneName(index); }
		}

		string IPane.PaneTitle {
			get { return string.Format("Memory {0}", index + 1); }
		}

		void IPane.Closed() {
			var vm = DataContext as MemoryVM;
			if (vm != null)
				vm.IsEnabled = false;
		}

		void IPane.Opened() {
			var vm = DataContext as MemoryVM;
			if (vm != null)
				vm.IsEnabled = true;
		}

		internal bool CanShow {
			get { return DebugManager.Instance.IsDebugging; }
		}

		internal void Show() {
			if (!MainWindow.Instance.IsBottomPaneVisible(this))
				MainWindow.Instance.ShowInBottomPane(this);
			FocusPane();
		}

		public void FocusPane() {
			UIUtils.Focus(this.hexBox);
		}

		internal void RefreshLines() {
			hexBox.RedrawModifiedLines();
		}
	}
}
