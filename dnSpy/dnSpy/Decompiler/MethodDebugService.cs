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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Decompiler {
	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_METHODDEBUGSERVICE)]
	sealed class MethodDebugServiceDocumentViewerListener : IDocumentViewerListener {
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		MethodDebugServiceDocumentViewerListener(IModuleIdProvider moduleIdProvider) => this.moduleIdProvider = moduleIdProvider;

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.GotNewContent)
				AddMethodDebugService(e.DocumentViewer, ((DocumentViewerGotNewContentEventArgs)e).Content);
		}

		void AddMethodDebugService(IDocumentViewer documentViewer, DocumentViewerContent content) {
			if (content is null)
				return;
			var service = new MethodDebugService(content.MethodDebugInfos, documentViewer.TextView.TextSnapshot, moduleIdProvider);
			documentViewer.AddContentData(MethodDebugServiceConstants.MethodDebugServiceKey, service);
		}
	}

	sealed class MethodDebugService : IMethodDebugService {
		readonly Dictionary<ModuleTokenId, MethodDebugInfo> dict;
		readonly ITextSnapshot snapshot;
		readonly IModuleIdProvider moduleIdProvider;
		MethodSourceStatement[]? sortedStatements;

		public int Count => dict.Count;

		public MethodDebugService(IReadOnlyList<MethodDebugInfo> methodDebugInfos, ITextSnapshot snapshot, IModuleIdProvider moduleIdProvider) {
			if (methodDebugInfos is null)
				throw new ArgumentNullException(nameof(methodDebugInfos));
			dict = new Dictionary<ModuleTokenId, MethodDebugInfo>(methodDebugInfos.Count);
			this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
			this.moduleIdProvider = moduleIdProvider ?? throw new ArgumentNullException(nameof(moduleIdProvider));

			var modIdDict = new Dictionary<ModuleDef, ModuleId>();
			foreach (var info in methodDebugInfos) {
				var module = info.Method.Module;
				if (module is null)
					continue;

				if (!modIdDict.TryGetValue(module, out var moduleId)) {
					moduleId = moduleIdProvider.Create(module);
					modIdDict.Add(module, moduleId);
				}
				var key = new ModuleTokenId(moduleId, info.Method.MDToken);
				if (dict.TryGetValue(key, out var oldDebugInfo)) {
					if (info.Statements.Length < oldDebugInfo.Statements.Length)
						continue;
				}
				dict[key] = info;
			}
		}

		public IList<MethodSourceStatement> FindByTextPosition(int textPosition, FindByTextPositionOptions options) {
			// We're called by the bookmark and BP code, and they pass in the same input
			if (resultFindByTextPosition.result is null || resultFindByTextPosition.textPosition != textPosition || resultFindByTextPosition.options != options)
				resultFindByTextPosition = (FindByTextPositionCore(textPosition, options), textPosition, options);
			return resultFindByTextPosition.result;
		}
		(IList<MethodSourceStatement> result, int textPosition, FindByTextPositionOptions options) resultFindByTextPosition;

		IList<MethodSourceStatement> FindByTextPositionCore(int textPosition, FindByTextPositionOptions options) {
			if (textPosition < 0)
				throw new ArgumentOutOfRangeException(nameof(textPosition));
			if (dict.Count == 0)
				return Array.Empty<MethodSourceStatement>();

			if (textPosition > snapshot.Length)
				return Array.Empty<MethodSourceStatement>();

			if ((options & FindByTextPositionOptions.OuterMostStatement) != 0 && TryGetOuterMostSpan(textPosition, out var outermostSpan))
				textPosition = outermostSpan.Start;
			var line = snapshot.GetLineFromPosition(textPosition);

			var scopeSpan = GetScopeSpan(textPosition);
			var lineStartPos = Math.Max(line.Start.Position, scopeSpan.Start);
			var lineEndPos = Math.Min(line.End.Position, scopeSpan.End);
			if (lineStartPos >= lineEndPos)
				return Array.Empty<MethodSourceStatement>();

			var methodStatements = FindByLineAndTextOffset(scopeSpan, lineStartPos, lineEndPos, textPosition);
			if (methodStatements is null && lineStartPos != textPosition)
				methodStatements = FindByLineAndTextOffset(scopeSpan, lineStartPos, lineEndPos, lineStartPos);
			if (!(methodStatements is null) && methodStatements.Count > 1) {
				// If there are two methods (get; set;) on the same line, only return one of them
				var exact = methodStatements.Where(a => a.Statement.TextSpan != scopeSpan && a.Statement.TextSpan.Contains(textPosition)).ToList();
				if (exact.Count != 0)
					methodStatements = exact;
				else
					methodStatements = null;
			}
			if (methodStatements is null)
				methodStatements = GetClosest(lineStartPos, lineEndPos, textPosition);

			methodStatements = Filter(methodStatements, textPosition);

			if (!(methodStatements is null)) {
				if ((options & FindByTextPositionOptions.SameMethod) == 0 || IsSameMethod(methodStatements, textPosition))
					return methodStatements;
			}
			return Array.Empty<MethodSourceStatement>();
		}

		TextSpan GetScopeSpan(int textPosition) {
			int stmtIndex = GetScopeSpanStartIndex(textPosition);
			Debug2.Assert(!(sortedStatements is null));
			if (stmtIndex >= 0) {
				var scopeSpan = sortedStatements[stmtIndex].Statement.TextSpan;
				if (scopeSpan.Contains(textPosition))
					return scopeSpan;
			}
			return new TextSpan(0, snapshot.Length);
		}

		List<MethodSourceStatement>? Filter(List<MethodSourceStatement>? methodStatements, int textPosition) {
			if (methodStatements is null || methodStatements.Count <= 1)
				return methodStatements;
			var res = new List<MethodSourceStatement>();
			foreach (var info in methodStatements) {
				if (info.Statement.TextSpan.Contains(textPosition)) {
					if (res.Count == 0)
						res.Add(info);
					else {
						var other = res[0];
						if (other.Statement.TextSpan == info.Statement.TextSpan)
							res.Add(info);
						else if (info.Statement.TextSpan.Length < other.Statement.TextSpan.Length) {
							res.Clear();
							res.Add(info);
						}
					}
				}
			}
			if (res.Count != 0)
				return res;
			return methodStatements;
		}

		bool IsSameMethod(List<MethodSourceStatement> methodStatements, int textPosition) {
			if (methodStatements.Count == 0)
				return false;
			var methodInfo = TryGetMethodDebugInfo(methodStatements[0].Method);
			Debug2.Assert(!(methodInfo is null));
			if (methodInfo is null)
				return false;
			var methodSpan = methodInfo.Span;
			if (methodSpan.Contains(textPosition))
				return true;

			// If it's a field initializer the statement isn't within the method
			if (methodInfo.Method.IsConstructor) {
				foreach (var statement in methodInfo.Statements) {
					// Allow end position too since it's probably at the end of the line
					if (statement.TextSpan.Intersects(textPosition))
						return true;
				}
			}
			return false;
		}

		List<MethodSourceStatement>? FindByLineAndTextOffset(TextSpan scopeSpan, int lineStart, int lineEnd, int textPosition) {
			List<MethodSourceStatement>? list = null;
			foreach (var kv in dict) {
				var info = kv.Value;
				var sourceStatement = info.GetSourceStatementByTextOffset(lineStart, lineEnd, textPosition);
				if (!(sourceStatement is null) && sourceStatement.Value.TextSpan.Start >= scopeSpan.Start && sourceStatement.Value.TextSpan.End <= scopeSpan.End) {
					if (list is null)
						list = new List<MethodSourceStatement>();
					list.Add(new MethodSourceStatement(info.Method, sourceStatement.Value));
				}
			}
			return list;
		}

		List<MethodSourceStatement>? GetClosest(int lineStart, int lineEnd, int textPosition) {
			var list = new List<MethodSourceStatement>();
			foreach (var kv in dict) {
				var info = kv.Value;
				MethodSourceStatement? methodSourceStatement = null;
				foreach (var sourceStatement in info.Statements) {
					if (lineStart >= sourceStatement.TextSpan.End)
						continue;
					if (methodSourceStatement is null)
						methodSourceStatement = new MethodSourceStatement(info.Method, sourceStatement);
					else {
						var d1 = GetDist(sourceStatement.TextSpan, textPosition);
						var d2 = GetDist(methodSourceStatement.Value.Statement.TextSpan, textPosition);
						if (d1 < d2 || (d1 == d2 && sourceStatement.TextSpan.Start < methodSourceStatement.Value.Statement.TextSpan.Start))
							methodSourceStatement = new MethodSourceStatement(info.Method, sourceStatement);
					}
				}
				if (!(methodSourceStatement is null)) {
					if (list.Count == 0)
						list.Add(methodSourceStatement.Value);
					else if (methodSourceStatement.Value.Statement.TextSpan.Start == list[0].Statement.TextSpan.Start)
						list.Add(methodSourceStatement.Value);
					else if (GetDist(methodSourceStatement.Value.Statement.TextSpan, textPosition) < GetDist(list[0].Statement.TextSpan, textPosition)) {
						list.Clear();
						list.Add(methodSourceStatement.Value);
					}
				}
			}

			if (list.Count == 0)
				return null;
			return list;
		}

		static int GetDist(TextSpan span, int textPosition) {
			int a = Math.Abs(span.Start - textPosition);
			int b = Math.Abs(span.End - textPosition);
			return Math.Min(a, b);
		}

		public MethodSourceStatement? FindByCodeOffset(MethodDef method, uint codeOffset) =>
			FindByCodeOffset(new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken), codeOffset);

		public MethodSourceStatement? FindByCodeOffset(ModuleTokenId token, uint codeOffset) {
			if (!dict.TryGetValue(token, out var info))
				return null;
			foreach (var sourceStatement in info.Statements) {
				if (sourceStatement.ILSpan.Start <= codeOffset && codeOffset < sourceStatement.ILSpan.End)
					return new MethodSourceStatement(info.Method, sourceStatement);
			}
			return null;
		}

		public MethodDebugInfo? TryGetMethodDebugInfo(MethodDef method) =>
			TryGetMethodDebugInfo(new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken));

		public MethodDebugInfo? TryGetMethodDebugInfo(ModuleTokenId token) {
			dict.TryGetValue(token, out var info);
			return info;
		}

		public IEnumerable<MethodSourceStatement> GetStatementsByTextSpan(Span span) {
			int position = span.Start;
			int end = span.End;
			int index = GetStartIndex(position);
			Debug2.Assert(!(sortedStatements is null));
			if (index < 0)
				yield break;
			var array = sortedStatements;
			while (index < array.Length) {
				var mss = array[index++];
				if (end < mss.Statement.TextSpan.Start)
					break;
				if (mss.Statement.TextSpan.End >= position)
					yield return mss;
			}
		}

		int GetStartIndex(int position) {
			if (sortedStatements is null)
				InitializeSortedStatements();
			return GetStartIndexCore(position);
		}

		bool TryGetOuterMostSpan(int position, out TextSpan outermostSpan) {
			outermostSpan = default;

			TextSpan span = default;
			foreach (var kv in dict) {
				foreach (var s in kv.Value.Statements) {
					if (s.TextSpan.Contains(position) && s.TextSpan.Length > span.Length)
						span = s.TextSpan;
				}
			}

			outermostSpan = span;
			return span.Length > 0;
		}

		int GetScopeSpanStartIndex(int position) {
			if (sortedStatements is null)
				InitializeSortedStatements();
			Debug2.Assert(!(sortedStatements is null));

			int index = GetStartIndexCore(position);
			var array = sortedStatements;
			if ((uint)index >= (uint)array.Length)
				return -1;
			TextSpan span;
			while ((uint)index < (uint)array.Length) {
				span = array[index].Statement.TextSpan;
				if (span.Contains(position))
					break;
				index--;
			}
			if ((uint)index >= (uint)array.Length)
				return -1;
			while ((uint)index < (uint)array.Length) {
				span = array[index].Statement.TextSpan;
				if (!span.Contains(position))
					break;
				index++;
			}
			index--;
			span = array[index].Statement.TextSpan;
			if (!span.Contains(position))
				return -1;
			while (index - 1 >= 0 && array[index - 1].Statement.TextSpan == span)
				index--;
			return index;
		}

		int GetStartIndexCore(int position) {
			Debug2.Assert(!(sortedStatements is null));
			var array = sortedStatements;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var mss = array[index];
				if (position < mss.Statement.TextSpan.Start)
					hi = index - 1;
				else if (position >= mss.Statement.TextSpan.End)
					lo = index + 1;
				else {
					if (index > 0 && array[index - 1].Statement.TextSpan.End == position)
						return index - 1;
					return index;
				}
			}
			if ((uint)hi < (uint)array.Length && array[hi].Statement.TextSpan.End == position)
				return hi;
			return lo < array.Length ? lo : -1;
		}

		void InitializeSortedStatements() {
			Debug2.Assert(sortedStatements is null);
			if (!(sortedStatements is null))
				return;
			var list = new List<MethodSourceStatement>();
			foreach (var kv in dict) {
				var info = kv.Value;
				foreach (var s in info.Statements)
					list.Add(new MethodSourceStatement(info.Method, s));
			}
			list.Sort(MethodSourceStatementComparer.Instance);
			sortedStatements = list.ToArray();
		}

		sealed class MethodSourceStatementComparer : IComparer<MethodSourceStatement> {
			public static readonly MethodSourceStatementComparer Instance = new MethodSourceStatementComparer();

			public int Compare([AllowNull] MethodSourceStatement x, [AllowNull] MethodSourceStatement y) {
				var tsx = x.Statement.TextSpan;
				var tsy = y.Statement.TextSpan;
				int c = tsx.Start - tsy.Start;
				if (c != 0)
					return c;
				return tsx.End - tsy.End;
			}
		}
	}
}
