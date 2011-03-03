using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ICSharpCode.Decompiler.Tests
{
	static class CodeSampleFileParser
	{
		public static IEnumerable<string> ListSections(string s)
		{
			var query = from line in ToLines(s)
						let sectionName = ReadSectionName(line)
						where sectionName != null
						select sectionName;
			return query;
		}

		public static string GetSection(string sectionName, string s)
		{
			var lines = ToLines(s);

			bool sectionFound = false;
			var sectionText = new StringBuilder();

			Action<string> parser = null;

			Action<string> commonSectionReader = line =>
				{
					if (IsCommonSectionEnd(line))
						parser = null;
					else
						sectionText.AppendLine(line);
				};

			Action<string> namedSectionReader = line =>
				{
					string name = ReadSectionName(line);
					if (name == null)
						sectionText.AppendLine(line);
					else if (name != sectionName)
						parser = null;
				};

			Action<string> defaultReader = line =>
				{
					if (IsCommonSectionStart(line))
						parser = commonSectionReader;
					else if (ReadSectionName(line) == sectionName)
					{
						parser = namedSectionReader;
						sectionFound = true;
					}
				};

			foreach(var line in lines)
			{
				(parser ?? defaultReader)(line);
			}

			if (sectionFound)
				return sectionText.ToString();
			else
				return "";
		}

		public static bool IsCommentOrBlank(string s)
		{
			if(String.IsNullOrWhiteSpace(s))
				return true;
			return s.Trim().StartsWith("//");
		}

		public static string ConcatLines(IEnumerable<string> lines)
		{
			var buffer = new StringBuilder();
			foreach (var line in lines)
			{
				buffer.AppendLine(line);
			}
			return buffer.ToString();
		}

		static string ReadSectionName(string line)
		{
			line = line.TrimStart();
			if (line.StartsWith("//$$"))
				return line.Substring(4).Trim();
			else
				return null;
		}

		static bool IsCommonSectionStart(string line)
		{
			return line.Trim() == "//$CS";
		}

		static bool IsCommonSectionEnd(string line)
		{
			return line.Trim() == "//$CE";
		}

		static IEnumerable<string> ToLines(string s)
		{
			var reader = new StringReader(s);
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				yield return line;
			}
		}
	}
}
