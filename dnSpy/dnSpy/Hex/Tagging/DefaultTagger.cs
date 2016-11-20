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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Tagging;
using TC = dnSpy.Contracts.Text.Classification;
using TEXT = dnSpy.Contracts.Text;

namespace dnSpy.Hex.Tagging {
	[Export(typeof(HexClassificationTags))]
	sealed class HexClassificationTags {
		public HexClassificationTag HexOffsetTag { get; }
		public HexClassificationTag HexValue0Tag { get; }
		public HexClassificationTag HexValue1Tag { get; }
		public HexClassificationTag HexAsciiTag { get; }
		public HexClassificationTag HexErrorTag { get; }

		[ImportingConstructor]
		HexClassificationTags(TC.IThemeClassificationTypeService themeClassificationTypeService) {
			HexOffsetTag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(TEXT.TextColor.HexOffset));
			HexValue0Tag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(TEXT.TextColor.HexByte0));
			HexValue1Tag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(TEXT.TextColor.HexByte1));
			HexAsciiTag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(TEXT.TextColor.HexAscii));
			HexErrorTag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(TEXT.TextColor.HexByteError));
		}
	}

	[Export(typeof(HexTaggerProvider))]
	[HexTagType(typeof(HexClassificationTag))]
	sealed class DefaultTaggerProvider : HexTaggerProvider {
		readonly HexClassificationTags hexClassificationTags;

		[ImportingConstructor]
		DefaultTaggerProvider(HexClassificationTags hexClassificationTags) {
			this.hexClassificationTags = hexClassificationTags;
		}

		public override HexTagger<T> CreateTagger<T>(HexBuffer buffer) =>
			new DefaultTagger(hexClassificationTags) as HexTagger<T>;
	}

	sealed class DefaultTagger : HexTagger<HexClassificationTag> {
		public override event EventHandler<HexBufferSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		readonly HexClassificationTags hexClassificationTags;

		public DefaultTagger(HexClassificationTags hexClassificationTags) {
			if (hexClassificationTags == null)
				throw new ArgumentNullException(nameof(hexClassificationTags));
			this.hexClassificationTags = hexClassificationTags;
		}

		static bool IsValid(HexCell cell, HexBufferLine line) {
			long len = checked((long)cell.BufferSpan.Length.ToUInt64());
			long b = checked((long)(cell.BufferStart - line.BufferStart).ToUInt64());
			for (long i = 0; i < len; i++) {
				if (!line.HexBytes.IsValid(b + i))
					return false;
			}
			return true;
		}

		public override IEnumerable<HexTextTagSpan<HexClassificationTag>> GetTags(HexTaggerContext context) {
			var allValid = context.Line.HexBytes.AllValid;
			if (allValid == null) {
				foreach (var cell in context.Line.ValueCells.GetVisibleCells()) {
					if (!IsValid(cell, context.Line))
						yield return new HexTextTagSpan<HexClassificationTag>(cell.FullSpan, hexClassificationTags.HexErrorTag);
				}
				foreach (var cell in context.Line.AsciiCells.GetVisibleCells()) {
					if (!IsValid(cell, context.Line))
						yield return new HexTextTagSpan<HexClassificationTag>(cell.FullSpan, hexClassificationTags.HexErrorTag);
				}
			}
			else if (!allValid.Value) {
				yield return new HexTextTagSpan<HexClassificationTag>(context.Line.GetValuesSpan(onlyVisibleCells: true), hexClassificationTags.HexErrorTag);
				yield return new HexTextTagSpan<HexClassificationTag>(context.Line.GetAsciiSpan(onlyVisibleCells: true), hexClassificationTags.HexErrorTag);
			}
		}

		public override IEnumerable<HexTagSpan<HexClassificationTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			foreach (var span in spans) {
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Offset, hexClassificationTags.HexOffsetTag);
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Ascii | HexSpanSelectionFlags.AllCells, hexClassificationTags.HexAsciiTag);
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Group0 | HexSpanSelectionFlags.AllCells, hexClassificationTags.HexValue0Tag);
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Group1 | HexSpanSelectionFlags.AllCells, hexClassificationTags.HexValue1Tag);
			}
		}
	}
}
