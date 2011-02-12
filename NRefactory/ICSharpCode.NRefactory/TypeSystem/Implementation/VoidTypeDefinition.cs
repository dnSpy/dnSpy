// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Special type definition for 'void'.
	/// </summary>
	public class VoidTypeDefinition : DefaultTypeDefinition
	{
		public VoidTypeDefinition(IProjectContent projectContent)
			: base(projectContent, "System", "Void")
		{
			this.ClassType = ClassType.Struct;
			this.Accessibility = Accessibility.Public;
			this.IsSealed = true;
		}
		
		public override IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter)
		{
			return EmptyList<IEvent>.Instance;
		}
		
		public override IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter)
		{
			return EmptyList<IField>.Instance;
		}
		
		public override IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter)
		{
			return EmptyList<IProperty>.Instance;
		}
	}
}
