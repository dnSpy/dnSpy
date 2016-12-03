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

using System.Windows;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Window base class
	/// </summary>
	public abstract class WindowBase : MetroWindow {
		/// <summary>
		/// Clicks the OK button
		/// </summary>
		protected void ClickOK() {
			DialogResult = true;
			Close();
		}

		/// <summary>
		/// Clicks the Cancel button
		/// </summary>
		protected void ClickCancel() {
			DialogResult = false;
			Close();
		}

		/// <summary>
		/// OK button handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void okButton_Click(object sender, RoutedEventArgs e) => ClickOK();

		/// <summary>
		/// Cancel button handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void cancelButton_Click(object sender, RoutedEventArgs e) => ClickCancel();
	}
}
