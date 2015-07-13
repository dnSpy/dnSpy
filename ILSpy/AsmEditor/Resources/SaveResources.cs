/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.IO;
using System.Linq;
using ICSharpCode.ILSpy.TreeNodes;

using WF = System.Windows.Forms;

namespace ICSharpCode.ILSpy.AsmEditor.Resources
{
	static class SaveResources
	{
		public static ResourceData[] GetResourceData(IResourceNode[] nodes, ResourceDataType resourceDataType)
		{
			return nodes.SelectMany(a => a.GetResourceData(resourceDataType)).ToArray();
		}

		public static void Save(IResourceNode[] nodes, bool useSubDirs, ResourceDataType resourceDataType)
		{
			if (nodes == null)
				return;

			var infos = GetResourceData(nodes, resourceDataType);
			if (infos.Length == 1) {
				var info = infos[0];
				var name = ResourceUtils.FixFileNamePart(ResourceUtils.GetFileName(info.Name));
				var dlg = new WF.SaveFileDialog {
					RestoreDirectory = true,
					ValidateNames = true,
					FileName = name,
				};
				var ext = Path.GetExtension(name);
				dlg.DefaultExt = string.IsNullOrEmpty(ext) ? string.Empty : ext.Substring(1);
				if (dlg.ShowDialog() != WF.DialogResult.OK)
					return;
				var ex = ResourceUtils.SaveFile(dlg.FileName, info.GetStream());
				if (ex != null)
					MainWindow.Instance.ShowMessageBox(string.Format("Could not save '{0}'\nERROR: {1}", dlg.FileName, ex.Message));
			}
			else {
				var dlg = new WF.FolderBrowserDialog();
				if (dlg.ShowDialog() != WF.DialogResult.OK)
					return;
				string baseDir = dlg.SelectedPath;
				foreach (var info in infos) {
					var name = ResourceUtils.GetCleanedPath(info.Name, useSubDirs);
					var pathName = Path.Combine(baseDir, name);
					var ex = ResourceUtils.SaveFile(pathName, info.GetStream());
					if (ex != null) {
						MainWindow.Instance.ShowMessageBox(string.Format("Could not save '{0}'\nERROR: {1}", pathName, ex.Message));
						break;
					}
				}
			}
		}
	}
}
