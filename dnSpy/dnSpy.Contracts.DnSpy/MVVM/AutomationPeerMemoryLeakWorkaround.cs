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
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Workaround for an <see cref="AutomationPeer"/> memory leak.
	/// Use it on all long-lived <see cref="ItemsControl"/>s (eg. <see cref="ListView"/>, <see cref="ListBox"/>, etc)
	/// </summary>
	public sealed class AutomationPeerMemoryLeakWorkaround {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static readonly DependencyProperty InitializeProperty = DependencyProperty.RegisterAttached(
			"Initialize", typeof(bool), typeof(AutomationPeerMemoryLeakWorkaround), new UIPropertyMetadata(false, InitializePropertyChangedCallback));

		public static void SetInitialize(ItemsControl element, bool value) => element.SetValue(InitializeProperty, value);
		public static bool GetInitialize(ItemsControl element) => (bool)element.GetValue(InitializeProperty);

		public static readonly DependencyProperty EmptyCountProperty = DependencyProperty.RegisterAttached(
			"EmptyCount", typeof(int), typeof(AutomationPeerMemoryLeakWorkaround), new UIPropertyMetadata(0));

		public static void SetEmptyCount(ItemsControl element, int value) => element.SetValue(EmptyCountProperty, value);
		public static int GetEmptyCount(ItemsControl element) => (int)element.GetValue(EmptyCountProperty);

		static void InitializePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if (!(d is ItemsControl itemsControl))
				return;
			if ((bool)e.NewValue)
				itemsControl.ItemContainerGenerator.ItemsChanged += (s, e2) => ItemContainerGenerator_ItemsChanged(itemsControl);
		}

		static void ItemContainerGenerator_ItemsChanged(ItemsControl itemsControl) {
			if (itemsControl.Items.Count <= GetEmptyCount(itemsControl))
				ClearAll(itemsControl);
		}

		public static void ClearAll(ItemsControl? itemsControl) {
			if (itemsControl is null)
				return;

			// Some of the cached items contain references to data that should be GC'd
			var method = itemsControl.ItemContainerGenerator.GetType().GetMethod("ResetRecyclableContainers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null);
			Debug2.Assert(method is not null);
			method?.Invoke(itemsControl.ItemContainerGenerator, Array.Empty<object>());

			var automationPeer = UIElementAutomationPeer.FromElement(itemsControl);
			if (automationPeer is not null) {
				PropertyInfo? prop;
				MethodInfo? getMethod;

				if (automationPeer is ItemsControlAutomationPeer) {
					// Clear _dataChildren
					prop = automationPeer.GetType().GetProperty("ItemPeers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					getMethod = prop?.GetGetMethod(nonPublic: true);
					Debug2.Assert(getMethod is not null);
					if (getMethod is not null) {
						var coll = getMethod.Invoke(automationPeer, Array.Empty<object>());
						Debug2.Assert(coll is not null);
						if (coll is not null) {
							var clearMethod = coll.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null);
							Debug2.Assert(clearMethod is not null);
							clearMethod?.Invoke(coll, Array.Empty<object>());
						}
					}

					// Clear _recentlyRealizedPeers
					prop = automationPeer.GetType().GetProperty("RecentlyRealizedPeers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					getMethod = prop?.GetGetMethod(nonPublic: true);
					Debug2.Assert(getMethod is not null);
					if (getMethod is not null) {
						var coll = getMethod.Invoke(automationPeer, Array.Empty<object>()) as System.Collections.IList;
						Debug2.Assert(coll is not null);
						coll?.Clear();
					}
				}

				// Set ChildrenValid = false
				prop = automationPeer.GetType().GetProperty("ChildrenValid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				var setMethod = prop?.GetSetMethod(nonPublic: true);
				Debug2.Assert(setMethod is not null);
				setMethod?.Invoke(automationPeer, new object[] { false });

				// Clear _children
				prop = automationPeer.GetType().GetProperty("Children", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				getMethod = prop?.GetGetMethod(nonPublic: true);
				Debug2.Assert(getMethod is not null);
				if (getMethod is not null) {
					var coll = getMethod.Invoke(automationPeer, Array.Empty<object>()) as System.Collections.IList;
					coll?.Clear();
				}

				// GTFOH!
				automationPeer.InvalidatePeer();

				// AutomationPeer is one big memory leak
			}
		}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
