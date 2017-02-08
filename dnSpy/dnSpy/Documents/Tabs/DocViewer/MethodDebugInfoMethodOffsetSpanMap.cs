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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class MethodDebugInfoMethodOffsetSpanMap : IMethodOffsetSpanMap {
		readonly IModuleIdProvider moduleIdProvider;
		readonly Dictionary<ModuleTokenId, MethodDebugInfo> toMethodDebugInfo;

		public MethodDebugInfoMethodOffsetSpanMap(IModuleIdProvider moduleIdProvider, IReadOnlyList<MethodDebugInfo> methodDebugInfos) {
			if (methodDebugInfos == null)
				throw new ArgumentNullException(nameof(methodDebugInfos));
			this.moduleIdProvider = moduleIdProvider;
			toMethodDebugInfo = new Dictionary<ModuleTokenId, MethodDebugInfo>(methodDebugInfos.Count);
			foreach (var info in methodDebugInfos) {
				var token = new ModuleTokenId(moduleIdProvider.Create(info.Method.Module), info.Method.MDToken);
				MethodDebugInfo otherInfo;
				if (toMethodDebugInfo.TryGetValue(token, out otherInfo)) {
					if (info.Statements.Length < otherInfo.Statements.Length)
						continue;
				}
				toMethodDebugInfo[token] = info;
			}
		}

		public Span? ToSpan(ModuleTokenId method, uint ilOffset) {
			MethodDebugInfo info;
			if (!toMethodDebugInfo.TryGetValue(method, out info))
				return null;
			var statement = info.GetSourceStatementByCodeOffset(ilOffset);
			if (statement == null)
				return null;
			var textSpan = statement.Value.TextSpan;
			return new Span(textSpan.Start, textSpan.Length);
		}
	}
}
