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

using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.ToolBars {
	/// <summary>
	/// Toolbar button base class (implements <see cref="IToolBarButton"/>
	/// </summary>
	public abstract class ToolBarButtonBase : IToolBarButton {
		/// <inheritdoc/>
		public abstract void Execute(IToolBarItemContext context);
		/// <inheritdoc/>
		public virtual bool IsEnabled(IToolBarItemContext context) => true;
		/// <inheritdoc/>
		public virtual bool IsVisible(IToolBarItemContext context) => true;
		/// <inheritdoc/>
		public virtual string GetHeader(IToolBarItemContext context) => null;
		/// <inheritdoc/>
		public virtual ImageReference? GetIcon(IToolBarItemContext context) => null;
		/// <inheritdoc/>
		public virtual string GetToolTip(IToolBarItemContext context) => null;
	}
}
