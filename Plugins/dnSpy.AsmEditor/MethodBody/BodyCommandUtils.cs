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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;

namespace dnSpy.AsmEditor.MethodBody {
	static class BodyCommandUtils {
		public static IList<SourceCodeMapping> GetMappings(IMenuItemContext context) {
			if (context == null)
				return null;
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;
			var uiContext = context.Find<IDocumentViewer>();
			if (uiContext == null)
				return null;
			var pos = context.Find<TextEditorLocation?>();
			if (pos == null)
				return null;
			return GetMappings(uiContext, pos.Value.Line, pos.Value.Column);
		}

		public static IList<SourceCodeMapping> GetMappings(IDocumentViewer documentViewer, int line, int col) {
			if (documentViewer == null)
				return null;
			var cm = documentViewer.GetCodeMappings();
			var list = cm.Find(line, col);
			if (list.Count == 0)
				return null;
			if (!(list[0].StartPosition.Line <= line && line <= list[0].EndPosition.Line))
				return null;
			return list;
		}

		public static uint[] GetInstructionOffsets(MethodDef method, IList<SourceCodeMapping> list) {
			if (method == null)
				return null;
			var body = method.Body;
			if (body == null)
				return null;

			var foundInstrs = new HashSet<uint>();
			// The instructions' offset field is assumed to be valid
			var instrs = body.Instructions.Select(a => a.Offset).ToArray();
			foreach (var range in list.Select(a => a.ILRange)) {
				int index = Array.BinarySearch(instrs, range.From);
				if (index < 0)
					continue;
				for (int i = index; i < instrs.Length; i++) {
					uint instrOffset = instrs[i];
					if (instrOffset >= range.To)
						break;

					foundInstrs.Add(instrOffset);
				}
			}

			return foundInstrs.ToArray();
		}
	}
}
