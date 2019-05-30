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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler.XmlDoc;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents.Tabs {
	[Export(typeof(IAppCommandLineArgsHandler))]
	sealed class AppCommandLineArgsHandler : IAppCommandLineArgsHandler {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		AppCommandLineArgsHandler(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

		public double Order => -1000;

		public void OnNewArgs(IAppCommandLineArgs args) {
			if (!SelectMember(args)) {
				var mod = GetLoadedFiles(args).FirstOrDefault();
				if (!(mod is null))
					documentTabService.FollowReference((object)mod.Assembly ?? mod);
				else {
					foreach (var filename in args.Filenames) {
						var key = new FilenameKey(filename);
						var document = documentTabService.DocumentTreeView.DocumentService.GetDocuments().FirstOrDefault(a => a.Key.Equals(key));
						if (!(document is null)) {
							documentTabService.FollowReference(document);
							break;
						}
					}
				}
			}
		}

		bool SelectMember(IAppCommandLineArgs args) {
			if (string.IsNullOrEmpty(args.SelectMember))
				return false;

			uint token = SimpleTypeConverter.ParseUInt32(args.SelectMember, uint.MinValue, uint.MaxValue, out var error);
			if (string.IsNullOrEmpty(error)) {
				var mod = GetLoadedFiles(args).FirstOrDefault();
				var member = mod?.ResolveToken(token);
				if (member is null)
					return false;
				documentTabService.FollowReference(member);
				return true;
			}

			foreach (var mod in GetLoadedFiles(args)) {
				const string XMLDOC_NS_PREFIX = "N:";
				bool isNamespace = args.SelectMember.StartsWith(XMLDOC_NS_PREFIX);
				if (isNamespace) {
					var ns = args.SelectMember.Substring(XMLDOC_NS_PREFIX.Length);
					var modNode = documentTabService.DocumentTreeView.FindNode(mod);
					var nsNode = modNode is null ? null : documentTabService.DocumentTreeView.FindNamespaceNode(modNode.Document, ns);
					if (!(nsNode is null)) {
						documentTabService.FollowReference(nsNode);
						return true;
					}
				}
				else {
					var member = XmlDocKeyProvider.FindMemberByKey(mod, args.SelectMember);
					if (!(member is null)) {
						documentTabService.FollowReference(member);
						return true;
					}
				}
			}

			return false;
		}

		IEnumerable<ModuleDef> GetLoadedFiles(IAppCommandLineArgs args) {
			foreach (var filename in args.Filenames) {
				var key = new FilenameKey(filename);
				var document = documentTabService.DocumentTreeView.DocumentService.GetDocuments().FirstOrDefault(a => key.Equals(a.Key));
				if (document?.ModuleDef is null)
					continue;
				if (!(document.AssemblyDef is null)) {
					foreach (var mod in document.AssemblyDef.Modules)
						yield return mod;
				}
				else
					yield return document.ModuleDef;
			}
		}
	}
}
