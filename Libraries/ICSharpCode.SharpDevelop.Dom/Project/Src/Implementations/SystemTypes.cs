// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class SystemTypes
	{
		public readonly IReturnType Void;
		public readonly IReturnType Object;
		public readonly IReturnType Delegate;
		public readonly IReturnType MulticastDelegate;
		public readonly IReturnType ValueType;
		public readonly IReturnType Enum;
		
		public readonly IReturnType Boolean;
		public readonly IReturnType Int32;
		public readonly IReturnType String;
		
		public readonly IReturnType Array;
		public readonly IReturnType Attribute;
		public readonly IReturnType Type;
		
		public readonly IReturnType Exception;
		public readonly IReturnType AsyncCallback;
		public readonly IReturnType IAsyncResult;
		public readonly IReturnType IDisposable;
		
		IProjectContent pc;
		
		public SystemTypes(IProjectContent pc)
		{
			this.pc = pc;
			Void      = new VoidReturnType(pc);
			Object    = CreateFromName("System.Object");
			Delegate  = CreateFromName("System.Delegate");
			MulticastDelegate = CreateFromName("System.MulticastDelegate");
			ValueType = CreateFromName("System.ValueType");
			Enum      = CreateFromName("System.Enum");
			
			Boolean = CreateFromName("System.Boolean");
			Int32   = CreateFromName("System.Int32");
			String  = CreateFromName("System.String");
			
			Array     = CreateFromName("System.Array");
			Attribute = CreateFromName("System.Attribute");
			Type      = CreateFromName("System.Type");
			
			Exception     = CreateFromName("System.Exception");
			AsyncCallback = CreateFromName("System.AsyncCallback");
			IAsyncResult  = CreateFromName("System.IAsyncResult");
			IDisposable = CreateFromName("System.IDisposable");
		}
		
		IReturnType CreateFromName(string name)
		{
			IClass c = pc.GetClass(name, 0);
			if (c != null) {
				return c.DefaultReturnType;
			} else {
				LoggingService.Warn("SystemTypes.CreateFromName could not find " + name);
				return Void;
			}
		}
		
		/// <summary>
		/// Creates the return type for a primitive system type.
		/// </summary>
		public IReturnType CreatePrimitive(Type type)
		{
			if (type.HasElementType || type.ContainsGenericParameters) {
				throw new ArgumentException("Only primitive types are supported.");
			}
			return CreateFromName(type.FullName);
		}
	}
	
	public sealed class VoidClass : DefaultClass
	{
		internal static readonly string VoidName = typeof(void).FullName;
		
		public VoidClass(IProjectContent pc)
			: base(new DefaultCompilationUnit(pc), VoidName)
		{
			this.ClassType = ClassType.Struct;
			this.Modifiers = ModifierEnum.Public | ModifierEnum.Sealed;
			Freeze();
		}
		
		protected override IReturnType CreateDefaultReturnType()
		{
			return ProjectContent.SystemTypes.Void;
		}
	}
	
	public sealed class VoidReturnType : AbstractReturnType
	{
		IProjectContent pc;
		
		public VoidReturnType(IProjectContent pc)
		{
			if (pc == null)
				throw new ArgumentNullException("pc");
			this.pc = pc;
			FullyQualifiedName = VoidClass.VoidName;
		}
		
		public override IClass GetUnderlyingClass()
		{
			return pc.GetClass("System.Void", 0, LanguageProperties.CSharp, GetClassOptions.LookInReferences);
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
	}
}
