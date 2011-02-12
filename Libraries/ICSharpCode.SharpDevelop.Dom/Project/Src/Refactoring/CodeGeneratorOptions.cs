// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public class CodeGeneratorOptions
	{
		public bool BracesOnSameLine = true;
		public bool EmptyLinesBetweenMembers = true;
		string indentString = "\t";
		
		public string IndentString {
			get { return indentString; }
			set {
				if (string.IsNullOrEmpty(value)) {
					throw new ArgumentNullException("value");
				}
				indentString = value;
			}
		}
	}
}
