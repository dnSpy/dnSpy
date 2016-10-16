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
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Creates WPF text elements
	/// </summary>
	public interface ITextElementProvider {
		/// <summary>
		/// Creates a WPF text element
		/// </summary>
		/// <param name="classificationFormatMap">Classification format map</param>
		/// <param name="context">Text classifier context</param>
		/// <param name="contentType">Content type</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		FrameworkElement CreateTextElement(IClassificationFormatMap classificationFormatMap, TextClassifierContext context, string contentType, TextElementFlags flags);
	}
}
