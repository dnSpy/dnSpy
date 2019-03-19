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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed partial class XmlParser {
		[ExportDocumentViewerToolTipProvider]
		sealed class DocumentViewerToolTipProvider : IDocumentViewerToolTipProvider {
			public object Create(IDocumentViewerToolTipProviderContext context, object @ref) {
				if (@ref is XmlNamespaceTextViewerReference nsRef)
					return Create(context, nsRef);
				return null;
			}

			static object Create(IDocumentViewerToolTipProviderContext context, XmlNamespaceTextViewerReference nsRef) {
				var provider = context.Create();
				provider.Image = DsImages.Namespace;

				var name = nsRef.XmlNamespaceReference.Definition.Name;
				const string prefix = "clr-namespace:";
				if (name.StartsWith(prefix)) {
					name = name.Substring(prefix.Length);
					ParseClrNamespace(name, out string assemblyName, out string @namespace);
					if (assemblyName == null && @namespace == null)
						provider.Output.Write(nsRef.XmlNamespaceReference.Definition.Name);
					else {
						if (!string.IsNullOrEmpty(@namespace))
							provider.Output.WriteNamespace(@namespace);
						if (!string.IsNullOrEmpty(assemblyName)) {
							if (!string.IsNullOrEmpty(@namespace))
								provider.Output.WriteLine();
							provider.Output.Write(BoxedTextColor.Assembly, assemblyName);
						}
					}
				}
				else
					provider.Output.Write(name);

				return provider.Create();
			}

			static void ParseClrNamespace(string name, out string assemblyName, out string @namespace) {
				assemblyName = null;
				@namespace = null;
				foreach (var part in name.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
					var kv = part.Split(new[] { '=' }, StringSplitOptions.None);
					if (kv.Length == 1)
						@namespace = kv[0];
					else if (kv.Length == 2) {
						if (kv[0] == "assembly")
							assemblyName = kv[1];
					}
				}
			}
		}
	}
}
