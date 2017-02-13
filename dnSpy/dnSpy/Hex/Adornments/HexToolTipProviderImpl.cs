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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Adornments;
using dnSpy.Contracts.Hex.Editor;
using VSTA = Microsoft.VisualStudio.Text.Adornments;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Adornments {
	sealed class HexToolTipProviderImpl : HexToolTipProvider {
		readonly WpfHexView wpfHexView;
		readonly HexSpaceReservationManager spaceReservationManager;
		HexSpaceReservationAgent toolTipAgent;

#pragma warning disable 0169
		[Export(typeof(HexSpaceReservationManagerDefinition))]
		[VSUTIL.Name(PredefinedHexSpaceReservationManagerNames.ToolTip)]
		static readonly HexSpaceReservationManagerDefinition toolTipSpaceReservationManagerDefinition;
#pragma warning restore 0169

		public HexToolTipProviderImpl(WpfHexView wpfHexView) {
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			spaceReservationManager = wpfHexView.GetSpaceReservationManager(PredefinedHexSpaceReservationManagerNames.ToolTip);
		}

		public override void ClearToolTip() {
			if (toolTipAgent != null)
				spaceReservationManager.RemoveAgent(toolTipAgent);
		}

		public override void ShowToolTip(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, object toolTipContent, VSTA.PopupStyles style) {
			if (bufferSpan.IsDefault)
				throw new ArgumentException();
			if (toolTipContent == null)
				throw new ArgumentNullException(nameof(toolTipContent));
			if ((style & (VSTA.PopupStyles.DismissOnMouseLeaveText | VSTA.PopupStyles.DismissOnMouseLeaveTextOrContent)) == (VSTA.PopupStyles.DismissOnMouseLeaveText | VSTA.PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(style));

			ClearToolTip();

			var uiElement = GetUIElement(toolTipContent);
			if (uiElement == null)
				throw new ArgumentException();

			spaceReservationManager.AgentChanged += SpaceReservationManager_AgentChanged;
			toolTipAgent = spaceReservationManager.CreatePopupAgent(bufferSpan, flags, style, uiElement);
			spaceReservationManager.AddAgent(toolTipAgent);
		}

		void SpaceReservationManager_AgentChanged(object sender, HexSpaceReservationAgentChangedEventArgs e) {
			if (e.OldAgent == toolTipAgent) {
				spaceReservationManager.AgentChanged -= SpaceReservationManager_AgentChanged;
				toolTipAgent = null;
			}
		}

		UIElement GetUIElement(object toolTipContent) {
			var elem = toolTipContent as UIElement;
			if (elem != null)
				return elem;
			var s = toolTipContent as string;
			if (s != null)
				return CreateUIElement(s);
			return null;
		}

		UIElement CreateUIElement(string s) {
			var border = new Border {
				Child = new TextBlock {
					Text = s,
					Padding = new Thickness(1),
				},
				BorderThickness = new Thickness(1),
			};
			border.SetResourceReference(Control.BackgroundProperty, "ToolTipBackground");
			border.SetResourceReference(Control.ForegroundProperty, "ToolTipForeground");
			border.SetResourceReference(Border.BorderBrushProperty, "ToolTipBorderBrush");
			return border;
		}
	}
}
