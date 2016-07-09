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

using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;
using System.Text;
using dnSpy.Contracts.Languages.XmlDoc;

namespace dnSpy.Languages.ILSpy.XmlDoc {
	/// <summary>
	/// Adds XML documentation for member definitions.
	/// </summary>
	struct AddXmlDocTransform {
		readonly StringBuilder stringBuilder;

		public AddXmlDocTransform(StringBuilder sb) {
			this.stringBuilder = sb;
		}

		public void Run(AstNode node) {
			if (node is EntityDeclaration) {
				IMemberRef mr = node.Annotation<IMemberRef>();
				if (mr != null && mr.Module != null) {
					var xmldoc = XmlDocLoader.LoadDocumentation(mr.Module);
					if (xmldoc != null) {
						string doc = xmldoc.GetDocumentation(XmlDocKeyProvider.GetKey(mr, stringBuilder));
						if (!string.IsNullOrEmpty(doc)) {
							InsertXmlDocumentation(node, doc);
						}
					}
				}
				if (!(node is TypeDeclaration))
					return; // don't recurse into attributed nodes, except for type definitions
			}
			foreach (AstNode child in node.Children)
				Run(child);
		}

		void InsertXmlDocumentation(AstNode node, string doc) {
			foreach (var info in new XmlDocLine(doc)) {
				stringBuilder.Clear();
				if (info != null) {
					stringBuilder.Append(' ');
					info.Value.WriteTo(stringBuilder);
				}
				node.Parent.InsertChildBefore(node, new Comment(stringBuilder.ToString(), CommentType.Documentation), Roles.Comment);
			}
		}
	}
}
