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

using System.Collections.Immutable;
using System.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	/// <summary>
	/// Context passed to the <see cref="TaggedText"/> classifiers
	/// </summary>
	sealed class TaggedTextClassifierContext : TextClassifierContext {
		/// <summary>
		/// Gets all tagged parts
		/// </summary>
		public ImmutableArray<TaggedText> TaggedParts { get; }

		TaggedTextClassifierContext(string text, string tag, ImmutableArray<TaggedText> taggedParts, bool colorize)
			: base(text, tag, colorize) {
			TaggedParts = taggedParts.IsDefault ? ImmutableArray<TaggedText>.Empty : taggedParts;
		}

		static string ToString(ImmutableArray<TaggedText> taggedParts) {
			if (taggedParts.IsDefault || taggedParts.Length == 0)
				return string.Empty;
			int length = taggedParts.Length;
			if (length == 1)
				return taggedParts[0].Text;
			if (length == 2)
				return taggedParts[0].Text + taggedParts[1].Text;
			if (length == 3)
				return taggedParts[0].Text + taggedParts[1].Text + taggedParts[2].Text;
			var sb = new StringBuilder();
			foreach (var part in taggedParts)
				sb.Append(part.Text);
			return sb.ToString();
		}

		/// <summary>
		/// Creates an instance
		/// </summary>
		/// <param name="tag">Tag, can be null</param>
		/// <param name="taggedParts">Tagged parts</param>
		/// <param name="colorize">true if it should be colorized</param>
		/// <returns></returns>
		public static TaggedTextClassifierContext Create(string tag, ImmutableArray<TaggedText> taggedParts, bool colorize) =>
			new TaggedTextClassifierContext(tag, ToString(taggedParts), taggedParts, colorize);
	}
}
