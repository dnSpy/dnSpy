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
using System.Windows;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Intellisense;
using VSLI = Microsoft.VisualStudio.Language.Intellisense;
using VSTA = Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Hex.Intellisense {
	sealed class HexSpaceReservationQuickInfoPresenter : HexQuickInfoPresenterBase, IHexPopupIntellisensePresenter {
		UIElement IHexPopupIntellisensePresenter.SurfaceElement => control;
		VSTA.PopupStyles IHexPopupIntellisensePresenter.PopupStyles => VSTA.PopupStyles.PositionClosest;
		string IHexPopupIntellisensePresenter.SpaceReservationManagerName => HexIntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName;
		event EventHandler IHexPopupIntellisensePresenter.SurfaceElementChanged { add { } remove { } }
		event EventHandler<VSLI.ValueChangedEventArgs<VSTA.PopupStyles>> IHexPopupIntellisensePresenter.PopupStylesChanged { add { } remove { } }
		public event EventHandler PresentationSpanChanged;

		public double Opacity {
			get { return control.Opacity; }
			set { control.Opacity = value; }
		}

		public HexBufferSpanSelection PresentationSpan {
			get { return presentationSpan; }
		}

		void SetPresentationSpan(HexBufferSpanSelection newValue) {
			if (!presentationSpan.Equals(newValue)) {
				presentationSpan = newValue;
				PresentationSpanChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		HexBufferSpanSelection presentationSpan;

		public HexSpaceReservationQuickInfoPresenter(HexQuickInfoSession session)
			: base(session) {
			SetPresentationSpan(session.ApplicableToSpan);
			session.ApplicableToSpanChanged += Session_ApplicableToSpanChanged;
		}

		void Session_ApplicableToSpanChanged(object sender, EventArgs e) {
			if (session.IsDismissed)
				return;
			SetPresentationSpan(session.ApplicableToSpan);
		}

		protected override void OnSessionDismissed() => session.ApplicableToSpanChanged -= Session_ApplicableToSpanChanged;
	}
}
