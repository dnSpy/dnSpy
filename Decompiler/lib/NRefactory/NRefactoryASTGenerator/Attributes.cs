// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.CodeDom;
using System.Reflection;
using ICSharpCode.EasyCodeDom;

namespace NRefactoryASTGenerator
{
	public enum NullableImplementation
	{
		/// <summary>
		/// Implement INullable with a virtual bool IsNull, create Null class and static instance
		/// of it.
		/// </summary>
		Default,
		/// <summary>
		/// Create Null class and a static instance using the "new" modifier.
		/// </summary>
		Shadow,
		/// <summary>
		/// Implement INullable with a virtual bool IsNull.
		/// </summary>
		Abstract,
		/// <summary>
		/// Complete an abstract nullable implementation by creating the Null class
		/// and the static instance.
		/// </summary>
		CompleteAbstract
	}
	
	public abstract class TypeImplementationModifierAttribute : Attribute
	{
		public abstract void ModifyImplementation(CodeNamespace cns, CodeTypeDeclaration ctd, Type type);
	}
	
	[AttributeUsage(AttributeTargets.Class)]
	public class CustomImplementationAttribute : Attribute
	{
	}
	
	[AttributeUsage(AttributeTargets.Class)]
	public class HasChildrenAttribute : Attribute
	{
	}
	
	[AttributeUsage(AttributeTargets.Field)]
	public class QuestionMarkDefaultAttribute : Attribute
	{
	}
	
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class IncludeMemberAttribute : TypeImplementationModifierAttribute
	{
		string code;
		
		public IncludeMemberAttribute(string code)
		{
			this.code = code;
		}
		
		public override void ModifyImplementation(CodeNamespace cns, CodeTypeDeclaration ctd, Type type)
		{
			ctd.Members.Add(new CodeSnippetTypeMember(code));
		}
	}
	
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class IncludeBoolPropertyAttribute : TypeImplementationModifierAttribute
	{
		string name;
		string code;
		
		public IncludeBoolPropertyAttribute(string name, string code)
		{
			this.name = name;
			this.code = code;
		}
		
		public override void ModifyImplementation(CodeNamespace cns, CodeTypeDeclaration ctd, Type type)
		{
			CodeMemberProperty prop = new CodeMemberProperty();
			prop.Name = name;
			prop.Type = new CodeTypeReference(typeof(bool));
			prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			prop.GetStatements.Add(new CodeSnippetStatement("\t\t\t\t" + code));
			ctd.Members.Add(prop);
		}
	}
	
	[AttributeUsage(AttributeTargets.Class)]
	public class ImplementNullableAttribute : TypeImplementationModifierAttribute
	{
		NullableImplementation implementation;
		
		public ImplementNullableAttribute()
		{
			this.implementation = NullableImplementation.Default;
		}
		
		public ImplementNullableAttribute(NullableImplementation implementation)
		{
			this.implementation = implementation;
		}
		
		public override void ModifyImplementation(CodeNamespace cns, CodeTypeDeclaration ctd, Type type)
		{
			if (implementation == NullableImplementation.Default || implementation == NullableImplementation.Abstract) {
				ctd.BaseTypes.Add(new CodeTypeReference("INullable"));
				CodeMemberProperty prop = new CodeMemberProperty();
				prop.Name = "IsNull";
				prop.Type = new CodeTypeReference(typeof(bool));
				prop.Attributes = MemberAttributes.Public;
				prop.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));
				ctd.Members.Add(prop);
			}
			if (implementation != NullableImplementation.Abstract) {
				EasyTypeDeclaration newType = new EasyTypeDeclaration("Null" + ctd.Name);
				newType.TypeAttributes = TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed;
				newType.BaseTypes.Add(new CodeTypeReference(ctd.Name));
				cns.Types.Add(newType);
				
				System.Reflection.ConstructorInfo baseCtor = MainClass.GetBaseCtor(type);
				if (baseCtor != null) {
					CodeConstructor ctor = new CodeConstructor();
					ctor.Attributes = MemberAttributes.Private;
					foreach (object o in baseCtor.GetParameters()) {
						ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(null));
					}
					newType.Members.Add(ctor);
				}
				
				CodeMemberField field = new CodeMemberField(newType.Name, "Instance");
				field.Attributes = MemberAttributes.Static | MemberAttributes.Assembly;
				field.InitExpression = new CodeObjectCreateExpression(new CodeTypeReference(newType.Name));
				newType.Members.Add(field);
				
				CodeMemberProperty prop = new CodeMemberProperty();
				prop.Name = "IsNull";
				prop.Type = new CodeTypeReference(typeof(bool));
				prop.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				prop.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));
				newType.Members.Add(prop);
				
				CodeMemberMethod method = new CodeMemberMethod();
				method.Name = "AcceptVisitor";
				method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				method.Parameters.Add(new CodeParameterDeclarationExpression("IAstVisitor", "visitor"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "data"));
				method.ReturnType = new CodeTypeReference(typeof(object));
				method.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
				newType.Members.Add(method);
				
				method = new CodeMemberMethod();
				method.Name = "ToString";
				method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				method.ReturnType = new CodeTypeReference(typeof(string));
				method.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("[" + newType.Name + "]")));
				newType.Members.Add(method);
				
				prop = new CodeMemberProperty();
				prop.Name = "Null";
				prop.Type = new CodeTypeReference(ctd.Name);
				prop.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				if (implementation == NullableImplementation.Shadow) {
					prop.Attributes |= MemberAttributes.New;
				}
				CodeExpression ex = new CodeTypeReferenceExpression(newType.Name);
				ex = new CodePropertyReferenceExpression(ex, "Instance");
				prop.GetStatements.Add(new CodeMethodReturnStatement(ex));
				ctd.Members.Add(prop);
			}
		}
	}
}
