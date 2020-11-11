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
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Method debug info
	/// </summary>
	public sealed class DbgMethodDebugInfo {
		/// <summary>
		/// Compiler used to compile the code
		/// </summary>
		public DbgCompilerKind Compiler { get; }

		/// <summary>
		/// Version number of this method debug info. If it gets incremented, any older instances with a different
		/// version should not be used again.
		/// </summary>
		public int DebugInfoVersion { get; }

		/// <summary>
		/// Gets the method
		/// </summary>
		public MethodDef Method { get; }

		/// <summary>
		/// Gets the parameters. There could be missing parameters, in which case use <see cref="Method"/>. This array isn't sorted.
		/// </summary>
		public DbgParameter[] Parameters { get; }

		/// <summary>
		/// Gets all statements, sorted by <see cref="DbgILSpan.Start"/>
		/// </summary>
		public DbgSourceStatement[] Statements { get; }

		/// <summary>
		/// Gets the async method debug info or null if it's not an async method
		/// </summary>
		public DbgAsyncMethodDebugInfo? AsyncInfo { get; }

		/// <summary>
		/// Gets the root scope
		/// </summary>
		public DbgMethodDebugScope Scope { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="compiler">Compiler</param>
		/// <param name="debugInfoVersion">Debug info version</param>
		/// <param name="method">Method</param>
		/// <param name="parameters">Parameters or null</param>
		/// <param name="statements">Statements</param>
		/// <param name="scope">Root scope</param>
		/// <param name="asyncMethodDebugInfo">Async info or null</param>
		public DbgMethodDebugInfo(DbgCompilerKind compiler, int debugInfoVersion, MethodDef method, DbgParameter[]? parameters, DbgSourceStatement[] statements, DbgMethodDebugScope scope, DbgAsyncMethodDebugInfo? asyncMethodDebugInfo) {
			if (statements is null)
				throw new ArgumentNullException(nameof(statements));
			Compiler = compiler;
			Method = method ?? throw new ArgumentNullException(nameof(method));
			Parameters = parameters ?? Array.Empty<DbgParameter>();
			if (statements.Length > 1)
				Array.Sort(statements, DbgSourceStatement.SpanStartComparer);
			DebugInfoVersion = debugInfoVersion;
			Statements = statements;
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			AsyncInfo = asyncMethodDebugInfo;
		}

		/// <summary>
		/// Gets step ranges
		/// </summary>
		/// <param name="sourceILSpans">Source statement spans</param>
		/// <returns></returns>
		public DbgILSpan[] GetRanges(DbgILSpan[] sourceILSpans) {
			var list = new List<DbgILSpan>(sourceILSpans.Length + GetUnusedILSpans().Length + 1);
			list.AddRange(sourceILSpans);
			list.AddRange(GetUnusedILSpans());
			return DbgILSpan.OrderAndCompactList(list).ToArray();
		}

		/// <summary>
		/// Gets unused step ranges
		/// </summary>
		/// <returns></returns>
		public DbgILSpan[] GetUnusedRanges() => GetUnusedILSpans();

		DbgILSpan[] GetUnusedILSpans() {
			if (cachedUnusedILSpans is not null)
				return cachedUnusedILSpans;
			var list = new List<DbgILSpan>(Statements.Length);
			foreach (var s in Statements)
				list.Add(s.ILSpan);
			return cachedUnusedILSpans = GetUnusedILSpans(list).ToArray();
		}
		DbgILSpan[]? cachedUnusedILSpans;

		List<DbgILSpan> GetUnusedILSpans(List<DbgILSpan> list) {
			uint codeSize = (uint)GetCodeSize(Method.Body);
			list = DbgILSpan.OrderAndCompactList(list);
			var res = new List<DbgILSpan>();
			if (list.Count == 0) {
				if (codeSize > 0)
					res.Add(new DbgILSpan(0, codeSize));
				return res;
			}
			uint prevEnd = 0;
			for (int i = 0; i < list.Count; i++) {
				var span = list[i];
				Debug.Assert(span.Start >= prevEnd);
				uint length = span.Start - prevEnd;
				if (length > 0)
					res.Add(new DbgILSpan(prevEnd, length));
				prevEnd = span.End;
			}
			Debug.Assert(prevEnd <= codeSize);
			if (prevEnd < codeSize)
				res.Add(new DbgILSpan(prevEnd, codeSize - prevEnd));
			return res;
		}

		static int GetCodeSize(CilBody? body) {
			if (body is null || body.Instructions.Count == 0)
				return 0;
			var instr = body.Instructions[body.Instructions.Count - 1];
			return (int)instr.Offset + instr.GetSize();
		}

		/// <summary>
		/// Gets a <see cref="DbgSourceStatement"/>
		/// </summary>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		public DbgSourceStatement? GetSourceStatementByCodeOffset(uint ilOffset) {
			foreach (var statement in Statements) {
				if (statement.ILSpan.Start <= ilOffset && ilOffset < statement.ILSpan.End)
					return statement;
			}
			return null;
		}

		/// <summary>
		/// Gets all ILSpans of a statement
		/// </summary>
		/// <param name="statementSpan">Statement span</param>
		/// <returns></returns>
		public DbgILSpan[] GetILSpansOfStatement(DbgTextSpan statementSpan) {
			if (statementsDict is null)
				Interlocked.CompareExchange(ref statementsDict, CreateStatementsDict(Statements), null);
			Debug2.Assert(statementsDict is not null);
			if (statementsDict.TryGetValue(statementSpan, out var list)) {
				var spans = list.ToArray();
#if DEBUG
				for (int i = 1; i < spans.Length; i++)
					Debug.Assert(spans[i - 1].End <= spans[i].Start);
#endif
				return spans;
			}
			return Array.Empty<DbgILSpan>();
		}
		Dictionary<DbgTextSpan, SmallList<DbgILSpan>>? statementsDict;

		static Dictionary<DbgTextSpan, SmallList<DbgILSpan>> CreateStatementsDict(DbgSourceStatement[] statements) {
			var dict = new Dictionary<DbgTextSpan, SmallList<DbgILSpan>>(statements.Length);
			foreach (var statement in statements) {
				dict.TryGetValue(statement.TextSpan, out var list);
				list.Add(statement.ILSpan);
				dict[statement.TextSpan] = list;
			}
			return dict;
		}
	}

	struct SmallList<T> {
		T firstValue;
		bool hasFirstValue;
		List<T>? list;

		public void Add(T value) {
			if (!hasFirstValue) {
				firstValue = value;
				hasFirstValue = true;
			}
			else {
				if (list is null)
					list = new List<T>(2) { firstValue };
				list.Add(value);
			}
		}

		public T[] ToArray() {
			if (list is not null)
				return list.ToArray();
			if (hasFirstValue)
				return new[] { firstValue };
			return Array.Empty<T>();
		}
	}
}
