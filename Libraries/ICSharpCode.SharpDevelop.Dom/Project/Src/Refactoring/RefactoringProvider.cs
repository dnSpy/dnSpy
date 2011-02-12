// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.NRefactory.Ast;
using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public abstract class RefactoringProvider
	{
		/// <summary>
		/// A RefactoringProvider instance that supports no refactorings.
		/// </summary>
		public static readonly RefactoringProvider DummyProvider = new DummyRefactoringProvider();
		
		protected RefactoringProvider() {}
		
		public abstract bool IsEnabledForFile(string fileName);
		
		private class DummyRefactoringProvider : RefactoringProvider
		{
			public override bool IsEnabledForFile(string fileName)
			{
				return false;
			}
		}
		
		#region ExtractInterface
		public virtual bool SupportsExtractInterface {
			get {
				return false;
			}
		}
		public virtual string GenerateInterfaceForClass(string newInterfaceName, string existingCode, IList<IMember> membersToKeep, IClass sourceClass, bool preserveComments)

		{
			throw new NotSupportedException();
		}
		
		public virtual string AddBaseTypeToClass(string existingCode, IClass targetClass, IClass newBaseType)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region FindUnusedUsingDeclarations
		public virtual bool SupportsFindUnusedUsingDeclarations {
			get {
				return false;
			}
		}
		
		public virtual IList<IUsing> FindUnusedUsingDeclarations(IDomProgressMonitor progressMonitor, string fileName, string fileContent, ICompilationUnit compilationUnit)
		{
			throw new NotSupportedException();
		}
		#endregion
		
		#region CreateNewFileLikeExisting
		public virtual bool SupportsCreateNewFileLikeExisting {
			get {
				return false;
			}
		}
		
		/// <summary>
		/// Creates a new file that uses same header, usings and namespace like an existing file.
		/// </summary>
		/// <returns>the content for the new file,
		/// or null if an error occurred (error will be displayed to the user)</returns>
		/// <param name="existingFileContent">Content of the exisiting file</param>
		/// <param name="codeForNewType">Code to put in the new file.</param>
		public virtual string CreateNewFileLikeExisting(string existingFileContent, string codeForNewType)
		{
			throw new NotSupportedException();
		}
		#endregion
		
		#region ExtractCodeForType
		public virtual bool SupportsGetFullCodeRangeForType {
			get {
				return false;
			}
		}
		
		public virtual DomRegion GetFullCodeRangeForType(string fileContent, IClass type)
		{
			throw new NotSupportedException();
		}
		#endregion
	}
}
