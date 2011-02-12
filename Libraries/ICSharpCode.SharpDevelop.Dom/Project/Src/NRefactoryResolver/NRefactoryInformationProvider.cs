// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	public class NRefactoryInformationProvider : IEnvironmentInformationProvider
	{
		IProjectContent _projectContent;
		
		public NRefactoryInformationProvider(IProjectContent projectContent)
		{
			if (projectContent == null)
				throw new ArgumentNullException("projectContent");
			_projectContent = projectContent;
		}
		
		public bool HasField(string reflectionTypeName, int typeParameterCount, string fieldName)
		{
			IClass c;
			if (typeParameterCount > 0) {
				c = _projectContent.GetClass(reflectionTypeName, typeParameterCount);
			} else {
				c = _projectContent.GetClassByReflectionName(reflectionTypeName, true);
			}
			if (c == null)
				return false;
			foreach (IField field in c.DefaultReturnType.GetFields()) {
				if (field.Name == fieldName)
					return true;
			}
			return false;
		}
	}
}
