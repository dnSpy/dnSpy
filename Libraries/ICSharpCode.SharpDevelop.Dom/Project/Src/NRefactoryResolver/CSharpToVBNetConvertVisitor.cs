// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.AstBuilder;
using ICSharpCode.NRefactory.Visitors;
using System.Runtime.InteropServices;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	/// <summary>
	/// This class converts C# constructs to their VB.NET equivalents.
	/// </summary>
	public class CSharpToVBNetConvertVisitor : CSharpConstructsConvertVisitor
	{
		NRefactoryResolver resolver;
		ParseInformation parseInformation;
		IProjectContent projectContent;
		public string RootNamespaceToRemove { get; set; }
		public string StartupObjectToMakePublic { get; set; }
		public IList<string> DefaultImportsToRemove { get; set; }
		
		public CSharpToVBNetConvertVisitor(IProjectContent pc, ParseInformation parseInfo)
		{
			this.resolver = new NRefactoryResolver(LanguageProperties.CSharp);
			this.projectContent = pc;
			this.parseInformation = parseInfo;
		}
		
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			base.VisitCompilationUnit(compilationUnit, data);
			ToVBNetConvertVisitor v = new ToVBNetConvertVisitor();
			compilationUnit.AcceptVisitor(v, data);
			return null;
		}
		
		IReturnType ResolveType(TypeReference typeRef)
		{
			return TypeVisitor.CreateReturnType(typeRef, resolver);
		}
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			if (RootNamespaceToRemove != null) {
				if (namespaceDeclaration.Name == RootNamespaceToRemove) {
					// remove namespace declaration
					INode insertAfter = namespaceDeclaration;
					foreach (INode child in namespaceDeclaration.Children) {
						InsertAfterSibling(insertAfter, child);
						insertAfter = child;
					}
					namespaceDeclaration.Children.Clear();
					RemoveCurrentNode();
				} else if (namespaceDeclaration.Name.StartsWith(RootNamespaceToRemove + ".")) {
					namespaceDeclaration.Name = namespaceDeclaration.Name.Substring(RootNamespaceToRemove.Length + 1);
				}
			}
			base.VisitNamespaceDeclaration(namespaceDeclaration, data);
			return null;
		}
		
		public override object VisitUsing(Using @using, object data)
		{
			base.VisitUsing(@using, data);
			if (DefaultImportsToRemove != null && !@using.IsAlias) {
				if (DefaultImportsToRemove.Contains(@using.Name)) {
					RemoveCurrentNode();
				}
			}
			return null;
		}
		
		public override object VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			base.VisitUsingDeclaration(usingDeclaration, data);
			if (usingDeclaration.Usings.Count == 0) {
				RemoveCurrentNode();
			}
			return null;
		}
		
		struct BaseType
		{
			internal readonly TypeReference TypeReference;
			internal readonly IReturnType ReturnType;
			internal readonly IClass UnderlyingClass;
			
			public BaseType(TypeReference typeReference, IReturnType returnType)
			{
				this.TypeReference = typeReference;
				this.ReturnType = returnType;
				this.UnderlyingClass = returnType.GetUnderlyingClass();
			}
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			// Initialize resolver for method:
			if (!methodDeclaration.Body.IsNull) {
				if (resolver.Initialize(parseInformation, methodDeclaration.Body.StartLocation.Y, methodDeclaration.Body.StartLocation.X)) {
					resolver.RunLookupTableVisitor(methodDeclaration);
				}
			}
			IMethod currentMethod = resolver.CallingMember as IMethod;
			CreateInterfaceImplementations(currentMethod, methodDeclaration, methodDeclaration.InterfaceImplementations);
			// Make "Main" public
			if (currentMethod != null && currentMethod.Name == "Main") {
				if (currentMethod.DeclaringType.FullyQualifiedName == StartupObjectToMakePublic) {
					if (currentMethod.IsStatic && currentMethod.IsPrivate) {
						methodDeclaration.Modifier &= ~Modifiers.Private;
						methodDeclaration.Modifier |= Modifiers.Internal;
					}
				}
			}
			if (resolver.CallingClass != null && resolver.CallingClass.BaseType != null) {
				// methods with the same name as a method in a base class must have 'Overloads'
				if ((methodDeclaration.Modifier & (Modifiers.Override | Modifiers.New)) == Modifiers.None) {
					if (resolver.CallingClass.BaseType.GetMethods()
					    .Any(m => string.Equals(m.Name, methodDeclaration.Name, StringComparison.OrdinalIgnoreCase))) {
						methodDeclaration.Modifier |= Modifiers.Overloads;
					}
				}
			}
			return base.VisitMethodDeclaration(methodDeclaration, data);
		}
		
		ClassFinder CreateContext()
		{
			return new ClassFinder(resolver.CallingClass, resolver.CallingMember, resolver.CaretLine, resolver.CaretColumn);
		}
		
		void CreateInterfaceImplementations(IMember currentMember, ParametrizedNode memberDecl, List<InterfaceImplementation> interfaceImplementations)
		{
			if (currentMember != null
			    && (memberDecl.Modifier & Modifiers.Visibility) == Modifiers.None
			    && interfaceImplementations.Count == 1)
			{
				// member is explicitly implementing an interface member
				// to convert explicit interface implementations to VB, make the member private
				// and ensure its name does not collide with another member
				memberDecl.Modifier |= Modifiers.Private;
				memberDecl.Name = interfaceImplementations[0].InterfaceType.Type.Replace('.', '_') + "_" + memberDecl.Name;
			}
			
			if (currentMember != null && currentMember.IsPublic
			    && currentMember.DeclaringType.ClassType != ClassType.Interface)
			{
				// member could be implicitly implementing an interface member,
				// search for interfaces containing the member
				foreach (IReturnType directBaseType in currentMember.DeclaringType.GetCompoundClass().BaseTypes) {
					IClass directBaseClass = directBaseType.GetUnderlyingClass();
					if (directBaseClass != null && directBaseClass.ClassType == ClassType.Interface) {
						// include members inherited from other interfaces in the search:
						foreach (IReturnType baseType in MemberLookupHelper.GetTypeInheritanceTree(directBaseType)) {
							IClass baseClass = baseType.GetUnderlyingClass();
							if (baseClass != null && baseClass.ClassType == ClassType.Interface) {
								IMember similarMember = MemberLookupHelper.FindSimilarMember(baseClass, currentMember);
								// add an interface implementation for similarMember
								// only when similarMember is not explicitly implemented by another member in this class
								if (similarMember != null && !HasExplicitImplementationFor(similarMember, baseType, memberDecl.Parent)) {
									interfaceImplementations.Add(new InterfaceImplementation(
										Refactoring.CodeGenerator.ConvertType(baseType, CreateContext()),
										currentMember.Name));
								}
							}
						}
					}
				}
			}
		}
		
		bool HasExplicitImplementationFor(IMember interfaceMember, IReturnType interfaceReference, INode typeDecl)
		{
			if (typeDecl == null)
				return false;
			foreach (INode node in typeDecl.Children) {
				MemberNode memberNode = node as MemberNode;
				if (memberNode != null && memberNode.InterfaceImplementations.Count > 0) {
					foreach (InterfaceImplementation impl in memberNode.InterfaceImplementations) {
						if (impl.MemberName == interfaceMember.Name
						    && object.Equals(ResolveType(impl.InterfaceType), interfaceReference))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			if (!constructorDeclaration.Body.IsNull) {
				if (resolver.Initialize(parseInformation, constructorDeclaration.Body.StartLocation.Y, constructorDeclaration.Body.StartLocation.X)) {
					resolver.RunLookupTableVisitor(constructorDeclaration);
				}
			}
			return base.VisitConstructorDeclaration(constructorDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			if (resolver.Initialize(parseInformation, propertyDeclaration.BodyStart.Y, propertyDeclaration.BodyStart.X)) {
				resolver.RunLookupTableVisitor(propertyDeclaration);
			}
			IProperty currentProperty = resolver.CallingMember as IProperty;
			CreateInterfaceImplementations(currentProperty, propertyDeclaration, propertyDeclaration.InterfaceImplementations);
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			if (resolver.CompilationUnit == null)
				return base.VisitExpressionStatement(expressionStatement, data);
			
			// Transform event invocations that aren't already transformed by a parent IfStatement to RaiseEvent statement
			InvocationExpression eventInvocation = expressionStatement.Expression as InvocationExpression;
			if (eventInvocation != null && eventInvocation.TargetObject is IdentifierExpression) {
				MemberResolveResult mrr = resolver.ResolveInternal(eventInvocation.TargetObject, ExpressionContext.Default) as MemberResolveResult;
				if (mrr != null && mrr.ResolvedMember is IEvent) {
					ReplaceCurrentNode(new RaiseEventStatement(
						((IdentifierExpression)eventInvocation.TargetObject).Identifier,
						eventInvocation.Arguments));
				}
			}
			base.VisitExpressionStatement(expressionStatement, data);
			
			HandleAssignmentStatement(expressionStatement.Expression as AssignmentExpression);
			return null;
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					ConvertEqualityToReferenceEqualityIfRequired(binaryOperatorExpression);
					break;
				case BinaryOperatorType.Add:
					ConvertArgumentsForStringConcatenationIfRequired(binaryOperatorExpression);
					break;
				case BinaryOperatorType.Divide:
					ConvertDivisionToIntegerDivisionIfRequired(binaryOperatorExpression);
					break;
			}
			return null;
		}
		
		void ConvertEqualityToReferenceEqualityIfRequired(BinaryOperatorExpression binaryOperatorExpression)
		{
			// maybe we have to convert Equality operator to ReferenceEquality
			ResolveResult left = resolver.ResolveInternal(binaryOperatorExpression.Left, ExpressionContext.Default);
			ResolveResult right = resolver.ResolveInternal(binaryOperatorExpression.Right, ExpressionContext.Default);
			if (left != null && right != null && left.ResolvedType != null && right.ResolvedType != null) {
				IClass cLeft = left.ResolvedType.GetUnderlyingClass();
				IClass cRight = right.ResolvedType.GetUnderlyingClass();
				if (cLeft != null && cRight != null) {
					if ((cLeft.ClassType != ClassType.Struct && cLeft.ClassType != ClassType.Enum)
					    || (cRight.ClassType != ClassType.Struct && cRight.ClassType != ClassType.Enum))
					{
						// this is a reference comparison
						if (cLeft.FullyQualifiedName != "System.String") {
							// and it's not a string comparison, so we'll use reference equality
							if (binaryOperatorExpression.Op == BinaryOperatorType.Equality) {
								binaryOperatorExpression.Op = BinaryOperatorType.ReferenceEquality;
							} else {
								binaryOperatorExpression.Op = BinaryOperatorType.ReferenceInequality;
							}
						}
					}
				}
			}
		}
		
		void ConvertArgumentsForStringConcatenationIfRequired(BinaryOperatorExpression binaryOperatorExpression)
		{
			ResolveResult left = resolver.ResolveInternal(binaryOperatorExpression.Left, ExpressionContext.Default);
			ResolveResult right = resolver.ResolveInternal(binaryOperatorExpression.Right, ExpressionContext.Default);
			
			if (left != null && right != null) {
				if (IsString(left.ResolvedType)) {
					binaryOperatorExpression.Op = BinaryOperatorType.Concat;
					if (NeedsExplicitConversionToString(right.ResolvedType)) {
						binaryOperatorExpression.Right = CreateExplicitConversionToString(binaryOperatorExpression.Right);
					}
				} else if (IsString(right.ResolvedType)) {
					binaryOperatorExpression.Op = BinaryOperatorType.Concat;
					if (NeedsExplicitConversionToString(left.ResolvedType)) {
						binaryOperatorExpression.Left = CreateExplicitConversionToString(binaryOperatorExpression.Left);
					}
				}
			}
		}
		
		void ConvertDivisionToIntegerDivisionIfRequired(BinaryOperatorExpression binaryOperatorExpression)
		{
			ResolveResult left = resolver.ResolveInternal(binaryOperatorExpression.Left, ExpressionContext.Default);
			ResolveResult right = resolver.ResolveInternal(binaryOperatorExpression.Right, ExpressionContext.Default);
			
			if (left != null && right != null) {
				if (IsInteger(left.ResolvedType) && IsInteger(right.ResolvedType)) {
					binaryOperatorExpression.Op = BinaryOperatorType.DivideInteger;
				}
			}
		}
		
		bool IsString(IReturnType rt)
		{
			return rt != null && rt.IsDefaultReturnType && rt.FullyQualifiedName == "System.String";
		}
		
		bool IsInteger(IReturnType rt)
		{
			if (rt != null && rt.IsDefaultReturnType) {
				switch (rt.FullyQualifiedName) {
					case "System.Byte":
					case "System.SByte":
					case "System.Int16":
					case "System.UInt16":
					case "System.Int32":
					case "System.UInt32":
					case "System.Int64":
					case "System.UInt64":
						return true;
				}
			}
			return false;
		}
		
		bool IsFloatingPoint(IReturnType rt)
		{
			if (rt != null && rt.IsDefaultReturnType) {
				switch (rt.FullyQualifiedName) {
					case "System.Single":
					case "System.Double":
					case "System.Decimal":
						return true;
				}
			}
			return false;
		}
		
		bool NeedsExplicitConversionToString(IReturnType rt)
		{
			if (rt != null) {
				if (rt.IsDefaultReturnType) {
					if (rt.FullyQualifiedName == "System.Object"
					    || !TypeReference.PrimitiveTypesVBReverse.ContainsKey(rt.FullyQualifiedName))
					{
						// object and non-primitive types need explicit conversion
						return true;
					} else {
						// primitive types except object don't need explicit conversion
						return false;
					}
				} else {
					return true;
				}
			}
			return false;
		}
		
		Expression CreateExplicitConversionToString(Expression expr)
		{
			return new IdentifierExpression("Convert").Call("ToString", expr);
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			base.VisitIdentifierExpression(identifierExpression, data);
			if (resolver.CompilationUnit == null)
				return null;
			
			InvocationExpression parentIE = identifierExpression.Parent as InvocationExpression;
			if (!(identifierExpression.Parent is AddressOfExpression)
			    && (parentIE == null || parentIE.TargetObject != identifierExpression))
			{
				ResolveResult rr = resolver.ResolveInternal(identifierExpression, ExpressionContext.Default);
				if (IsMethodGroup(rr)) {
					ReplaceCurrentNode(new AddressOfExpression(identifierExpression));
				}
			}
			return null;
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression fieldReferenceExpression, object data)
		{
			base.VisitMemberReferenceExpression(fieldReferenceExpression, data);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			InvocationExpression parentIE = fieldReferenceExpression.Parent as InvocationExpression;
			if (!(fieldReferenceExpression.Parent is AddressOfExpression)
			    && (parentIE == null || parentIE.TargetObject != fieldReferenceExpression))
			{
				ResolveResult rr = resolver.ResolveInternal(fieldReferenceExpression, ExpressionContext.Default);
				if (IsMethodGroup(rr)) {
					ReplaceCurrentNode(new AddressOfExpression(fieldReferenceExpression));
				}
			}
			
			return null;
		}
		
		static bool IsMethodGroup(ResolveResult rr)
		{
			MethodGroupResolveResult mgrr = rr as MethodGroupResolveResult;
			if (mgrr != null) {
				return mgrr.Methods.Any(g=>g.Count > 0);
			}
			return false;
		}
		
		void HandleAssignmentStatement(AssignmentExpression assignmentExpression)
		{
			if (resolver.CompilationUnit == null || assignmentExpression == null)
				return;
			
			if (assignmentExpression.Op == AssignmentOperatorType.Add || assignmentExpression.Op == AssignmentOperatorType.Subtract) {
				ResolveResult rr = resolver.ResolveInternal(assignmentExpression.Left, ExpressionContext.Default);
				if (rr is MemberResolveResult && (rr as MemberResolveResult).ResolvedMember is IEvent) {
					if (assignmentExpression.Op == AssignmentOperatorType.Add) {
						ReplaceCurrentNode(new AddHandlerStatement(assignmentExpression.Left, assignmentExpression.Right));
					} else {
						ReplaceCurrentNode(new RemoveHandlerStatement(assignmentExpression.Left, assignmentExpression.Right));
					}
				} else if (rr != null && rr.ResolvedType != null) {
					IClass c = rr.ResolvedType.GetUnderlyingClass();
					if (c != null && c.ClassType == ClassType.Delegate) {
						InvocationExpression invocation =
							new IdentifierExpression("Delegate").Call(
								assignmentExpression.Op == AssignmentOperatorType.Add ? "Combine" : "Remove",
								assignmentExpression.Left);
						invocation.Arguments.Add(assignmentExpression.Right);
						
						assignmentExpression.Op = AssignmentOperatorType.Assign;
						assignmentExpression.Right = new CastExpression(
							Refactoring.CodeGenerator.ConvertType(rr.ResolvedType, CreateContext()),
							invocation, CastType.Cast);
					}
				}
			}
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			base.VisitCastExpression(castExpression, data);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			IReturnType targetType = ResolveType(castExpression.CastTo);
			IClass targetClass = targetType != null ? targetType.GetUnderlyingClass() : null;
			if (castExpression.CastType != CastType.TryCast) {
				if (targetClass != null && (targetClass.ClassType == ClassType.Struct || targetClass.ClassType == ClassType.Enum)) {
					// cast to value type is a conversion
					castExpression.CastType = CastType.Conversion;
					if (IsInteger(targetType)) {
						ResolveResult sourceRR = resolver.ResolveInternal(castExpression.Expression, ExpressionContext.Default);
						IReturnType sourceType = sourceRR != null ? sourceRR.ResolvedType : null;
						if (IsFloatingPoint(sourceType)) {
							// casts from float to int in C# truncate, but VB rounds
							// we'll have to introduce a call to Math.Truncate
							castExpression.Expression = ExpressionBuilder.Identifier("Math").Call("Truncate", castExpression.Expression);
						} else if (sourceType != null && sourceType.FullyQualifiedName == "System.Char") {
							// casts from char to int are valid in C#, but need to use AscW in VB
							castExpression.Expression = ExpressionBuilder.Identifier("AscW").Call(castExpression.Expression);
							if (targetType != null && targetType.FullyQualifiedName == "System.Int32") {
								// AscW already returns int, so skip the cast
								ReplaceCurrentNode(castExpression.Expression);
								return null;
							}
						}
					}
				}
				if (targetClass != null && targetClass.FullyQualifiedName == "System.Char") {
					// C# cast to char is done using ChrW function
					ResolveResult sourceRR = resolver.ResolveInternal(castExpression.Expression, ExpressionContext.Default);
					IReturnType sourceType = sourceRR != null ? sourceRR.ResolvedType : null;
					if (IsInteger(sourceType)) {
						ReplaceCurrentNode(new IdentifierExpression("ChrW").Call(castExpression.Expression));
					}
				}
			}
			return null;
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			base.VisitUnaryOperatorExpression(unaryOperatorExpression, data);
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Dereference:
					ReplaceCurrentNode(unaryOperatorExpression.Expression.Member("Target"));
					break;
				case UnaryOperatorType.AddressOf:
					ResolveResult rr = resolver.ResolveInternal(unaryOperatorExpression.Expression, ExpressionContext.Default);
					if (rr != null && rr.ResolvedType != null) {
						TypeReference targetType = Refactoring.CodeGenerator.ConvertType(rr.ResolvedType, CreateContext());
						TypeReference pointerType = new TypeReference("Pointer", new List<TypeReference> { targetType });
						ReplaceCurrentNode(pointerType.New(unaryOperatorExpression.Expression));
					}
					break;
			}
			return null;
		}
		
		public override object VisitTypeReference(TypeReference typeReference, object data)
		{
			while (typeReference.PointerNestingLevel > 0) {
				TypeReference tr = new TypeReference(typeReference.Type) {
					IsKeyword = typeReference.IsKeyword,
					IsGlobal = typeReference.IsGlobal,
				};
				tr.GenericTypes.AddRange(typeReference.GenericTypes);
				
				typeReference = new TypeReference("Pointer") {
					StartLocation = typeReference.StartLocation,
					EndLocation = typeReference.EndLocation,
					PointerNestingLevel = typeReference.PointerNestingLevel - 1,
					GenericTypes = { tr },
					RankSpecifier = typeReference.RankSpecifier
				};
			}
			ReplaceCurrentNode(typeReference);
			return base.VisitTypeReference(typeReference, data);
		}
		
		public override object VisitUnsafeStatement(UnsafeStatement unsafeStatement, object data)
		{
			base.VisitUnsafeStatement(unsafeStatement, data);
			ReplaceCurrentNode(unsafeStatement.Block);
			return null;
		}
	}
}
