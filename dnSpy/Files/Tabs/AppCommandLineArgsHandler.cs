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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Shared.Languages.XmlDoc;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.Tabs {
	[Export(typeof(IAppCommandLineArgsHandler))]
	sealed class AppCommandLineArgsHandler : IAppCommandLineArgsHandler {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		AppCommandLineArgsHandler(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		public double Order {
			get { return -1000; }
		}

		public void OnNewArgs(IAppCommandLineArgs args) {
			if (!SelectMember(args)) {
				var mod = GetLoadedFiles(args).FirstOrDefault();
				if (mod != null)
					fileTabManager.FollowReference((object)mod.Assembly ?? mod);
				else {
					foreach (var filename in args.Filenames) {
						var key = new FilenameKey(filename);
						var file = fileTabManager.FileTreeView.FileManager.GetFiles().FirstOrDefault(a => a.Key.Equals(key));
						if (file != null) {
							fileTabManager.FollowReference(file);
							break;
						}
					}
				}
			}
		}

		bool SelectMember(IAppCommandLineArgs args) {
			if (string.IsNullOrEmpty(args.SelectMember))
				return false;

			string error;
			uint token = NumberVMUtils.ParseUInt32(args.SelectMember, uint.MinValue, uint.MaxValue, out error);
			if (string.IsNullOrEmpty(error)) {
				var mod = GetLoadedFiles(args).FirstOrDefault();
				var member = mod == null ? null : mod.ResolveToken(token);
				if (member == null)
					return false;
				fileTabManager.FollowReference(member);
				return true;
			}

			foreach (var mod in GetLoadedFiles(args)) {
				var member = XmlDocKeyProvider.FindMemberByKey(mod, args.SelectMember);
				if (member != null) {
					fileTabManager.FollowReference(member);
					return true;
				}
			}

			return false;
		}

		IEnumerable<ModuleDef> GetLoadedFiles(IAppCommandLineArgs args) {
			foreach (var filename in args.Filenames) {
				var key = new FilenameKey(filename);
				var file = fileTabManager.FileTreeView.FileManager.GetFiles().FirstOrDefault(a => key.Equals(a.Key));
				if (file == null || file.ModuleDef == null)
					continue;
				if (file.AssemblyDef != null) {
					foreach (var mod in file.AssemblyDef.Modules)
						yield return mod;
				}
				else
					yield return file.ModuleDef;
			}
		}
	}
}
