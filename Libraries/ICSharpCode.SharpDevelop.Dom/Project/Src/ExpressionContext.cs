// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Class describing a context in which an expression can be.
	/// Serves as filter for code completion results, but the contexts exposed as static fields
	/// can also be used as a kind of enumeration for special behaviour in the resolver.
	/// </summary>
	public abstract class ExpressionContext
	{
		#region Instance members
		public abstract bool ShowEntry(ICompletionEntry o);
		
		protected bool readOnly = true;
		object suggestedItem;
		
		/// <summary>
		/// Gets if the expression is in the context of an object creation.
		/// </summary>
		public virtual bool IsObjectCreation {
			get {
				return false;
			}
			set {
				if (value)
					throw new NotSupportedException();
			}
		}
		
		/// <summary>
		/// Gets/Sets the default item that should be included in a code completion popup
		/// in this context and selected as default value.
		/// </summary>
		/// <example>
		/// "List&lt;TypeName&gt; var = new *expr*();" has as suggested item the pseudo-class
		/// "List&lt;TypeName&gt;".
		/// </example>
		public object SuggestedItem {
			get {
				return suggestedItem;
			}
			set {
				if (readOnly)
					throw new NotSupportedException();
				suggestedItem = value;
			}
		}
		
		public virtual bool IsTypeContext {
			get { return false; }
		}
		#endregion
		
		#region VB specific contexts (public static fields) * MOVE TO ANOTHER CLASS *
		/// <summary>The context expects a new parameter declaration</summary>
		/// <example>Function Test(*expr*, *expr*, ...)</example>
		public static readonly ExpressionContext Parameter = new DefaultExpressionContext("Parameter");
		#endregion
		
		#region Default contexts (public static fields)
		/// <summary>Default/unknown context</summary>
		public readonly static ExpressionContext Default = new DefaultExpressionContext("Default");
		
		/// <summary>The context expects the base type of an enum.</summary>
		/// <example>enum Name : *expr* {}</example>
		public readonly static ExpressionContext EnumBaseType = new EnumBaseTypeExpressionContext();
		
		/// <summary>Context expects a non-sealed type or interface</summary>
		/// <example>class C : *expr* {}</example>
		public readonly static ExpressionContext InheritableType = new InheritableTypeExpressionContext();
		
		/// <summary>Context expects a namespace name</summary>
		/// <example>using *expr*;</example>
		public readonly static ExpressionContext Namespace = new ImportableExpressionContext(false);
		
		/// <summary>Context expects an importable type (namespace or class with public static members)</summary>
		/// <example>Imports *expr*;</example>
		public readonly static ExpressionContext Importable = new ImportableExpressionContext(true);
		
		/// <summary>Context expects a type name</summary>
		/// <example>typeof(*expr*)</example>
		public readonly static ExpressionContext Type = new TypeExpressionContext(null, false, true);
		
		/// <summary>Context expects the name of a non-static, non-void type</summary>
		/// <example>is *expr*, *expr* variableName</example>
		public readonly static ExpressionContext NonStaticNonVoidType = new NonStaticTypeExpressionContext("NonStaticType", false);
		
		/// <summary>Context expects a non-abstract type that has accessible constructors</summary>
		/// <example>new *expr*();</example>
		/// <remarks>When using this context, a resolver should treat the expression as object creation,
		/// even when the keyword "new" is not part of the expression.</remarks>
		public readonly static ExpressionContext ObjectCreation = new TypeExpressionContext(null, true, true);
		
		/// <summary>Context expects a type deriving from System.Attribute.</summary>
		/// <example>[*expr*()]</example>
		/// <remarks>When using this context, a resolver should try resolving typenames with an
		/// appended "Attribute" suffix and treat "invocations" of the attribute type as
		/// object creation.</remarks>
		public readonly static ExpressionContext Attribute = new AttributeExpressionContext();
		
		/// <summary>Context expects a type name which has special base type</summary>
		/// <param name="baseClass">The class the expression must derive from.</param>
		/// <param name="isObjectCreation">Specifies whether classes must be constructable.</param>
		/// <example>catch(*expr* ...), using(*expr* ...), throw new *expr*();</example>
		public static ExpressionContext TypeDerivingFrom(IReturnType baseType, bool isObjectCreation)
		{
			return new TypeExpressionContext(baseType, isObjectCreation, false);
		}
		
		/// <summary>Context expects an interface</summary>
		/// <example>interface C : *expr* {}</example>
		/// <example>Implements *expr*</example>
		public readonly static ExpressionContext Interface = new ClassTypeExpressionContext(ClassType.Interface);
		
		/// <summary>Context expects a delegate</summary>
		/// <example>public event *expr*</example>
		public readonly static ExpressionContext DelegateType = new ClassTypeExpressionContext(ClassType.Delegate);
		
		/// <summary>The context expects a new identifier</summary>
		/// <example>class *expr* {}; string *expr*;</example>
		public readonly static ExpressionContext IdentifierExpected = new DefaultExpressionContext("IdentifierExpected");
		
		/// <summary>The context is outside of any type declaration, expecting a global-level keyword.</summary>
		public readonly static ExpressionContext Global = new DefaultExpressionContext("Global");
		
		/// <summary>The context is the body of a type declaration.</summary>
		public readonly static ExpressionContext TypeDeclaration = new ExpressionContext.NonStaticTypeExpressionContext("TypeDeclaration", true);
		
		/// <summary>The context is the body of a method.</summary>
		/// <example>void Main () { *expr* }</example>
		public readonly static ExpressionContext MethodBody = new ExpressionContext.DefaultExpressionContext("MethodBody");
		#endregion
		
		#region DefaultExpressionContext
		internal sealed class DefaultExpressionContext : ExpressionContext
		{
			string name;
			
			public DefaultExpressionContext(string name)
			{
				this.name = name;
			}
			
			public override bool ShowEntry(ICompletionEntry o)
			{
				return true;
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + ": " + name + "]";
			}
		}
		#endregion
		
		#region NamespaceExpressionContext
		sealed class ImportableExpressionContext : ExpressionContext
		{
			bool allowImportClasses;
			
			public ImportableExpressionContext(bool allowImportClasses)
			{
				this.allowImportClasses = allowImportClasses;
			}
			
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (!(o is IEntity))
					return true;
				IClass c = o as IClass;
				if (allowImportClasses && c != null) {
					return c.HasPublicOrInternalStaticMembers;
				}
				return false;
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + " AllowImportClasses=" + allowImportClasses.ToString() + "]";
			}
		}
		#endregion
		
		#region TypeExpressionContext
		sealed class TypeExpressionContext : ExpressionContext
		{
			IClass baseClass;
			bool isObjectCreation;
			
			public TypeExpressionContext(IReturnType baseType, bool isObjectCreation, bool readOnly)
			{
				if (baseType != null)
					baseClass = baseType.GetUnderlyingClass();
				this.isObjectCreation = isObjectCreation;
				this.readOnly = readOnly;
			}
			
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (!(o is IEntity))
					return true;
				IClass c = o as IClass;
				if (c == null)
					return false;
				if (isObjectCreation) {
					if (c.IsAbstract || c.IsStatic)    return false;
					if (c.ClassType == ClassType.Enum || c.ClassType == ClassType.Interface)
						return false;
				}
				if (baseClass == null)
					return true;
				return c.IsTypeInInheritanceTree(baseClass);
			}
			
			public override bool IsObjectCreation {
				get {
					return isObjectCreation;
				}
				set {
					if (readOnly && value != isObjectCreation)
						throw new NotSupportedException();
					isObjectCreation = value;
				}
			}
			
			public override bool IsTypeContext {
				get { return true; }
			}
			
			public override string ToString()
			{
				if (baseClass != null)
					return "[" + GetType().Name + ": " + baseClass.FullyQualifiedName
						+ " IsObjectCreation=" + IsObjectCreation + "]";
				else
					return "[" + GetType().Name + " IsObjectCreation=" + IsObjectCreation + "]";
			}
			
			public override bool Equals(object obj)
			{
				TypeExpressionContext o = obj as TypeExpressionContext;
				return o != null && object.Equals(baseClass, o.baseClass) && IsObjectCreation == o.IsObjectCreation;
			}
			
			public override int GetHashCode()
			{
				return ((baseClass != null) ? baseClass.GetHashCode() : 0)
					^ isObjectCreation.GetHashCode();
			}
		}
		#endregion
		
		#region CombinedExpressionContext
		public static ExpressionContext operator | (ExpressionContext a, ExpressionContext b)
		{
			return new CombinedExpressionContext(0, a, b);
		}
		
		public static ExpressionContext operator & (ExpressionContext a, ExpressionContext b)
		{
			return new CombinedExpressionContext(1, a, b);
		}
		
		public static ExpressionContext operator ^ (ExpressionContext a, ExpressionContext b)
		{
			return new CombinedExpressionContext(2, a, b);
		}
		
		sealed class CombinedExpressionContext : ExpressionContext
		{
			byte opType; // 0 = or ; 1 = and ; 2 = xor
			ExpressionContext a;
			ExpressionContext b;
			
			public CombinedExpressionContext(byte opType, ExpressionContext a, ExpressionContext b)
			{
				if (a == null)
					throw new ArgumentNullException("a");
				if (b == null)
					throw new ArgumentNullException("a");
				this.opType = opType;
				this.a = a;
				this.b = b;
			}
			
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (opType == 0)
					return a.ShowEntry(o) || b.ShowEntry(o);
				else if (opType == 1)
					return a.ShowEntry(o) && b.ShowEntry(o);
				else
					return a.ShowEntry(o) ^ b.ShowEntry(o);
			}
			
			public override string ToString()
			{
				string op;
				if (opType == 0)
					op = " OR ";
				else if (opType == 1)
					op = " AND ";
				else
					op = " XOR ";
				return "[" + GetType().Name + ": " + a + op + b + "]";
			}
			
			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked {
					hashCode += opType.GetHashCode();
					if (a != null) hashCode += a.GetHashCode() * 3;
					if (b != null) hashCode += b.GetHashCode() * 181247123;
				}
				return hashCode;
			}
			
			public override bool Equals(object obj)
			{
				CombinedExpressionContext cec = obj as CombinedExpressionContext;
				return cec != null && this.opType == cec.opType && object.Equals(this.a, cec.a) && object.Equals(this.b, cec.b);
			}
		}
		#endregion
		
		#region EnumBaseTypeExpressionContext
		sealed class EnumBaseTypeExpressionContext : ExpressionContext
		{
			public override bool ShowEntry(ICompletionEntry o)
			{
				IClass c = o as IClass;
				if (c != null) {
					// use this hack to show dummy classes like "short"
					// (go from the dummy class to the real class)
					if (c.Methods.Count > 0) {
						c = c.Methods[0].DeclaringType;
					}
					switch (c.FullyQualifiedName) {
						case "System.Byte":
						case "System.SByte":
						case "System.Int16":
						case "System.UInt16":
						case "System.Int32":
						case "System.UInt32":
						case "System.Int64":
						case "System.UInt64":
							return true;
						default:
							return false;
					}
				} else {
					return false;
				}
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + "]";
			}
		}
		#endregion
		
		#region AttributeExpressionContext
		sealed class AttributeExpressionContext : ExpressionContext
		{
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (!(o is IEntity))
					return true;
				IClass c = o as IClass;
				if (c != null && !c.IsAbstract) {
					return c.IsTypeInInheritanceTree(c.ProjectContent.SystemTypes.Attribute.GetUnderlyingClass());
				} else {
					return false;
				}
			}
			
			public override bool IsTypeContext {
				get { return true; }
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + "]";
			}
		}
		#endregion
		
		#region InheritableTypeExpressionContext
		sealed class InheritableTypeExpressionContext : ExpressionContext
		{
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (!(o is IEntity)) return true;
				IClass c = o as IClass;
				if (c != null) {
					foreach (IClass innerClass in c.InnerClasses) {
						if (ShowEntry(innerClass)) return true;
					}
					if (c.ClassType == ClassType.Interface) return true;
					if (c.ClassType == ClassType.Class) {
						if (!c.IsSealed && !c.IsStatic) return true;
					}
				}
				return false;
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + "]";
			}
		}
		#endregion
		
		#region ClassTypeExpressionContext
		sealed class ClassTypeExpressionContext : ExpressionContext
		{
			readonly ClassType expectedType;
			
			public ClassTypeExpressionContext(ClassType expectedType)
			{
				this.expectedType = expectedType;
			}
			
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (!(o is IEntity)) return true;
				IClass c = o as IClass;
				if (c != null) {
					foreach (IClass innerClass in c.InnerClasses) {
						if (ShowEntry(innerClass)) return true;
					}
					if (c.ClassType == expectedType) return true;
				}
				return false;
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + " expectedType=" + expectedType.ToString() + "]";
			}
		}
		#endregion
		
		#region NonStaticTypeExpressionContext
		internal sealed class NonStaticTypeExpressionContext : ExpressionContext
		{
			string name;
			bool allowVoid;
			
			public NonStaticTypeExpressionContext(string name, bool allowVoid)
			{
				this.name = name;
				this.allowVoid = allowVoid;
			}
			
			public override bool ShowEntry(ICompletionEntry o)
			{
				if (!(o is IEntity)) return true;
				IClass c = o as IClass;
				if (c != null) {
					if (!allowVoid) {
						if (c.FullyQualifiedName == "System.Void" || c.FullyQualifiedName == "void") return false;
					}
					
					foreach (IClass innerClass in c.InnerClasses) {
						if (ShowEntry(innerClass)) return true;
					}
					if (!c.IsStatic && c.ClassType != ClassType.Module) return true;
				}
				return false;
			}
			
			public override string ToString()
			{
				return "[" + GetType().Name + " " + name + "]";
			}
		}
		#endregion
	}
}
