// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Visitors
{
	public class CodeDomVisitor : AbstractAstVisitor
	{
		Stack<CodeNamespace>  namespaceDeclarations = new Stack<CodeNamespace>();
		Stack<CodeTypeDeclaration> typeDeclarations = new Stack<CodeTypeDeclaration>();
		Stack<CodeStatementCollection> codeStack    = new Stack<CodeStatementCollection>();
		List<CodeVariableDeclarationStatement> variables = new List<CodeVariableDeclarationStatement>();
		List<CodeParameterDeclarationExpression> parameters = new List<CodeParameterDeclarationExpression>();
		Stack<Breakable> breakableStack = new Stack<Breakable>();
		
		TypeDeclaration currentTypeDeclaration = null;
		
		IEnvironmentInformationProvider environmentInformationProvider = DummyEnvironmentInformationProvider.Instance;

		// track break and continue statements
		class Breakable
		{
			public static int NextId = 0;

			public int Id = 0;
			public bool IsBreak = false;
			public bool IsContinue = false;
			public bool AllowContinue = true;

			public Breakable()
			{
				Id = ++NextId;
			}

			public Breakable(bool allowContinue)
				: this()
			{
				AllowContinue = allowContinue;
			}
		}

		public IEnvironmentInformationProvider EnvironmentInformationProvider {
			get { return environmentInformationProvider; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				environmentInformationProvider = value;
			}
		}
		
		// dummy collection used to swallow statements
		CodeStatementCollection NullStmtCollection = new CodeStatementCollection();
		
		public CodeCompileUnit codeCompileUnit   = new CodeCompileUnit();

		// RG
		//
		// Initialise Scope Variables for Current Method
		void InitMethodScope()
		{
			usingId = 0;
			foreachId = 0;
			switchId = 0;
			doId = 0;
			Breakable.NextId = 0;
			variables.Clear();
			parameters.Clear();
		}
		
		string GetDotNetNameFromTypeReference(TypeReference type)
		{
			string name;
			InnerClassTypeReference ictr = type as InnerClassTypeReference;
			if (ictr != null) {
				name = GetDotNetNameFromTypeReference(ictr.BaseType) + "+" + ictr.Type;
			} else {
				name = type.Type;
			}
			if (type.GenericTypes.Count != 0)
				name = name + "`" + type.GenericTypes.Count.ToString();
			return name;
		}
		
		CodeTypeReference ConvType(TypeReference type)
		{
			if (type == null) {
				throw new ArgumentNullException("type");
			}
			
			CodeTypeReference t = new CodeTypeReference(GetDotNetNameFromTypeReference(type));
			InnerClassTypeReference ictr = type as InnerClassTypeReference;
			if (ictr != null) {
				type = ictr.CombineToNormalTypeReference();
			}
			foreach (TypeReference gt in type.GenericTypes) {
				t.TypeArguments.Add(ConvType(gt));
			}
			if (type.IsArrayType) {
				for (int i = type.RankSpecifier.Length - 1; i >= 0; --i)
				{
					t = new CodeTypeReference(t, type.RankSpecifier[i] + 1);
				}
			}
			
			return t;
		}
		
		void AddStmt(CodeStatement stmt)
		{
			if (codeStack.Count == 0)
				return;
			CodeStatementCollection stmtCollection = codeStack.Peek();
			if (stmtCollection != null) {
				stmtCollection.Add(stmt);
			}
		}
		
		static MemberAttributes ConvMemberAttributes(Modifiers modifier)
		{
			MemberAttributes attr = (MemberAttributes)0;
			
			if ((modifier & Modifiers.Abstract) != 0)
				attr |=  MemberAttributes.Abstract;
			if ((modifier & Modifiers.Const) != 0)
				attr |=  MemberAttributes.Const;
			if ((modifier & Modifiers.Sealed) != 0)
				attr |=  MemberAttributes.Final;
			if ((modifier & Modifiers.New) != 0)
				attr |=  MemberAttributes.New;
			if ((modifier & Modifiers.Virtual) != 0)
				attr |=  MemberAttributes.Overloaded;
			if ((modifier & Modifiers.Override) != 0)
				attr |=  MemberAttributes.Override;
			if ((modifier & Modifiers.Static) != 0)
				attr |=  MemberAttributes.Static;
			
			if ((modifier & Modifiers.Public) != 0)
				attr |=  MemberAttributes.Public;
			else if ((modifier & Modifiers.Internal) != 0 && (modifier & Modifiers.Protected) != 0)
				attr |=  MemberAttributes.FamilyOrAssembly;
			else if ((modifier & Modifiers.Internal) != 0)
				attr |=  MemberAttributes.Assembly;
			else if ((modifier & Modifiers.Protected) != 0)
				attr |=  MemberAttributes.Family;
			else if ((modifier & Modifiers.Private) != 0)
				attr |= MemberAttributes.Private;
			
			return attr;
		}
		
		static TypeAttributes ConvTypeAttributes(Modifiers modifier)
		{
			TypeAttributes attr = (TypeAttributes)0;
			if ((modifier & Modifiers.Abstract) != 0)
				attr |= TypeAttributes.Abstract;
			if ((modifier & Modifiers.Sealed) != 0)
				attr |= TypeAttributes.Sealed;
			if ((modifier & Modifiers.Static) != 0)
				attr |= TypeAttributes.Abstract | TypeAttributes.Sealed;
			
			if ((modifier & Modifiers.Public) != 0)
				attr |= TypeAttributes.Public;
			else
				attr |= TypeAttributes.NotPublic;
			
			return attr;
		}
		
		#region ICSharpCode.SharpRefactory.Parser.IAstVisitor interface implementation
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			if (compilationUnit == null) {
				throw new ArgumentNullException("compilationUnit");
			}
			CodeNamespace globalNamespace = new CodeNamespace("Global");
			//namespaces.Add(globalNamespace);
			namespaceDeclarations.Push(globalNamespace);
			compilationUnit.AcceptChildren(this, data);
			codeCompileUnit.Namespaces.Add(globalNamespace);
			return globalNamespace;
		}
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			CodeNamespace currentNamespace = new CodeNamespace(namespaceDeclaration.Name);
			//namespaces.Add(currentNamespace);
			// add imports from mother namespace
			foreach (CodeNamespaceImport import in ((CodeNamespace)namespaceDeclarations.Peek()).Imports) {
				currentNamespace.Imports.Add(import);
			}
			namespaceDeclarations.Push(currentNamespace);
			namespaceDeclaration.AcceptChildren(this, data);
			namespaceDeclarations.Pop();
			codeCompileUnit.Namespaces.Add(currentNamespace);
			
			// Nested namespaces are not allowed in CodeDOM
			return null;
		}
		
		public override object VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			foreach (Using u in usingDeclaration.Usings) {
				namespaceDeclarations.Peek().Imports.Add(new CodeNamespaceImport(u.Name));
			}
			return null;
		}

		// RG
		public override object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			CodeMemberEvent evt = new CodeMemberEvent();
			evt.Type = ConvType(eventDeclaration.TypeReference);
			evt.Name = eventDeclaration.Name;

			evt.Attributes = ConvMemberAttributes(eventDeclaration.Modifier);

			typeDeclarations.Peek().Members.Add(evt);

			return null;
		}
		
		public override object VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			return null;
		}

		// RG: CodeTypeReferenceExpression
		public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return new CodeTypeReferenceExpression(ConvType(typeReferenceExpression.TypeReference));
		}

		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			TypeDeclaration oldTypeDeclaration = currentTypeDeclaration;
			this.currentTypeDeclaration = typeDeclaration;
			CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration(typeDeclaration.Name);
			codeTypeDeclaration.TypeAttributes = ConvTypeAttributes(typeDeclaration.Modifier);
			codeTypeDeclaration.IsClass     = typeDeclaration.Type == ClassType.Class;
			codeTypeDeclaration.IsEnum      = typeDeclaration.Type == ClassType.Enum;
			codeTypeDeclaration.IsInterface = typeDeclaration.Type == ClassType.Interface;
			codeTypeDeclaration.IsStruct    = typeDeclaration.Type == ClassType.Struct;
			codeTypeDeclaration.IsPartial = (typeDeclaration.Modifier & Modifiers.Partial) != 0;
			
			if (typeDeclaration.BaseTypes != null) {
				foreach (TypeReference typeRef in typeDeclaration.BaseTypes) {
					codeTypeDeclaration.BaseTypes.Add(ConvType(typeRef));
				}
			}
			
			typeDeclarations.Push(codeTypeDeclaration);
			typeDeclaration.AcceptChildren(this, data);
			typeDeclarations.Pop();
			
			if (typeDeclarations.Count > 0) {
				typeDeclarations.Peek().Members.Add(codeTypeDeclaration);
			} else {
				namespaceDeclarations.Peek().Types.Add(codeTypeDeclaration);
			}
			currentTypeDeclaration = oldTypeDeclaration;
			
			return null;
		}
		
		public override object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			CodeTypeDelegate codeTypeDelegate = new CodeTypeDelegate(delegateDeclaration.Name);
			codeTypeDelegate.Attributes = ConvMemberAttributes(delegateDeclaration.Modifier);
			codeTypeDelegate.ReturnType = ConvType(delegateDeclaration.ReturnType);

			foreach (ParameterDeclarationExpression parameter in delegateDeclaration.Parameters)
			{
				codeTypeDelegate.Parameters.Add((CodeParameterDeclarationExpression)VisitParameterDeclarationExpression(parameter, data));
			}

			if (typeDeclarations.Count > 0)
			{
				typeDeclarations.Peek().Members.Add(codeTypeDelegate);
			}
			else
			{
				namespaceDeclarations.Peek().Types.Add(codeTypeDelegate);
			}

			return null;
		}
		
		public override object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			return null;
		}
		
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			for (int i = 0; i < fieldDeclaration.Fields.Count; ++i) {
				VariableDeclaration field = (VariableDeclaration)fieldDeclaration.Fields[i];
				
				if ((fieldDeclaration.Modifier & Modifiers.WithEvents) != 0) {
					//this.withEventsFields.Add(field);
				}
				TypeReference fieldType = fieldDeclaration.GetTypeForField(i);
				
				if (fieldType.IsNull) {
					fieldType = new TypeReference(typeDeclarations.Peek().Name);
				}
				
				CodeMemberField memberField = new CodeMemberField(ConvType(fieldType), field.Name);
				memberField.Attributes = ConvMemberAttributes(fieldDeclaration.Modifier);
				if (!field.Initializer.IsNull) {
					memberField.InitExpression = (CodeExpression)field.Initializer.AcceptVisitor(this, data);
				}
				
				typeDeclarations.Peek().Members.Add(memberField);
			}
			
			return null;
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			InitMethodScope();

			CodeMemberMethod memberMethod = new CodeMemberMethod();
			memberMethod.Name = methodDeclaration.Name;
			memberMethod.Attributes = ConvMemberAttributes(methodDeclaration.Modifier);
			memberMethod.ReturnType = ConvType(methodDeclaration.TypeReference);

			// RG: Private Interface Decl
			if ((memberMethod.Attributes & MemberAttributes.Public) != MemberAttributes.Public &&
			    methodDeclaration.InterfaceImplementations.Count > 0)
			{
				memberMethod.PrivateImplementationType = ConvType(methodDeclaration.InterfaceImplementations[0].InterfaceType);
			}

			codeStack.Push(memberMethod.Statements);
			
			typeDeclarations.Peek().Members.Add(memberMethod);
			
			// Add Method Parameters
			parameters.Clear();

			foreach (ParameterDeclarationExpression parameter in methodDeclaration.Parameters)
			{
				memberMethod.Parameters.Add((CodeParameterDeclarationExpression)VisitParameterDeclarationExpression(parameter, data));
			}

			usingId = 0; // RG
			foreachId = 0;
			switchId = 0;
			doId = 0;
			Breakable.NextId = 0;
			variables.Clear();
			methodDeclaration.Body.AcceptChildren(this, data);
			
			codeStack.Pop();
			
			return null;
		}

		// RG
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			CodeMemberProperty memberProperty = new CodeMemberProperty();
			memberProperty.Name = propertyDeclaration.Name;
			memberProperty.Attributes = ConvMemberAttributes(propertyDeclaration.Modifier);
			memberProperty.HasGet = propertyDeclaration.HasGetRegion;
			memberProperty.HasSet = propertyDeclaration.HasSetRegion;
			memberProperty.Type = ConvType(propertyDeclaration.TypeReference);

			typeDeclarations.Peek().Members.Add(memberProperty);

			// Add Method Parameters
			foreach (ParameterDeclarationExpression parameter in propertyDeclaration.Parameters)
			{
				memberProperty.Parameters.Add((CodeParameterDeclarationExpression)VisitParameterDeclarationExpression(parameter, data));
			}

			if (memberProperty.HasGet)
			{
				codeStack.Push(memberProperty.GetStatements);
				propertyDeclaration.GetRegion.Block.AcceptChildren(this, data);
				codeStack.Pop();
			}

			if (memberProperty.HasSet)
			{
				codeStack.Push(memberProperty.SetStatements);
				propertyDeclaration.SetRegion.Block.AcceptChildren(this, data);
				codeStack.Pop();
			}

			return null;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			InitMethodScope();

			CodeConstructor memberMethod = new CodeConstructor();
			memberMethod.Attributes = ConvMemberAttributes(constructorDeclaration.Modifier);

			typeDeclarations.Peek().Members.Add(memberMethod);

			codeStack.Push(NullStmtCollection);
			foreach (ParameterDeclarationExpression parameter in constructorDeclaration.Parameters)
			{
				memberMethod.Parameters.Add((CodeParameterDeclarationExpression)VisitParameterDeclarationExpression(parameter, data));
			}
			
			if (constructorDeclaration.ConstructorInitializer != null)
			{
				if (constructorDeclaration.ConstructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base)
				{
					if (constructorDeclaration.ConstructorInitializer.Arguments.Count == 0)
					{
						memberMethod.BaseConstructorArgs.Add(new CodeSnippetExpression());
					}
					
					foreach (Expression o in constructorDeclaration.ConstructorInitializer.Arguments)
					{
						memberMethod.BaseConstructorArgs.Add((CodeExpression)o.AcceptVisitor(this, data));
					}
				}

				if (constructorDeclaration.ConstructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This)
				{
					if (constructorDeclaration.ConstructorInitializer.Arguments.Count == 0)
					{
						memberMethod.ChainedConstructorArgs.Add(new CodeSnippetExpression());
					}

					foreach (Expression o in constructorDeclaration.ConstructorInitializer.Arguments)
					{
						memberMethod.ChainedConstructorArgs.Add((CodeExpression)o.AcceptVisitor(this, data));
					}
				}
			}
			codeStack.Pop();

			codeStack.Push(memberMethod.Statements);
			constructorDeclaration.Body.AcceptChildren(this, data);
			codeStack.Pop();
			
			return null;
		}

		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			blockStatement.AcceptChildren(this, data);
			return null;
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			object exp = expressionStatement.Expression.AcceptVisitor(this, data);
			if (exp is CodeExpression) {
				AddStmt(new CodeExpressionStatement((CodeExpression)exp));
			}
			return exp;
		}
		
		public override object VisitLocalVariableDeclaration(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			CodeVariableDeclarationStatement declStmt = null;
			
			for (int i = 0; i < localVariableDeclaration.Variables.Count; ++i) {
				CodeTypeReference type = ConvType(localVariableDeclaration.GetTypeForVariable(i) ?? new TypeReference("System.Object", true));
				VariableDeclaration var = (VariableDeclaration)localVariableDeclaration.Variables[i];
				if (!var.Initializer.IsNull) {
					declStmt = new CodeVariableDeclarationStatement(type,
					                                                var.Name,
					                                                (CodeExpression)((INode)var.Initializer).AcceptVisitor(this, data));
				} else {
					declStmt = new CodeVariableDeclarationStatement(type,
					                                                var.Name);
				}
				variables.Add(declStmt);
				AddStmt(declStmt);
			}
			
			return declStmt;
		}
		
		public override object VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			CodeMethodReturnStatement returnStmt;
			if (returnStatement.Expression.IsNull)
				returnStmt = new CodeMethodReturnStatement();
			else
				returnStmt = new CodeMethodReturnStatement((CodeExpression)returnStatement.Expression.AcceptVisitor(this,data));
			
			AddStmt(returnStmt);
			
			return returnStmt;
		}
		
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			CodeConditionStatement ifStmt = new CodeConditionStatement();
			
			ifStmt.Condition = (CodeExpression)ifElseStatement.Condition.AcceptVisitor(this, data);
			
			codeStack.Push(ifStmt.TrueStatements);
			foreach (Statement stmt in ifElseStatement.TrueStatement) {
				if (stmt is BlockStatement) {
					stmt.AcceptChildren(this, data);
				} else {
					stmt.AcceptVisitor(this, data);
				}
			}
			codeStack.Pop();
			
			codeStack.Push(ifStmt.FalseStatements);
			foreach (Statement stmt in ifElseStatement.FalseStatement) {
				if (stmt is BlockStatement) {
					stmt.AcceptChildren(this, data);
				} else {
					stmt.AcceptVisitor(this, data);
				}
			}
			codeStack.Pop();
			
			AddStmt(ifStmt);
			
			return ifStmt;
		}

		int foreachId = 0; // in case of nested foreach statments

		public override object VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			// RG:
			//  foreach (T t in x)
			//  {
			//      stmts;
			//  }
			//
			// Emulate with
			//
			//  for (System.Collections.IEnumerator _it = x.GetEnumerator(); _it.MoveNext(); )
			//  {
			//      T t = ((T)_it.Current);
			//
			//      stmts;
			//  }

			foreachId++;
			string name = "_it" + foreachId.ToString();

			CodeIterationStatement _for1 = new CodeIterationStatement();
			breakableStack.Push(new Breakable());

			// init
			CodeVariableDeclarationStatement _decl2 = new CodeVariableDeclarationStatement();
			CodeMethodInvokeExpression _invoke1 = new CodeMethodInvokeExpression();
			CodeMethodReferenceExpression _GetEnumerator_method1 = new CodeMethodReferenceExpression();
			_GetEnumerator_method1.MethodName = "GetEnumerator";
			
			//CodeCastExpression _cast1 = new CodeCastExpression();
			//codeStack.Push(NullStmtCollection);
			//_cast1.Expression = (CodeExpression)foreachStatement.Expression.AcceptVisitor(this, data);
			//codeStack.Pop();
			//CodeTypeReference _IEnumerable_type1 = new CodeTypeReference("System.Collections.IEnumerable");
			//_cast1.TargetType = _IEnumerable_type1;

			//_GetEnumerator_method1.TargetObject = _cast1;

			codeStack.Push(NullStmtCollection);
			_GetEnumerator_method1.TargetObject = (CodeExpression)foreachStatement.Expression.AcceptVisitor(this, data);
			codeStack.Pop();

			_invoke1.Method = _GetEnumerator_method1;
			_decl2.InitExpression = _invoke1;
			_decl2.Name = name;
			CodeTypeReference _IEnumerator_type1 = new CodeTypeReference("System.Collections.IEnumerator");
			_decl2.Type = _IEnumerator_type1;
			_for1.InitStatement = _decl2;

			// Condition
			CodeMethodInvokeExpression _invoke2 = new CodeMethodInvokeExpression();
			CodeMethodReferenceExpression _MoveNext_method1 = new CodeMethodReferenceExpression();
			_MoveNext_method1.MethodName = "MoveNext";
			CodeVariableReferenceExpression _arg2 = new CodeVariableReferenceExpression();
			_arg2.VariableName = name;
			_MoveNext_method1.TargetObject = _arg2;
			_invoke2.Method = _MoveNext_method1;
			_for1.TestExpression = _invoke2;

			// Empty Increment
			_for1.IncrementStatement = new CodeExpressionStatement(new CodeSnippetExpression());

			// T t = ((T)_it.Current);
			CodeVariableDeclarationStatement _decl3 = new CodeVariableDeclarationStatement();
			CodeCastExpression _cast2 = new CodeCastExpression();
			CodePropertyReferenceExpression _prop1 = new CodePropertyReferenceExpression();
			_prop1.PropertyName = "Current";
			CodeVariableReferenceExpression _arg3 = new CodeVariableReferenceExpression();
			_arg3.VariableName = name;
			_prop1.TargetObject = _arg3;
			_cast2.Expression = _prop1;
			CodeTypeReference _System_String_type5 = ConvType(foreachStatement.TypeReference);
			_cast2.TargetType = _System_String_type5;
			_decl3.InitExpression = _cast2;
			_decl3.Name = foreachStatement.VariableName;
			CodeTypeReference _System_String_type6 = ConvType(foreachStatement.TypeReference);
			_decl3.Type = _System_String_type6;
			_for1.Statements.Add(_decl3);
			_for1.Statements.Add(new CodeSnippetStatement());

			codeStack.Push(_for1.Statements);
			foreachStatement.EmbeddedStatement.AcceptVisitor(this, data);
			codeStack.Pop();

			Breakable breakable = breakableStack.Pop();

			if (breakable.IsContinue)
			{
				_for1.Statements.Add(new CodeSnippetStatement());
				_for1.Statements.Add(new CodeLabeledStatement("continue" + breakable.Id, new CodeExpressionStatement(new CodeSnippetExpression())));
			}

			AddStmt(_for1);

			if (breakable.IsBreak)
			{
				AddStmt(new CodeLabeledStatement("break" + breakable.Id, new CodeExpressionStatement(new CodeSnippetExpression())));
			}

			return _for1;
		}

		int doId = 0;

		// RG:
		public override object VisitDoLoopStatement(DoLoopStatement doLoopStatement, object data)
		{
			CodeIterationStatement forLoop = new CodeIterationStatement();
			breakableStack.Push(new Breakable());

			codeStack.Push(NullStmtCollection);

			if (doLoopStatement.ConditionPosition == ConditionPosition.End)
			{
				// do { } while (expr);
				//
				// emulate with:
				//  for (bool _do = true; _do; _do = expr) {}
				//
				doId++;
				string name = "_do" + doId;

				forLoop.InitStatement = new CodeVariableDeclarationStatement(typeof(System.Boolean), name, new CodePrimitiveExpression(true));
				forLoop.TestExpression = new CodeVariableReferenceExpression(name);

				forLoop.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(name),
				                                                     doLoopStatement.Condition == null ? new CodePrimitiveExpression(true) : (CodeExpression)doLoopStatement.Condition.AcceptVisitor(this, data));
			}
			else
			{
				// while (expr) {}
				//
				// emulate with:
				//  for (; expr;) {}
				//

				// Empty Init and Increment Statements
				forLoop.InitStatement = new CodeExpressionStatement(new CodeSnippetExpression());
				forLoop.IncrementStatement = new CodeExpressionStatement(new CodeSnippetExpression());

				if (doLoopStatement.Condition == null)
				{
					forLoop.TestExpression = new CodePrimitiveExpression(true);
				}
				else
				{
					forLoop.TestExpression = (CodeExpression)doLoopStatement.Condition.AcceptVisitor(this, data);
				}
			}

			codeStack.Pop();

			codeStack.Push(forLoop.Statements);
			doLoopStatement.EmbeddedStatement.AcceptVisitor(this, data);
			codeStack.Pop();

			if (forLoop.Statements.Count == 0)
			{
				forLoop.Statements.Add(new CodeSnippetStatement());
			}

			Breakable breakable = breakableStack.Pop();

			if (breakable.IsContinue)
			{
				forLoop.Statements.Add(new CodeSnippetStatement());
				forLoop.Statements.Add(new CodeLabeledStatement("continue" + breakable.Id, new CodeExpressionStatement(new CodeSnippetExpression())));
			}

			AddStmt(forLoop);

			if (breakable.IsBreak)
			{
				AddStmt(new CodeLabeledStatement("break" + breakable.Id, new CodeExpressionStatement(new CodeSnippetExpression())));
			}

			return forLoop;
		}
		
		public override object VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			System.CodeDom.CodeLabeledStatement labelStmt = new CodeLabeledStatement(labelStatement.Label);
			
			// Add Statement to Current Statement Collection
			AddStmt(labelStmt);
			
			return labelStmt;
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			System.CodeDom.CodeGotoStatement gotoStmt = new CodeGotoStatement(gotoStatement.Label);
			
			// Add Statement to Current Statement Collection
			AddStmt(gotoStmt);
			
			return gotoStmt;
		}

		// RG
		int switchId = 0;

		public override object VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			// switch(arg) { case label1: expr1; case label2: expr2; default: expr3; }
			//
			// Emulate With:
			// 
			//  object _switch1 = arg;
			//  if (arg.Equals(label1))
			//  {
			//      expr1;
			//  }
			//  else
			//  {
			//      if (arg.Equals(label2))
			//      {
			//          expr2;
			//      }
			//      else
			//      {
			//          expr3;
			//      }
			//  }
			//

			switchId++; // in case nested switch() statements
			string name = "_switch" + switchId.ToString();

			breakableStack.Push(new Breakable(false));

			bool isSwitchArg = false;

			CodeVariableReferenceExpression switchArg = null;
			SwitchSection defaultSection = null;

			// get default section
			foreach (SwitchSection section in switchStatement.SwitchSections)
			{
				foreach (CaseLabel label in section.SwitchLabels)
				{
					if (label.IsDefault)
					{
						defaultSection = section;
						break;
					}
				}

				if (defaultSection != null)
					break;
			}


			CodeConditionStatement _if = null;

			// get default section
			foreach (SwitchSection section in switchStatement.SwitchSections)
			{
				if (section != defaultSection)
				{
					if (!isSwitchArg)
					{
						isSwitchArg = true;

						codeStack.Push(NullStmtCollection);
						CodeVariableDeclarationStatement switchStmt = new CodeVariableDeclarationStatement("System.Object", name, (CodeExpression)switchStatement.SwitchExpression.AcceptVisitor(this, data));
						codeStack.Pop();

						switchArg = new CodeVariableReferenceExpression(name);

						AddStmt(switchStmt);
						AddStmt(new CodeSnippetStatement());
					}

					codeStack.Push(NullStmtCollection);

					CodeExpression condition = null;
					foreach (CaseLabel label in section.SwitchLabels)
					{
						CodeMethodInvokeExpression cond = new CodeMethodInvokeExpression(switchArg, "Equals", (CodeExpression)label.Label.AcceptVisitor(this, data));
						if (condition == null)
						{
							condition = cond;
						}
						else
						{
							condition = new CodeBinaryOperatorExpression(condition, CodeBinaryOperatorType.BooleanOr, cond);
						}
					}

					codeStack.Pop();

					if (_if == null)
					{
						_if = new CodeConditionStatement();
						_if.Condition = condition;

						AddStmt(_if);
					}
					else
					{
						CodeConditionStatement _if2 = new CodeConditionStatement();
						_if2.Condition = condition;

						_if.FalseStatements.Add(_if2);

						_if = _if2;
					}

					codeStack.Push(_if.TrueStatements);

					for (int i = 0; i < section.Children.Count; i++)
					{
						INode stmt = section.Children[i];

//						if (i == section.Children.Count - 1 && stmt is BreakStatement)
//							break;

						stmt.AcceptVisitor(this, data);
					}

					codeStack.Pop();
				}
			}

			if (defaultSection != null)
			{
				if (_if != null)
					codeStack.Push(_if.FalseStatements);

				for (int i = 0; i < defaultSection.Children.Count; i++)
				{
					INode stmt = defaultSection.Children[i];

//					if (i == defaultSection.Children.Count - 1 && stmt is BreakStatement)
//						break;

					stmt.AcceptVisitor(this, data);
				}

				if (_if != null)
					codeStack.Pop();
			}

			Breakable breakable = breakableStack.Pop();

			if (breakable.IsContinue)
			{
				throw new Exception("Continue Inside Switch Not Supported");
			}

			if (breakable.IsBreak)
			{
				AddStmt(new CodeLabeledStatement("break" + breakable.Id, new CodeExpressionStatement(new CodeSnippetExpression())));
			}

			return null;
		}
		
		public override object VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			// add a try-catch-finally
			CodeTryCatchFinallyStatement tryStmt = new CodeTryCatchFinallyStatement();
			
			codeStack.Push(tryStmt.TryStatements);
			
			tryCatchStatement.StatementBlock.AcceptChildren(this, data);
			codeStack.Pop();
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				codeStack.Push(tryStmt.FinallyStatements);
				
				tryCatchStatement.FinallyBlock.AcceptChildren(this,data);
				codeStack.Pop();
			}
			
			foreach (CatchClause clause in tryCatchStatement.CatchClauses)
			{
				CodeCatchClause catchClause = new CodeCatchClause(clause.VariableName);
				catchClause.CatchExceptionType = ConvType(clause.TypeReference);
				tryStmt.CatchClauses.Add(catchClause);
				
				codeStack.Push(catchClause.Statements);
				
				clause.StatementBlock.AcceptChildren(this, data);
				codeStack.Pop();
			}
			
			// Add Statement to Current Statement Collection
			AddStmt(tryStmt);
			
			return tryStmt;
		}
		
		public override object VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			CodeThrowExceptionStatement throwStmt = new CodeThrowExceptionStatement((CodeExpression)throwStatement.Expression.AcceptVisitor(this, data));
			
			// Add Statement to Current Statement Collection
			AddStmt(throwStmt);
			
			return throwStmt;
		}
		
		#region Expressions
		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			return new CodePrimitiveExpression(primitiveExpression.Value);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			CodeBinaryOperatorType op = CodeBinaryOperatorType.Add;
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Add:
					op = CodeBinaryOperatorType.Add;
					break;
				case BinaryOperatorType.BitwiseAnd:
					op = CodeBinaryOperatorType.BitwiseAnd;
					break;
				case BinaryOperatorType.BitwiseOr:
					op = CodeBinaryOperatorType.BitwiseOr;
					break;
				case BinaryOperatorType.LogicalAnd:
					op = CodeBinaryOperatorType.BooleanAnd;
					break;
				case BinaryOperatorType.LogicalOr:
					op = CodeBinaryOperatorType.BooleanOr;
					break;
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.DivideInteger:
					op = CodeBinaryOperatorType.Divide;
					break;
				case BinaryOperatorType.GreaterThan:
					op = CodeBinaryOperatorType.GreaterThan;
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					op = CodeBinaryOperatorType.GreaterThanOrEqual;
					break;
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					op = CodeBinaryOperatorType.ValueEquality;
					break;
				case BinaryOperatorType.LessThan:
					op = CodeBinaryOperatorType.LessThan;
					break;
				case BinaryOperatorType.LessThanOrEqual:
					op = CodeBinaryOperatorType.LessThanOrEqual;
					break;
				case BinaryOperatorType.Modulus:
					op = CodeBinaryOperatorType.Modulus;
					break;
				case BinaryOperatorType.Multiply:
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.Subtract:
					op = CodeBinaryOperatorType.Subtract;
					break;
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.ReferenceEquality:
					op = CodeBinaryOperatorType.IdentityEquality;
					break;
				case BinaryOperatorType.ReferenceInequality:
					op = CodeBinaryOperatorType.IdentityInequality;
					break;
					
				case BinaryOperatorType.ExclusiveOr:
					// CodeDom doesn't support ExclusiveOr
					op = CodeBinaryOperatorType.BitwiseAnd;
					break;
			}

			System.Diagnostics.Debug.Assert(!binaryOperatorExpression.Left.IsNull);
			System.Diagnostics.Debug.Assert(!binaryOperatorExpression.Right.IsNull);

			var cboe = new CodeBinaryOperatorExpression(
				(CodeExpression)binaryOperatorExpression.Left.AcceptVisitor(this, data),
				op,
				(CodeExpression)binaryOperatorExpression.Right.AcceptVisitor(this, data));
			if (binaryOperatorExpression.Op == BinaryOperatorType.InEquality) {
				cboe = new CodeBinaryOperatorExpression(cboe, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(false));
			}
			return cboe;
		}
		
		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			Expression     target     = invocationExpression.TargetObject;
			CodeExpression targetExpr;
			string         methodName = null;
			if (target == null) {
				targetExpr = new CodeThisReferenceExpression();
			} else if (target is MemberReferenceExpression) {
				MemberReferenceExpression fRef = (MemberReferenceExpression)target;
				targetExpr = null;
				if (fRef.TargetObject is MemberReferenceExpression) {
					if (IsPossibleTypeReference((MemberReferenceExpression)fRef.TargetObject)) {
						targetExpr = ConvertToTypeReference((MemberReferenceExpression)fRef.TargetObject);
					}
				}
				if (targetExpr == null)
					targetExpr = (CodeExpression)fRef.TargetObject.AcceptVisitor(this, data);
				
				methodName = fRef.MemberName;
				// HACK for : Microsoft.VisualBasic.ChrW(NUMBER)
				if (methodName == "ChrW") {
					return new CodeCastExpression("System.Char", GetExpressionList(invocationExpression.Arguments)[0]);
				}
			} else if (target is IdentifierExpression) {
				targetExpr = new CodeThisReferenceExpression();
				methodName = ((IdentifierExpression)target).Identifier;
			} else {
				targetExpr = (CodeExpression)target.AcceptVisitor(this, data);
			}
			return new CodeMethodInvokeExpression(targetExpr, methodName, GetExpressionList(invocationExpression.Arguments));
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			if (!IsLocalVariable(identifierExpression.Identifier) && IsField(identifierExpression.Identifier)) {
				return new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), identifierExpression.Identifier);
			}
			return new CodeVariableReferenceExpression(identifierExpression.Identifier);
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			CodeExpression var;
			CodeAssignStatement assign;

			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Minus:
					if (unaryOperatorExpression.Expression is PrimitiveExpression) {
						PrimitiveExpression expression = (PrimitiveExpression)unaryOperatorExpression.Expression;
						if (expression.Value is int) {
							return new CodePrimitiveExpression(- (int)expression.Value);
						}
						if (expression.Value is System.UInt32 || expression.Value is System.UInt16) {
							return new CodePrimitiveExpression(Int32.Parse("-" + expression.StringValue));
						}
						
						if (expression.Value is long) {
							return new CodePrimitiveExpression(- (long)expression.Value);
						}
						if (expression.Value is double) {
							return new CodePrimitiveExpression(- (double)expression.Value);
						}
						if (expression.Value is float) {
							return new CodePrimitiveExpression(- (float)expression.Value);
						}
						
					}
					return  new CodeBinaryOperatorExpression(new CodePrimitiveExpression(0),
					                                         CodeBinaryOperatorType.Subtract,
					                                         (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Plus:
					return unaryOperatorExpression.Expression.AcceptVisitor(this, data);
					
				case UnaryOperatorType.PostIncrement:
					// emulate i++, with i = i + 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					assign = new CodeAssignStatement(var,
					                                 new CodeBinaryOperatorExpression(var,
					                                                                  CodeBinaryOperatorType.Add,
					                                                                  new CodePrimitiveExpression(1)));

					AddStmt(assign);

					return assign;

					//return new CodeAssignStatement(var,
					//               new CodeBinaryOperatorExpression(var,
					//                                                CodeBinaryOperatorType.Add,
					//                                                new CodePrimitiveExpression(1)));

					// RG: needs to return an Expression - Not a Statement
					//return new CodeBinaryOperatorExpression(var,
					//                               CodeBinaryOperatorType.Assign,
					//                               new CodeBinaryOperatorExpression(var,
					//                                                                CodeBinaryOperatorType.Add,
					//                                                                new CodePrimitiveExpression(1)));
					
				case UnaryOperatorType.PostDecrement:
					// emulate i--, with i = i - 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					assign = new CodeAssignStatement(var,
					                                 new CodeBinaryOperatorExpression(var,
					                                                                  CodeBinaryOperatorType.Subtract,
					                                                                  new CodePrimitiveExpression(1)));

					AddStmt(assign);

					return assign;
					
					//return new CodeAssignStatement(var,
					//                               new CodeBinaryOperatorExpression(var,
					//                                                                CodeBinaryOperatorType.Subtract,
					//                                                                new CodePrimitiveExpression(1)));

					// RG: needs to return an Expression - Not a Statement
					//return new CodeBinaryOperatorExpression(var,
					//               CodeBinaryOperatorType.Assign,
					//               new CodeBinaryOperatorExpression(var,
					//                                                CodeBinaryOperatorType.Subtract,
					//                                                new CodePrimitiveExpression(1)));
					
				case UnaryOperatorType.Decrement:
					// emulate --i, with i = i - 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					assign = new CodeAssignStatement(var,
					                                 new CodeBinaryOperatorExpression(var,
					                                                                  CodeBinaryOperatorType.Subtract,
					                                                                  new CodePrimitiveExpression(1)));
					AddStmt(assign);

					return assign;
					//return new CodeAssignStatement(var,
					//                               new CodeBinaryOperatorExpression(var,
					//                                                                CodeBinaryOperatorType.Subtract,
					//                                                                new CodePrimitiveExpression(1)));

					//return new CodeBinaryOperatorExpression(var,
					//                CodeBinaryOperatorType.Assign,
					//               new CodeBinaryOperatorExpression(var,
					//                                                CodeBinaryOperatorType.Subtract,
					//                                                new CodePrimitiveExpression(1)));
					
				case UnaryOperatorType.Increment:
					// emulate ++i, with i = i + 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					assign = new CodeAssignStatement(var,
					                                 new CodeBinaryOperatorExpression(var,
					                                                                  CodeBinaryOperatorType.Add,
					                                                                  new CodePrimitiveExpression(1)));

					AddStmt(assign);

					return assign;

					//return new CodeAssignStatement(var,
					//                               new CodeBinaryOperatorExpression(var,
					//                                                                CodeBinaryOperatorType.Add,
					//                                                                new CodePrimitiveExpression(1)));

					//return new CodeBinaryOperatorExpression(var,
					//                CodeBinaryOperatorType.Assign,
					//                new CodeBinaryOperatorExpression(var,
					//                                                CodeBinaryOperatorType.Add,
					//                                                new CodePrimitiveExpression(1)));

					// RG:
				case UnaryOperatorType.Not:
					// emulate !a with a == false
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);
					
					CodeBinaryOperatorExpression cboe = var as CodeBinaryOperatorExpression;
					if (cboe != null && cboe.Operator == CodeBinaryOperatorType.IdentityEquality) {
						return new CodeBinaryOperatorExpression(cboe.Left, CodeBinaryOperatorType.IdentityInequality, cboe.Right);
					} else {
						return new CodeBinaryOperatorExpression(var,CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(false));
					}

				default:
					throw new NotSupportedException("CodeDom does not support Unary Operators");
			}
		}

		bool methodReference = false;
		
		void AddEventHandler(Expression eventExpr, Expression handler, object data)
		{
			methodReference = true;
			CodeExpression methodInvoker = (CodeExpression)handler.AcceptVisitor(this, data);
			methodReference = false;
			if (!(methodInvoker is CodeObjectCreateExpression)) {
				// we need to create an event handler here
				methodInvoker = new CodeObjectCreateExpression(new CodeTypeReference("System.EventHandler"), methodInvoker);
			}
			
			if (eventExpr is IdentifierExpression) {
				AddStmt(new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeThisReferenceExpression(), ((IdentifierExpression)eventExpr).Identifier),
				                                     methodInvoker));
			} else {
				MemberReferenceExpression fr = (MemberReferenceExpression)eventExpr;
				AddStmt(new CodeAttachEventStatement(new CodeEventReferenceExpression((CodeExpression)fr.TargetObject.AcceptVisitor(this, data), fr.MemberName),
				                                     methodInvoker));
			}
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			if (assignmentExpression.Op == AssignmentOperatorType.Add) {
				AddEventHandler(assignmentExpression.Left, assignmentExpression.Right, data);
			} else {
				if (assignmentExpression.Left is IdentifierExpression) {
					AddStmt(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				} else {
					AddStmt(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				}
			}
			return null;
		}
		
		public override object VisitAddHandlerStatement(AddHandlerStatement addHandlerStatement, object data)
		{
			AddEventHandler(addHandlerStatement.EventExpression, addHandlerStatement.HandlerExpression, data);
			return null;
		}
		
		public override object VisitAddressOfExpression(AddressOfExpression addressOfExpression, object data)
		{
			return addressOfExpression.Expression.AcceptVisitor(this, data);
		}

		public override object VisitUsing(Using @using, object data)
		{
			return base.VisitUsing(@using, data);
		}

		// RG
		int usingId = 0;

		public override object VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			// using (new expr) { stmts; }
			//
			// emulate with
			//      object _dispose;
			//      try
			//      {
			//          _dispose = new expr;
			//
			//          stmts;
			//      }
			//      finally
			//      {
			//          if (((_dispose != null)
			//              && (typeof(System.IDisposable).IsInstanceOfType(_dispose) == true)))
			//          {
			//              ((System.IDisposable)(_dispose)).Dispose();
			//          }
			//      }
			//

			usingId++; // in case nested using() statements
			string name = "_dispose" + usingId.ToString();

			CodeVariableDeclarationStatement disposable = new CodeVariableDeclarationStatement("System.Object", name, new CodePrimitiveExpression(null));

			AddStmt(disposable);

			CodeTryCatchFinallyStatement tryStmt = new CodeTryCatchFinallyStatement();

			CodeVariableReferenceExpression left1 = new CodeVariableReferenceExpression(name);

			codeStack.Push(NullStmtCollection); // send statements to nul Statement collection
			CodeExpression right1 = (CodeExpression)usingStatement.ResourceAcquisition.AcceptVisitor(this, data);
			codeStack.Pop();

			CodeAssignStatement assign1 = new CodeAssignStatement(left1, right1);

			tryStmt.TryStatements.Add(assign1);
			tryStmt.TryStatements.Add(new CodeSnippetStatement());

			codeStack.Push(tryStmt.TryStatements);
			usingStatement.EmbeddedStatement.AcceptChildren(this, data);
			codeStack.Pop();

			CodeMethodInvokeExpression isInstanceOfType = new CodeMethodInvokeExpression(new CodeTypeOfExpression(typeof(IDisposable)), "IsInstanceOfType", new CodeExpression[] { left1 });

			CodeConditionStatement if1 = new CodeConditionStatement();
			if1.Condition = new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(left1, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)),
			                                                 CodeBinaryOperatorType.BooleanAnd,
			                                                 new CodeBinaryOperatorExpression(isInstanceOfType, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(true)));
			if1.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeCastExpression(typeof(IDisposable),left1), "Dispose", new CodeExpression[] { }));

			tryStmt.FinallyStatements.Add(if1);

			// Add Statement to Current Statement Collection
			AddStmt(tryStmt);

			return null;
		}
		
		public override object VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			return new CodeTypeOfExpression(ConvType(typeOfExpression.TypeReference));
		}

		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			CodeTypeReference typeRef = ConvType(castExpression.CastTo);
			return new CodeCastExpression(typeRef, (CodeExpression)castExpression.Expression.AcceptVisitor(this, data));
		}
		
		public override object VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return new CodeThisReferenceExpression();
		}
		
		public override object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			return new CodeBaseReferenceExpression();
		}
		
		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.ArrayInitializer.IsNull) {
				return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType),
				                                     arrayCreateExpression.Arguments[0].AcceptVisitor(this, data) as CodeExpression);
			}
			return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType),
			                                     GetExpressionList(arrayCreateExpression.ArrayInitializer.CreateExpressions));
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			return new CodeObjectCreateExpression(ConvType(objectCreateExpression.CreateType),
			                                      objectCreateExpression.Parameters == null ? null : GetExpressionList(objectCreateExpression.Parameters));
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression(ConvType(parameterDeclarationExpression.TypeReference), parameterDeclarationExpression.ParameterName);

			parameters.Add(parameter);

			return parameter;
		}

		bool IsField(string reflectionTypeName, string fieldName)
		{
			return environmentInformationProvider.HasField(reflectionTypeName, 0, fieldName);
		}
		
		bool IsFieldReferenceExpression(MemberReferenceExpression fieldReferenceExpression)
		{
			if (fieldReferenceExpression.TargetObject is ThisReferenceExpression
			    || fieldReferenceExpression.TargetObject is BaseReferenceExpression)
			{
				//field detection for fields\props inherited from base classes
				return IsField(fieldReferenceExpression.MemberName);
			}
			return false;
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression fieldReferenceExpression, object data)
		{
			if (methodReference) {
				methodReference = false;
				return new CodeMethodReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data), fieldReferenceExpression.MemberName);
			}
			if (IsFieldReferenceExpression(fieldReferenceExpression)) {
				return new CodeFieldReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data),
				                                        fieldReferenceExpression.MemberName);
			} else {
				if (fieldReferenceExpression.TargetObject is MemberReferenceExpression) {
					if (IsPossibleTypeReference((MemberReferenceExpression)fieldReferenceExpression.TargetObject)) {
						CodeTypeReferenceExpression typeRef = ConvertToTypeReference((MemberReferenceExpression)fieldReferenceExpression.TargetObject);
						if (IsField(typeRef.Type.BaseType, fieldReferenceExpression.MemberName)) {
							return new CodeFieldReferenceExpression(typeRef,
							                                        fieldReferenceExpression.MemberName);
						} else {
							return new CodePropertyReferenceExpression(typeRef,
							                                           fieldReferenceExpression.MemberName);
						}
					}
				}
				
				CodeExpression codeExpression = (CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data);
				return new CodePropertyReferenceExpression(codeExpression,
				                                           fieldReferenceExpression.MemberName);
			}
		}

		#endregion
		
		#endregion
		bool IsPossibleTypeReference(MemberReferenceExpression fieldReferenceExpression)
		{
			while (fieldReferenceExpression.TargetObject is MemberReferenceExpression) {
				fieldReferenceExpression = (MemberReferenceExpression)fieldReferenceExpression.TargetObject;
			}
			IdentifierExpression identifier = fieldReferenceExpression.TargetObject as IdentifierExpression;
			if (identifier != null)
				return !IsField(identifier.Identifier) && !IsLocalVariable(identifier.Identifier);
			TypeReferenceExpression tre = fieldReferenceExpression.TargetObject as TypeReferenceExpression;
			if (tre != null)
				return true;
			return false;
		}
		
		bool IsLocalVariable(string identifier)
		{
			foreach (CodeVariableDeclarationStatement variable in variables) {
				if (variable.Name == identifier)
					return true;
			}

			foreach (CodeParameterDeclarationExpression parameter in parameters)
			{
				if (parameter.Name == identifier)
					return true;
			}

			return false;
		}
		
		bool IsField(string identifier)
		{
			if (currentTypeDeclaration == null) // e.g. in unit tests
				return false;
			foreach (INode node in currentTypeDeclaration.Children) {
				if (node is FieldDeclaration) {
					FieldDeclaration fd = (FieldDeclaration)node;
					if (fd.GetVariableDeclaration(identifier) != null) {
						return true;
					}
				}
			}
			//field detection for fields\props inherited from base classes
			if (currentTypeDeclaration.BaseTypes.Count > 0) {
				return environmentInformationProvider.HasField(currentTypeDeclaration.BaseTypes[0].Type, currentTypeDeclaration.BaseTypes[0].GenericTypes.Count, identifier);
			}
			return false;
		}
		
		static CodeTypeReferenceExpression ConvertToTypeReference(MemberReferenceExpression fieldReferenceExpression)
		{
			StringBuilder type = new StringBuilder("");
			
			while (fieldReferenceExpression.TargetObject is MemberReferenceExpression) {
				type.Insert(0,'.');
				type.Insert(1,fieldReferenceExpression.MemberName.ToCharArray());
				fieldReferenceExpression = (MemberReferenceExpression)fieldReferenceExpression.TargetObject;
			}
			
			type.Insert(0,'.');
			type.Insert(1,fieldReferenceExpression.MemberName.ToCharArray());
			
			if (fieldReferenceExpression.TargetObject is IdentifierExpression) {
				type.Insert(0, ((IdentifierExpression)fieldReferenceExpression.TargetObject).Identifier.ToCharArray());
				string oldType = type.ToString();
				int idx = oldType.LastIndexOf('.');
				while (idx > 0) {
					if (Type.GetType(type.ToString()) != null) {
						break;
					}
					string stype = type.ToString().Substring(idx + 1);
					type = new StringBuilder(type.ToString().Substring(0, idx));
					type.Append("+");
					type.Append(stype);
					idx = type.ToString().LastIndexOf('.');
				}
				if (Type.GetType(type.ToString()) == null) {
					type = new StringBuilder(oldType);
				}
				return new CodeTypeReferenceExpression(type.ToString());
			} else if (fieldReferenceExpression.TargetObject is TypeReferenceExpression) {
				type.Insert(0, ((TypeReferenceExpression)fieldReferenceExpression.TargetObject).TypeReference.Type);
				return new CodeTypeReferenceExpression(type.ToString());
			} else {
				return null;
			}
		}
		
		CodeExpression[] GetExpressionList(IList expressionList)
		{
			if (expressionList == null) {
				return new CodeExpression[0];
			}
			CodeExpression[] list = new CodeExpression[expressionList.Count];
			for (int i = 0; i < expressionList.Count; ++i) {
				list[i] = (CodeExpression)((Expression)expressionList[i]).AcceptVisitor(this, null);
				if (list[i] == null) {
					list[i] = new CodePrimitiveExpression(0);
				}
			}
			return list;
		}
		
		
	}
}
