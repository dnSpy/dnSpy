// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
			s = s.Trim();
			return s.StartsWith("//") || s.StartsWith("#");	// Also ignore #pragmas for warning suppression
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
