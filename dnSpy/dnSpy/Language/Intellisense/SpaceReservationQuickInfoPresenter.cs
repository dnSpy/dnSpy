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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Language.Intellisense {
	sealed class SpaceReservationQuickInfoPresenter : QuickInfoPresenterBase, IPopupIntellisensePresenter {
		UIElement IPopupIntellisensePresenter.SurfaceElement => control;
		PopupStyles IPopupIntellisensePresenter.PopupStyles => PopupStyles.PositionClosest;
		string IPopupIntellisensePresenter.SpaceReservationManagerName => PredefinedSpaceReservationManagerNames.QuickInfo;
		event EventHandler IPopupIntellisensePresenter.SurfaceElementChanged { add { } remove { } }
		event EventHandler<ValueChangedEventArgs<PopupStyles>> IPopupIntellisensePresenter.PopupStylesChanged { add { } remove { } }
		public event EventHandler PresentationSpanChanged;

		public double Opacity {
			get { return control.Opacity; }
			set { control.Opacity = value; }
		}

		public ITrackingSpan PresentationSpan {
			get { return presentationSpan; }
			private set {
				if (!TrackingSpanHelpers.IsSameTrackingSpan(presentationSpan, value)) {
					presentationSpan = value;
					PresentationSpanChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		ITrackingSpan presentationSpan;

		public SpaceReservationQuickInfoPresenter(IQuickInfoSession session)
			: base(session) {
			PresentationSpan = session.ApplicableToSpan;
			session.ApplicableToSpanChanged += Session_ApplicableToSpanChanged;
		}

		void Session_ApplicableToSpanChanged(object sender, EventArgs e) {
			if (session.IsDismissed)
				return;
			PresentationSpan = session.ApplicableToSpan;
		}

		protected override void OnSessionDismissed() => session.ApplicableToSpanChanged -= Session_ApplicableToSpanChanged;
	}
}
