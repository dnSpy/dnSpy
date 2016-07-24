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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Decompiler.Shared;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Decompiler {
	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_METHODDEBUGSERVICECREATOR)]
	sealed class MethodDebugServiceDocumentViewerListener : IDocumentViewerListener {
		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.GotNewContent)
				AddMethodDebugService(e.DocumentViewer, ((DocumentViewerGotNewContentEventArgs)e).Content);
		}

		void AddMethodDebugService(IDocumentViewer documentViewer, DocumentViewerContent content) {
			if (content == null)
				return;
			var service = new MethodDebugService(content.MethodDebugInfos, documentViewer.TextView.TextSnapshot);
			documentViewer.AddContentData(MethodDebugServiceConstants.MethodDebugServiceKey, service);
		}
	}

	sealed class MethodDebugService : IMethodDebugService {
		readonly IList<MethodDebugInfo> methodDebugInfos;
		readonly ITextSnapshot snapshot;

		public MethodDebugService() {
			this.methodDebugInfos = Array.Empty<MethodDebugInfo>();
		}

		public MethodDebugService(IList<MethodDebugInfo> methodDebugInfos, ITextSnapshot snapshot) {
			if (methodDebugInfos == null)
				throw new ArgumentNullException(nameof(methodDebugInfos));
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			this.methodDebugInfos = methodDebugInfos;
			this.snapshot = snapshot;
		}

		public IList<MethodSourceStatement> FindByTextPosition(int textPosition) {
			if (textPosition < 0)
				throw new ArgumentOutOfRangeException(nameof(textPosition));
			if (methodDebugInfos.Count == 0)
				return Array.Empty<MethodSourceStatement>();

			Debug.Assert(textPosition <= snapshot.Length);
			if (textPosition > snapshot.Length)
				return Array.Empty<MethodSourceStatement>();
			var line = snapshot.GetLineFromPosition(textPosition);
			var methodStatements = FindByLineAndTextOffset(line.Start.Position, line.End.Position, textPosition);
			if (methodStatements == null && line.Start.Position != textPosition)
				methodStatements = FindByLineAndTextOffset(line.Start.Position, line.End.Position, line.Start.Position);
			if (methodStatements == null)
				methodStatements = GetClosest(line.Start.Position, line.End.Position);

			if (methodStatements != null)
				return methodStatements;
			return Array.Empty<MethodSourceStatement>();
		}

		List<MethodSourceStatement> FindByLineAndTextOffset(int lineStart, int lineEnd, int textPosition) {
			List<MethodSourceStatement> list = null;
			foreach (var info in methodDebugInfos) {
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
			foreach (var info in methodDebugInfos) {
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

		public MethodSourceStatement? FindByCodeOffset(MethodDef method, uint ilOffset) {
			foreach (var info in methodDebugInfos) {
				if (info.Method != method)
					continue;
				foreach (var sourceStatement in info.Statements) {
					if (sourceStatement.BinSpan.Start <= ilOffset && ilOffset < sourceStatement.BinSpan.End)
						return new MethodSourceStatement(info.Method, sourceStatement);
				}
			}
			return null;
		}
	}
}
