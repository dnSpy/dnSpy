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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class DocumentViewerDotNetSpanMap : IDotNetSpanMap {
		readonly IModuleIdProvider moduleIdProvider;
		readonly IReadOnlyList<MethodDebugInfo> methodDebugInfos;
		readonly SpanDataCollection<ReferenceInfo> references;
		Dictionary<ModuleTokenId, MethodDebugInfo> toMethodDebugInfo;
		Dictionary<ModuleTokenId, Span> toTokenInfo;

		public DocumentViewerDotNetSpanMap(IModuleIdProvider moduleIdProvider, IReadOnlyList<MethodDebugInfo> methodDebugInfos, SpanDataCollection<ReferenceInfo> references) {
			this.moduleIdProvider = moduleIdProvider ?? throw new ArgumentNullException(nameof(moduleIdProvider));
			this.methodDebugInfos = methodDebugInfos ?? throw new ArgumentNullException(nameof(methodDebugInfos));
			this.references = references ?? throw new ArgumentNullException(nameof(references));
		}

		Span? IDotNetSpanMap.ToSpan(ModuleId module, uint token, uint ilOffset) {
			if (toMethodDebugInfo == null) {
				toMethodDebugInfo = new Dictionary<ModuleTokenId, MethodDebugInfo>(methodDebugInfos.Count);
				foreach (var info in methodDebugInfos) {
					var tokenId = new ModuleTokenId(moduleIdProvider.Create(info.Method.Module), info.Method.MDToken);
					if (toMethodDebugInfo.TryGetValue(tokenId, out var otherInfo)) {
						if (info.Statements.Length < otherInfo.Statements.Length)
							continue;
					}
					toMethodDebugInfo[tokenId] = info;
				}
			}
			if (!toMethodDebugInfo.TryGetValue(new ModuleTokenId(module, token), out var info2))
				return null;
			var statement = info2.GetSourceStatementByCodeOffset(ilOffset);
			if (statement == null)
				return null;
			var textSpan = statement.Value.TextSpan;
			return new Span(textSpan.Start, textSpan.Length);
		}

		Span? IDotNetSpanMap.ToSpan(ModuleId module, uint token) {
			if (toTokenInfo == null) {
				toTokenInfo = new Dictionary<ModuleTokenId, Span>();
				foreach (var data in references) {
					if (!data.Data.IsDefinition)
						continue;
					var def = data.Data.Reference as IMemberDef;
					if (def == null)
						continue;
					var tokenId = new ModuleTokenId(moduleIdProvider.Create(def.Module), def.MDToken);
					toTokenInfo[tokenId] = data.Span;
				}
			}
			if (!toTokenInfo.TryGetValue(new ModuleTokenId(module, token), out var span))
				return null;
			return span;
		}
	}
}
