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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts;
using dnSpy.Shared.UI.Images;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.AsmEditor {
	abstract class ListBoxHelperBase<T> where T : class, IIndexedItem {
		readonly string menuTypeName;
		protected readonly ListBox listBox;
		protected IndexObservableCollection<T> coll;
		List<ContextMenuHandler> contextMenuHandlers = new List<ContextMenuHandler>();

		// Data on the clipboard must be serializable but our data isn't and would require too
		// much work to fix so store the copied data here and serializable data in the clipboard.
		T[] copiedData;
		readonly int copiedDataId;  // unique id per instance
		static int classCopiedDataId;
		static readonly string DataFormat = typeof(ListBoxHelperBase<T>).ToString();
		[Serializable]
		sealed class ClipboardData {
			public readonly int Id;
			public ClipboardData(int id) {
				this.Id = id;
			}
		}

		sealed class MyCommand : ICommand {
			readonly ListBoxHelperBase<T> owner;
			readonly ICommand cmd;

			public MyCommand(ListBoxHelperBase<T> owner, ICommand cmd) {
				this.owner = owner;
				this.cmd = cmd;
			}

			public bool CanExecute(object parameter) {
				return cmd.CanExecute(owner.GetSelectedItems());
			}

			public event EventHandler CanExecuteChanged {
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public void Execute(object parameter) {
				cmd.Execute(owner.GetSelectedItems());
			}
		}

		protected abstract T[] GetSelectedItems();
		protected abstract void CopyItemsAsText(T[] items);
		protected abstract void OnDataContextChangedInternal(object dataContext);

		protected virtual bool CopyItemsAsTextCanExecute(T[] items) {
			return items.Length > 0;
		}

		protected ListBoxHelperBase(ListBox listBox, string menuTypeName) {
			this.menuTypeName = menuTypeName;
			this.listBox = listBox;
			this.listBox.ContextMenu = new ContextMenu();
			this.listBox.ContextMenuOpening += (s, e) => ShowContextMenu(e, listBox, contextMenuHandlers, GetSelectedItems());
			this.copiedDataId = Interlocked.Increment(ref classCopiedDataId);
		}

		protected void AddSeparator() {
			contextMenuHandlers.Add(null);
		}

		protected void Add(ContextMenuHandler handler) {
			contextMenuHandlers.Add(handler);
		}

		protected void AddStandardMenuHandlers(string addNewItemIcon = null) {
			AddAddNewItemHandlers(addNewItemIcon);
			AddSeparator();
			AddMoveItemHandlers();
			AddSeparator();
			AddRemoveItemHandlers();
			AddSeparator();
			AddCopyHandlers();
		}

		protected void AddAddNewItemHandlers(string addNewItemIcon = null) {
			if (!coll.CanCreateNewItems)
				return;
			Add(new ContextMenuHandler {
				Header = string.Format("Add New {0} Be_fore Selection", menuTypeName),
				Command = coll.AddItemBeforeCommand,
				Icon = addNewItemIcon ?? "AddNewItem",
				InputGestureText = "F",
				Modifiers = ModifierKeys.None,
				Key = Key.F,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("Add New {0} After Sele_ction", menuTypeName),
				Command = coll.AddItemAfterCommand,
				Icon = addNewItemIcon ?? "AddNewItem",
				InputGestureText = "C",
				Modifiers = ModifierKeys.None,
				Key = Key.C,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("_Append New {0}", menuTypeName),
				Command = coll.AppendItemCommand,
				InputGestureText = "A",
				Modifiers = ModifierKeys.None,
				Key = Key.A,
			});
		}

		protected void AddMoveItemHandlers() {
			if (!coll.CanMoveItems)
				return;
			Add(new ContextMenuHandler {
				Header = "Move _Up",
				Command = coll.ItemMoveUpCommand,
				Icon = "ArrowUp",
				InputGestureText = "U",
				Modifiers = ModifierKeys.None,
				Key = Key.U,
			});
			Add(new ContextMenuHandler {
				Header = "Move _Down",
				Command = coll.ItemMoveDownCommand,
				Icon = "ArrowDown",
				InputGestureText = "D",
				Modifiers = ModifierKeys.None,
				Key = Key.D,
			});
		}

		protected void AddRemoveItemHandlers() {
			if (!coll.CanRemoveItems)
				return;
			Add(new ContextMenuHandler {
				Header = string.Format("_Remove {0}", menuTypeName),
				HeaderPlural = string.Format("_Remove {0}s", menuTypeName),
				Command = coll.RemoveItemCommand,
				Icon = "Delete",
				InputGestureText = "Del",
				Modifiers = ModifierKeys.None,
				Key = Key.Delete,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("Remo_ve All {0}s", menuTypeName),
				Command = coll.RemoveAllItemsCommand,
				Icon = "Delete",
				InputGestureText = "Ctrl+Del",
				Modifiers = ModifierKeys.Control,
				Key = Key.Delete,
			});
		}

		protected void AddCopyHandlers() {
			Add(new ContextMenuHandler {
				Header = "Copy as Te_xt",
				Command = new RelayCommand(a => CopyItemsAsText((T[])a), a => CopyItemsAsTextCanExecute((T[])a)),
				Icon = "Copy",
				InputGestureText = "Ctrl+T",
				Modifiers = ModifierKeys.Control,
				Key = Key.T,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("Cu_t"),
				Command = new RelayCommand(a => CutItems((T[])a), a => CutItemsCanExecute((T[])a)),
				Icon = "Cut",
				InputGestureText = "Ctrl+X",
				Modifiers = ModifierKeys.Control,
				Key = Key.X,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("Cop_y"),
				Command = new RelayCommand(a => CopyItems((T[])a), a => CopyItemsCanExecute((T[])a)),
				Icon = "Copy",
				InputGestureText = "Ctrl+C",
				Modifiers = ModifierKeys.Control,
				Key = Key.C,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("_Paste"),
				Command = new RelayCommand(a => PasteItems(), a => PasteItemsCanExecute()),
				Icon = "Paste",
				InputGestureText = "Ctrl+V",
				Modifiers = ModifierKeys.Control,
				Key = Key.V,
			});
			Add(new ContextMenuHandler {
				Header = string.Format("Paste After Se_lection"),
				Command = new RelayCommand(a => PasteAfterItems(), a => PasteAfterItemsCanExecute()),
				Icon = "Paste",
				InputGestureText = "Ctrl+Alt+V",
				Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
				Key = Key.V,
			});
		}

		public void OnDataContextChanged(object dataContext) {
			if (coll != null)
				throw new InvalidOperationException("DataContext changed more than once");

			// Can't add M, N etc as shortcuts so must use a key down handler
			listBox.KeyDown += listBox_KeyDown;

			OnDataContextChangedInternal(dataContext);

			foreach (var handler in contextMenuHandlers) {
				if (handler == null)
					continue;
				if (handler.Modifiers == ModifierKeys.None &&
					(Key.A <= handler.Key && handler.Key <= Key.Z))
					continue;

				listBox.InputBindings.Add(new KeyBinding(new MyCommand(this, handler.Command), handler.Key, handler.Modifiers));
			}
		}

		void listBox_KeyDown(object sender, KeyEventArgs e) {
			if (e.OriginalSource is TextBox || e.OriginalSource is ComboBox || e.OriginalSource is ComboBoxItem)
				return;

			if (Keyboard.Modifiers != ModifierKeys.None)
				return;

			foreach (var handler in contextMenuHandlers) {
				if (handler == null)
					continue;
				if (handler.Modifiers != Keyboard.Modifiers)
					continue;
				if (handler.Key != e.Key)
					continue;

				var items = GetSelectedItems();
				if (handler.Command.CanExecute(items)) {
					handler.Command.Execute(items);
					e.Handled = true;
					return;
				}
				// Make sure that no default keyboard handler gets called
				e.Handled = true;
				break;
			}
		}

		static void ShowContextMenu(ContextMenuEventArgs e, ListBox listBox, IList<ContextMenuHandler> handlers, object parameter) {
			var ctxMenu = new ContextMenu();

			bool addSep = false;
			foreach (var handler in handlers) {
				if (handler == null) {
					addSep = true;
					continue;
				}

				var menuItem = new MenuItem();
				menuItem.IsEnabled = handler.Command.CanExecute(parameter);
				menuItem.Header = listBox.SelectedItems.Count > 1 ? handler.HeaderPlural ?? handler.Header : handler.Header;
				var tmpHandler = handler;
				menuItem.Click += (s, e2) => tmpHandler.Command.Execute(parameter);
				if (handler.Icon != null)
					Globals.App.ImageManager.Add16x16Image(menuItem, typeof(ListBoxHelperBase<T>).Assembly, handler.Icon, true, menuItem.IsEnabled);
				if (handler.InputGestureText != null)
					menuItem.InputGestureText = handler.InputGestureText;

				if (addSep) {
					if (ctxMenu.Items.Count > 0 && !(ctxMenu.Items[ctxMenu.Items.Count - 1] is Separator))
						ctxMenu.Items.Add(new Separator());
					addSep = false;
				}
				ctxMenu.Items.Add(menuItem);
			}

			if (ctxMenu.Items.Count != 0)
				listBox.ContextMenu = ctxMenu;
			else
				e.Handled = true;
		}

		void AddToClipboard(T[] items) {
			copiedData = items;
			Clipboard.SetDataObject(new DataObject(DataFormat, new ClipboardData(copiedDataId)), false);
		}

		static T[] SortClipboardItems(T[] items) {
			// We must sort the items since they're not sorted when you'd think they're
			// sorted, eg. when pressing Ctrl+A to select everything.
			Array.Sort(items, (a, b) => a.Index.CompareTo(b.Index));
			return items;
		}

		void CutItems(T[] items) {
			var list = SortClipboardItems(items).ToList();
			for (int i = list.Count - 1; i >= 0; i--) {
				var item = list[i];
				Debug.Assert(item.Index >= 0 && item.Index < coll.Count && item == coll[item.Index]);
				coll.RemoveAt(item.Index);
			}
			AddToClipboard(items);
		}

		bool CutItemsCanExecute(T[] items) {
			return items.Length > 0;
		}

		static T[] CloneData(T[] items) {
			return items.Select(a => (T)a.Clone()).ToArray();
		}

		void CopyItems(T[] items) {
			AddToClipboard(CloneData(SortClipboardItems(items)));
		}

		bool CopyItemsCanExecute(T[] items) {
			return items.Length > 0;
		}

		int GetPasteIndex(int relIndex) {
			int index = listBox.SelectedIndex;
			if (index < 0 || index > coll.Count)
				index = coll.Count;
			index += relIndex;
			if (index < 0 || index > coll.Count)
				index = coll.Count;
			return index;
		}

		ClipboardData GetClipboardData() {
			if (copiedData == null)
				return null;
			if (!Clipboard.ContainsData(DataFormat))
				return null;
			var data = Clipboard.GetData(DataFormat) as ClipboardData;
			if (data == null)
				return null;
			if (data.Id != copiedDataId)
				return null;

			return data;
		}

		void PasteItems(int relIndex) {
			var data = GetClipboardData();
			if (data == null)
				return;

			int index = GetPasteIndex(relIndex);
			for (int i = 0; i < copiedData.Length; i++)
				coll.Insert(index + i, copiedData[i]);

			copiedData = CloneData(copiedData);
		}

		void PasteItems() {
			PasteItems(0);
		}

		bool PasteItemsCanExecute() {
			return GetClipboardData() != null;
		}

		void PasteAfterItems() {
			PasteItems(1);
		}

		bool PasteAfterItemsCanExecute() {
			return listBox.SelectedIndex >= 0 &&
				GetClipboardData() != null;
		}
	}
}
