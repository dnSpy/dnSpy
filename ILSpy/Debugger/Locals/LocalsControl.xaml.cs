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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.MVVM;
using dnSpy.Tabs;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.Debugger.Locals {
	interface IMethodLocalProvider {
		void GetMethodInfo(MethodKey method, out Parameter[] parameters, out Local[] locals, out ILVariable[] decLocals);
		event EventHandler NewMethodInfoAvailable;
	}

	[Export(typeof(IPaneCreator))]
	sealed class LocalsControlCreator : IPaneCreator {
		sealed class MethodLocalProvider : IMethodLocalProvider {
			public static readonly MethodLocalProvider Instance = new MethodLocalProvider();

			MethodLocalProvider() {
				MainWindow.Instance.ExecuteWhenLoaded(() => {
					MainWindow.Instance.OnTabStateChanged += (sender, e) => OnTabStateChanged(e.OldTabState, e.NewTabState);
					foreach (var tabState in MainWindow.Instance.AllVisibleDecompileTabStates)
						OnTabStateChanged(null, tabState);
				});
			}

			void OnTabStateChanged(TabState oldTabState, TabState newTabState) {
				var oldTsd = oldTabState as DecompileTabState;
				if (oldTsd != null)
					oldTsd.TextView.OnShowOutput -= DecompilerTextView_OnShowOutput;
				var newTsd = newTabState as DecompileTabState;
				if (newTsd != null)
					newTsd.TextView.OnShowOutput += DecompilerTextView_OnShowOutput;
			}

			void DecompilerTextView_OnShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
				if (NewMethodInfoAvailable != null)
					NewMethodInfoAvailable(this, EventArgs.Empty);
			}

			public event EventHandler NewMethodInfoAvailable;

			public void GetMethodInfo(MethodKey key, out Parameter[] parameters, out Local[] locals, out ILVariable[] decLocals) {
				parameters = null;
				locals = null;
				decLocals = null;

				foreach (var textView in MainWindow.Instance.AllTextViews) {
					if (parameters != null && decLocals != null)
						break;

					var cm = textView.CodeMappings;
					if (cm == null)
						continue;
					MemberMapping mapping;
					if (!cm.TryGetValue(key, out mapping))
						continue;
					var method = mapping.MethodDefinition;
					if (mapping.LocalVariables != null && method.Body != null) {
						locals = method.Body.Variables.ToArray();
						decLocals = new ILVariable[method.Body.Variables.Count];
						foreach (var v in mapping.LocalVariables) {
							if (v.IsGenerated)
								continue;
							if (v.OriginalVariable == null)
								continue;
							if ((uint)v.OriginalVariable.Index >= decLocals.Length)
								continue;
							decLocals[v.OriginalVariable.Index] = v;
						}
					}

					parameters = method.Parameters.ToArray();
				}
			}
		}

		LocalsControlCreator() {
		}

		public IPane Create(string name) {
			if (name == LocalsControl.PANE_TYPE_NAME)
				return LocalsControlInstance;
			return null;
		}

		internal static LocalsControl LocalsControlInstance {
			get {
				if (localsControl == null) {
					localsControl = new LocalsControl();
					var vm = new LocalsVM(MethodLocalProvider.Instance);
					localsControl.DataContext = vm;
					InitializeCommandShortcuts(localsControl.treeView);
					DebugManager.Instance.ProcessRunning += DebugManager_ProcessRunning;
					DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
				}
				return localsControl;
			}
		}

		static void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SyntaxHighlightLocals") {
				var vm = LocalsControlInstance.DataContext as LocalsVM;
				if (vm != null)
					vm.RefreshSyntaxHighlightFields();
			}
		}

		static void DebugManager_ProcessRunning(object sender, EventArgs e) {
			var vm = LocalsControlInstance.DataContext as LocalsVM;
			if (vm != null)
				vm.InitializeLocals();
		}

		static LocalsControl localsControl;

		static void InitializeCommandShortcuts(ListView listView) {
			listView.AddCommandBinding(ApplicationCommands.Copy, new LocalsCtxMenuCommandProxy(new CopyLocalsCtxMenuCommand()));
			listView.InputBindings.Add(new KeyBinding(new LocalsCtxMenuCommandProxy(new EditValueLocalsCtxMenuCommand()), Key.F2, ModifierKeys.None));
			listView.InputBindings.Add(new KeyBinding(new LocalsCtxMenuCommandProxy(new CopyValueLocalsCtxMenuCommand()), Key.C, ModifierKeys.Control | ModifierKeys.Shift));
		}
	}

	public partial class LocalsControl : UserControl, IPane {
		public static readonly string PANE_TYPE_NAME = "locals window";

		public LocalsControl() {
			InitializeComponent();
			MainWindow.InitializeTreeView(treeView, true);
			dntheme.Themes.ThemeChanged += Themes_ThemeChanged;
		}

		public ICommand ShowCommand {
			get { return new RelayCommand(a => Show(), a => CanShow); }
		}

		void Themes_ThemeChanged(object sender, EventArgs e) {
			var vm = DataContext as LocalsVM;
			if (vm != null)
				vm.RefreshThemeFields();
		}

		string IPane.PaneName {
			get { return PANE_TYPE_NAME; }
		}

		string IPane.PaneTitle {
			get { return "Locals"; }
		}

		void IPane.Closed() {
			var vm = DataContext as LocalsVM;
			if (vm != null)
				vm.IsEnabled = false;
		}

		void IPane.Opened() {
			var vm = DataContext as LocalsVM;
			if (vm != null)
				vm.IsEnabled = true;
		}

		bool CanShow {
			get { return DebugManager.Instance.IsDebugging; }
		}

		void Show() {
			if (!MainWindow.Instance.IsBottomPaneVisible(this))
				MainWindow.Instance.ShowInBottomPane(((IPane)this).PaneTitle, this);
			UIUtils.FocusSelector(treeView);
		}
	}
}
