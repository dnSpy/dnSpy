// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.AstBuilder;
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
		//   XML literals -> XLinq
		
		/// <summary>
		/// Specifies whether the "Add default value to local variable declarations without initializer"
		/// operation is executed by this convert visitor.
		/// </summary>
		public bool AddDefaultValueInitializerToLocalVariableDeclarations = true;
		
		public bool OptionInfer { get; set; }
		
		public bool OptionStrict { get; set; }
		
		Dictionary<string, string> usings;
		List<UsingDeclaration> addedUsings;
		TypeDeclaration currentTypeDeclaration;
		int withStatementCount;
		
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			usings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			addedUsings = new List<UsingDeclaration>();
			withStatementCount = 0;
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
				method.TypeReference = new TypeReference("System.Void", true);
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
			
			if (methodDeclaration.TypeReference.Type != "System.Void" && methodDeclaration.Body.Children.Count > 0) {
				ReplaceAllFunctionAssignments(methodDeclaration.Body, methodDeclaration.Name, methodDeclaration.TypeReference);
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
			internal List<IdentifierExpression> expressionsToReplace = new List<IdentifierExpression>();
			internal bool hasExit = false;
			
			public ReturnStatementForFunctionAssignment(string functionName)
			{
				this.functionName = functionName;
			}
			
			public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
			{
				if (identifierExpression.Identifier.Equals(functionName, StringComparison.InvariantCultureIgnoreCase)) {
					if (!(identifierExpression.Parent is AddressOfExpression) && !(identifierExpression.Parent is InvocationExpression)) {
						expressionsToReplace.Add(identifierExpression);
					}
				}
				return base.VisitIdentifierExpression(identifierExpression, data);
			}
			
			public override object VisitExitStatement(ExitStatement exitStatement, object data)
			{
				if (exitStatement.ExitType == ExitType.Function || exitStatement.ExitType == ExitType.Property) {
					hasExit = true;
					IdentifierExpression expr = new IdentifierExpression("tmp");
					expressionsToReplace.Add(expr);
					var newNode = new ReturnStatement(expr);
					ReplaceCurrentNode(newNode);
					return base.VisitReturnStatement(newNode, data);
				}
				return base.VisitExitStatement(exitStatement, data);
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
			
			if (propertyDeclaration.HasGetRegion && propertyDeclaration.GetRegion.Block.Children.Count > 0) {
				BlockStatement block = propertyDeclaration.GetRegion.Block;
				ReplaceAllFunctionAssignments(block, propertyDeclaration.Name, propertyDeclaration.TypeReference);
			}
			
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}
		
		void ReplaceAllFunctionAssignments(BlockStatement block, string functionName, TypeReference typeReference)
		{
			ReturnStatementForFunctionAssignment visitor = new ReturnStatementForFunctionAssignment(functionName);
			block.AcceptVisitor(visitor, null);
			if (visitor.expressionsToReplace.Count == 1 && !visitor.hasExit && IsAssignmentTo(block.Children.Last(), functionName)) {
				Expression returnValue = GetAssignmentFromStatement(block.Children.Last()).Right;
				block.Children.RemoveAt(block.Children.Count - 1);
				block.Return(returnValue);
			} else {
				if (visitor.expressionsToReplace.Count > 0) {
					foreach (var expr in visitor.expressionsToReplace) {
						expr.Identifier = FunctionReturnValueName;
					}
					Expression init;
					init = ExpressionBuilder.CreateDefaultValueForType(typeReference);
					block.Children.Insert(0, new LocalVariableDeclaration(new VariableDeclaration(FunctionReturnValueName, init, typeReference)));
					block.Children[0].Parent = block;
					block.Return(new IdentifierExpression(FunctionReturnValueName));
				}
			}
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
				new MemberReferenceExpression(new TypeReferenceExpression(new TypeReference("System.String", true)), "IsNullOrEmpty"),
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
			for (int i = 0; i < localVariableDeclaration.Variables.Count; i++) {
				VariableDeclaration decl = localVariableDeclaration.Variables[i];
				if (AddDefaultValueInitializerToLocalVariableDeclarations &&
				    (localVariableDeclaration.Modifier & Modifiers.Static) == 0 &&
				    decl.FixedArrayInitialization.IsNull && decl.Initializer.IsNull) {
					TypeReference type = localVariableDeclaration.GetTypeForVariable(i);
					decl.Initializer = ExpressionBuilder.CreateDefaultValueForType(type);
				}
				if (decl.TypeReference.IsNull) {
					if (OptionInfer && !(decl.Initializer is PrimitiveExpression && (decl.Initializer as PrimitiveExpression).Value == null))
						decl.TypeReference = new TypeReference("var", true);
					else if (OptionStrict)
						decl.TypeReference = new TypeReference("System.Object", true);
					else
						decl.TypeReference = new TypeReference("dynamic", true);
				}
			}
			return base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
		}
		
		public override object VisitOptionDeclaration(OptionDeclaration optionDeclaration, object data)
		{
			if (optionDeclaration.OptionType == OptionType.Infer)
				OptionInfer = optionDeclaration.OptionValue;
			if (optionDeclaration.OptionType == OptionType.Strict)
				OptionStrict = optionDeclaration.OptionValue;
			return base.VisitOptionDeclaration(optionDeclaration, data);
		}
		
		public override object VisitXmlContentExpression(XmlContentExpression xmlContentExpression, object data)
		{

			Expression newNode = ConvertXmlContentExpression(xmlContentExpression);
			
			if (newNode == null)
				return base.VisitXmlContentExpression(xmlContentExpression, data);
			
			ReplaceCurrentNode(newNode);
			
			if (newNode is ObjectCreateExpression)
				return base.VisitObjectCreateExpression((ObjectCreateExpression)newNode, data);
			else if (newNode is PrimitiveExpression)
				return base.VisitPrimitiveExpression((PrimitiveExpression)newNode , data);
			
			return null;
		}

		Expression ConvertXmlContentExpression(XmlContentExpression xmlContentExpression)
		{
			Expression newNode = null;
			switch (xmlContentExpression.Type) {
				case XmlContentType.Comment:
					newNode = new ObjectCreateExpression(new TypeReference("XComment"), Expressions(xmlContentExpression.Content));
					break;
				case XmlContentType.Text:
					newNode = new PrimitiveExpression(ConvertEntities(xmlContentExpression.Content));
					break;
				case XmlContentType.CData:
					newNode = new ObjectCreateExpression(new TypeReference("XCData"), Expressions(xmlContentExpression.Content));
					break;
				case XmlContentType.ProcessingInstruction:
					string content = xmlContentExpression.Content.Trim();
					if (content.StartsWith("xml", StringComparison.OrdinalIgnoreCase)) {
						XDeclaration decl;
						try {
							decl = XDocument.Parse("<?" + content + "?><Dummy />").Declaration;
						} catch (XmlException) {
							decl = new XDeclaration(null, null, null);
						}
						newNode = new ObjectCreateExpression(new TypeReference("XDeclaration"), Expressions(decl.Version, decl.Encoding, decl.Standalone));
					} else {
						string target = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
						string piData = content.IndexOf(' ') > -1 ? content.Substring(content.IndexOf(' ')) : "";
						newNode = new ObjectCreateExpression(new TypeReference("XProcessingInstruction"), Expressions(target, piData));
					}
					break;
				default:
					throw new Exception("Invalid value for XmlContentType");
			}
			return newNode;
		}
		
		string ConvertEntities(string content)
		{
			try {
				return XElement.Parse("<Dummy>" + content + "</Dummy>").Value;
			} catch (XmlException) {
				return content;
			}
		}
		
		public override object VisitXmlDocumentExpression(XmlDocumentExpression xmlDocumentExpression, object data)
		{
			var newNode = new ObjectCreateExpression(new TypeReference("XDocument"), null);
			
			foreach (XmlExpression expr in xmlDocumentExpression.Expressions)
				newNode.Parameters.Add(ConvertXmlExpression(expr));
			
			ReplaceCurrentNode(newNode);
			
			return base.VisitObjectCreateExpression(newNode, data);
		}
		
		public override object VisitXmlElementExpression(XmlElementExpression xmlElementExpression, object data)
		{
			ObjectCreateExpression newNode = ConvertXmlElementExpression(xmlElementExpression);
			
			ReplaceCurrentNode(newNode);
			
			return base.VisitObjectCreateExpression(newNode, data);
		}

		ObjectCreateExpression ConvertXmlElementExpression(XmlElementExpression xmlElementExpression)
		{
			var newNode = new ObjectCreateExpression(new TypeReference("XElement"), xmlElementExpression.NameIsExpression ? new List<Expression> { xmlElementExpression.NameExpression } : Expressions(xmlElementExpression.XmlName));
			
			foreach (XmlExpression attr in xmlElementExpression.Attributes) {
				if (attr is XmlAttributeExpression) {
					var a = attr as XmlAttributeExpression;
					newNode.Parameters.Add(new ObjectCreateExpression(new TypeReference("XAttribute"), new List<Expression> {
					                                                  	new PrimitiveExpression(a.Name),
					                                                  	a.IsLiteralValue ? new PrimitiveExpression(ConvertEntities(a.LiteralValue)) : a.ExpressionValue
					                                                  }));
				} else if (attr is XmlEmbeddedExpression) {
					newNode.Parameters.Add((attr as XmlEmbeddedExpression).InlineVBExpression);
				}
			}
			
			foreach (XmlExpression expr in xmlElementExpression.Children) {
				XmlContentExpression c = expr as XmlContentExpression;
				// skip whitespace text
				if (!(expr is XmlContentExpression && c.Type == XmlContentType.Text && string.IsNullOrWhiteSpace(c.Content)))
					newNode.Parameters.Add(ConvertXmlExpression(expr));
			}
			
			return newNode;
		}
		
		Expression ConvertXmlExpression(XmlExpression expr)
		{
			if (expr is XmlElementExpression)
				return ConvertXmlElementExpression(expr as XmlElementExpression);
			else if (expr is XmlContentExpression)
				return ConvertXmlContentExpression(expr as XmlContentExpression);
			else if (expr is XmlEmbeddedExpression)
				return (expr as XmlEmbeddedExpression).InlineVBExpression;
			
			throw new Exception();
		}
		
		List<Expression> Expressions(params string[] exprs)
		{
			return new List<Expression>(exprs.Select(expr => new PrimitiveExpression(expr)));
		}
		
		public override object VisitWithStatement(WithStatement withStatement, object data)
		{
			withStatementCount++;
			string varName = "_with" + withStatementCount;
			WithConvertVisitor converter = new WithConvertVisitor(varName);
			
			LocalVariableDeclaration withVariable = new LocalVariableDeclaration(new VariableDeclaration(varName, withStatement.Expression, new TypeReference("var", true)));
			
			withStatement.Body.AcceptVisitor(converter, null);
			
			base.VisitWithStatement(withStatement, data);
			
			var statements = withStatement.Body.Children;
			
			statements.Insert(0, withVariable);
			
			withVariable.Parent = withStatement.Body;
			
			statements.Reverse();
			
			foreach (var stmt in statements) {
				InsertAfterSibling(withStatement, stmt);
			}
			
			RemoveCurrentNode();
			
			return null;
		}
		
		class WithConvertVisitor : AbstractAstTransformer
		{
			string withAccessor;
			
			public WithConvertVisitor(string withAccessor)
			{
				this.withAccessor = withAccessor;
			}
			
			public override object VisitWithStatement(WithStatement withStatement, object data)
			{
				// skip any nested with statement
				var block = withStatement.Body;
				withStatement.Body = BlockStatement.Null;
				base.VisitWithStatement(withStatement, data);
				withStatement.Body = block;
				return null;
			}
			
			public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
			{
				if (memberReferenceExpression.TargetObject.IsNull) {
					IdentifierExpression id = new IdentifierExpression(withAccessor);
					memberReferenceExpression.TargetObject = id;
					id.Parent = memberReferenceExpression;
				}
				
				return base.VisitMemberReferenceExpression(memberReferenceExpression, data);
			}
			
			public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
			{
				if (binaryOperatorExpression.Left.IsNull && binaryOperatorExpression.Op == BinaryOperatorType.DictionaryAccess) {
					IdentifierExpression id = new IdentifierExpression(withAccessor);
					binaryOperatorExpression.Left = id;
					id.Parent = binaryOperatorExpression;
				}
				
				return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
			}
			
			public override object VisitXmlMemberAccessExpression(XmlMemberAccessExpression xmlMemberAccessExpression, object data)
			{
				if (xmlMemberAccessExpression.TargetObject.IsNull) {
					IdentifierExpression id = new IdentifierExpression(withAccessor);
					xmlMemberAccessExpression.TargetObject = id;
					id.Parent = xmlMemberAccessExpression;
				}
				
				return base.VisitXmlMemberAccessExpression(xmlMemberAccessExpression, data);
			}
		}
	}
}
