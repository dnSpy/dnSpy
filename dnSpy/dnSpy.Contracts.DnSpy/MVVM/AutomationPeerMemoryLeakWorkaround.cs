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

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Workaround for an <see cref="AutomationPeer"/> memory leak.
	/// Use it on all long-lived <see cref="ItemsControl"/>s (eg. <see cref="ListView"/>, <see cref="ListBox"/>, etc)
	/// </summary>
	public sealed class AutomationPeerMemoryLeakWorkaround {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static readonly DependencyProperty InitializeProperty = DependencyProperty.RegisterAttached(
			"Initialize", typeof(bool), typeof(AutomationPeerMemoryLeakWorkaround), new UIPropertyMetadata(false, InitializePropertyChangedCallback));

		public static void SetInitialize(ItemsControl element, bool value) => element.SetValue(InitializeProperty, value);
		public static bool GetInitialize(ItemsControl element) => (bool)element.GetValue(InitializeProperty);

		static void InitializePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if (!(d is ItemsControl itemsControl))
				return;
			if ((bool)e.NewValue)
				itemsControl.ItemContainerGenerator.ItemsChanged += (s, e2) => ItemContainerGenerator_ItemsChanged(itemsControl);
		}

		static void ItemContainerGenerator_ItemsChanged(ItemsControl itemsControl) {
			if (itemsControl.Items.Count == 0) {
				// Some of the cached items contain references to data that should be GC'd
				((IItemContainerGenerator)itemsControl.ItemContainerGenerator).RemoveAll();

				// GTFOH!
				UIElementAutomationPeer.FromElement(itemsControl)?.InvalidatePeer();
			}
		}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
