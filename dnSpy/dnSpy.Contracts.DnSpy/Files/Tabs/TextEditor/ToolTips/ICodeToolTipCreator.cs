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

using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Files.Tabs.TextEditor.ToolTips {
	/// <summary>
	/// Creates code tooltips
	/// </summary>
	public interface ICodeToolTipCreator {
		/// <summary>
		/// Sets the image that should be shown in the tooltip or null if none should be shown
		/// </summary>
		ImageReference? Image { get; set; }

		/// <summary>
		/// Initializes <see cref="Image"/> with an image
		/// </summary>
		/// <param name="ref">A dnlib type, method, field, local, etc</param>
		void SetImage(object @ref);

		/// <summary>
		/// Gets the current output
		/// </summary>
		ICodeToolTipWriter Output { get; }

		/// <summary>
		/// Creates a new output that is shown on a new line
		/// </summary>
		/// <returns></returns>
		ICodeToolTipWriter CreateNewOutput();

		/// <summary>
		/// Creates the tooltip
		/// </summary>
		/// <returns></returns>
		object Create();
	}
}
