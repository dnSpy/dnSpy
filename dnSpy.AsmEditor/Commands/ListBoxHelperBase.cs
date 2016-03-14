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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.Images;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Resources;

namespace dnSpy.AsmEditor.Commands {
	[ExportAutoLoaded]
	sealed class ListBoxHelperBase_ImageManagerLoader : IAutoLoaded {
		[ImportingConstructor]
		ListBoxHelperBase_ImageManagerLoader(IImageManager imageManager) {
			ListBoxHelperBase_ImageManagerLoader.imageManager = imageManager;
		}

		public static IImageManager ImageManager {
			get { return imageManager; }
		}
		static IImageManager imageManager;
	}

	abstract class ListBoxHelperBase<T> where T : class, IIndexedItem {
		protected readonly ListBox listBox;
		protected IndexObservableCollection<T> coll;
		List<ContextMenuHandler> contextMenuHandlers = new List<ContextMenuHandler>();

		static int classCopiedDataId;
		readonly int copiedDataId;
		sealed class ClipboardData {
			public readonly T[] Data;
			public readonly int Id;
			public ClipboardData(T[] data, int id) {
				this.Data = data;
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

		protected ListBoxHelperBase(ListBox listBox) {
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

		protected abstract string AddNewBeforeSelectionMessage { get; }
		protected abstract string AddNewAfterSelectionMessage { get; }
		protected abstract string AppendNewMessage { get; }
		protected abstract string RemoveSingularMessage { get; }
		protected abstract string RemovePluralMessage { get; }
		protected abstract string RemoveAllMessage { get; }

		protected void AddAddNewItemHandlers(string addNewItemIcon = null) {
			if (!coll.CanCreateNewItems)
				return;
			Add(new ContextMenuHandler {
				Header = AddNewBeforeSelectionMessage,
				Command = coll.AddItemBeforeCommand,
				Icon = addNewItemIcon ?? "AddNewItem",
				InputGestureText = "res:ShortCutKeyF",
				Modifiers = ModifierKeys.None,
				Key = Key.F,
			});
			Add(new ContextMenuHandler {
				Header = AddNewAfterSelectionMessage,
				Command = coll.AddItemAfterCommand,
				Icon = addNewItemIcon ?? "AddNewItem",
				InputGestureText = "res:ShortCutKeyC",
				Modifiers = ModifierKeys.None,
				Key = Key.C,
			});
			Add(new ContextMenuHandler {
				Header = AppendNewMessage,
				Command = coll.AppendItemCommand,
				InputGestureText = "res:ShortCutKeyA",
				Modifiers = ModifierKeys.None,
				Key = Key.A,
			});
		}

		protected void AddMoveItemHandlers() {
			if (!coll.CanMoveItems)
				return;
			Add(new ContextMenuHandler {
				Header = "res:MoveUpCommand",
				Command = coll.ItemMoveUpCommand,
				Icon = "ArrowUp",
				InputGestureText = "res:ShortCutKeyU",
				Modifiers = ModifierKeys.None,
				Key = Key.U,
			});
			Add(new ContextMenuHandler {
				Header = "res:MoveDownCommand",
				Command = coll.ItemMoveDownCommand,
				Icon = "ArrowDown",
				InputGestureText = "res:ShortCutKeyD",
				Modifiers = ModifierKeys.None,
				Key = Key.D,
			});
		}

		protected void AddRemoveItemHandlers() {
			if (!coll.CanRemoveItems)
				return;
			Add(new ContextMenuHandler {
				Header = RemoveSingularMessage,
				HeaderPlural = RemovePluralMessage,
				Command = coll.RemoveItemCommand,
				Icon = "Delete",
				InputGestureText = "res:DeleteCommandKey",
				Modifiers = ModifierKeys.None,
				Key = Key.Delete,
			});
			Add(new ContextMenuHandler {
				Header = RemoveAllMessage,
				Command = coll.RemoveAllItemsCommand,
				Icon = "Delete",
				InputGestureText = "res:ShortCutKeyCtrlDel",
				Modifiers = ModifierKeys.Control,
				Key = Key.Delete,
			});
		}

		protected void AddCopyHandlers() {
			Add(new ContextMenuHandler {
				Header = "res:CopyAsTextCommand",
				Command = new RelayCommand(a => CopyItemsAsText((T[])a), a => CopyItemsAsTextCanExecute((T[])a)),
				Icon = "Copy",
				InputGestureText = "res:ShortCutKeyCtrlT",
				Modifiers = ModifierKeys.Control,
				Key = Key.T,
			});
			Add(new ContextMenuHandler {
				Header = "res:CutCommand",
				Command = new RelayCommand(a => CutItems((T[])a), a => CutItemsCanExecute((T[])a)),
				Icon = "Cut",
				InputGestureText = "res:ShortCutKeyCtrlX",
				Modifiers = ModifierKeys.Control,
				Key = Key.X,
			});
			Add(new ContextMenuHandler {
				Header = "res:CopyCommand",
				Command = new RelayCommand(a => CopyItems((T[])a), a => CopyItemsCanExecute((T[])a)),
				Icon = "Copy",
				InputGestureText = "res:ShortCutKeyCtrlC",
				Modifiers = ModifierKeys.Control,
				Key = Key.C,
			});
			Add(new ContextMenuHandler {
				Header = "res:PasteCommand",
				Command = new RelayCommand(a => PasteItems(), a => PasteItemsCanExecute()),
				Icon = "Paste",
				InputGestureText = "res:ShortCutKeyCtrlV",
				Modifiers = ModifierKeys.Control,
				Key = Key.V,
			});
			Add(new ContextMenuHandler {
				Header = "res:PasteAfterSelectionCommand",
				Command = new RelayCommand(a => PasteAfterItems(), a => PasteAfterItemsCanExecute()),
				Icon = "Paste",
				InputGestureText = "res:ShortCutKeyCtrlAltV",
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

		protected static void Add16x16Image(MenuItem menuItem, string icon, bool isCtxMenu, bool? enable = null) {
			ListBoxHelperBase_ImageManagerLoader.ImageManager.Add16x16Image(menuItem, typeof(ListBoxHelperBase<T>).Assembly, icon, isCtxMenu, enable);
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
				menuItem.Header = ResourceHelper.GetString(handler, listBox.SelectedItems.Count > 1 ? handler.HeaderPlural ?? handler.Header : handler.Header);
				var tmpHandler = handler;
				menuItem.Click += (s, e2) => tmpHandler.Command.Execute(parameter);
				if (handler.Icon != null)
					Add16x16Image(menuItem, handler.Icon, true, menuItem.IsEnabled);
				if (handler.InputGestureText != null)
					menuItem.InputGestureText = ResourceHelper.GetString(handler, handler.InputGestureText);

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
			ClipboardDataHolder.Add(new ClipboardData(items, copiedDataId));
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
			var cpData = ClipboardDataHolder.TryGet<ClipboardData>();
			if (cpData == null)
				return null;
			if (!CanUseClipboardData(cpData.Data, cpData.Id == copiedDataId))
				return null;
			return cpData;
		}

		protected virtual bool CanUseClipboardData(T[] data, bool fromThisInstance) {
			return fromThisInstance;
		}

		protected virtual T[] BeforeCopyingData(T[] data, bool fromThisInstance) {
			return data;
		}

		protected virtual void AfterCopyingData(T[] data, T[] origData, bool fromThisInstance) {
		}

		void PasteItems(int relIndex) {
			var cpData = GetClipboardData();
			if (cpData == null)
				return;
			var copiedData = cpData.Data;

			int index = GetPasteIndex(relIndex);
			var origClonedData = CloneData(copiedData);
			copiedData = BeforeCopyingData(origClonedData, cpData.Id == copiedDataId);
			for (int i = 0; i < copiedData.Length; i++)
				coll.Insert(index + i, copiedData[i]);
			AfterCopyingData(copiedData, origClonedData, cpData.Id == copiedDataId);
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
