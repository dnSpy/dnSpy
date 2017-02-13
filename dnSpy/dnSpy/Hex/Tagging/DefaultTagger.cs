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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Tagging;
using CT = dnSpy.Contracts.Text;
using CTC = dnSpy.Contracts.Text.Classification;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex.Tagging {
	[Export(typeof(HexClassificationTags))]
	sealed class HexClassificationTags {
		public HexClassificationTag HexOffsetTag { get; }
		public HexClassificationTag HexValue0Tag { get; }
		public HexClassificationTag HexValue1Tag { get; }
		public HexClassificationTag HexAsciiTag { get; }
		public HexClassificationTag HexErrorTag { get; }

		[ImportingConstructor]
		HexClassificationTags(CTC.IThemeClassificationTypeService themeClassificationTypeService) {
			HexOffsetTag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(CT.TextColor.HexOffset));
			HexValue0Tag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(CT.TextColor.HexByte0));
			HexValue1Tag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(CT.TextColor.HexByte1));
			HexAsciiTag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(CT.TextColor.HexAscii));
			HexErrorTag = new HexClassificationTag(themeClassificationTypeService.GetClassificationType(CT.TextColor.HexByteError));
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

		public override IHexTagger<T> CreateTagger<T>(HexBuffer buffer) =>
			new DefaultTagger(hexClassificationTags) as IHexTagger<T>;
	}

	sealed class DefaultTagger : HexTagger<HexClassificationTag> {
		public override event EventHandler<HexBufferSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		readonly HexClassificationTags hexClassificationTags;

		public DefaultTagger(HexClassificationTags hexClassificationTags) {
			this.hexClassificationTags = hexClassificationTags ?? throw new ArgumentNullException(nameof(hexClassificationTags));
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

		public override IEnumerable<IHexTextTagSpan<HexClassificationTag>> GetTags(HexTaggerContext context) {
			// Minimize color switches between columns: colorize the space between eg. OFFSET/VALUES and
			// VALUES/ASCII. This results in a small speed up displaying the lines.
			var columnOrder = context.Line.ColumnOrder;
			const HexColumnType INVALID_COLUMN = (HexColumnType)(-1);
			var prevColumnType = INVALID_COLUMN;
			for (int i = 0; i < columnOrder.Count; i++) {
				var columnType = columnOrder[i];
				if (!context.Line.IsColumnPresent(columnType))
					continue;

				if (prevColumnType != INVALID_COLUMN) {
					// Don't prefer VALUES column since it has two group colors
					var columnToUse = prevColumnType != HexColumnType.Values ? prevColumnType : columnType;
					HexClassificationTag tag;
					switch (columnToUse) {
					case HexColumnType.Offset:
						tag = hexClassificationTags.HexOffsetTag;
						break;
					case HexColumnType.Ascii:
						tag = hexClassificationTags.HexAsciiTag;
						break;
					case HexColumnType.Values:
						Debug.Fail("Should never happen");
						tag = hexClassificationTags.HexValue0Tag;
						break;

					default: throw new InvalidOperationException();
					}
					var start = context.Line.GetSpan(prevColumnType, onlyVisibleCells: false).End;
					var end = context.Line.GetSpan(columnType, onlyVisibleCells: false).Start;
					if (start < end)
						yield return new HexTextTagSpan<HexClassificationTag>(VST.Span.FromBounds(start, end), tag);
				}

				prevColumnType = columnType;
			}

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

		public override IEnumerable<IHexTagSpan<HexClassificationTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			foreach (var span in spans) {
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Offset, hexClassificationTags.HexOffsetTag);
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Ascii | HexSpanSelectionFlags.AllCells, hexClassificationTags.HexAsciiTag);
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Group0 | HexSpanSelectionFlags.AllCells, hexClassificationTags.HexValue0Tag);
				yield return new HexTagSpan<HexClassificationTag>(span, HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Group1 | HexSpanSelectionFlags.AllCells, hexClassificationTags.HexValue1Tag);
			}
		}
	}
}
