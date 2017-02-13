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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.Groups {
	/// <summary>
	/// Text view option changed event args
	/// </summary>
	public sealed class TextViewOptionChangedEventArgs : EventArgs {
		/// <summary>
		/// Content type
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Option id, eg. <see cref="DefaultTextViewOptions.WordWrapStyleName"/>
		/// </summary>
		public string OptionId { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contentType">Content type</param>
		/// <param name="optionId">Option id, eg. <see cref="DefaultTextViewOptions.WordWrapStyleName"/></param>
		public TextViewOptionChangedEventArgs(string contentType, string optionId) {
			ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			OptionId = optionId ?? throw new ArgumentNullException(nameof(optionId));
		}
	}
}
