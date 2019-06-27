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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AppHostPatcher {
	class Program {
		static void Usage() =>
			Console.WriteLine("apphostpatcher <apphost.exe> <origdllpath> <newdllpath>");

		const int maxPathBytes = 1024;

		static int Main(string[] args) {
			try {
				if (args.Length != 3) {
					Usage();
					return 1;
				}
				var apphostExe = args[0];
				var origPath = args[1];
				var newPath = args[2];
				if (!File.Exists(apphostExe)) {
					Console.WriteLine($"Apphost '{apphostExe}' does not exist");
					return 1;
				}
				if (origPath == string.Empty) {
					Console.WriteLine("Original path is empty");
					return 1;
				}
				var origPathBytes = Encoding.UTF8.GetBytes(origPath + "\0");
				Debug.Assert(origPathBytes.Length > 0);
				var newPathBytes = Encoding.UTF8.GetBytes(newPath + "\0");
				if (origPathBytes.Length > maxPathBytes) {
					Console.WriteLine($"Original path is too long");
					return 1;
				}
				if (newPathBytes.Length > maxPathBytes) {
					Console.WriteLine($"New path is too long");
					return 1;
				}

				var apphostExeBytes = File.ReadAllBytes(apphostExe);
				int offset = GetOffset(apphostExeBytes, origPathBytes);
				if (offset < 0) {
					Console.WriteLine($"Could not find original path '{origPath}'");
					return 1;
				}
				if (offset + newPathBytes.Length > apphostExeBytes.Length) {
					Console.WriteLine($"New path is too long: {newPath}");
					return 1;
				}
				for (int i = 0; i < newPathBytes.Length; i++)
					apphostExeBytes[offset + i] = newPathBytes[i];
				File.WriteAllBytes(apphostExe, apphostExeBytes);
				return 0;
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				return 1;
			}
		}

		static int GetOffset(byte[] bytes, byte[] pattern) {
			int si = 0;
			var b = pattern[0];
			while (si < bytes.Length) {
				si = Array.IndexOf(bytes, b, si);
				if (si < 0)
					break;
				if (Match(bytes, si, pattern))
					return si;
				si++;
			}
			return -1;
		}

		static bool Match(byte[] bytes, int index, byte[] pattern) {
			if (index + pattern.Length > bytes.Length)
				return false;
			for (int i = 0; i < pattern.Length; i++) {
				if (bytes[index + i] != pattern[i])
					return false;
			}
			return true;
		}
	}
}
