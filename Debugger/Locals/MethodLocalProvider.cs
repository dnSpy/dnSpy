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
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using ICSharpCode.Decompiler.ILAst;

namespace dnSpy.Debugger.Locals {
	interface IMethodLocalProvider {
		void GetMethodInfo(SerializedDnToken method, out Parameter[] parameters, out Local[] locals, out ILVariable[] decLocals);
		event EventHandler NewMethodInfoAvailable;
	}

	[Export, Export(typeof(IMethodLocalProvider)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class MethodLocalProvider : IMethodLocalProvider {
		public event EventHandler NewMethodInfoAvailable;

		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		MethodLocalProvider(IFileTabManager fileTabManager, ITextEditorUIContextManager textEditorUIContextManager) {
			this.fileTabManager = fileTabManager;
			textEditorUIContextManager.Add(OnTextEditorUIContextEvent, TextEditorUIContextManagerConstants.ORDER_DEBUGGER_METHODLOCALPROVIDER);
		}

		void OnTextEditorUIContextEvent(TextEditorUIContextListenerEvent @event, ITextEditorUIContext uiContext, object data) {
			if (NewMethodInfoAvailable != null)
				NewMethodInfoAvailable(this, EventArgs.Empty);
		}

		public void GetMethodInfo(SerializedDnToken key, out Parameter[] parameters, out Local[] locals, out ILVariable[] decLocals) {
			parameters = null;
			locals = null;
			decLocals = null;

			foreach (var tab in fileTabManager.VisibleFirstTabs) {
				if (parameters != null && decLocals != null)
					break;

				var uiContext = tab.UIContext as ITextEditorUIContext;
				var cm = uiContext.TryGetCodeMappings();
				if (cm == null)
					continue;
				var mapping = cm.TryGetMapping(key);
				if (mapping == null)
					continue;
				var method = mapping.MethodDef;
				if (mapping.LocalVariables != null && method.Body != null) {
					locals = method.Body.Variables.ToArray();
					decLocals = new ILVariable[method.Body.Variables.Count];
					foreach (var v in mapping.LocalVariables) {
						if (v.IsGenerated)
							continue;
						if (v.OriginalVariable == null)
							continue;
						if ((uint)v.OriginalVariable.Index >= decLocals.Length)
							continue;
						decLocals[v.OriginalVariable.Index] = v;
					}
				}

				parameters = method.Parameters.ToArray();
			}
		}
	}
}
