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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Internal.Helpers {
	static class SymbolDisplayPartExtensions {
		public static ImmutableArray<TaggedText> ToTaggedText(this ImmutableArray<SymbolDisplayPart> parts) {
			if (parts.Length == 0)
				return ImmutableArray<TaggedText>.Empty;
			var builder = ImmutableArray.CreateBuilder<TaggedText>(parts.Length);
			for (int i = 0; i < parts.Length; i++)
				builder.Add(parts[i].ToTaggedText());
			return builder.MoveToImmutable();
		}

		public static IList<TaggedText> ToTaggedText(this IList<SymbolDisplayPart> parts) {
			if (parts.Count == 0)
				return Array.Empty<TaggedText>();
			var list = new List<TaggedText>(parts.Count);
			for (int i = 0; i < parts.Count; i++)
				list.Add(parts[i].ToTaggedText());
			return list;
		}

		public static TaggedText ToTaggedText(this SymbolDisplayPart part) =>
			new TaggedText(SymbolDisplayPartKindTags.GetTag(part.Kind), part.ToString());

		public static Func<CancellationToken, IEnumerable<TaggedText>> ToTaggedTextFunc(this Func<CancellationToken, IEnumerable<SymbolDisplayPart>> func) =>
			new TaggedTextEnumerableHelper(func).CreateTaggedText;

		sealed class TaggedTextEnumerableHelper {
			readonly Func<CancellationToken, IEnumerable<SymbolDisplayPart>> func;

			public TaggedTextEnumerableHelper(Func<CancellationToken, IEnumerable<SymbolDisplayPart>> func) {
				if (func == null)
					throw new ArgumentNullException(nameof(func));
				this.func = func;
			}

			public IEnumerable<TaggedText> CreateTaggedText(CancellationToken cancellationToken) {
				foreach (var part in func(cancellationToken))
					yield return part.ToTaggedText();
			}
		}
	}
}
