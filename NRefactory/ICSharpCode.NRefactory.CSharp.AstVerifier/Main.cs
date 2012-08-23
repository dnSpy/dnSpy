using System;
using System.IO;

namespace ICSharpCode.NRefactory.CSharp.AstVerifier
{
	class MainClass
	{
		static bool IsMatch (string src1, string src2, out int i, out int j)
		{
			i = 0;
			j = 0;
			while (i < src1.Length && j < src2.Length) {
				char c1 = src1 [i];
				char c2 = src2 [j];
				if (char.IsWhiteSpace (c1)) {
					i++;
					continue;
				}
				if (char.IsWhiteSpace (c2)) {
					j++;
					continue;
				}
				if (c1 != c2)
					return false;
				i++;
				j++;
			}
			while (i < src1.Length && char.IsWhiteSpace (src1[i])) {
				i++;
			}
			while (j < src2.Length && char.IsWhiteSpace (src2[j])) {
				j++;
			}

			return i == src1.Length && j == src2.Length;
		}

		public static void Main (string[] args)
		{
			if (args.Length == 0) {
				Console.WriteLine ("Usage: AstVerifier [-v|-verbose] [Directory]");
				return;
			}
			string directory = args[args.Length - 1];
			bool verboseOutput =  args.Length > 1 && (args[0] == "-v" || args[0] == "-verbose");

			try {
				if (!Directory.Exists (directory)) {
					Console.WriteLine ("Directory not found.");
					return;
				}
			} catch (IOException) {
				Console.WriteLine ("Exception while trying to access the directory.");
				return;
			}
			int failed = 0, passed = 0;
			Console.WriteLine ("search in " + directory);
			foreach (var file in Directory.GetFileSystemEntries (directory, "*", SearchOption.AllDirectories)) {
				if (!file.EndsWith (".cs"))
					continue;
				string text = File.ReadAllText (file);
				var unit = SyntaxTree.Parse (text, file);
				if (unit == null)
					continue;
				string generated = unit.GetText ();
				int i, j;
				if (!IsMatch (text, generated, out i, out j)) {
					if (i > 0 && j > 0 && verboseOutput) {
						Console.WriteLine ("fail :" + file + "----original:");
						Console.WriteLine (text.Substring (0, Math.Min (text.Length, i + 1)));
						Console.WriteLine ("----generated:");
						Console.WriteLine (generated.Substring (0, Math.Min (generated.Length, j + 1)));
					}
					failed++;
				} else {
					passed++;
				}
			}

			Console.WriteLine ("{0} passed, {1} failed", passed, failed);
		}
	}
}
