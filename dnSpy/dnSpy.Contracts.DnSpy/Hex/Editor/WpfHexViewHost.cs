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
using System.Windows.Controls;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// WPF hex view host
	/// </summary>
	public abstract class WpfHexViewHost {
		/// <summary>
		/// Constructor
		/// </summary>
		protected WpfHexViewHost() { }

		/// <summary>
		/// Closes this host and its hex view
		/// </summary>
		public abstract void Close();

		/// <summary>
		/// true if the host has been closed
		/// </summary>
		public abstract bool IsClosed { get; }

		/// <summary>
		/// Raised when it is closed
		/// </summary>
		public abstract event EventHandler Closed;

		/// <summary>
		/// Gets a margin or null if it doesn't exist
		/// </summary>
		/// <param name="marginName">Name of margin</param>
		/// <returns></returns>
		public abstract WpfHexViewMargin GetHexViewMargin(string marginName);

		/// <summary>
		/// Gets the hex view
		/// </summary>
		public abstract WpfHexView HexView { get; }

		/// <summary>
		/// Gets the UI element
		/// </summary>
		public abstract Control HostControl { get; }
	}
}
