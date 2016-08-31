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
using System.Diagnostics;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Method statements
	/// </summary>
	public sealed class MethodDebugInfo {
		/// <summary>
		/// Gets the method
		/// </summary>
		public MethodDef Method { get; }

		/// <summary>
		/// Gets all statements, sorted by <see cref="BinSpan.Start"/>
		/// </summary>
		public SourceStatement[] Statements { get; }

		/// <summary>
		/// Gets all locals used by the decompiler. This list can be a subset of the
		/// real locals in the method.
		/// </summary>
		public SourceLocal[] Locals { get; }

		/// <summary>
		/// Method span or the default value (position 0, length 0) if it's not known
		/// </summary>
		public TextSpan Span { get; }

		/// <summary>
		/// true if <see cref="Span"/> is a valid method span
		/// </summary>
		public bool HasSpan => Span.Start != 0 && Span.End != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="statements">Statements</param>
		/// <param name="locals">Locals</param>
		/// <param name="methodSpan">Method span or null to calculate it from <paramref name="statements"/></param>
		public MethodDebugInfo(MethodDef method, SourceStatement[] statements, SourceLocal[] locals, TextSpan? methodSpan) {
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (statements == null)
				throw new ArgumentNullException(nameof(statements));
			if (locals == null)
				throw new ArgumentNullException(nameof(locals));
			Method = method;
			if (statements.Length > 1)
				Array.Sort(statements, SourceStatement.SpanStartComparer);
			Statements = statements;
			Locals = locals;
			Span = methodSpan ?? CalculateMethodSpan(statements) ?? new TextSpan(0, 0);
		}

		static TextSpan? CalculateMethodSpan(SourceStatement[] statements) {
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (var statement in statements) {
				if (min > statement.TextSpan.Start)
					min = statement.TextSpan.Start;
				if (max < statement.TextSpan.End)
					max = statement.TextSpan.End;
			}
			return min <= max ? TextSpan.FromBounds(min, max) : (TextSpan?)null;
		}

		/// <summary>
		/// Gets step ranges
		/// </summary>
		/// <param name="sourceStatement">Source statement</param>
		/// <returns></returns>
		public uint[] GetRanges(SourceStatement sourceStatement) {
			var list = new List<BinSpan>(GetUnusedBinSpans().Length + 1);
			list.Add(sourceStatement.BinSpan);
			list.AddRange(GetUnusedBinSpans());

			var orderedList = BinSpan.OrderAndCompactList(list);
			if (orderedList.Count == 0)
				return Array.Empty<uint>();
			var binSpanArray = new uint[orderedList.Count * 2];
			for (int i = 0; i < orderedList.Count; i++) {
				binSpanArray[i * 2 + 0] = orderedList[i].Start;
				binSpanArray[i * 2 + 1] = orderedList[i].End;
			}
			return binSpanArray;
		}

		/// <summary>
		/// Gets unused step ranges
		/// </summary>
		/// <returns></returns>
		public uint[] GetUnusedRanges() {
			var orderedList = GetUnusedBinSpans();
			if (orderedList.Length == 0)
				return Array.Empty<uint>();
			var binSpanArray = new uint[orderedList.Length * 2];
			for (int i = 0; i < orderedList.Length; i++) {
				binSpanArray[i * 2 + 0] = orderedList[i].Start;
				binSpanArray[i * 2 + 1] = orderedList[i].End;
			}
			return binSpanArray;
		}

		BinSpan[] GetUnusedBinSpans() {
			if (cachedUnusedBinSpans != null)
				return cachedUnusedBinSpans;
			var list = new List<BinSpan>(Statements.Length);
			foreach (var s in Statements)
				list.Add(s.BinSpan);
			return cachedUnusedBinSpans = GetUnusedBinSpans(list).ToArray();
		}
		BinSpan[] cachedUnusedBinSpans;

		List<BinSpan> GetUnusedBinSpans(List<BinSpan> list) {
			uint codeSize = (uint)Method.Body.GetCodeSize();
			list = BinSpan.OrderAndCompact(list);
			var res = new List<BinSpan>();
			if (list.Count == 0) {
				if (codeSize > 0)
					res.Add(new BinSpan(0, codeSize));
				return res;
			}
			uint prevEnd = 0;
			for (int i = 0; i < list.Count; i++) {
				var span = list[i];
				Debug.Assert(span.Start >= prevEnd);
				uint length = span.Start - prevEnd;
				if (length > 0)
					res.Add(new BinSpan(prevEnd, length));
				prevEnd = span.End;
			}
			Debug.Assert(prevEnd <= codeSize);
			if (prevEnd < codeSize)
				res.Add(new BinSpan(prevEnd, codeSize - prevEnd));
			return res;
		}

		/// <summary>
		/// Gets a <see cref="SourceStatement"/>
		/// </summary>
		/// <param name="lineStart">Offset of start of line</param>
		/// <param name="lineEnd">Offset of end of line</param>
		/// <param name="textPosition">Position in text document</param>
		/// <returns></returns>
		public SourceStatement? GetSourceStatementByTextOffset(int lineStart, int lineEnd, int textPosition) {
			if (lineStart >= Span.End || lineEnd < Span.Start)
				return null;

			SourceStatement? intersection = null;
			foreach (var statement in Statements) {
				if (statement.TextSpan.Start <= textPosition) {
					if (textPosition < statement.TextSpan.End)
						return statement;
					if (textPosition == statement.TextSpan.End)
						intersection = statement;
				}
			}
			if (intersection != null)
				return intersection;

			var list = new List<SourceStatement>();
			foreach (var statement in Statements) {
				if (lineStart < statement.TextSpan.End && lineEnd > statement.TextSpan.Start)
					list.Add(statement);
			}
			list.Sort((a, b) => {
				var d = Math.Abs(a.TextSpan.Start - textPosition) - Math.Abs(b.TextSpan.Start - textPosition);
				if (d != 0)
					return d;
				return (int)(a.BinSpan.Start - b.BinSpan.Start);
			});
			if (list.Count > 0)
				return list[0];
			return null;
		}

		/// <summary>
		/// Gets a <see cref="SourceStatement"/>
		/// </summary>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		public SourceStatement? GetSourceStatementByCodeOffset(uint ilOffset) {
			foreach (var statement in Statements) {
				if (statement.BinSpan.Start <= ilOffset && ilOffset < statement.BinSpan.End)
					return statement;
			}
			return null;
		}
	}
}
