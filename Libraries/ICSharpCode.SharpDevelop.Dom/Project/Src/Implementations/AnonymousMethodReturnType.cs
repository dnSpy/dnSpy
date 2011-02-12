// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// The return type of anonymous method expressions or lambda expressions.
	/// </summary>
	public class AnonymousMethodReturnType : DecoratingReturnType
	{
		IReturnType returnType;
		IList<IParameter> parameters;
		ICompilationUnit cu;
		
		public AnonymousMethodReturnType(ICompilationUnit cu)
		{
			this.cu = cu;
		}
		
		public override bool Equals(IReturnType other)
		{
			if (other == null) return false;
			AnonymousMethodReturnType o = other.CastToDecoratingReturnType<AnonymousMethodReturnType>();
			if (o == null) return false;
			return this.FullyQualifiedName == o.FullyQualifiedName;
		}
		
		public override int GetHashCode()
		{
			return this.FullyQualifiedName.GetHashCode();
		}
		
		public override T CastToDecoratingReturnType<T>()
		{
			if (typeof(T) == typeof(AnonymousMethodReturnType)) {
				return (T)(object)this;
			} else {
				return null;
			}
		}
		
		public IReturnType ToDefaultDelegate()
		{
			IReturnType type = new GetClassReturnType(cu.ProjectContent, "System.Func", 0);
			List<IReturnType> parameters = new List<IReturnType>();
			
			if (this.HasParameterList)
				parameters = MethodParameters.Select(p => p.ReturnType ?? new GetClassReturnType(cu.ProjectContent, "System.Object", 0)).ToList();
			
			if (this.MethodReturnType != null && this.MethodReturnType.FullyQualifiedName == "System.Void")
				type = new GetClassReturnType(cu.ProjectContent, "System.Action", 0);
			else {
				var rt = this.MethodReturnType;
				if (rt == null)
					rt = new GetClassReturnType(cu.ProjectContent, "System.Object", 0);
				parameters.Add(rt);
			}
			
			return new ConstructedReturnType(type, parameters);
		}
		
		/// <summary>
		/// Return type of the anonymous method. Can be null if inferred from context.
		/// </summary>
		public IReturnType MethodReturnType {
			get { return returnType; }
			set { returnType = value; }
		}
		
		public virtual IReturnType ResolveReturnType()
		{
			return returnType;
		}
		
		public virtual IReturnType ResolveReturnType(IReturnType[] parameterTypes)
		{
			return returnType;
		}
		
		/// <summary>
		/// Gets the list of method parameters. Can be null if the anonymous method has no parameter list.
		/// </summary>
		public IList<IParameter> MethodParameters {
			get { return parameters; }
			set { parameters = value; }
		}
		
		public virtual bool CanBeConvertedToExpressionTree {
			get { return false; }
		}
		
		public bool HasParameterList {
			get { return parameters != null; }
		}
		
		public bool HasImplicitlyTypedParameters {
			get {
				if (parameters == null)
					return false;
				else
					return parameters.Any(p => p.ReturnType == null);
			}
		}
		
		DefaultClass cachedClass;
		
		public override IClass GetUnderlyingClass()
		{
			if (cachedClass != null) return cachedClass;
			DefaultClass c = new DefaultClass(cu, ClassType.Delegate, ModifierEnum.None, DomRegion.Empty, null);
			c.BaseTypes.Add(cu.ProjectContent.SystemTypes.Delegate);
			AddDefaultDelegateMethod(c, returnType ?? cu.ProjectContent.SystemTypes.Object, parameters ?? new IParameter[0]);
			cachedClass = c;
			return c;
		}
		
		internal static void AddDefaultDelegateMethod(DefaultClass c, IReturnType returnType, IList<IParameter> parameters)
		{
			ModifierEnum modifiers = ModifierEnum.Public | ModifierEnum.Synthetic;
			DefaultMethod invokeMethod = new DefaultMethod("Invoke", returnType, modifiers, c.Region, DomRegion.Empty, c);
			foreach (IParameter par in parameters) {
				invokeMethod.Parameters.Add(par);
			}
			c.Methods.Add(invokeMethod);
			invokeMethod = new DefaultMethod("BeginInvoke", c.ProjectContent.SystemTypes.IAsyncResult, modifiers, c.Region, DomRegion.Empty, c);
			foreach (IParameter par in parameters) {
				invokeMethod.Parameters.Add(par);
			}
			invokeMethod.Parameters.Add(new DefaultParameter("callback", c.ProjectContent.SystemTypes.AsyncCallback, DomRegion.Empty));
			invokeMethod.Parameters.Add(new DefaultParameter("object", c.ProjectContent.SystemTypes.Object, DomRegion.Empty));
			c.Methods.Add(invokeMethod);
			invokeMethod = new DefaultMethod("EndInvoke", returnType, modifiers, c.Region, DomRegion.Empty, c);
			invokeMethod.Parameters.Add(new DefaultParameter("result", c.ProjectContent.SystemTypes.IAsyncResult, DomRegion.Empty));
			c.Methods.Add(invokeMethod);
		}
		
		public override IReturnType BaseType {
			get {
				return GetUnderlyingClass().DefaultReturnType;
			}
		}
		
		public override string Name {
			get {
				return "delegate";
			}
		}
		
		public override string FullyQualifiedName {
			get {
				StringBuilder b = new StringBuilder("delegate");
				if (HasParameterList) {
					bool first = true;
					b.Append("(");
					foreach (IParameter p in parameters) {
						if (first) first = false; else b.Append(", ");
						b.Append(p.Name);
						if (p.ReturnType != null) {
							b.Append(":");
							b.Append(p.ReturnType.Name);
						}
					}
					b.Append(")");
				}
				if (returnType != null) {
					b.Append(":");
					b.Append(returnType.Name);
				}
				return b.ToString();
			}
		}
		
		public override string Namespace {
			get {
				return "";
			}
		}
		
		public override string DotNetName {
			get {
				return Name;
			}
		}
	}
}
