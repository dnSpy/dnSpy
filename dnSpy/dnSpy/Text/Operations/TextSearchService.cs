/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Operations {
	[Export(typeof(ITextSearchService))]
	[Export(typeof(ITextSearchService2))]
	sealed class TextSearchService : ITextSearchService2 {
		readonly ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;
		readonly List<CachedRegex> cachedRegexes;
		readonly object cachedRegexDictLock;
		const int MAX_CACHED_REGEXES = 4;

		readonly struct CachedRegex {
			readonly string pattern;
			readonly FindOptions findOptions;
			readonly CultureInfo culture;
			const FindOptions FindOptionsMask = FindOptions.MatchCase | FindOptions.SearchReverse | FindOptions.Multiline | FindOptions.SingleLine;

			public Regex Regex { get; }

			public CachedRegex(string pattern, FindOptions findOptions) {
				this.pattern = pattern;
				this.findOptions = findOptions & FindOptionsMask;
				culture = CultureInfo.CurrentCulture;
				var options = RegexOptions.None;
				if ((findOptions & FindOptions.MatchCase) == 0)
					options |= RegexOptions.IgnoreCase;
				if ((findOptions & FindOptions.SearchReverse) != 0)
					options |= RegexOptions.RightToLeft;
				if ((findOptions & FindOptions.Multiline) != 0)
					options |= RegexOptions.Multiline;
				if ((findOptions & FindOptions.SingleLine) != 0)
					options |= RegexOptions.Singleline;
				Regex = new Regex(pattern, options);
			}

			public bool Equals(string pattern, FindOptions findOptions) => StringComparer.Ordinal.Equals(pattern, this.pattern) && (findOptions & FindOptionsMask) == this.findOptions && CultureInfo.CurrentCulture == culture;
		}

		[ImportingConstructor]
		TextSearchService(ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService) {
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
			cachedRegexes = new List<CachedRegex>(MAX_CACHED_REGEXES);
			cachedRegexDictLock = new object();
		}

		readonly struct FindResult {
			public int Position { get; }
			public int Length { get; }
			public string? ExpandedReplacePattern { get; }
			public FindResult(int position, int length, string? expandedReplacePattern) {
				Position = position;
				Length = length;
				ExpandedReplacePattern = expandedReplacePattern;
			}
		}

		bool IsWholeWord(ITextStructureNavigator? textStructureNavigator, ITextSnapshot snapshot, FindResult result) {
			Debug2.Assert(textStructureNavigator is not null);
			if (textStructureNavigator is null)
				return false;
			if (result.Length == 0)
				return false;
			var start = textStructureNavigator.GetExtentOfWord(new SnapshotPoint(snapshot, result.Position));
			if (start.Span.Start.Position != result.Position)
				return false;
			var end = textStructureNavigator.GetExtentOfWord(new SnapshotPoint(snapshot, result.Position + result.Length - 1));
			return end.Span.End.Position == result.Position + result.Length;
		}

		public SnapshotSpan? FindNext(int startIndex, bool wraparound, FindData findData) {
			if (findData.TextSnapshotToSearch is null)
				throw new ArgumentException();
			if ((uint)startIndex > (uint)findData.TextSnapshotToSearch.Length)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			var searchRange = new SnapshotSpan(findData.TextSnapshotToSearch, 0, findData.TextSnapshotToSearch.Length);
			var startingPosition = new SnapshotPoint(findData.TextSnapshotToSearch, startIndex);
			var options = findData.FindOptions;
			if (wraparound)
				options |= FindOptions.Wrap;

			bool wholeWords = (findData.FindOptions & FindOptions.WholeWord) != 0;
			ITextStructureNavigator? textStructureNavigator = null;
			if (wholeWords) {
				textStructureNavigator = textStructureNavigatorSelectorService.GetTextStructureNavigator(findData.TextSnapshotToSearch.TextBuffer);
				options &= ~FindOptions.WholeWord;
			}
			foreach (var res in FindAllCore(searchRange, startingPosition, findData.SearchString, options, null)) {
				if (!wholeWords || IsWholeWord(textStructureNavigator, findData.TextSnapshotToSearch, res))
					return new SnapshotSpan(findData.TextSnapshotToSearch, res.Position, res.Length);
			}

			return null;
		}

		public Collection<SnapshotSpan> FindAll(FindData findData) {
			if (findData.TextSnapshotToSearch is null)
				throw new ArgumentException();

			var searchRange = new SnapshotSpan(findData.TextSnapshotToSearch, 0, findData.TextSnapshotToSearch.Length);
			var startingPosition = new SnapshotPoint(findData.TextSnapshotToSearch, 0);
			var options = findData.FindOptions;
			var coll = new Collection<SnapshotSpan>();

			bool wholeWords = (findData.FindOptions & FindOptions.WholeWord) != 0;
			ITextStructureNavigator? textStructureNavigator = null;
			if (wholeWords) {
				textStructureNavigator = textStructureNavigatorSelectorService.GetTextStructureNavigator(findData.TextSnapshotToSearch.TextBuffer);
				options &= ~FindOptions.WholeWord;
			}
			foreach (var res in FindAllCore(searchRange, startingPosition, findData.SearchString, options, null)) {
				if (!wholeWords || IsWholeWord(textStructureNavigator, findData.TextSnapshotToSearch, res))
					coll.Add(new SnapshotSpan(findData.TextSnapshotToSearch, res.Position, res.Length));
			}

			return coll;
		}

		public SnapshotSpan? Find(SnapshotPoint startingPosition, string searchPattern, FindOptions options) {
			if (startingPosition.Snapshot is null)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));
			return Find(new SnapshotSpan(startingPosition.Snapshot, 0, startingPosition.Snapshot.Length), startingPosition, searchPattern, options);
		}

		public SnapshotSpan? Find(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options) {
			if (searchRange.Snapshot is null)
				throw new ArgumentException();
			if (startingPosition.Snapshot is null)
				throw new ArgumentException();
			if (searchRange.Snapshot != startingPosition.Snapshot)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));
			foreach (var result in FindAll(searchRange, startingPosition, searchPattern, options))
				return result;
			return null;
		}

		public IEnumerable<SnapshotSpan> FindAll(SnapshotSpan searchRange, string searchPattern, FindOptions options) {
			if (searchRange.Snapshot is null)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));
			return FindAll(searchRange, searchRange.Start, searchPattern, options);
		}

		public IEnumerable<SnapshotSpan> FindAll(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options) {
			if (searchRange.Snapshot is null)
				throw new ArgumentException();
			if (startingPosition.Snapshot is null)
				throw new ArgumentException();
			if (searchRange.Snapshot != startingPosition.Snapshot)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));

			var snapshot = searchRange.Snapshot;
			foreach (var res in FindAllCore(searchRange, startingPosition, searchPattern, options, null))
				yield return new SnapshotSpan(snapshot, res.Position, res.Length);
		}

		public SnapshotSpan? FindForReplace(SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options, out string? expandedReplacePattern) {
			if (searchRange.Snapshot is null)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));
			if (replacePattern is null)
				throw new ArgumentNullException(nameof(replacePattern));
			var startingPosition = (options & FindOptions.SearchReverse) != 0 ? searchRange.End : searchRange.Start;
			foreach (var t in FindAllForReplace(searchRange, startingPosition, searchPattern, replacePattern, options)) {
				expandedReplacePattern = t.Item2;
				return t.Item1;
			}
			expandedReplacePattern = null;
			return null;
		}

		public SnapshotSpan? FindForReplace(SnapshotPoint startingPosition, string searchPattern, string replacePattern, FindOptions options, out string? expandedReplacePattern) {
			if (startingPosition.Snapshot is null)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));
			if (replacePattern is null)
				throw new ArgumentNullException(nameof(replacePattern));
			var searchRange = new SnapshotSpan(startingPosition.Snapshot, 0, startingPosition.Snapshot.Length);
			foreach (var t in FindAllForReplace(searchRange, startingPosition, searchPattern, replacePattern, options)) {
				expandedReplacePattern = t.Item2;
				return t.Item1;
			}
			expandedReplacePattern = null;
			return null;
		}

		public IEnumerable<Tuple<SnapshotSpan, string?>> FindAllForReplace(SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options) {
			if (searchRange.Snapshot is null)
				throw new ArgumentException();
			if (searchPattern is null)
				throw new ArgumentNullException(nameof(searchPattern));
			if (searchPattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(searchPattern));
			if (replacePattern is null)
				throw new ArgumentNullException(nameof(replacePattern));
			return FindAllForReplace(searchRange, searchRange.Start, searchPattern, replacePattern, options);
		}

		IEnumerable<Tuple<SnapshotSpan, string?>> FindAllForReplace(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, string replacePattern, FindOptions options) {
			var snapshot = searchRange.Snapshot;
			foreach (var res in FindAllCore(searchRange, startingPosition, searchPattern, options, replacePattern))
				yield return Tuple.Create(new SnapshotSpan(snapshot, res.Position, res.Length), res.ExpandedReplacePattern);
		}

		static StringComparison GetStringComparison(FindOptions options) {
			if ((options & FindOptions.OrdinalComparison) != 0)
				return (options & FindOptions.MatchCase) != 0 ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			return (options & FindOptions.MatchCase) != 0 ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
		}

		IEnumerable<FindResult> FindAllCore(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options, string? replacePattern) {
			if (!(searchRange.Start <= startingPosition && startingPosition <= searchRange.End))
				return Array.Empty<FindResult>();
			if ((options & FindOptions.Multiline) != 0)
				return FindAllCoreMultiline(searchRange, startingPosition, searchPattern, options, replacePattern);
			return FindAllCoreSingleLine(searchRange, startingPosition, searchPattern, options, replacePattern);
		}

		IEnumerable<FindResult> FindAllCoreSingleLine(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options, string? replacePattern) {
			Debug.Assert((options & FindOptions.Multiline) == 0);

			var firstLine = searchRange.Start.GetContainingLine();
			var lastLine = searchRange.End.GetContainingLine();
			var startLine = startingPosition.GetContainingLine();
			var stringComparison = GetStringComparison(options);
			if ((options & FindOptions.SearchReverse) != 0) {
				// reverse search

				var range1 = new SnapshotSpan(searchRange.Start, startingPosition);
				if (range1.Length > 0) {
					foreach (var res in FindAllSingleLineReverse(firstLine, startLine, range1, searchPattern, options, stringComparison, replacePattern))
						yield return res;
				}

				if ((options & FindOptions.Wrap) != 0) {
					var range2 = new SnapshotSpan(startLine.Start, searchRange.End);
					if (range2.Length > 0) {
						foreach (var res in FindAllSingleLineReverse(startLine, lastLine, range2, searchPattern, options, stringComparison, replacePattern)) {
							if (res.Position + res.Length <= startingPosition.Position)
								break;
							yield return res;
						}
					}
				}
			}
			else {
				// forward search

				var range1 = new SnapshotSpan(startingPosition, searchRange.End);
				if (range1.Length > 0) {
					foreach (var res in FindAllSingleLineForward(startLine, lastLine, range1, searchPattern, options, stringComparison, replacePattern))
						yield return res;
				}

				if ((options & FindOptions.Wrap) != 0) {
					var range2 = new SnapshotSpan(searchRange.Start, startLine.EndIncludingLineBreak);
					if (range2.Length > 0) {
						foreach (var res in FindAllSingleLineForward(firstLine, startLine, range2, searchPattern, options, stringComparison, replacePattern)) {
							if (res.Position >= startingPosition.Position)
								break;
							yield return res;
						}
					}
				}
			}
		}

		IEnumerable<FindResult> FindAllSingleLineForward(ITextSnapshotLine startLine, ITextSnapshotLine endLine, SnapshotSpan searchRange, string searchPattern, FindOptions options, StringComparison stringComparison, string? replacePattern) {
			Debug.Assert((options & FindOptions.SearchReverse) == 0);
			int endLineNumber = endLine.LineNumber;
			var snapshot = searchRange.Snapshot;
			bool onlyWords = (options & FindOptions.WholeWord) != 0;
			var regex = (options & FindOptions.UseRegularExpressions) != 0 ? GetRegex(searchPattern, options) : null;
			for (int lineNumber = startLine.LineNumber; lineNumber <= endLineNumber; lineNumber++) {
				var line = snapshot.GetLineFromLineNumber(lineNumber);
				var range = searchRange.Intersection(line.ExtentIncludingLineBreak);
				if (range is null || range.Value.Length == 0)
					continue;
				var text = range.Value.GetText();
				int index = 0;
				if (regex is not null) {
					foreach (var res in GetRegexResults(regex, range.Value.Start, text, index, searchPattern, options, replacePattern))
						yield return res;
				}
				else {
					while (index < text.Length) {
						index = text.IndexOf(searchPattern, index, text.Length - index, stringComparison);
						if (index < 0)
							break;
						if (!onlyWords || UnicodeUtilities.IsWord(line, range.Value.Start.Position - line.Start.Position + index, searchPattern.Length))
							yield return new FindResult(range.Value.Start.Position + index, searchPattern.Length, replacePattern);
						index++;
					}
				}
			}
		}

		IEnumerable<FindResult> FindAllSingleLineReverse(ITextSnapshotLine startLine, ITextSnapshotLine endLine, SnapshotSpan searchRange, string searchPattern, FindOptions options, StringComparison stringComparison, string? replacePattern) {
			Debug.Assert((options & FindOptions.SearchReverse) != 0);
			int startLineNumber = startLine.LineNumber;
			var snapshot = searchRange.Snapshot;
			bool onlyWords = (options & FindOptions.WholeWord) != 0;
			var regex = (options & FindOptions.UseRegularExpressions) != 0 ? GetRegex(searchPattern, options) : null;
			for (int lineNumber = endLine.LineNumber; lineNumber >= startLineNumber; lineNumber--) {
				var line = snapshot.GetLineFromLineNumber(lineNumber);
				var range = searchRange.Intersection(line.ExtentIncludingLineBreak);
				if (range is null || range.Value.Length == 0)
					continue;
				var text = range.Value.GetText();
				int index = text.Length;
				if (regex is not null) {
					foreach (var res in GetRegexResults(regex, range.Value.Start, text, index, searchPattern, options, replacePattern))
						yield return res;
				}
				else {
					while (index > 0) {
						index = text.LastIndexOf(searchPattern, index - 1, index, stringComparison);
						if (index < 0)
							break;
						if (!onlyWords || UnicodeUtilities.IsWord(line, range.Value.Start.Position - line.Start.Position + index, searchPattern.Length))
							yield return new FindResult(range.Value.Start.Position + index, searchPattern.Length, replacePattern);
						index += searchPattern.Length - 1;
					}
				}
			}
		}

		IEnumerable<FindResult> FindAllCoreMultiline(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options, string? replacePattern) {
			Debug.Assert((options & FindOptions.Multiline) == FindOptions.Multiline);

			var stringComparison = GetStringComparison(options);
			if ((options & FindOptions.SearchReverse) != 0) {
				// reverse search

				var searchText = searchRange.GetText();
				var range1 = new SnapshotSpan(searchRange.Start, startingPosition);
				if (range1.Length > 0) {
					foreach (var res in FindAllCoreMultilineReverse(searchText, searchRange.Start, startingPosition, searchPattern, options, stringComparison, replacePattern))
						yield return res;
				}

				if ((options & FindOptions.Wrap) != 0) {
					var range2 = new SnapshotSpan(startingPosition, searchRange.End);
					if (range2.Length > 0) {
						foreach (var res in FindAllCoreMultilineReverse(searchText, searchRange.Start, searchRange.End, searchPattern, options, stringComparison, replacePattern)) {
							if (res.Position + res.Length <= startingPosition.Position)
								break;
							yield return res;
						}
					}
				}
			}
			else {
				// forward search

				var searchText = searchRange.GetText();
				var range1 = new SnapshotSpan(startingPosition, searchRange.End);
				if (range1.Length > 0) {
					foreach (var res in FindAllCoreMultilineForward(searchText, searchRange.Start, startingPosition, searchPattern, options, stringComparison, replacePattern))
						yield return res;
				}

				if ((options & FindOptions.Wrap) != 0) {
					var range2 = new SnapshotSpan(searchRange.Start, startingPosition);
					if (range2.Length > 0) {
						foreach (var res in FindAllCoreMultilineForward(searchText, searchRange.Start, searchRange.Start, searchPattern, options, stringComparison, replacePattern)) {
							if (res.Position >= startingPosition.Position)
								break;
							yield return res;
						}
					}
				}
			}
		}

		static bool IsWord(ITextSnapshot snapshot, int position, int length) {
			bool valid = position >= 0 && length >= 0 && (uint)(position + length) <= (uint)snapshot.Length;
			Debug.Assert(valid);
			if (!valid)
				return false;
			var line = snapshot.GetLineFromPosition(position);
			return UnicodeUtilities.IsWord(line, position - line.Start.Position, length);
		}

		IEnumerable<FindResult> GetRegexResults(Regex regex, SnapshotPoint searchTextPosition, string searchText, int index, string searchPattern, FindOptions options, string? replacePattern) {
			bool onlyWords = (options & FindOptions.WholeWord) != 0;
			foreach (Match? match in regex.Matches(searchText, index)) {
				Debug2.Assert(match is not null);
				int position = searchTextPosition.Position + match.Index;
				if (!onlyWords || IsWord(searchTextPosition.Snapshot, position, searchPattern.Length))
					yield return new FindResult(position, match.Length, replacePattern is null ? null : match.Result(replacePattern));
			}
		}

		IEnumerable<FindResult> FindAllCoreMultilineForward(string searchText, SnapshotPoint searchTextPosition, SnapshotPoint startingPosition, string searchPattern, FindOptions options, StringComparison stringComparison, string? replacePattern) {
			Debug.Assert((options & FindOptions.SearchReverse) == 0);
			bool onlyWords = (options & FindOptions.WholeWord) != 0;
			int index = startingPosition - searchTextPosition;
			if ((options & FindOptions.UseRegularExpressions) != 0) {
				var regex = GetRegex(searchPattern, options);
				foreach (var res in GetRegexResults(regex, searchTextPosition, searchText, index, searchPattern, options, replacePattern))
					yield return res;
			}
			else {
				while (index < searchText.Length) {
					index = searchText.IndexOf(searchPattern, index, searchText.Length - index, stringComparison);
					if (index < 0)
						break;
					if (!onlyWords || IsWord(searchTextPosition.Snapshot, searchTextPosition.Position + index, searchPattern.Length))
						yield return new FindResult(searchTextPosition.Position + index, searchPattern.Length, replacePattern);
					index++;
				}
			}
		}

		IEnumerable<FindResult> FindAllCoreMultilineReverse(string searchText, SnapshotPoint searchTextPosition, SnapshotPoint startingPosition, string searchPattern, FindOptions options, StringComparison stringComparison, string? replacePattern) {
			Debug.Assert((options & FindOptions.SearchReverse) != 0);
			bool onlyWords = (options & FindOptions.WholeWord) != 0;
			int index = startingPosition - searchTextPosition;
			if ((options & FindOptions.UseRegularExpressions) != 0) {
				var regex = GetRegex(searchPattern, options);
				foreach (var res in GetRegexResults(regex, searchTextPosition, searchText, index, searchPattern, options, replacePattern))
					yield return res;
			}
			else {
				while (index > 0) {
					index = searchText.LastIndexOf(searchPattern, index - 1, index, stringComparison);
					if (index < 0)
						break;
					if (!onlyWords || IsWord(searchTextPosition.Snapshot, searchTextPosition.Position + index, searchPattern.Length))
						yield return new FindResult(searchTextPosition.Position + index, searchPattern.Length, replacePattern);
					index += searchPattern.Length - 1;
				}
			}
		}

		Regex GetRegex(string pattern, FindOptions options) {
			lock (cachedRegexDictLock) {
				CachedRegex cachedRegex;
				int index = FindCachedRegexIndex_NoLock(pattern, options);
				if (index >= 0) {
					cachedRegex = cachedRegexes[index];
					if (index + 1 != cachedRegexes.Count) {
						cachedRegexes.RemoveAt(index);
						cachedRegexes.Add(cachedRegex);
					}
					return cachedRegex.Regex;
				}
				// Could throw if pattern is invalid
				cachedRegex = new CachedRegex(pattern, options);
				if (cachedRegexes.Count >= MAX_CACHED_REGEXES)
					cachedRegexes.RemoveAt(0);
				cachedRegexes.Add(cachedRegex);
				return cachedRegex.Regex;
			}
		}

		int FindCachedRegexIndex_NoLock(string searchPattern, FindOptions options) {
			for (int i = cachedRegexes.Count - 1; i >= 0; i--) {
				if (cachedRegexes[i].Equals(searchPattern, options))
					return i;
			}
			return -1;
		}
	}
}
