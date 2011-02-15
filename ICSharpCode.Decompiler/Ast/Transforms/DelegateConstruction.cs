// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace Decompiler.Transforms
{
	/// <summary>
	/// Converts "new Action(obj, ldftn(func))" into "new Action(obj.func)".
	/// </summary>
	public class DelegateConstruction : DepthFirstAstVisitor<object, object>
	{
		internal sealed class Annotation
		{
			/// <summary>
			/// ldftn or ldvirtftn?
			/// </summary>
			public readonly bool IsVirtual;
			
			/// <summary>
			/// The method being decompiled.
			/// </summary>
			public readonly TypeDefinition ContainingType;
			
			public Annotation(bool isVirtual, TypeDefinition containingType)
			{
				this.IsVirtual = isVirtual;
				this.ContainingType = containingType;
			}
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (objectCreateExpression.Arguments.Count() == 2) {
				Expression obj = objectCreateExpression.Arguments.First();
				Expression func = objectCreateExpression.Arguments.Last();
				Annotation annotation = func.Annotation<Annotation>();
				if (annotation != null) {
					IdentifierExpression methodIdent = (IdentifierExpression)((InvocationExpression)func).Arguments.Single();
					MethodReference method = methodIdent.Annotation<MethodReference>();
					if (method != null) {
						// Perform the transformation:
						obj.Remove();
						methodIdent.Remove();
						if (!annotation.IsVirtual && obj is ThisReferenceExpression) {
							// maybe it's getting the pointer of a base method?
							if (method.DeclaringType != annotation.ContainingType) {
								obj = new BaseReferenceExpression();
							}
						}
						if (!annotation.IsVirtual && obj is NullReferenceExpression && !method.HasThis) {
							// We're loading a static method.
							// However it is possible to load extension methods with an instance, so we compare the number of arguments:
							bool isExtensionMethod = false;
							TypeReference delegateType = objectCreateExpression.Type.Annotation<TypeReference>();
							if (delegateType != null) {
								TypeDefinition delegateTypeDef = delegateType.Resolve();
								if (delegateTypeDef != null) {
									MethodDefinition invokeMethod = delegateTypeDef.Methods.FirstOrDefault(m => m.Name == "Invoke");
									if (invokeMethod != null) {
										isExtensionMethod = (invokeMethod.Parameters.Count + 1 == method.Parameters.Count);
									}
								}
							}
							if (!isExtensionMethod) {
								obj = new TypeReferenceExpression { Type = AstBuilder.ConvertType(method.DeclaringType) };
							}
						}
						// now transform the identifier into a member reference
						MemberReferenceExpression mre = new MemberReferenceExpression {
							Target = obj,
							MemberName = methodIdent.Identifier,
							TypeArguments = methodIdent.TypeArguments
						};
						mre.AddAnnotation(method);
						objectCreateExpression.Arguments = new [] { mre };
						return null;
					}
				}
			}
			return base.VisitObjectCreateExpression(objectCreateExpression, data);
		}
	}
}
