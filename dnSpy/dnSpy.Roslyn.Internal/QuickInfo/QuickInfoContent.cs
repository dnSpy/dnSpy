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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.QuickInfo {
	abstract class QuickInfoContent {
		public abstract string Type { get; }
	}

	sealed class InformationQuickInfoContent : QuickInfoContent {
		public override string Type => PredefinedQuickInfoContentTypes.Information;

		public Glyph? SymbolGlyph { get; }
		public Glyph? WarningGlyph { get; }
		public ImmutableArray<TaggedText> MainDescription { get; }
		public ImmutableArray<TaggedText> Documentation { get; }
		public ImmutableArray<TaggedText> TypeParameterMap { get; }
		public ImmutableArray<TaggedText> AnonymousTypes { get; }
		public ImmutableArray<TaggedText> UsageText { get; }
		public ImmutableArray<TaggedText> ExceptionText { get; }

		public InformationQuickInfoContent(Glyph? symbolGlyph, Glyph? warningGlyph, ImmutableArray<TaggedText> mainDescription, ImmutableArray<TaggedText> documentation, ImmutableArray<TaggedText> typeParameterMap, ImmutableArray<TaggedText> anonymousTypes, ImmutableArray<TaggedText> usageText, ImmutableArray<TaggedText> exceptionText) {
			SymbolGlyph = symbolGlyph;
			WarningGlyph = warningGlyph;
			MainDescription = mainDescription.IsDefault ? ImmutableArray<TaggedText>.Empty : mainDescription;
			Documentation = documentation.IsDefault ? ImmutableArray<TaggedText>.Empty : documentation;
			TypeParameterMap = typeParameterMap.IsDefault ? ImmutableArray<TaggedText>.Empty : typeParameterMap;
			AnonymousTypes = anonymousTypes.IsDefault ? ImmutableArray<TaggedText>.Empty : anonymousTypes;
			UsageText = usageText.IsDefault ? ImmutableArray<TaggedText>.Empty : usageText;
			ExceptionText = exceptionText.IsDefault ? ImmutableArray<TaggedText>.Empty : exceptionText;
		}
	}

	sealed class CodeSpanQuickInfoContent : QuickInfoContent {
		public override string Type => PredefinedQuickInfoContentTypes.CodeSpan;

		public TextSpan Span { get; }

		public CodeSpanQuickInfoContent(TextSpan span) {
			Span = span;
		}
	}

	static class PredefinedQuickInfoContentTypes {
		/// <summary>
		/// Normal quick info tooltip content: information about a type, member, local, etc...
		/// </summary>
		public const string Information = nameof(Information);

		/// <summary>
		/// Some span of text from the document should be shown to the user, eg. the tooltip shown when hovering over the closing curly brace (})
		/// </summary>
		public const string CodeSpan = nameof(CodeSpan);
	}
}
