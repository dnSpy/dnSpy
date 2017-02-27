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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Debugger.UI {
	sealed class ListBoxSelectedItemsAP {
		public static readonly DependencyProperty SelectedItemsVMProperty = DependencyProperty.RegisterAttached(
			"SelectedItemsVM", typeof(IList), typeof(ListBoxSelectedItemsAP), new UIPropertyMetadata(null, SelectedItemsVMPropertyChangedCallback));
		static readonly DependencyProperty InstanceProperty = DependencyProperty.RegisterAttached(
			"Instance", typeof(ListBoxSelectedItemsAP), typeof(ListBoxSelectedItemsAP), new UIPropertyMetadata(null));

		public static void SetSelectedItemsVM(ListBox listBox, IList value) =>
			listBox.SetValue(SelectedItemsVMProperty, value);
		public static IList GetSelectedItemsVM(ListBox listBox) =>
			(IList)listBox.GetValue(SelectedItemsVMProperty);

		static void SelectedItemsVMPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var lb = d as ListBox;
			if (lb == null)
				return;
			var vmColl = e.NewValue as IList;
			if (vmColl == null)
				return;
			var ncc = vmColl as INotifyCollectionChanged;
			if (ncc == null)
				return;

			var inst = lb.GetValue(InstanceProperty) as ListBoxSelectedItemsAP;
			if (inst == null)
				lb.SetValue(InstanceProperty, inst = new ListBoxSelectedItemsAP());
			inst.Initialize(lb, vmColl, ncc);
		}

		ListBox listBox;
		IList vmColl;
		INotifyCollectionChanged vmCollNcc;

		void Initialize(ListBox listBox, IList vmColl, INotifyCollectionChanged vmCollNcc) {
			UnregisterEvents();
			this.listBox = listBox;
			this.vmColl = vmColl;
			this.vmCollNcc = vmCollNcc;
			vmColl.Clear();
			foreach (var item in listBox.SelectedItems)
				vmColl.Add(item);
			RegisterEvents();
		}

		void UnregisterEvents() {
			if (listBox != null)
				listBox.SelectionChanged -= ListBox_SelectionChanged;
			if (vmCollNcc != null)
				vmCollNcc.CollectionChanged -= VmCollNcc_CollectionChanged;
		}

		void RegisterEvents() {
			listBox.SelectionChanged += ListBox_SelectionChanged;
			vmCollNcc.CollectionChanged += VmCollNcc_CollectionChanged;
		}

		void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (ListBox_SelectionChanged_ignoreCalls)
				return;
			try {
				VmCollNcc_CollectionChanged_ignoreCalls = true;
				if (e.AddedItems != null) {
					foreach (var vmItem in e.AddedItems)
						vmColl.Add(vmItem);
				}
				if (e.RemovedItems != null) {
					foreach (var vmItem in e.RemovedItems)
						vmColl.Remove(vmItem);
				}
			}
			finally {
				VmCollNcc_CollectionChanged_ignoreCalls = false;
			}
		}
		bool ListBox_SelectionChanged_ignoreCalls;
		bool VmCollNcc_CollectionChanged_ignoreCalls;

		void VmCollNcc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (VmCollNcc_CollectionChanged_ignoreCalls)
				return;
			try {
				ListBox_SelectionChanged_ignoreCalls = true;
				switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					Debug.Assert(e.NewItems != null);
					if (e.NewItems != null) {
						foreach (var item in e.NewItems)
							listBox.SelectedItems.Add(item);
					}
					break;

				case NotifyCollectionChangedAction.Remove:
					Debug.Assert(e.OldItems != null);
					if (e.OldItems != null) {
						foreach (var item in e.OldItems)
							listBox.SelectedItems.Remove(item);
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					listBox.SelectedItems.Clear();
					if (e.NewItems != null) {
						foreach (var item in e.NewItems)
							listBox.SelectedItems.Add(item);
					}
					break;

				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
				default:
					throw new InvalidOperationException();
				}
			}
			finally {
				ListBox_SelectionChanged_ignoreCalls = false;
			}
		}
	}
}
