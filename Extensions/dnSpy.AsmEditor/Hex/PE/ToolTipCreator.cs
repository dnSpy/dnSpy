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
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Hex.Classification.DnSpy;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ToolTipCreator {
		readonly HexTextElementCreatorProvider hexTextElementCreatorProvider;
		readonly List<HexTextElementCreator> creators;

		public ImageReference Image { get; set; }
		public ITextColorWriter Writer => creators[creators.Count - 1].Writer;

		public ToolTipCreator(HexTextElementCreatorProvider hexTextElementCreatorProvider) {
			if (hexTextElementCreatorProvider == null)
				throw new ArgumentNullException(nameof(hexTextElementCreatorProvider));
			this.hexTextElementCreatorProvider = hexTextElementCreatorProvider;
			creators = new List<HexTextElementCreator>();
			CreateNewWriter();
		}

		public ITextColorWriter CreateNewWriter() {
			creators.Add(hexTextElementCreatorProvider.Create());
			return Writer;
		}

		public object Create() {
			var res = new StackPanel {
				Orientation = Orientation.Vertical,
			};
			var sigGrid = new Grid();
			sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			res.Children.Add(sigGrid);
			for (int i = 1; i < creators.Count; i++) {
				var creator = creators[i];
				if (!creator.IsEmpty)
					res.Children.Add(creator.CreateTextElement());
			}
			if (!Image.IsDefault) {
				var img = new DsImage {
					ImageReference = Image,
					Margin = new Thickness(0, 0, 4, 0),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
				};
				Grid.SetColumn(img, 0);
				sigGrid.Children.Add(img);
			}
			var sig = creators[0].CreateTextElement();
			Grid.SetColumn(sig, 1);
			sigGrid.Children.Add(sig);
			return res;
		}
	}
}
