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
using dnSpy.Contracts.Text;
using CTC = dnSpy.Contracts.Text.Classification;

namespace dnSpy.Contracts.Hex.Classification.DnSpy {
	/// <summary>
	/// Creates text elements that can be shown in tooltips
	/// </summary>
	public abstract class HexTextElementCreator {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexTextElementCreator() { }

		/// <summary>
		/// Gets the writer
		/// </summary>
		public abstract ITextColorWriter Writer { get; }

		/// <summary>
		/// true if no text has been written to <see cref="Writer"/>
		/// </summary>
		public abstract bool IsEmpty { get; }

		/// <summary>
		/// Creates the text element
		/// </summary>
		/// <param name="tag">Tag (<see cref="CTC.PredefinedTextClassifierTags"/>), can be null</param>
		/// <param name="colorize">true if it should be colorized</param>
		/// <returns></returns>
		public abstract FrameworkElement CreateTextElement(bool colorize = true, string tag = null);
	}
}
