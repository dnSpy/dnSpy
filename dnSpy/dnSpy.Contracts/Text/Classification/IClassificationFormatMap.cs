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
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Maps from a <see cref="IClassificationType"/> to a <see cref="TextFormattingRunProperties"/> object
	/// </summary>
	public interface IClassificationFormatMap {
		/// <summary>
		/// Occurs when this <see cref="IClassificationFormatMap"/> changes
		/// </summary>
		event EventHandler<EventArgs> ClassificationFormatMappingChanged;

		/// <summary>
		/// Gets the default properties that are applied to all classification types
		/// </summary>
		TextFormattingRunProperties DefaultTextProperties { get; }

		/// <summary>
		/// Gets the explicit <see cref="TextFormattingRunProperties"/> for the specified classification type
		/// </summary>
		/// <param name="classificationType">Classification type</param>
		/// <returns></returns>
		TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType);

		/// <summary>
		/// Gets the <see cref="TextFormattingRunProperties"/> for a given text classification type
		/// </summary>
		/// <param name="classificationType">Classification type</param>
		/// <returns></returns>
		TextFormattingRunProperties GetTextProperties(IClassificationType classificationType);
	}
}
