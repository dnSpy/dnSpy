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
using System.Windows.Media;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	sealed class HexSpaceReservationStackImpl : HexSpaceReservationStack {
		public override event EventHandler GotAggregateFocus;
		public override event EventHandler LostAggregateFocus;
		public override bool HasAggregateFocus => hasAggregateFocus;
		bool hasAggregateFocus;

		public override bool IsMouseOver {
			get {
				foreach (var mgr in SpaceReservationManagers) {
					if (mgr.IsMouseOver)
						return true;
				}
				return false;
			}
		}

		IEnumerable<HexSpaceReservationManagerImpl> SpaceReservationManagers {
			get {
				foreach (var mgr in spaceReservationManagers) {
					if (mgr != null)
						yield return mgr;
				}
			}
		}

		readonly WpfHexView wpfHexView;
		readonly string[] spaceReservationManagerNames;
		readonly HexSpaceReservationManagerImpl[] spaceReservationManagers;

		public HexSpaceReservationStackImpl(WpfHexView wpfHexView, string[] spaceReservationManagerNames) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			if (spaceReservationManagerNames == null)
				throw new ArgumentNullException(nameof(spaceReservationManagerNames));
			this.wpfHexView = wpfHexView;
			this.spaceReservationManagerNames = spaceReservationManagerNames;
			spaceReservationManagers = new HexSpaceReservationManagerImpl[spaceReservationManagerNames.Length];
			wpfHexView.Closed += WpfHexView_Closed;
		}

		int GetNameIndex(string name) {
			for (int i = 0; i < spaceReservationManagerNames.Length; i++) {
				if (spaceReservationManagerNames[i] == name)
					return i;
			}
			return -1;
		}

		public override HexSpaceReservationManager GetSpaceReservationManager(string name) {
			if (wpfHexView.IsClosed)
				throw new InvalidOperationException();
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			int index = GetNameIndex(name);
			if (index < 0)
				throw new ArgumentException();
			var mgr = spaceReservationManagers[index];
			if (mgr == null) {
				mgr = new HexSpaceReservationManagerImpl(wpfHexView);
				mgr.GotAggregateFocus += HexSpaceReservationManager_GotAggregateFocus;
				mgr.LostAggregateFocus += HexSpaceReservationManager_LostAggregateFocus;
				spaceReservationManagers[index] = mgr;
			}
			return mgr;
		}

		void HexSpaceReservationManager_GotAggregateFocus(object sender, EventArgs e) => UpdateAggregateFocus();
		void HexSpaceReservationManager_LostAggregateFocus(object sender, EventArgs e) => UpdateAggregateFocus();

		void UpdateAggregateFocus() {
			if (wpfHexView.IsClosed)
				return;
			bool newValue = CalculateAggregateFocus();
			if (newValue != HasAggregateFocus) {
				hasAggregateFocus = newValue;
				if (newValue)
					GotAggregateFocus?.Invoke(this, EventArgs.Empty);
				else
					LostAggregateFocus?.Invoke(this, EventArgs.Empty);
			}
		}

		bool CalculateAggregateFocus() {
			foreach (var mgr in SpaceReservationManagers) {
				if (mgr.HasAggregateFocus)
					return true;
			}
			return false;
		}

		public override void Refresh() {
			if (wpfHexView.IsClosed)
				return;
			GeometryGroup geometry = null;
			foreach (var mgr in SpaceReservationManagers) {
				if (geometry == null)
					geometry = new GeometryGroup();
				mgr.PositionAndDisplay(geometry);
			}
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			wpfHexView.Closed -= WpfHexView_Closed;
			for (int i = 0; i < spaceReservationManagers.Length; i++) {
				var mgr = spaceReservationManagers[i];
				if (mgr != null) {
					spaceReservationManagers[i] = null;
					mgr.GotAggregateFocus -= HexSpaceReservationManager_GotAggregateFocus;
					mgr.LostAggregateFocus -= HexSpaceReservationManager_LostAggregateFocus;
				}
			}
		}
	}
}
