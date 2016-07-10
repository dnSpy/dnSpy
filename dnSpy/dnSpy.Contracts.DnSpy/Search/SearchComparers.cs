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
using System.Globalization;
using System.Text.RegularExpressions;
using dnlib.DotNet;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// <see cref="ISearchComparer"/> factory
	/// </summary>
	public static class SearchComparerFactory {
		/// <summary>
		/// Creates a <see cref="ISearchComparer"/>
		/// </summary>
		/// <param name="searchText">Search text</param>
		/// <param name="caseSensitive">true if case sensitive</param>
		/// <param name="matchWholeWords">true to match whole words</param>
		/// <param name="matchAnyWords">true to match any word</param>
		/// <returns></returns>
		public static ISearchComparer Create(string searchText, bool caseSensitive, bool matchWholeWords, bool matchAnyWords) {
			var regex = TryCreateRegEx(searchText, caseSensitive);
			if (regex != null)
				return new RegExSearchComparer(regex);

			var searchTerms = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (matchAnyWords)
				return new OrSearchComparer(searchTerms, caseSensitive, matchWholeWords);
			return new AndSearchComparer(searchTerms, caseSensitive, matchWholeWords);
		}

		/// <summary>
		/// Creates a <see cref="ISearchComparer"/> that compares literals
		/// </summary>
		/// <param name="searchText">Search text</param>
		/// <param name="caseSensitive">true if case sensitive</param>
		/// <param name="matchWholeWords">true to match whole words</param>
		/// <param name="matchAnyWords">true to match any word</param>
		/// <returns></returns>
		public static ISearchComparer CreateLiteral(string searchText, bool caseSensitive, bool matchWholeWords, bool matchAnyWords) {
			var s = searchText.Trim();

			var val64 = TryParseInt64(s);
			if (val64 != null)
				return new IntegerLiteralSearchComparer(val64.Value);
			var uval64 = TryParseUInt64(s);
			if (uval64 != null)
				return new IntegerLiteralSearchComparer(unchecked((long)uval64.Value));
			double dbl;
			if (double.TryParse(s, out dbl))
				return new DoubleLiteralSearchComparer(dbl);

			if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
				s = s.Substring(1, s.Length - 2);
			else {
				var regex = TryCreateRegEx(s, caseSensitive);
				if (regex != null)
					return new RegExStringLiteralSearchComparer(regex);
			}
			return new StringLiteralSearchComparer(s, caseSensitive, matchWholeWords);
		}

		static Regex TryCreateRegEx(string s, bool caseSensitive) {
			s = s.Trim();
			if (s.Length > 2 && s[0] == '/' && s[s.Length - 1] == '/') {
				var regexOpts = RegexOptions.Compiled;
				if (!caseSensitive)
					regexOpts |= RegexOptions.IgnoreCase;
				try {
					return new Regex(s.Substring(1, s.Length - 2), regexOpts);
				}
				catch (ArgumentException) {
				}
			}
			return null;
		}

		static long? TryParseInt64(string s) {
			bool isSigned = s.StartsWith("-", StringComparison.OrdinalIgnoreCase);
			if (isSigned)
				s = s.Substring(1);
			if (s != s.Trim())
				return null;
			ulong val;
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				s = s.Substring(2);
				if (s != s.Trim())
					return null;
				if (!ulong.TryParse(s, NumberStyles.HexNumber, null, out val))
					return null;
			}
			else {
				if (!ulong.TryParse(s, out val))
					return null;
			}
			if (isSigned) {
				if (val > (ulong)long.MaxValue + 1)
					return null;
				return unchecked(-(long)val);
			}
			else {
				if (val > long.MaxValue)
					return null;
				return (long)val;
			}
		}

		static ulong? TryParseUInt64(string s) {
			ulong val;
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				s = s.Substring(2);
				if (s != s.Trim())
					return null;
				if (!ulong.TryParse(s, NumberStyles.HexNumber, null, out val))
					return null;
			}
			else {
				if (!ulong.TryParse(s, out val))
					return null;
			}
			return val;
		}
	}

	sealed class RegExStringLiteralSearchComparer : ISearchComparer {
		readonly Regex regex;

		public RegExStringLiteralSearchComparer(Regex regex) {
			if (regex == null)
				throw new ArgumentNullException();
			this.regex = regex;
		}

		public bool IsMatch(string text, object obj) {
			var hc = obj as IHasConstant;
			if (hc != null && hc.Constant != null)
				obj = hc.Constant.Value;

			text = obj as string;
			return text != null && regex.IsMatch(text);
		}
	}

	sealed class StringLiteralSearchComparer : ISearchComparer {
		readonly string str;
		readonly StringComparison stringComparison;
		readonly bool matchWholeString;

		public StringLiteralSearchComparer(string s, bool caseSensitive = false, bool matchWholeString = false) {
			if (s == null)
				throw new ArgumentNullException();
			this.str = s;
			this.stringComparison = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			this.matchWholeString = matchWholeString;
		}

		public bool IsMatch(string text, object obj) {
			var hc = obj as IHasConstant;
			if (hc != null && hc.Constant != null)
				obj = hc.Constant.Value;

			text = obj as string;
			if (text == null)
				return false;
			if (matchWholeString)
				return text.Equals(str, stringComparison);
			return text.IndexOf(str, stringComparison) >= 0;
		}
	}

	sealed class IntegerLiteralSearchComparer : ISearchComparer {
		readonly long searchValue;

		public IntegerLiteralSearchComparer(long value) {
			this.searchValue = value;
		}

		public bool IsMatch(string text, object obj) {
			var hc = obj as IHasConstant;
			if (hc != null && hc.Constant != null)
				obj = hc.Constant.Value;
			if (obj == null)
				return false;

			switch (Type.GetTypeCode(obj.GetType())) {
			case TypeCode.Char:		return searchValue == (char)obj;
			case TypeCode.SByte:	return searchValue == (sbyte)obj;
			case TypeCode.Byte:		return searchValue == (byte)obj;
			case TypeCode.Int16:	return searchValue == (short)obj;
			case TypeCode.UInt16:	return searchValue == (ushort)obj;
			case TypeCode.Int32:	return searchValue == (int)obj;
			case TypeCode.UInt32:	return searchValue == (uint)obj;
			case TypeCode.Int64:	return searchValue == (long)obj;
			case TypeCode.UInt64:	return searchValue == unchecked((long)(ulong)obj);
			case TypeCode.Single:	return searchValue == (float)obj;
			case TypeCode.Double:	return searchValue == (double)obj;
			case TypeCode.Decimal:	return searchValue == (decimal)obj;
			case TypeCode.DateTime: return new DateTime(searchValue) == (DateTime)obj;
			}

			return false;
		}
	}

	sealed class DoubleLiteralSearchComparer : ISearchComparer {
		readonly double searchValue;

		public DoubleLiteralSearchComparer(double value) {
			this.searchValue = value;
		}

		public bool IsMatch(string text, object obj) {
			var hc = obj as IHasConstant;
			if (hc != null && hc.Constant != null)
				obj = hc.Constant.Value;
			if (obj == null)
				return false;

			switch (Type.GetTypeCode(obj.GetType())) {
			case TypeCode.Char:		return searchValue == (char)obj;
			case TypeCode.SByte:	return searchValue == (sbyte)obj;
			case TypeCode.Byte:		return searchValue == (byte)obj;
			case TypeCode.Int16:	return searchValue == (short)obj;
			case TypeCode.UInt16:	return searchValue == (ushort)obj;
			case TypeCode.Int32:	return searchValue == (int)obj;
			case TypeCode.UInt32:	return searchValue == (uint)obj;
			case TypeCode.Int64:	return searchValue == (long)obj;
			case TypeCode.UInt64:	return searchValue == (ulong)obj;
			case TypeCode.Single:	return searchValue == (float)obj;
			case TypeCode.Double:	return searchValue == (double)obj;
			}

			return false;
		}
	}

	sealed class RegExSearchComparer : ISearchComparer {
		readonly Regex regex;

		public RegExSearchComparer(Regex regex) {
			if (regex == null)
				throw new ArgumentNullException();
			this.regex = regex;
		}

		public bool IsMatch(string text, object obj) {
			if (text == null)
				return false;
			return regex.IsMatch(text);
		}
	}

	sealed class AndSearchComparer : ISearchComparer {
		readonly string[] searchTerms;
		readonly StringComparison stringComparison;
		readonly bool matchWholeWords;

		public AndSearchComparer(string[] searchTerms, bool caseSensitive = false, bool matchWholeWords = false)
			: this(searchTerms, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase, matchWholeWords) {
		}

		public AndSearchComparer(string[] searchTerms, StringComparison stringComparison, bool matchWholeWords = false) {
			this.searchTerms = searchTerms;
			this.stringComparison = stringComparison;
			this.matchWholeWords = matchWholeWords;
		}

		public bool IsMatch(string text, object obj) {
			if (text == null)
				return false;
			foreach (var searchTerm in searchTerms) {
				if (matchWholeWords) {
					if (!text.Equals(searchTerm, stringComparison))
						return false;
				}
				else {
					if (text.IndexOf(searchTerm, stringComparison) < 0)
						return false;
				}
			}

			return true;
		}
	}

	sealed class OrSearchComparer : ISearchComparer {
		readonly string[] searchTerms;
		readonly StringComparison stringComparison;
		readonly bool matchWholeWords;

		public OrSearchComparer(string[] searchTerms, bool caseSensitive = false, bool matchWholeWords = false)
			: this(searchTerms, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase, matchWholeWords) {
		}

		public OrSearchComparer(string[] searchTerms, StringComparison stringComparison, bool matchWholeWords = false) {
			this.searchTerms = searchTerms;
			this.stringComparison = stringComparison;
			this.matchWholeWords = matchWholeWords;
		}

		public bool IsMatch(string text, object obj) {
			if (text == null)
				return false;
			foreach (var searchTerm in searchTerms) {
				if (matchWholeWords) {
					if (text.Equals(searchTerm, stringComparison))
						return true;
				}
				else {
					if (text.IndexOf(searchTerm, stringComparison) >= 0)
						return true;
				}
			}

			return false;
		}
	}
}
