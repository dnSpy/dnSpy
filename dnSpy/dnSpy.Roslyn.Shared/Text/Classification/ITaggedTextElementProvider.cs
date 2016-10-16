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
using System.Collections.Immutable;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	/// <summary>
	/// Creates a <see cref="TextBlock"/>. Call its <see cref="IDisposable.Dispose"/> method
	/// to clean up its resources.
	/// </summary>
	interface ITaggedTextElementProvider : IDisposable {
		/// <summary>
		/// Creates a <see cref="TextBlock"/>
		/// </summary>
		/// <param name="tag">Tag, can be null</param>
		/// <param name="taggedParts">Tagged parts to classify</param>
		/// <param name="colorize">true if it should be colorized</param>
		/// <returns></returns>
		TextBlock Create(string tag, ImmutableArray<TaggedText> taggedParts, bool colorize);
	}
}
