// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Class that stores a source code context and can resolve type names
	/// in that context.
	/// </summary>
	public sealed class ClassFinder
	{
		int caretLine, caretColumn;
		ICompilationUnit cu;
		IClass callingClass;
		IProjectContent projectContent;
		
		public IClass CallingClass {
			get {
				return callingClass;
			}
		}
		
		public IProjectContent ProjectContent {
			get {
				return projectContent;
			}
		}
		
		public LanguageProperties Language {
			get {
				return projectContent.Language;
			}
		}
		
		public int CaretLine {
			get { return caretLine; }
		}
		
		public int CaretColumn {
			get { return caretColumn; }
		}
		
		public ClassFinder(ParseInformation parseInfo, string fileContent, int offset)
		{
			caretLine = 0;
			caretColumn = 0;
			for (int i = 0; i < offset; i++) {
				if (fileContent[i] == '\n') {
					caretLine++;
					caretColumn = 0;
				} else {
					caretColumn++;
				}
			}
			Init(parseInfo);
		}
		
		public ClassFinder(ParseInformation parseInfo, int caretLineNumber, int caretColumn)
		{
			this.caretLine   = caretLineNumber;
			this.caretColumn = caretColumn;
			
			Init(parseInfo);
		}
		
		public ClassFinder(IMember classMember)
			: this(classMember.DeclaringType, classMember.Region.BeginLine, classMember.Region.BeginColumn)
		{
		}
		
		public ClassFinder(IClass callingClass, int caretLine, int caretColumn)
		{
			if (callingClass == null)
				throw new ArgumentNullException("callingClass");
			if (callingClass is CompoundClass)
				throw new ArgumentException("Cannot use compound class for ClassFinder - must pass a specific class part.");
			this.caretLine = caretLine;
			this.caretColumn = caretColumn;
			this.callingClass = callingClass;
			this.cu = callingClass.CompilationUnit;
			this.projectContent = cu.ProjectContent;
			if (projectContent == null)
				throw new ArgumentException("callingClass must have a project content!");
		}
		
		// currently callingMember is not required
		public ClassFinder(IClass callingClass, IMember callingMember, int caretLine, int caretColumn)
			: this(callingClass, caretLine, caretColumn)
		{
		}
		
		void Init(ParseInformation parseInfo)
		{
			if (parseInfo != null) {
				cu = parseInfo.CompilationUnit;
			}
			
			if (cu != null) {
				callingClass = cu.GetInnermostClass(caretLine, caretColumn);
				projectContent = cu.ProjectContent;
			} else {
				projectContent = DefaultProjectContent.DummyProjectContent;
			}
			if (projectContent == null)
				throw new ArgumentException("projectContent not found!");
		}
		
		public IClass GetClass(string fullName, int typeParameterCount)
		{
			return projectContent.GetClass(fullName, typeParameterCount);
		}
		
		public IReturnType SearchType(string name, int typeParameterCount)
		{
			return Search(name, typeParameterCount).Result;
		}
		
		public SearchTypeResult Search(string name, int typeParameterCount)
		{
			return projectContent.SearchType(new SearchTypeRequest(name, typeParameterCount, callingClass, cu, caretLine, caretColumn));
		}
		
		public string SearchNamespace(string name)
		{
			return Search(name, 0).NamespaceResult;
		}
	}
}
