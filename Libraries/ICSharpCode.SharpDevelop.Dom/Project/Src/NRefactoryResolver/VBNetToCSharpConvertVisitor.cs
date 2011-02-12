// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.AstBuilder;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	/// <summary>
	/// This class converts C# constructs to their VB.NET equivalents.
	/// </summary>
	public class VBNetToCSharpConvertVisitor : VBNetConstructsConvertVisitor
	{
		// Fixes identifier casing
		// Adds using statements for the default usings
		// Convert "ReDim" statement
		// Convert "WithEvents" fields/"Handles" clauses
		// Insert InitializeComponents() call into default constructor
		
		public string NamespacePrefixToAdd { get; set; }
		
		protected readonly IProjectContent projectContent;
		protected readonly NRefactoryResolver resolver;
		protected readonly ParseInformation parseInformation;
		
		public VBNetToCSharpConvertVisitor(IProjectContent pc, ParseInformation parseInfo)
		{
			this.resolver = new NRefactoryResolver(LanguageProperties.VBNet);
			this.projectContent = pc;
			this.parseInformation = parseInfo;
		}
		
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			base.VisitCompilationUnit(compilationUnit, data);
			if (!string.IsNullOrEmpty(NamespacePrefixToAdd)) {
				for (int i = 0; i < compilationUnit.Children.Count; i++) {
					NamespaceDeclaration ns = compilationUnit.Children[i] as NamespaceDeclaration;
					if (ns != null) {
						ns.Name = NamespacePrefixToAdd + "." + ns.Name;
					}
					if (compilationUnit.Children[i] is TypeDeclaration || compilationUnit.Children[i] is DelegateDeclaration) {
						ns = new NamespaceDeclaration(NamespacePrefixToAdd);
						ns.AddChild(compilationUnit.Children[i]);
						compilationUnit.Children[i] = ns;
					}
				}
			}
			
			ToCSharpConvertVisitor v = new ToCSharpConvertVisitor();
			compilationUnit.AcceptVisitor(v, data);
			if (projectContent != null && projectContent.DefaultImports != null) {
				int index = 0;
				foreach (string u in projectContent.DefaultImports.Usings) {
					compilationUnit.Children.Insert(index++, new UsingDeclaration(u));
				}
			}
			return null;
		}
		
		public override object VisitUsing(Using @using, object data)
		{
			base.VisitUsing(@using, data);
			if (projectContent != null && projectContent.DefaultImports != null) {
				if (!@using.IsAlias) {
					// remove using if it is already part of the project-wide imports
					foreach (string defaultImport in projectContent.DefaultImports.Usings) {
						if (resolver.IsSameName(defaultImport, @using.Name)) {
							RemoveCurrentNode();
							break;
						}
					}
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
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			resolver.Initialize(parseInformation, typeDeclaration.BodyStartLocation.Line, typeDeclaration.BodyStartLocation.Column);
			
			if (resolver.CallingClass != null) {
				// add Partial modifier to all parts of the class
				IClass callingClass = resolver.CallingClass.GetCompoundClass();
				if (callingClass.IsPartial) {
					typeDeclaration.Modifier |= Modifiers.Partial;
				}
				// determine if the type contains handles clauses referring to the current type
				bool containsClassHandlesClauses = false;
				bool hasConstructors = false;
				foreach (IMethod method in callingClass.Methods) {
					// do not count compiler-generated constructors
					if (method.IsSynthetic) continue;
					
					hasConstructors |= method.IsConstructor;
					foreach (string handles in method.HandlesClauses) {
						containsClassHandlesClauses |= !handles.Contains(".");
					}
				}
				// ensure the type has at least one constructor to which the AddHandlerStatements can be added
				CompoundClass compoundClass = callingClass as CompoundClass;
				if (!hasConstructors) {
					// add constructor only to one part
					if (compoundClass == null || compoundClass.Parts[0] == resolver.CallingClass) {
						if (containsClassHandlesClauses || RequiresConstructor(callingClass)) {
							AddDefaultConstructor(callingClass, typeDeclaration);
						}
					}
				}
			}
			
			base.VisitTypeDeclaration(typeDeclaration, data);
			return null;
		}
		
		/// <summary>
		/// Gets if the converter should add a default constructor to the current class if the
		/// class does not have any constructors.
		/// </summary>
		protected virtual bool RequiresConstructor(IClass currentClass)
		{
			// the VB compiler automatically adds the InitializeComponents() call to the
			// default constructor, so the converter has to an explicit constructor
			// and place the call there
			return IsAutomaticallyCallingInitializeComponent(currentClass);
		}
		
		bool IsAutomaticallyCallingInitializeComponent(IClass currentClass)
		{
			if (currentClass != null) {
				if (currentClass.SearchMember("InitializeComponent", LanguageProperties.VBNet) is IMethod) {
					foreach (IAttribute at in currentClass.Attributes) {
						if (at.AttributeType.FullyQualifiedName == "Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute") {
							return true;
						}
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Adds a default constructor to the type.
		/// </summary>
		protected virtual ConstructorDeclaration AddDefaultConstructor(IClass currentClass, TypeDeclaration typeDeclaration)
		{
			ConstructorDeclaration cd = new ConstructorDeclaration(typeDeclaration.Name, Modifiers.Public, null, null);
			cd.Body = new BlockStatement();
			// location is required to make Resolve() work in the constructor
			cd.StartLocation = cd.Body.StartLocation = cd.EndLocation = cd.Body.EndLocation = typeDeclaration.BodyStartLocation;
			typeDeclaration.AddChild(cd);
			if (IsAutomaticallyCallingInitializeComponent(currentClass)) {
				// the VB compiler automatically adds the InitializeComponents() call to the
				// default constructor, so the converter has to add the call when creating an explicit
				// constructor
				cd.Body.AddStatement(new IdentifierExpression("InitializeComponent").Call());
			}
			return cd;
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			// Initialize resolver for method:
			if (!methodDeclaration.Body.IsNull) {
				if (resolver.Initialize(parseInformation, methodDeclaration.Body.StartLocation.Line, methodDeclaration.Body.StartLocation.Column)) {
					resolver.RunLookupTableVisitor(methodDeclaration);
				}
			}
			
			methodDeclaration.HandlesClause.Clear();
			
			return base.VisitMethodDeclaration(methodDeclaration, data);
		}
		
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			resolver.Initialize(parseInformation, fieldDeclaration.StartLocation.Line, fieldDeclaration.StartLocation.Column);
			
			base.VisitFieldDeclaration(fieldDeclaration, data);
			
			if ((fieldDeclaration.Modifier & Modifiers.WithEvents) == Modifiers.WithEvents) {
				TransformWithEventsField(fieldDeclaration);
				if (fieldDeclaration.Fields.Count == 0) {
					RemoveCurrentNode();
				}
			}
			
			return null;
		}
		
		void TransformWithEventsField(FieldDeclaration fieldDeclaration)
		{
			if (resolver.CallingClass == null)
				return;
			
			INode insertAfter = fieldDeclaration;
			
			for (int i = 0; i < fieldDeclaration.Fields.Count;) {
				VariableDeclaration field = fieldDeclaration.Fields[i];
				
				IdentifierExpression backingFieldNameExpression = null;
				PropertyDeclaration createdProperty = null;
				foreach (IMethod m in resolver.CallingClass.GetCompoundClass().Methods) {
					foreach (string handlesClause in m.HandlesClauses) {
						int pos = handlesClause.IndexOf('.');
						if (pos > 0) {
							string fieldName = handlesClause.Substring(0, pos);
							string eventName = handlesClause.Substring(pos + 1);
							if (resolver.IsSameName(fieldName, field.Name)) {
								if (createdProperty == null) {
									FieldDeclaration backingField = new FieldDeclaration(null);
									backingField.Fields.Add(new VariableDeclaration(
										"withEventsField_" + field.Name, field.Initializer, fieldDeclaration.GetTypeForField(i)));
									backingField.Modifier = Modifiers.Private;
									InsertAfterSibling(insertAfter, backingField);
									createdProperty = new PropertyDeclaration(fieldDeclaration.Modifier, null, field.Name, null);
									createdProperty.TypeReference = fieldDeclaration.GetTypeForField(i);
									createdProperty.StartLocation = fieldDeclaration.StartLocation;
									createdProperty.EndLocation = fieldDeclaration.EndLocation;
									
									backingFieldNameExpression = new IdentifierExpression(backingField.Fields[0].Name);
									
									createdProperty.GetRegion = new PropertyGetRegion(new BlockStatement(), null);
									createdProperty.GetRegion.Block.AddChild(new ReturnStatement(
										backingFieldNameExpression));
									
									Expression backingFieldNotNullTest = new BinaryOperatorExpression(
										backingFieldNameExpression,
										BinaryOperatorType.InEquality,
										new PrimitiveExpression(null, "null"));
									
									createdProperty.SetRegion = new PropertySetRegion(new BlockStatement(), null);
									createdProperty.SetRegion.Block.AddChild(new IfElseStatement(
										backingFieldNotNullTest, new BlockStatement()
									));
									createdProperty.SetRegion.Block.AddChild(new ExpressionStatement(
										new AssignmentExpression(
											backingFieldNameExpression,
											AssignmentOperatorType.Assign,
											new IdentifierExpression("value"))));
									createdProperty.SetRegion.Block.AddChild(new IfElseStatement(
										backingFieldNotNullTest, new BlockStatement()
									));
									InsertAfterSibling(backingField, createdProperty);
									insertAfter = createdProperty;
								}
								
								// insert code to remove the event handler
								IfElseStatement ies = (IfElseStatement)createdProperty.SetRegion.Block.Children[0];
								ies.TrueStatement[0].AddChild(new RemoveHandlerStatement(
									new MemberReferenceExpression(backingFieldNameExpression, eventName),
									new AddressOfExpression(new IdentifierExpression(m.Name))));
								
								// insert code to add the event handler
								ies = (IfElseStatement)createdProperty.SetRegion.Block.Children[2];
								ies.TrueStatement[0].AddChild(new AddHandlerStatement(
									new MemberReferenceExpression(backingFieldNameExpression, eventName),
									new AddressOfExpression(new IdentifierExpression(m.Name))));
							}
						}
					}
				}
				
				if (createdProperty != null) {
					// field replaced with property
					fieldDeclaration.Fields.RemoveAt(i);
				} else {
					i++;
				}
			}
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			if (!constructorDeclaration.Body.IsNull) {
				if (resolver.Initialize(parseInformation, constructorDeclaration.Body.StartLocation.Line, constructorDeclaration.Body.StartLocation.Column)) {
					resolver.RunLookupTableVisitor(constructorDeclaration);
				}
			}
			base.VisitConstructorDeclaration(constructorDeclaration, data);
			if (resolver.CallingClass != null) {
				if (constructorDeclaration.ConstructorInitializer.IsNull
				    || constructorDeclaration.ConstructorInitializer.ConstructorInitializerType != ConstructorInitializerType.This)
				{
					AddClassEventHandlersToConstructor(constructorDeclaration);
				}
			}
			return null;
		}
		
		void AddClassEventHandlersToConstructor(ConstructorDeclaration constructorDeclaration)
		{
			foreach (IMethod method in resolver.CallingClass.GetCompoundClass().Methods) {
				foreach (string handles in method.HandlesClauses) {
					if (!handles.Contains(".")) {
						AddHandlerStatement ahs = new AddHandlerStatement(
							new IdentifierExpression(handles),
							new AddressOfExpression(new IdentifierExpression(method.Name))
						);
						constructorDeclaration.Body.Children.Insert(0, ahs);
						ahs.Parent = constructorDeclaration.Body;
					}
				}
			}
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			if (resolver.Initialize(parseInformation, propertyDeclaration.BodyStart.Line, propertyDeclaration.BodyStart.Column)) {
				resolver.RunLookupTableVisitor(propertyDeclaration);
			}
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}

		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			base.VisitIdentifierExpression(identifierExpression, data);
			if (resolver.CompilationUnit == null)
				return null;
			
			ResolveResult rr = resolver.ResolveInternal(identifierExpression, ExpressionContext.Default);
			string ident = GetIdentifierFromResult(rr);
			if (ident != null) {
				identifierExpression.Identifier = ident;
			}
			
			if (ReplaceWithInvocation(identifierExpression, rr)) {}
			else if (FullyQualifyModuleMemberReference(identifierExpression, rr)) {}
			else if (FullyQualifyNamespaceReference(identifierExpression, rr)) {}
			
			return null;
		}

		public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			base.VisitMemberReferenceExpression(memberReferenceExpression, data);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			ResolveResult rr = Resolve(memberReferenceExpression);
			string ident = GetIdentifierFromResult(rr);
			if (ident != null) {
				memberReferenceExpression.MemberName = ident;
			}
			if (ReplaceWithInvocation(memberReferenceExpression, rr)) {}
			else if (FullyQualifyModuleMemberReference(memberReferenceExpression, rr)) {}
			
			return rr;
		}
		
		protected bool IsReferenceToInstanceMember(ResolveResult rr)
		{
			MemberResolveResult memberRR = rr as MemberResolveResult;
			if (memberRR != null)
				return memberRR.ResolvedMember.IsStatic == false;
			MethodGroupResolveResult methodRR = rr as MethodGroupResolveResult;
			if (methodRR != null && methodRR.ContainingType != null) {
				foreach (IMethod m in methodRR.ContainingType.GetMethods()) {
					if (resolver.IsSameName(m.Name, methodRR.Name)) {
						return !m.IsStatic;
					}
				}
			}
			return false;
		}
		
		bool ReplaceWithInvocation(Expression expression, ResolveResult rr)
		{
			// replace with invocation if rr is a method
			// and were not taking the address and it's not already being invoked
			MethodGroupResolveResult mgrr = rr as MethodGroupResolveResult;
			if (mgrr != null
			    && mgrr.Methods.Any(g=>g.Count>0)
			    && !(expression.Parent is AddressOfExpression)
			    && !(NRefactoryResolver.IsInvoked(expression)))
			{
				InvocationExpression ie = new InvocationExpression(expression);
				ReplaceCurrentNode(ie);
				return true;
			}
			return false;
		}
		
		IReturnType GetContainingTypeOfStaticMember(ResolveResult rr)
		{
			MethodGroupResolveResult methodRR = rr as MethodGroupResolveResult;
			if (methodRR != null) {
				return methodRR.ContainingType;
			}
			MemberResolveResult memberRR = rr as MemberResolveResult;
			if (memberRR != null && memberRR.ResolvedMember.IsStatic) {
				return memberRR.ResolvedMember.DeclaringTypeReference;
			}
			return null;
		}
		
		bool FullyQualifyModuleMemberReference(IdentifierExpression ident, ResolveResult rr)
		{
			IReturnType containingType = GetContainingTypeOfStaticMember(rr);
			if (containingType == null)
				return false;
			if (resolver.CallingClass != null) {
				if (resolver.CallingClass.IsTypeInInheritanceTree(containingType.GetUnderlyingClass()))
					return false;
			}
			ReplaceCurrentNode(new MemberReferenceExpression(
				new TypeReferenceExpression(ConvertType(containingType)),
				ident.Identifier
			));
			return true;
		}
		
		bool FullyQualifyNamespaceReference(IdentifierExpression ident, ResolveResult rr)
		{
			NamespaceResolveResult nrr = rr as NamespaceResolveResult;
			if (nrr == null)
				return false;
			if (nrr.Name.IndexOf('.') >= 0) {
				// simple identifier points to complex namespace
				ReplaceCurrentNode(MakeFieldReferenceExpression(nrr.Name));
			} else {
				ident.Identifier = nrr.Name;
			}
			return true;
		}
		
		TypeReference ConvertType(IReturnType type)
		{
			return Refactoring.CodeGenerator.ConvertType(type, CreateContext());
		}
		
		bool FullyQualifyModuleMemberReference(MemberReferenceExpression mre, ResolveResult rr)
		{
			IReturnType containingType = GetContainingTypeOfStaticMember(rr);
			if (containingType == null)
				return false;
			
			ResolveResult targetRR = resolver.ResolveInternal(mre.TargetObject, ExpressionContext.Default);
			if (targetRR is NamespaceResolveResult) {
				mre.TargetObject = new TypeReferenceExpression(ConvertType(containingType));
				return true;
			}
			return false;
		}

		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			base.VisitInvocationExpression(invocationExpression, data);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			if (!(invocationExpression.Parent is ReDimStatement))
			{
				ProcessInvocationExpression(invocationExpression);
			}
			
			return null;
		}
		
		protected ResolveResult Resolve(Expression expression)
		{
			if (resolver.CompilationUnit == null) {
				return null;
			} else {
				return resolver.ResolveInternal(expression, ExpressionContext.Default);
			}
		}
		
		void ProcessInvocationExpression(InvocationExpression invocationExpression)
		{
			MemberResolveResult rr = Resolve(invocationExpression) as MemberResolveResult;
			if (rr != null) {
				IProperty p = rr.ResolvedMember as IProperty;
				if (p != null && invocationExpression.Arguments.Count > 0) {
					// col(i) -> col[i] or col.Items(i) -> col[i] ?
					Expression targetObject = invocationExpression.TargetObject;
					MemberReferenceExpression targetObjectFre = targetObject as MemberReferenceExpression;
					if (p.IsIndexer && targetObjectFre != null) {
						MemberResolveResult rr2 = Resolve(targetObjectFre) as MemberResolveResult;
						if (rr2 != null && rr2.ResolvedMember.FullyQualifiedName == rr.ResolvedMember.FullyQualifiedName) {
							// remove ".Items"
							targetObject = targetObjectFre.TargetObject;
						}
					}
					ReplaceCurrentNode(new IndexerExpression(targetObject, invocationExpression.Arguments));
				}
				IMethod m = rr.ResolvedMember as IMethod;
				if (m != null && invocationExpression.Arguments.Count == m.Parameters.Count) {
					for (int i = 0; i < m.Parameters.Count; i++) {
						if (m.Parameters[i].IsOut) {
							invocationExpression.Arguments[i] = new DirectionExpression(
								FieldDirection.Out, invocationExpression.Arguments[i]);
						} else if (m.Parameters[i].IsRef) {
							invocationExpression.Arguments[i] = new DirectionExpression(
								FieldDirection.Ref, invocationExpression.Arguments[i]);
						}
					}
				}
			}
		}

		ClassFinder CreateContext()
		{
			return new ClassFinder(resolver.CallingClass, resolver.CallingMember, resolver.CaretLine, resolver.CaretColumn);
		}

		public override object VisitReDimStatement(ReDimStatement reDimStatement, object data)
		{
			base.VisitReDimStatement(reDimStatement, data);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			if (reDimStatement.ReDimClauses.Count != 1)
				return null;
			
			if (reDimStatement.IsPreserve) {
				if (reDimStatement.ReDimClauses[0].Arguments.Count > 1) {
					// multidimensional Redim Preserve
					// replace with:
					// MyArray = (int[,])Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(MyArray, new int[dim1+1, dim2+1]);
					
					ResolveResult rr = Resolve(reDimStatement.ReDimClauses[0].TargetObject);
					if (rr != null && rr.ResolvedType != null && rr.ResolvedType.IsArrayReturnType) {
						ArrayCreateExpression ace = new ArrayCreateExpression(ConvertType(rr.ResolvedType));
						foreach (Expression arg in reDimStatement.ReDimClauses[0].Arguments) {
							ace.Arguments.Add(Expression.AddInteger(arg, 1));
						}
						
						ReplaceCurrentNode(new ExpressionStatement(
							new AssignmentExpression(
								reDimStatement.ReDimClauses[0].TargetObject,
								AssignmentOperatorType.Assign,
								new CastExpression(
									ace.CreateType,
									new InvocationExpression(
										MakeFieldReferenceExpression("Microsoft.VisualBasic.CompilerServices.Utils.CopyArray"),
										new List<Expression> {
											reDimStatement.ReDimClauses[0].TargetObject,
											ace
										}
									),
									CastType.Cast
								)
							)));
					}
				}
			} else {
				// replace with array create expression
				
				ResolveResult rr = Resolve(reDimStatement.ReDimClauses[0].TargetObject);
				if (rr != null && rr.ResolvedType != null && rr.ResolvedType.IsArrayReturnType) {
					ArrayCreateExpression ace = new ArrayCreateExpression(ConvertType(rr.ResolvedType));
					foreach (Expression arg in reDimStatement.ReDimClauses[0].Arguments) {
						ace.Arguments.Add(Expression.AddInteger(arg, 1));
					}
					
					ReplaceCurrentNode(new ExpressionStatement(
						new AssignmentExpression(reDimStatement.ReDimClauses[0].TargetObject, AssignmentOperatorType.Assign, ace)));
				}
			}
			return null;
		}

		protected Expression MakeFieldReferenceExpression(string name)
		{
			Expression e = null;
			foreach (string n in name.Split('.')) {
				if (e == null)
					e = new IdentifierExpression(n);
				else
					e = new MemberReferenceExpression(e, n);
			}
			return e;
		}

		public override object VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			base.VisitDefaultValueExpression(defaultValueExpression, data);
			
			IReturnType type = FixTypeReferenceCasing(defaultValueExpression.TypeReference, defaultValueExpression.StartLocation);
			// the VBNetConstructsConvertVisitor will initialize local variables to
			// default(TypeReference).
			// MyType m = null; looks nicer than MyType m = default(MyType))
			// so we replace default(ReferenceType) with null
			if (type != null && type.IsReferenceType == true) {
				ReplaceCurrentNode(new PrimitiveExpression(null, "null"));
			}
			return null;
		}
		
		public override object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			FixTypeReferenceCasing(variableDeclaration.TypeReference, variableDeclaration.StartLocation);
			return base.VisitVariableDeclaration(variableDeclaration, data);
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			FixTypeReferenceCasing(parameterDeclarationExpression.TypeReference, parameterDeclarationExpression.StartLocation);
			return base.VisitParameterDeclarationExpression(parameterDeclarationExpression, data);
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			FixTypeReferenceCasing(objectCreateExpression.CreateType, objectCreateExpression.StartLocation);
			return base.VisitObjectCreateExpression(objectCreateExpression, data);
		}
		
		IReturnType FixTypeReferenceCasing(TypeReference tr, Location loc)
		{
			if (resolver.CompilationUnit == null) return null;
			if (tr.IsNull) return null;
			var searchTypeResult = resolver.SearchType(tr.Type, tr.GenericTypes.Count, loc);
			IReturnType rt = searchTypeResult.Result;
			if (rt != null) {
				IClass c = rt.GetUnderlyingClass();
				if (c != null) {
					if (string.Equals(tr.Type, c.Name, StringComparison.OrdinalIgnoreCase)) {
						tr.Type = c.Name;
					} else if (string.Equals(tr.Type, c.FullyQualifiedName, StringComparison.OrdinalIgnoreCase)) {
						tr.Type = c.FullyQualifiedName;
					} else if (searchTypeResult.UsedUsing != null && !searchTypeResult.UsedUsing.HasAliases) {
						tr.Type = c.FullyQualifiedName;
					}
				}
			}
			foreach (TypeReference arg in tr.GenericTypes) {
				FixTypeReferenceCasing(arg, loc);
			}
			return rt;
		}

		string GetIdentifierFromResult(ResolveResult rr)
		{
			LocalResolveResult lrr = rr as LocalResolveResult;
			if (lrr != null)
				return lrr.VariableName;
			MemberResolveResult mrr = rr as MemberResolveResult;
			if (mrr != null)
				return mrr.ResolvedMember.Name;
			MethodGroupResolveResult mtrr = rr as MethodGroupResolveResult;
			if (mtrr != null)
				return mtrr.Name;
			TypeResolveResult trr = rr as TypeResolveResult;
			if (trr != null && trr.ResolvedClass != null)
				return trr.ResolvedClass.Name;
			return null;
		}
		
		public override object VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			base.VisitForeachStatement(foreachStatement, data);
			
			FixTypeReferenceCasing(foreachStatement.TypeReference, foreachStatement.StartLocation);
			
			if (resolver.CompilationUnit == null)
				return null;
			
			if (foreachStatement.TypeReference.IsNull) {
				ResolveResult rr = resolver.ResolveIdentifier(foreachStatement.VariableName, foreachStatement.StartLocation, ExpressionContext.Default);
				if (rr != null && rr.ResolvedType != null) {
					BlockStatement blockStatement = foreachStatement.EmbeddedStatement as BlockStatement;
					if (blockStatement == null) {
						blockStatement = new BlockStatement();
						blockStatement.AddChild(foreachStatement.EmbeddedStatement);
						foreachStatement.EmbeddedStatement = blockStatement;
					}
					
					string newVariableName = foreachStatement.VariableName + "_loopVariable";
					
					ExpressionStatement st = new ExpressionStatement(
						new AssignmentExpression(
							new IdentifierExpression(foreachStatement.VariableName),
							AssignmentOperatorType.Assign,
							new IdentifierExpression(newVariableName)));
					blockStatement.Children.Insert(0, st);
					st.Parent = blockStatement;
					
					foreachStatement.VariableName = newVariableName;
					foreachStatement.TypeReference = ConvertType(rr.ResolvedType);
				}
			}
			return null;
		}
	}
}
