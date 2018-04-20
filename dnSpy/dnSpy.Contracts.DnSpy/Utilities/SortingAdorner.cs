/*
    Copyright (C) 2014-2018 eichhorn@posteo.de

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

using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace dnSpy.Contracts.Utilities {

	/// <summary>
	/// Adorner for sorting
	/// </summary>
	sealed class SortingAdorner : Adorner {
		private static Geometry _arrowUp = Geometry.Parse("M 5,5 15,5 10,0 5,5");
		private static Geometry _arrowDown = Geometry.Parse("M 5,0 10,5 15,0 5,0");
		private Geometry _sortDirection;

		/// <summary>
		/// 
		/// </summary>
		public SortingAdorner(GridViewColumnHeader adornedElement,
						 ListSortDirection sortDirection) : base(adornedElement) {
			_sortDirection = sortDirection == ListSortDirection.Ascending ?
																 _arrowUp :
																 _arrowDown;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void OnRender(DrawingContext drawingContext) {
			double x = AdornedElement.RenderSize.Width - 20;
			double y = (AdornedElement.RenderSize.Height - 5) / 2;

			if (x >= 20) {
				// Right order of the statements is important
				drawingContext.PushTransform(new TranslateTransform(x, y));
				drawingContext.DrawGeometry(Brushes.Black, null, _sortDirection);
				drawingContext.Pop();
			}
		}
	}
}
