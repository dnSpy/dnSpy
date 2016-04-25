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
using System.IO;

namespace dnSpy.Scripting.Roslyn.Common {
	static class ResponseFileReader {
		public static IEnumerable<Tuple<string, string>> Read(string filename) {
			if (!File.Exists(filename))
				yield break;
			foreach (var tmp in File.ReadAllLines(filename)) {
				var line = tmp.TrimStart();
				if (string.IsNullOrEmpty(line))
					continue;
				if (line.StartsWith("#"))
					continue;
				var cmd = line;
				string arg1;
				int index = cmd.IndexOf(':');
				if (index < 0)
					arg1 = string.Empty;
				else {
					arg1 = cmd.Substring(index + 1);
					cmd = cmd.Substring(0, index);
				}
				if (cmd.Length == 0)
					continue;
				if (cmd[0] != '/' && cmd[0] != '-')
					continue;
				cmd = cmd.Substring(1);
				yield return Tuple.Create(cmd.Trim(), arg1.Trim());
			}
		}
	}
}
