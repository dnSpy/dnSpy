// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class DynamicReturnType : AbstractReturnType
	{
		readonly IProjectContent pc;
		
		public DynamicReturnType(IProjectContent pc)
		{
			if (pc == null)
				throw new ArgumentNullException("pc");
			this.pc = pc;
		}
		
		public override IClass GetUnderlyingClass()
		{
			return null;
		}
		
		public override List<IMethod> GetMethods()
		{
			return new List<IMethod>();
		}
		public override List<IProperty> GetProperties()
		{
			return new List<IProperty>();
		}
		public override List<IField> GetFields()
		{
			return new List<IField>();
		}
		public override List<IEvent> GetEvents()
		{
			return new List<IEvent>();
		}
		
		public override string Name {
			get { return "dynamic"; }
		}
		
		public override string FullyQualifiedName {
			get { return "dynamic"; }
			set { throw new NotSupportedException(); }
		}
	}
}
