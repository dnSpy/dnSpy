// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class DefaultCompilationUnit : AbstractFreezable, ICompilationUnit
	{
		public static readonly ICompilationUnit DummyCompilationUnit = new DefaultCompilationUnit(DefaultProjectContent.DummyProjectContent).FreezeAndReturnSelf();
		
		DefaultCompilationUnit FreezeAndReturnSelf()
		{
			Freeze();
			return this;
		}
		
		IUsingScope usingScope = new DefaultUsingScope();
		IList<IClass> classes = new List<IClass>();
		IList<IAttribute> attributes = new List<IAttribute>();
		IList<FoldingRegion> foldingRegions = new List<FoldingRegion>();
		IList<TagComment> tagComments = new List<TagComment>();
		
		protected override void FreezeInternal()
		{
			// Deep Freeze: freeze lists and their contents
			classes = FreezeList(classes);
			attributes = FreezeList(attributes);
			foldingRegions = FreezeList(foldingRegions);
			tagComments = FreezeList(tagComments);
			usingScope.Freeze();
			
			base.FreezeInternal();
		}
		
		bool errorsDuringCompile = false;
		object tag               = null;
		string fileName          = null;
		IProjectContent projectContent;
		
		/// <summary>
		/// Source code file this compilation unit was created from. For compiled are compiler-generated
		/// code, this property returns null.
		/// </summary>
		public string FileName {
			get {
				return fileName;
			}
			set {
				CheckBeforeMutation();
				fileName = value;
			}
		}
		
		public IProjectContent ProjectContent {
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return projectContent;
			}
		}
		
		public bool ErrorsDuringCompile {
			get {
				return errorsDuringCompile;
			}
			set {
				CheckBeforeMutation();
				errorsDuringCompile = value;
			}
		}
		
		public object Tag {
			get {
				return tag;
			}
			set {
				CheckBeforeMutation();
				tag = value;
			}
		}
		
		public virtual IUsingScope UsingScope {
			get { return usingScope; }
			set {
				if (value == null)
					throw new ArgumentNullException("UsingScope");
				CheckBeforeMutation();
				usingScope = value;
			}
		}

		public virtual IList<IAttribute> Attributes {
			get {
				return attributes;
			}
		}

		public virtual IList<IClass> Classes {
			get {
				return classes;
			}
		}
		
		public IList<FoldingRegion> FoldingRegions {
			get {
				return foldingRegions;
			}
		}

		public virtual IList<IComment> MiscComments {
			get {
				return null;
			}
		}

		public virtual IList<IComment> DokuComments {
			get {
				return null;
			}
		}

		public virtual IList<TagComment> TagComments {
			get {
				return tagComments;
			}
		}
		
		public DefaultCompilationUnit(IProjectContent projectContent)
		{
			if (projectContent == null)
				throw new ArgumentNullException("projectContent");
			this.projectContent = projectContent;
		}
		
		public IClass GetInnermostClass(int caretLine, int caretColumn)
		{
			foreach (IClass c in Classes) {
				if (c != null && DefaultClass.IsInside(c, caretLine, caretColumn)) {
					return c.GetInnermostClass(caretLine, caretColumn);
				}
			}
			return null;
		}
		
		public override string ToString() 
		{
			return String.Format("[CompilationUnit: classes = {0}, fileName = {1}]",
			                     classes.Count,
			                     fileName);
		}
		
		public LanguageProperties Language {
			get {
				return projectContent.Language;
			}
		}
	}
}
