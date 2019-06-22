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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Code.TextEditor;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.DotNet.Code.TextEditor {
	[Export(typeof(DbgTextViewCodeLocationProvider))]
	sealed class DbgTextViewCodeLocationProviderImpl : DbgTextViewCodeLocationProvider {
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		DbgTextViewCodeLocationProviderImpl(Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory, IModuleIdProvider moduleIdProvider) {
			this.dbgDotNetCodeLocationFactory = dbgDotNetCodeLocationFactory;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override DbgTextViewBreakpointLocationResult? CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position) {
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer is null)
				return null;
			var methodDebugService = documentViewer.GetMethodDebugService();
			if (methodDebugService is null)
				return null;
			var methodStatements = methodDebugService.FindByTextPosition(position.Position, FindByTextPositionOptions.None);
			if (methodStatements.Count == 0)
				return null;
			var textSpan = methodStatements[0].Statement.TextSpan;
			var snapshot = textView.TextSnapshot;
			if (textSpan.End > snapshot.Length)
				return null;
			var span = new VirtualSnapshotSpan(new SnapshotSpan(snapshot, new Span(textSpan.Start, textSpan.Length)));
			var locations = new DbgCodeLocation[methodStatements.Count];
			for (int i = 0; i < methodStatements.Count; i++) {
				var statement = methodStatements[i];
				var moduleId = moduleIdProvider.Create(statement.Method.Module);
				locations[i] = dbgDotNetCodeLocationFactory.Value.Create(moduleId, statement.Method.MDToken.Raw, statement.Statement.ILSpan.Start);
			}
			return new DbgTextViewBreakpointLocationResult(locations, span);
		}
	}
}
