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
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Code {
	sealed class DecompilerOutputImpl : IDecompilerOutput {
		int textLength;
		int indentLevel;
		bool addIndent;
		uint methodToken;
		DbgMethodDebugInfo? methodDebugInfo;
		DbgMethodDebugInfo? kickoffMethodDebugInfo;

		public int Length => textLength;
		public int NextPosition => textLength + (addIndent ? indentLevel : 0);
		public bool UsesCustomData => true;

		public DecompilerOutputImpl() => addIndent = true;

		internal void Clear() {
			textLength = 0;
			indentLevel = 0;
			addIndent = true;
			methodToken = 0;
			methodDebugInfo = null;
			kickoffMethodDebugInfo = null;
		}

		public void Initialize(uint methodToken) => this.methodToken = methodToken;
		public (DbgMethodDebugInfo debugInfo, DbgMethodDebugInfo? stateMachineDebugInfo) TryGetMethodDebugInfo() {
			if (!(methodDebugInfo is null)) {
				if (!(kickoffMethodDebugInfo is null))
					return (kickoffMethodDebugInfo, methodDebugInfo);
				return (methodDebugInfo, null);
			}
			return default;
		}

		public void AddCustomData<TData>(string id, TData data) {
			if (id == PredefinedCustomDataIds.DebugInfo && data is MethodDebugInfo debugInfo) {
				if (debugInfo.Method.MDToken.Raw == methodToken)
					methodDebugInfo = DbgMethodDebugInfoUtils.ToDbgMethodDebugInfo(debugInfo);
				else if (debugInfo.KickoffMethod?.MDToken.Raw == methodToken) {
					var m = debugInfo.KickoffMethod!;
					var body = m.Body;
					int bodySize = body?.GetCodeSize() ?? 0;
					var scope = new MethodDebugScope(new ILSpan(0, (uint)bodySize), Array.Empty<MethodDebugScope>(), Array.Empty<SourceLocal>(), Array.Empty<ImportInfo>(), Array.Empty<MethodDebugConstant>());
					kickoffMethodDebugInfo = DbgMethodDebugInfoUtils.ToDbgMethodDebugInfo(new MethodDebugInfo(debugInfo.CompilerName, debugInfo.DecompilerSettingsVersion, StateMachineKind.None, m, null, null, Array.Empty<SourceStatement>(), scope, null, null));
					methodDebugInfo = DbgMethodDebugInfoUtils.ToDbgMethodDebugInfo(debugInfo);
				}
			}
		}

		public void DecreaseIndent() => indentLevel--;
		public void IncreaseIndent() => indentLevel++;

		public void WriteLine() {
			addIndent = true;
			textLength += Environment.NewLine.Length;
		}

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			textLength += indentLevel;
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			textLength += text.Length;
		}

		void AddText(string text, int index, int length, object color) {
			if (addIndent)
				AddIndent();
			textLength += length;
		}

		public void Write(string text, object color) => AddText(text, color);
		public void Write(string text, int index, int length, object color) => AddText(text, index, length, color);

		public void Write(string text, object? reference, DecompilerReferenceFlags flags, object color) =>
			Write(text, 0, text.Length, reference, flags, color);

		public void Write(string text, int index, int length, object? reference, DecompilerReferenceFlags flags, object color) {
			if (addIndent)
				AddIndent();
			AddText(text, index, length, color);
		}
	}
}
