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
using System.Diagnostics;
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
		MethodDebugServiceDocumentViewerListener(IModuleIdProvider moduleIdProvider) {
			this.moduleIdProvider = moduleIdProvider;
		}

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.GotNewContent)
				AddMethodDebugService(e.DocumentViewer, ((DocumentViewerGotNewContentEventArgs)e).Content);
		}

		void AddMethodDebugService(IDocumentViewer documentViewer, DocumentViewerContent content) {
			if (content == null)
				return;
			var service = new MethodDebugService(content.MethodDebugInfos, documentViewer.TextView.TextSnapshot, moduleIdProvider);
			documentViewer.AddContentData(MethodDebugServiceConstants.MethodDebugServiceKey, service);
		}
	}

	sealed class MethodDebugService : IMethodDebugService {
		readonly Dictionary<ModuleTokenId, MethodDebugInfo> dict;
		readonly ITextSnapshot snapshot;
		readonly IModuleIdProvider moduleIdProvider;
		MethodSourceStatement[] sortedStatements;

		public int Count => dict.Count;

		public MethodDebugService(IReadOnlyList<MethodDebugInfo> methodDebugInfos, ITextSnapshot snapshot, IModuleIdProvider moduleIdProvider) {
			if (methodDebugInfos == null)
				throw new ArgumentNullException(nameof(methodDebugInfos));
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (moduleIdProvider == null)
				throw new ArgumentNullException(nameof(moduleIdProvider));

			dict = new Dictionary<ModuleTokenId, MethodDebugInfo>(methodDebugInfos.Count);
			this.snapshot = snapshot;
			this.moduleIdProvider = moduleIdProvider;

			var modIdDict = new Dictionary<ModuleDef, ModuleId>();
			foreach (var info in methodDebugInfos) {
				var module = info.Method.Module;
				if (module == null)
					continue;

				ModuleId moduleId;
				if (!modIdDict.TryGetValue(module, out moduleId)) {
					moduleId = moduleIdProvider.Create(module);
					modIdDict.Add(module, moduleId);
				}
				var key = new ModuleTokenId(moduleId, info.Method.MDToken);
				MethodDebugInfo oldDebugInfo;
				if (dict.TryGetValue(key, out oldDebugInfo)) {
					if (info.Statements.Length < oldDebugInfo.Statements.Length)
						continue;
				}
				dict[key] = info;
			}
		}

		public IList<MethodSourceStatement> FindByTextPosition(int textPosition, bool sameMethod) {
			if (textPosition < 0)
				throw new ArgumentOutOfRangeException(nameof(textPosition));
			if (dict.Count == 0)
				return Array.Empty<MethodSourceStatement>();

			if (textPosition > snapshot.Length)
				return Array.Empty<MethodSourceStatement>();
			var line = snapshot.GetLineFromPosition(textPosition);
			var methodStatements = FindByLineAndTextOffset(line.Start.Position, line.End.Position, textPosition);
			if (methodStatements == null && line.Start.Position != textPosition)
				methodStatements = FindByLineAndTextOffset(line.Start.Position, line.End.Position, line.Start.Position);
			if (methodStatements == null)
				methodStatements = GetClosest(line.Start.Position, line.End.Position);

			if (methodStatements != null) {
				if (!sameMethod || IsSameMethod(methodStatements, textPosition))
					return methodStatements;
			}
			return Array.Empty<MethodSourceStatement>();
		}

		bool IsSameMethod(List<MethodSourceStatement> methodStatements, int textPosition) {
			if (methodStatements.Count == 0)
				return false;
			var methodInfo = TryGetMethodDebugInfo(methodStatements[0].Method);
			Debug.Assert(methodInfo != null);
			if (methodInfo == null)
				return false;
			var methodSpan = methodInfo.Span;
			if (textPosition >= methodSpan.Start && textPosition < methodSpan.End)
				return true;

			// If it's a field initializer the statement isn't within the method
			if (methodInfo.Method.IsConstructor) {
				foreach (var statement in methodInfo.Statements) {
					// Allow end position too since it's probably at the end of the line
					if (textPosition >= statement.TextSpan.Start && textPosition <= statement.TextSpan.End)
						return true;
				}
			}
			return false;
		}

		List<MethodSourceStatement> FindByLineAndTextOffset(int lineStart, int lineEnd, int textPosition) {
			List<MethodSourceStatement> list = null;
			foreach (var info in dict.Values) {
				var sourceStatement = info.GetSourceStatementByTextOffset(lineStart, lineEnd, textPosition);
				if (sourceStatement != null) {
					if (list == null)
						list = new List<MethodSourceStatement>();
					list.Add(new MethodSourceStatement(info.Method, sourceStatement.Value));
				}
			}
			return list == null ? null : list.Distinct().ToList();
		}

		List<MethodSourceStatement> GetClosest(int lineStart, int lineEnd) {
			var list = new List<MethodSourceStatement>();
			foreach (var info in dict.Values) {
				MethodSourceStatement? methodSourceStatement = null;
				foreach (var sourceStatement in info.Statements) {
					if (lineStart >= sourceStatement.TextSpan.End)
						continue;
					if (methodSourceStatement == null || sourceStatement.TextSpan.Start < methodSourceStatement.Value.Statement.TextSpan.Start)
						methodSourceStatement = new MethodSourceStatement(info.Method, sourceStatement);
				}
				if (methodSourceStatement != null) {
					if (list.Count == 0)
						list.Add(methodSourceStatement.Value);
					else if (methodSourceStatement.Value.Statement.TextSpan.Start == list[0].Statement.TextSpan.Start)
						list.Add(methodSourceStatement.Value);
					else if (methodSourceStatement.Value.Statement.TextSpan.Start < list[0].Statement.TextSpan.Start) {
						list.Clear();
						list.Add(methodSourceStatement.Value);
					}
				}
			}

			if (list.Count == 0)
				return null;
			return list.Distinct().ToList();
		}

		public MethodSourceStatement? FindByCodeOffset(MethodDef method, uint codeOffset) =>
			FindByCodeOffset(new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken), codeOffset);

		public MethodSourceStatement? FindByCodeOffset(ModuleTokenId token, uint codeOffset) {
			MethodDebugInfo info;
			if (!dict.TryGetValue(token, out info))
				return null;
			foreach (var sourceStatement in info.Statements) {
				if (sourceStatement.BinSpan.Start <= codeOffset && codeOffset < sourceStatement.BinSpan.End)
					return new MethodSourceStatement(info.Method, sourceStatement);
			}
			return null;
		}

		public MethodDebugInfo TryGetMethodDebugInfo(MethodDef method) =>
			TryGetMethodDebugInfo(new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken));

		public MethodDebugInfo TryGetMethodDebugInfo(ModuleTokenId token) {
			MethodDebugInfo info;
			dict.TryGetValue(token, out info);
			return info;
		}

		public IEnumerable<MethodSourceStatement> GetStatementsByTextSpan(Span span) {
			if (sortedStatements == null)
				InitializeSortedStatements();

			int position = span.Start;
			int end = span.End;
			int index = GetStartIndex(position);
			if (index < 0)
				yield break;
			var array = sortedStatements;
			while (index < array.Length) {
				var mss = array[index++];
				if (end < mss.Statement.TextSpan.Start)
					break;
				Debug.Assert(mss.Statement.TextSpan.Start <= end && mss.Statement.TextSpan.End >= position);
				yield return mss;
			}
		}

		int GetStartIndex(int position) {
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
			Debug.Assert(sortedStatements == null);
			if (sortedStatements != null)
				return;
			var list = new List<MethodSourceStatement>();
			foreach (var info in dict.Values) {
				foreach (var s in info.Statements)
					list.Add(new MethodSourceStatement(info.Method, s));
			}
			list.Sort(MethodSourceStatementComparer.Instance);
			sortedStatements = list.ToArray();
		}

		sealed class MethodSourceStatementComparer : IComparer<MethodSourceStatement> {
			public static readonly MethodSourceStatementComparer Instance = new MethodSourceStatementComparer();

			public int Compare(MethodSourceStatement x, MethodSourceStatement y) {
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
