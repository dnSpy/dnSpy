// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.IO;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.XmlDoc
{
	/// <summary>
	/// Adds XML documentation for member definitions.
	/// </summary>
	static class AddXmlDocTransform
	{
		public static void Run(AstNode node)
		{
			if (node is EntityDeclaration) {
				MemberReference mr = node.Annotation<MemberReference>();
				if (mr != null && mr.Module != null) {
					var xmldoc = XmlDocLoader.LoadDocumentation(mr.Module);
					if (xmldoc != null) {
						string doc = xmldoc.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
						if (doc != null) {
							InsertXmlDocumentation(node, new StringReader(doc));
						}
					}
				}
				if (!(node is TypeDeclaration))
					return; // don't recurse into attributed nodes, except for type definitions
			}
			foreach (AstNode child in node.Children)
				Run(child);
		}
		
		static void InsertXmlDocumentation(AstNode node, StringReader r)
		{
			// Find the first non-empty line:
			string firstLine;
			do {
				firstLine = r.ReadLine();
				if (firstLine == null)
					return;
			} while (string.IsNullOrWhiteSpace(firstLine));
			string indentation = firstLine.Substring(0, firstLine.Length - firstLine.TrimStart().Length);
			string line = firstLine;
			int skippedWhitespaceLines = 0;
			// Copy all lines from input to output, except for empty lines at the end.
			while (line != null) {
				if (string.IsNullOrWhiteSpace(line)) {
					skippedWhitespaceLines++;
				} else {
					while (skippedWhitespaceLines > 0) {
						node.Parent.InsertChildBefore(node, new Comment(string.Empty, CommentType.Documentation), Roles.Comment);
						skippedWhitespaceLines--;
					}
					if (line.StartsWith(indentation, StringComparison.Ordinal))
						line = line.Substring(indentation.Length);
					node.Parent.InsertChildBefore(node, new Comment(" " + line, CommentType.Documentation), Roles.Comment);
				}
				line = r.ReadLine();
			}
		}
	}
}
