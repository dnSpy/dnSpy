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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Debugger.Locals {
	interface IMethodLocalProvider {
		void GetMethodInfo(SerializedDnToken method, out Parameter[] parameters, out Local[] locals, out SourceLocal[] decompilerLocals);
		event EventHandler NewMethodInfoAvailable;
	}

	[Export(typeof(IMethodLocalProvider))]
	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_DEBUGGER_METHODLOCALPROVIDER)]
	sealed class MethodLocalProvider : IMethodLocalProvider, IDocumentViewerListener {
		public event EventHandler NewMethodInfoAvailable;

		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		MethodLocalProvider(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.GotNewContent)
				NewMethodInfoAvailable?.Invoke(this, EventArgs.Empty);
		}

		public void GetMethodInfo(SerializedDnToken key, out Parameter[] parameters, out Local[] locals, out SourceLocal[] decompilerLocals) {
			parameters = null;
			locals = null;
			decompilerLocals = null;

			foreach (var tab in fileTabManager.VisibleFirstTabs) {
				if (parameters != null && decompilerLocals != null)
					break;

				var uiContext = tab.UIContext as IDocumentViewer;
				var methodDebugService = uiContext.TryGetMethodDebugService();
				if (methodDebugService == null)
					continue;
				var info = methodDebugService.TryGetMethodDebugInfo(key);
				if (info == null)
					continue;
				var method = info.Method;
				if (info.Locals.Length != 0 && method.Body != null) {
					locals = method.Body.Variables.ToArray();
					decompilerLocals = new SourceLocal[method.Body.Variables.Count];
					foreach (var v in info.Locals) {
						if ((uint)v.Local.Index >= decompilerLocals.Length)
							continue;
						decompilerLocals[v.Local.Index] = v;
					}
				}

				parameters = method.Parameters.ToArray();
			}
		}
	}
}
