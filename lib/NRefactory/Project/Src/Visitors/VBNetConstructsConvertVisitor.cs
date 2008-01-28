// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Reflection;

using ICSharpCode.NRefactory.Ast;
using Attribute = ICSharpCode.NRefactory.Ast.Attribute;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Converts special VB constructs to use more general AST classes.
	/// </summary>
	public class VBNetConstructsConvertVisitor : ConvertVisitorBase
	{
		// The following conversions are implemented:
		//   MyBase.New() and MyClass.New() calls inside the constructor are converted to :base() and :this()
		//   Add Public Modifier to inner types, methods, properties and fields in structures
		//   Override Finalize => Destructor
		//   IIF(cond, true, false) => ConditionalExpression
		//   Built-in methods => Prefix with class name
		//   Function A() \n A = SomeValue \n End Function -> convert to return statement
		//   Array creation => add 1 to upper bound to get array length
		//   Comparison with empty string literal -> string.IsNullOrEmpty
		//   Add default value to local variable declarations without initializer
		
		/// <summary>
		/// Specifies whether the "Add default value to local variable declarations without initializer"
		/// operation is executed by this convert visitor.
		/// </summary>
		public bool AddDefaultValueInitializerToLocalVariableDeclarations = true;
		
		Dictionary<string, string> usings;
		List<UsingDeclaration> addedUsings;
		TypeDeclaration currentTypeDeclaration;
		
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			usings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			addedUsings = new List<UsingDeclaration>();
			base.VisitCompilationUnit(compilationUnit, data);
			int i;
			for (i = 0; i < compilationUnit.Children.Count; i++) {
				if (!(compilationUnit.Children[i] is UsingDeclaration))
					break;
			}
			foreach (UsingDeclaration decl in addedUsings) {
				decl.Parent = compilationUnit;
				compilationUnit.Children.Insert(i++, decl);
			}
			usings = null;
			addedUsings = null;
			return null;
		}
		
		public override object VisitUsing(Using @using, object data)
		{
			if (usings != null && !@using.IsAlias) {
				usings[@using.Name] = @using.Name;
			}
			return base.VisitUsing(@using, data);
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			// fix default visibility of inner classes
			if (currentTypeDeclaration != null && (typeDeclaration.Modifier & Modifiers.Visibility) == 0)
				typeDeclaration.Modifier |= Modifiers.Public;
			
			TypeDeclaration oldTypeDeclaration = currentTypeDeclaration;
			currentTypeDeclaration = typeDeclaration;
			base.VisitTypeDeclaration(typeDeclaration, data);
			currentTypeDeclaration = oldTypeDeclaration;
			return null;
		}
		
		public override object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			// fix default visibility of inner classes
			if (currentTypeDeclaration != null && (delegateDeclaration.Modifier & Modifiers.Visibility) == 0)
				delegateDeclaration.Modifier |= Modifiers.Public;
			
			return base.VisitDelegateDeclaration(delegateDeclaration, data);
		}
		
		bool IsClassType(ClassType c)
		{
			if (currentTypeDeclaration == null) return false;
			return currentTypeDeclaration.Type == c;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			// make constructor public if visiblity is not set (unless constructor is static)
			if ((constructorDeclaration.Modifier & (Modifiers.Visibility | Modifiers.Static)) == 0)
				constructorDeclaration.Modifier |= Modifiers.Public;
			
			// MyBase.New() and MyClass.New() calls inside the constructor are converted to :base() and :this()
			BlockStatement body = constructorDeclaration.Body;
			if (body != null && body.Children.Count > 0) {
				ExpressionStatement se = body.Children[0] as ExpressionStatement;
				if (se != null) {
					InvocationExpression ie = se.Expression as InvocationExpression;
					if (ie != null) {
						MemberReferenceExpression fre = ie.TargetObject as MemberReferenceExpression;
						if (fre != null && "New".Equals(fre.MemberName, StringComparison.InvariantCultureIgnoreCase)) {
							if (fre.TargetObject is BaseReferenceExpression || fre.TargetObject is ClassReferenceExpression || fre.TargetObject is ThisReferenceExpression) {
								body.Children.RemoveAt(0);
								ConstructorInitializer ci = new ConstructorInitializer();
								ci.Arguments = ie.Arguments;
								if (fre.TargetObject is BaseReferenceExpression)
									ci.ConstructorInitializerType = ConstructorInitializerType.Base;
								else
									ci.ConstructorInitializerType = ConstructorInitializerType.This;
								constructorDeclaration.ConstructorInitializer = ci;
							}
						}
					}
				}
			}
			return base.VisitConstructorDeclaration(constructorDeclaration, data);
		}
		
		public override object VisitDeclareDeclaration(DeclareDeclaration declareDeclaration, object data)
		{
			if (usings != null && !usings.ContainsKey("System.Runtime.InteropServices")) {
				UsingDeclaration @using = new UsingDeclaration("System.Runtime.InteropServices");
				addedUsings.Add(@using);
				base.VisitUsingDeclaration(@using, data);
			}
			
			MethodDeclaration method = new MethodDeclaration {
				Name = declareDeclaration.Name,
				Modifier = declareDeclaration.Modifier,
				TypeReference = declareDeclaration.TypeReference,
				Parameters = declareDeclaration.Parameters,
				Attributes = declareDeclaration.Attributes
			};
			
			if ((method.Modifier & Modifiers.Visibility) == 0)
				method.Modifier |= Modifiers.Public;
			method.Modifier |= Modifiers.Extern | Modifiers.Static;
			
			if (method.TypeReference.IsNull) {
				method.TypeReference = new TypeReference("System.Void");
			}
			
			Attribute att = new Attribute("DllImport", null, null);
			att.PositionalArguments.Add(CreateStringLiteral(declareDeclaration.Library));
			if (declareDeclaration.Alias.Length > 0) {
				att.NamedArguments.Add(new NamedArgumentExpression("EntryPoint", CreateStringLiteral(declareDeclaration.Alias)));
			}
			switch (declareDeclaration.Charset) {
				case CharsetModifier.Auto:
					att.NamedArguments.Add(new NamedArgumentExpression("CharSet",
					                                                   new MemberReferenceExpression(new IdentifierExpression("CharSet"),
					                                                                                 "Auto")));
					break;
				case CharsetModifier.Unicode:
					att.NamedArguments.Add(new NamedArgumentExpression("CharSet",
					                                                   new MemberReferenceExpression(new IdentifierExpression("CharSet"),
					                                                                                 "Unicode")));
					break;
				default:
					att.NamedArguments.Add(new NamedArgumentExpression("CharSet",
					                                                   new MemberReferenceExpression(new IdentifierExpression("CharSet"),
					                                                                                 "Ansi")));
					break;
			}
			att.NamedArguments.Add(new NamedArgumentExpression("SetLastError", new PrimitiveExpression(true, true.ToString())));
			att.NamedArguments.Add(new NamedArgumentExpression("ExactSpelling", new PrimitiveExpression(true, true.ToString())));
			method.Attributes.Add(new AttributeSection { Attributes = { att } });
			ReplaceCurrentNode(method);
			return base.VisitMethodDeclaration(method, data);
		}
		
		static PrimitiveExpression CreateStringLiteral(string text)
		{
			return new PrimitiveExpression(text, text);
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			if (!IsClassType(ClassType.Interface) && (methodDeclaration.Modifier & Modifiers.Visibility) == 0)
				methodDeclaration.Modifier |= Modifiers.Public;
			
			if ("Finalize".Equals(methodDeclaration.Name, StringComparison.InvariantCultureIgnoreCase)
			    && methodDeclaration.Parameters.Count == 0
			    && methodDeclaration.Modifier == (Modifiers.Protected | Modifiers.Override)
			    && methodDeclaration.Body.Children.Count == 1)
			{
				TryCatchStatement tcs = methodDeclaration.Body.Children[0] as TryCatchStatement;
				if (tcs != null
				    && tcs.StatementBlock is BlockStatement
				    && tcs.CatchClauses.Count == 0
				    && tcs.FinallyBlock is BlockStatement
				    && tcs.FinallyBlock.Children.Count == 1)
				{
					ExpressionStatement se = tcs.FinallyBlock.Children[0] as ExpressionStatement;
					if (se != null) {
						InvocationExpression ie = se.Expression as InvocationExpression;
						if (ie != null
						    && ie.Arguments.Count == 0
						    && ie.TargetObject is MemberReferenceExpression
						    && (ie.TargetObject as MemberReferenceExpression).TargetObject is BaseReferenceExpression
						    && "Finalize".Equals((ie.TargetObject as MemberReferenceExpression).MemberName, StringComparison.InvariantCultureIgnoreCase))
						{
							DestructorDeclaration des = new DestructorDeclaration("Destructor", Modifiers.None, methodDeclaration.Attributes);
							ReplaceCurrentNode(des);
							des.Body = (BlockStatement)tcs.StatementBlock;
							return base.VisitDestructorDeclaration(des, data);
						}
					}
				}
			}
			
			if ((methodDeclaration.Modifier & (Modifiers.Static | Modifiers.Extern)) == Modifiers.Static
			    && methodDeclaration.Body.Children.Count == 0)
			{
				foreach (AttributeSection sec in methodDeclaration.Attributes) {
					foreach (Attribute att in sec.Attributes) {
						if ("DllImport".Equals(att.Name, StringComparison.InvariantCultureIgnoreCase)) {
							methodDeclaration.Modifier |= Modifiers.Extern;
							methodDeclaration.Body = null;
						}
					}
				}
			}
			
			if (methodDeclaration.TypeReference.SystemType != "System.Void" && methodDeclaration.Body.Children.Count > 0) {
				if (IsAssignmentTo(methodDeclaration.Body.Children[methodDeclaration.Body.Children.Count - 1], methodDeclaration.Name))
				{
					ReturnStatement rs = new ReturnStatement(GetAssignmentFromStatement(methodDeclaration.Body.Children[methodDeclaration.Body.Children.Count - 1]).Right);
					methodDeclaration.Body.Children.RemoveAt(methodDeclaration.Body.Children.Count - 1);
					methodDeclaration.Body.AddChild(rs);
				} else {
					ReturnStatementForFunctionAssignment visitor = new ReturnStatementForFunctionAssignment(methodDeclaration.Name);
					methodDeclaration.Body.AcceptVisitor(visitor, null);
					if (visitor.replacementCount > 0) {
						Expression init;
						init = GetDefaultValueForType(methodDeclaration.TypeReference);
						methodDeclaration.Body.Children.Insert(0, new LocalVariableDeclaration(new VariableDeclaration(FunctionReturnValueName, init, methodDeclaration.TypeReference)));
						methodDeclaration.Body.Children[0].Parent = methodDeclaration.Body;
						methodDeclaration.Body.AddChild(new ReturnStatement(new IdentifierExpression(FunctionReturnValueName)));
					}
				}
			}
			
			return base.VisitMethodDeclaration(methodDeclaration, data);
		}
		
		public const string FunctionReturnValueName = "functionReturnValue";
		
		static AssignmentExpression GetAssignmentFromStatement(INode statement)
		{
			ExpressionStatement se = statement as ExpressionStatement;
			if (se == null) return null;
			return se.Expression as AssignmentExpression;
		}
		
		static bool IsAssignmentTo(INode statement, string varName)
		{
			AssignmentExpression ass = GetAssignmentFromStatement(statement);
			if (ass == null) return false;
			IdentifierExpression ident = ass.Left as IdentifierExpression;
			if (ident == null) return false;
			return ident.Identifier.Equals(varName, StringComparison.InvariantCultureIgnoreCase);
		}
		
		#region Create return statement for assignment to function name
		class ReturnStatementForFunctionAssignment : AbstractAstTransformer
		{
			string functionName;
			internal int replacementCount = 0;
			
			public ReturnStatementForFunctionAssignment(string functionName)
			{
				this.functionName = functionName;
			}
			
			public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
			{
				if (identifierExpression.Identifier.Equals(functionName, StringComparison.InvariantCultureIgnoreCase)) {
					if (!(identifierExpression.Parent is AddressOfExpression) && !(identifierExpression.Parent is InvocationExpression)) {
						identifierExpression.Identifier = FunctionReturnValueName;
						replacementCount++;
					}
				}
				return base.VisitIdentifierExpression(identifierExpression, data);
			}
		}
		#endregion
		
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			fieldDeclaration.Modifier &= ~Modifiers.Dim; // remove "Dim" flag
			if (IsClassType(ClassType.Struct)) {
				if ((fieldDeclaration.Modifier & Modifiers.Visibility) == 0)
					fieldDeclaration.Modifier |= Modifiers.Public;
			}
			return base.VisitFieldDeclaration(fieldDeclaration, data);
		}
		
		public override object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			if (!IsClassType(ClassType.Interface) && (eventDeclaration.Modifier & Modifiers.Visibility) == 0)
				eventDeclaration.Modifier |= Modifiers.Public;
			
			return base.VisitEventDeclaration(eventDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			if (!IsClassType(ClassType.Interface) && (propertyDeclaration.Modifier & Modifiers.Visibility) == 0)
				propertyDeclaration.Modifier |= Modifiers.Public;
			
			if (propertyDeclaration.HasSetRegion) {
				string from = "Value";
				if (propertyDeclaration.SetRegion.Parameters.Count > 0) {
					ParameterDeclarationExpression p = propertyDeclaration.SetRegion.Parameters[0];
					from = p.ParameterName;
					p.ParameterName = "Value";
				}
				propertyDeclaration.SetRegion.AcceptVisitor(new RenameIdentifierVisitor(from, "value", StringComparer.InvariantCultureIgnoreCase), null);
			}
			
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}
		
		static volatile Dictionary<string, Expression> constantTable;
		static volatile Dictionary<string, Expression> methodTable;
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1802:UseLiteralsWhereAppropriate")]
		public static readonly string VBAssemblyName = "Microsoft.VisualBasic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
		
		static Dictionary<string, Expression> CreateDictionary(params string[] classNames)
		{
			Dictionary<string, Expression> d = new Dictionary<string, Expression>(StringComparer.InvariantCultureIgnoreCase);
			Assembly asm = Assembly.Load(VBAssemblyName);
			foreach (string className in classNames) {
				Type type = asm.GetType("Microsoft.VisualBasic." + className);
				Expression expr = new IdentifierExpression(className);
				foreach (MemberInfo member in type.GetMembers()) {
					if (member.DeclaringType == type) { // only direct members
						d[member.Name] = expr;
					}
				}
			}
			return d;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			if (constantTable == null) {
				constantTable = CreateDictionary("Constants");
			}
			Expression expr;
			if (constantTable.TryGetValue(identifierExpression.Identifier, out expr)) {
				MemberReferenceExpression fre = new MemberReferenceExpression(expr, identifierExpression.Identifier);
				ReplaceCurrentNode(fre);
				return base.VisitMemberReferenceExpression(fre, data);
			}
			return base.VisitIdentifierExpression(identifierExpression, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			IdentifierExpression ident = invocationExpression.TargetObject as IdentifierExpression;
			if (ident != null) {
				if ("IIF".Equals(ident.Identifier, StringComparison.InvariantCultureIgnoreCase)
				    && invocationExpression.Arguments.Count == 3)
				{
					ConditionalExpression ce = new ConditionalExpression(invocationExpression.Arguments[0],
					                                                     invocationExpression.Arguments[1],
					                                                     invocationExpression.Arguments[2]);
					ReplaceCurrentNode(new ParenthesizedExpression(ce));
					return base.VisitConditionalExpression(ce, data);
				}
				if ("IsNothing".Equals(ident.Identifier, StringComparison.InvariantCultureIgnoreCase)
				    && invocationExpression.Arguments.Count == 1)
				{
					BinaryOperatorExpression boe = new BinaryOperatorExpression(invocationExpression.Arguments[0],
					                                                            BinaryOperatorType.ReferenceEquality,
					                                                            new PrimitiveExpression(null, "null"));
					ReplaceCurrentNode(new ParenthesizedExpression(boe));
					return base.VisitBinaryOperatorExpression(boe, data);
				}
				if (methodTable == null) {
					methodTable = CreateDictionary("Conversion", "FileSystem", "Financial", "Information",
					                               "Interaction", "Strings", "VBMath");
				}
				Expression expr;
				if (methodTable.TryGetValue(ident.Identifier, out expr)) {
					MemberReferenceExpression fre = new MemberReferenceExpression(expr, ident.Identifier);
					invocationExpression.TargetObject = fre;
				}
			}
			return base.VisitInvocationExpression(invocationExpression, data);
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			base.VisitUnaryOperatorExpression(unaryOperatorExpression, data);
			if (unaryOperatorExpression.Op == UnaryOperatorType.Not) {
				if (unaryOperatorExpression.Expression is BinaryOperatorExpression) {
					unaryOperatorExpression.Expression = new ParenthesizedExpression(unaryOperatorExpression.Expression);
				}
				ParenthesizedExpression pe = unaryOperatorExpression.Expression as ParenthesizedExpression;
				if (pe != null) {
					BinaryOperatorExpression boe = pe.Expression as BinaryOperatorExpression;
					if (boe != null && boe.Op == BinaryOperatorType.ReferenceEquality) {
						boe.Op = BinaryOperatorType.ReferenceInequality;
						ReplaceCurrentNode(pe);
					}
				}
			}
			return null;
		}
		
		public override object VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			LocalVariableDeclaration lvd = usingStatement.ResourceAcquisition as LocalVariableDeclaration;
			if (lvd != null && lvd.Variables.Count > 1) {
				usingStatement.ResourceAcquisition = new LocalVariableDeclaration(lvd.Variables[0]);
				for (int i = 1; i < lvd.Variables.Count; i++) {
					UsingStatement n = new UsingStatement(new LocalVariableDeclaration(lvd.Variables[i]),
					                                      usingStatement.EmbeddedStatement);
					usingStatement.EmbeddedStatement = new BlockStatement();
					usingStatement.EmbeddedStatement.AddChild(n);
					usingStatement = n;
				}
			}
			return base.VisitUsingStatement(usingStatement, data);
		}
		
		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			for (int i = 0; i < arrayCreateExpression.Arguments.Count; i++) {
				arrayCreateExpression.Arguments[i] = Expression.AddInteger(arrayCreateExpression.Arguments[i], 1);
			}
			if (arrayCreateExpression.ArrayInitializer.CreateExpressions.Count == 0) {
				arrayCreateExpression.ArrayInitializer = null;
			}
			return base.VisitArrayCreateExpression(arrayCreateExpression, data);
		}
		
		bool IsEmptyStringLiteral(Expression expression)
		{
			PrimitiveExpression pe = expression as PrimitiveExpression;
			if (pe != null) {
				return (pe.Value as string) == "";
			} else {
				return false;
			}
		}
		
		Expression CallStringIsNullOrEmpty(Expression stringVariable)
		{
			List<Expression> arguments = new List<Expression>();
			arguments.Add(stringVariable);
			return new InvocationExpression(
				new MemberReferenceExpression(new TypeReferenceExpression("System.String"), "IsNullOrEmpty"),
				arguments);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
			if (IsEmptyStringLiteral(binaryOperatorExpression.Right)) {
				if (binaryOperatorExpression.Op == BinaryOperatorType.Equality) {
					ReplaceCurrentNode(CallStringIsNullOrEmpty(binaryOperatorExpression.Left));
				} else if (binaryOperatorExpression.Op == BinaryOperatorType.InEquality) {
					ReplaceCurrentNode(new UnaryOperatorExpression(CallStringIsNullOrEmpty(binaryOperatorExpression.Left),
					                                               UnaryOperatorType.Not));
				}
			} else if (IsEmptyStringLiteral(binaryOperatorExpression.Left)) {
				if (binaryOperatorExpression.Op == BinaryOperatorType.Equality) {
					ReplaceCurrentNode(CallStringIsNullOrEmpty(binaryOperatorExpression.Right));
				} else if (binaryOperatorExpression.Op == BinaryOperatorType.InEquality) {
					ReplaceCurrentNode(new UnaryOperatorExpression(CallStringIsNullOrEmpty(binaryOperatorExpression.Right),
					                                               UnaryOperatorType.Not));
				}
			}
			return null;
		}
		
		public override object VisitLocalVariableDeclaration(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			if (AddDefaultValueInitializerToLocalVariableDeclarations) {
				for (int i = 0; i < localVariableDeclaration.Variables.Count; i++) {
					VariableDeclaration decl = localVariableDeclaration.Variables[i];
					if (decl.FixedArrayInitialization.IsNull && decl.Initializer.IsNull) {
						TypeReference type = localVariableDeclaration.GetTypeForVariable(i);
						decl.Initializer = GetDefaultValueForType(type);
					}
				}
			}
			return base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
		}
		
		Expression GetDefaultValueForType(TypeReference type)
		{
			if (type != null && !type.IsArrayType) {
				switch (type.SystemType) {
					case "System.SByte":
					case "System.Byte":
					case "System.Int16":
					case "System.UInt16":
					case "System.Int32":
					case "System.UInt32":
					case "System.Int64":
					case "System.UInt64":
					case "System.Single":
					case "System.Double":
						return new PrimitiveExpression(0, "0");
					case "System.Char":
						return new PrimitiveExpression('\0', "'\\0'");
					case "System.Object":
					case "System.String":
						return new PrimitiveExpression(null, "null");
					case "System.Boolean":
						return new PrimitiveExpression(false, "false");
					default:
						return new DefaultValueExpression(type);
				}
			} else {
				return new PrimitiveExpression(null, "null");
			}
		}
	}
}
