/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.ToolBars {
	/// <summary>
	/// A button in the toolbar
	/// </summary>
	public interface IToolBarButton : IToolBarItem {
		/// <summary>
		/// Returns true if the toolbar item is enabled and its <see cref="Execute(IToolBarItemContext)"/>
		/// method can be called.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsEnabled(IToolBarItemContext context);

		/// <summary>
		/// Executes the command
		/// </summary>
		/// <param name="context">Context</param>
		void Execute(IToolBarItemContext context);
	}

	/// <summary>
	/// Extends <see cref="IToolBarButton"/> with some extra methods
	/// </summary>
	public interface IToolBarButton2 {
		/// <summary>
		/// Gets the header or null to use the header from the attribute
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string GetHeader(IToolBarItemContext context);

		/// <summary>
		/// Gets the icon name or null to use the icon from the attribute
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string GetIcon(IToolBarItemContext context);

		/// <summary>
		/// Gets the tooltip or null to use the tooltip from the attribute
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string GetToolTip(IToolBarItemContext context);
	}
}
