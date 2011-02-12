// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

// created on 04.08.2003 at 17:49
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.SharpDevelop.Dom.VBNet;
using AST = ICSharpCode.NRefactory.Ast;
using RefParser = ICSharpCode.NRefactory;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	public class NRefactoryASTConvertVisitor : AbstractAstVisitor
	{
		DefaultCompilationUnit cu;
		DefaultUsingScope currentNamespace;
		Stack<DefaultClass> currentClass = new Stack<DefaultClass>();
		public string VBRootNamespace { get; set; }
		
		public ICompilationUnit Cu {
			get {
				return cu;
			}
		}
		
		public NRefactoryASTConvertVisitor(IProjectContent projectContent, SupportedLanguage language)
		{
			if (language == SupportedLanguage.VBNet)
				cu = new VBNetCompilationUnit(projectContent);
			else
				cu = new DefaultCompilationUnit(projectContent);
		}
		
		DefaultClass GetCurrentClass()
		{
			return currentClass.Count == 0 ? null : currentClass.Peek();
		}
		
		ModifierEnum ConvertModifier(AST.Modifiers m)
		{
			if (this.IsVisualBasic)
				return ConvertModifier(m, ModifierEnum.Public);
			else if (currentClass.Count > 0 && currentClass.Peek().ClassType == ClassType.Interface)
				return ConvertModifier(m, ModifierEnum.Public);
			else
				return ConvertModifier(m, ModifierEnum.Private);
		}
		
		ModifierEnum ConvertTypeModifier(AST.Modifiers m)
		{
			if (this.IsVisualBasic)
				return ConvertModifier(m, ModifierEnum.Public);
			if (currentClass.Count > 0)
				return ConvertModifier(m, ModifierEnum.Private);
			else
				return ConvertModifier(m, ModifierEnum.Internal);
		}
		
		ModifierEnum ConvertModifier(AST.Modifiers m, ModifierEnum defaultVisibility)
		{
			ModifierEnum r = (ModifierEnum)m;
			if ((r & ModifierEnum.VisibilityMask) == ModifierEnum.None)
				return r | defaultVisibility;
			else
				return r;
		}
		
		List<RefParser.ISpecial> specials;
		
		/// <summary>
		/// Gets/Sets the list of specials used to read the documentation.
		/// The list must be sorted by the start position of the specials!
		/// </summary>
		public List<RefParser.ISpecial> Specials {
			get {
				return specials;
			}
			set {
				specials = value;
			}
		}
		
		string GetDocumentation(int line, IList<AST.AttributeSection> attributes)
		{
			foreach (AST.AttributeSection att in attributes) {
				if (att.StartLocation.Y > 0 && att.StartLocation.Y < line)
					line = att.StartLocation.Y;
			}
			List<string> lines = new List<string>();
			int length = 0;
			while (line > 0) {
				line--;
				string doku = null;
				bool foundPreprocessing = false;
				var specialsOnLine = GetSpecialsFromLine(line);
				foreach (RefParser.ISpecial special in specialsOnLine) {
					RefParser.Comment comment = special as RefParser.Comment;
					if (comment != null && comment.CommentType == RefParser.CommentType.Documentation) {
						doku = comment.CommentText;
						break;
					} else if (special is RefParser.PreprocessingDirective) {
						foundPreprocessing = true;
					}
				}
				if (doku == null && !foundPreprocessing)
					break;
				if (doku != null) {
					length += 2 + doku.Length;
					lines.Add(doku);
				}
			}
			StringBuilder b = new StringBuilder(length);
			for (int i = lines.Count - 1; i >= 0; --i) {
				b.AppendLine(lines[i]);
			}
			return b.ToString();
		}
		
		string GetDocumentationFromLine(int line)
		{
			foreach (RefParser.ISpecial special in GetSpecialsFromLine(line)) {
				RefParser.Comment comment = special as RefParser.Comment;
				if (comment != null && comment.CommentType == RefParser.CommentType.Documentation) {
					return comment.CommentText;
				}
			}
			return null;
		}
		
		IEnumerable<RefParser.ISpecial> GetSpecialsFromLine(int line)
		{
			List<RefParser.ISpecial> result = new List<RefParser.ISpecial>();
			if (specials == null) return result;
			if (line < 0) return result;
			// specials is a sorted list: use interpolation search
			int left = 0;
			int right = specials.Count - 1;
			int m;
			
			while (left <= right) {
				int leftLine  = specials[left].StartPosition.Y;
				if (line < leftLine)
					break;
				int rightLine = specials[right].StartPosition.Y;
				if (line > rightLine)
					break;
				if (leftLine == rightLine) {
					if (leftLine == line)
						m = left;
					else
						break;
				} else {
					m = (int)(left + Math.BigMul((line - leftLine), (right - left)) / (rightLine - leftLine));
				}
				
				int mLine = specials[m].StartPosition.Y;
				if (mLine < line) { // found line smaller than line we are looking for
					left = m + 1;
				} else if (mLine > line) {
					right = m - 1;
				} else {
					// correct line found,
					// look for first special in that line
					while (--m >= 0 && specials[m].StartPosition.Y == line);
					// look at all specials in that line: find doku-comment
					while (++m < specials.Count && specials[m].StartPosition.Y == line) {
						result.Add(specials[m]);
					}
					break;
				}
			}
			return result;
		}
		
		public override object VisitCompilationUnit(AST.CompilationUnit compilationUnit, object data)
		{
			if (compilationUnit == null) {
				return null;
			}
			currentNamespace = new DefaultUsingScope();
			if (!string.IsNullOrEmpty(VBRootNamespace)) {
				foreach (string name in VBRootNamespace.Split('.')) {
					currentNamespace = new DefaultUsingScope {
						Parent = currentNamespace,
						NamespaceName = PrependCurrentNamespace(name),
					};
					currentNamespace.Parent.ChildScopes.Add(currentNamespace);
				}
			}
			cu.UsingScope = currentNamespace;
			compilationUnit.AcceptChildren(this, data);
			return cu;
		}
		
		public override object VisitUsingDeclaration(AST.UsingDeclaration usingDeclaration, object data)
		{
			DefaultUsing us = new DefaultUsing(cu.ProjectContent, GetRegion(usingDeclaration.StartLocation, usingDeclaration.EndLocation));
			foreach (AST.Using u in usingDeclaration.Usings) {
				u.AcceptVisitor(this, us);
			}
			currentNamespace.Usings.Add(us);
			return data;
		}
		
		public override object VisitUsing(AST.Using u, object data)
		{
			Debug.Assert(data is DefaultUsing);
			DefaultUsing us = (DefaultUsing)data;
			if (u.IsAlias) {
				IReturnType rt = CreateReturnType(u.Alias);
				if (rt != null) {
					us.AddAlias(u.Name, rt);
				}
			} else {
				us.Usings.Add(u.Name);
			}
			return data;
		}
		
		public override object VisitOptionDeclaration(ICSharpCode.NRefactory.Ast.OptionDeclaration optionDeclaration, object data)
		{
			if (cu is VBNetCompilationUnit) {
				VBNetCompilationUnit provider = cu as VBNetCompilationUnit;
				
				switch (optionDeclaration.OptionType) {
					case ICSharpCode.NRefactory.Ast.OptionType.Explicit:
						provider.OptionExplicit = optionDeclaration.OptionValue;
						break;
					case ICSharpCode.NRefactory.Ast.OptionType.Strict:
						provider.OptionStrict = optionDeclaration.OptionValue;
						break;
					case ICSharpCode.NRefactory.Ast.OptionType.CompareBinary:
						provider.OptionCompare = CompareKind.Binary;
						break;
					case ICSharpCode.NRefactory.Ast.OptionType.CompareText:
						provider.OptionCompare = CompareKind.Text;
						break;
					case ICSharpCode.NRefactory.Ast.OptionType.Infer:
						provider.OptionInfer = optionDeclaration.OptionValue;
						break;
				}
				
				return null;
			}
			
			return base.VisitOptionDeclaration(optionDeclaration, data);
		}
		
		void ConvertAttributes(AST.AttributedNode from, AbstractEntity to)
		{
			if (from.Attributes.Count == 0) {
				to.Attributes = DefaultAttribute.EmptyAttributeList;
			} else {
				ICSharpCode.NRefactory.Location location = from.Attributes[0].StartLocation;
				ClassFinder context;
				if (to is IClass) {
					context = new ClassFinder((IClass)to, location.Line, location.Column);
				} else {
					context = new ClassFinder(to.DeclaringType, location.Line, location.Column);
				}
				to.Attributes = VisitAttributes(from.Attributes, context);
			}
		}
		
		List<IAttribute> VisitAttributes(IList<AST.AttributeSection> attributes, ClassFinder context)
		{
			// TODO Expressions???
			List<IAttribute> result = new List<IAttribute>();
			foreach (AST.AttributeSection section in attributes) {
				
				AttributeTarget target = AttributeTarget.None;
				if (section.AttributeTarget != null && section.AttributeTarget != "") {
					switch (section.AttributeTarget.ToUpperInvariant()) {
						case "ASSEMBLY":
							target = AttributeTarget.Assembly;
							break;
						case "FIELD":
							target = AttributeTarget.Field;
							break;
						case "EVENT":
							target = AttributeTarget.Event;
							break;
						case "METHOD":
							target = AttributeTarget.Method;
							break;
						case "MODULE":
							target = AttributeTarget.Module;
							break;
						case "PARAM":
							target = AttributeTarget.Param;
							break;
						case "PROPERTY":
							target = AttributeTarget.Property;
							break;
						case "RETURN":
							target = AttributeTarget.Return;
							break;
						case "TYPE":
							target = AttributeTarget.Type;
							break;
						default:
							target = AttributeTarget.None;
							break;
							
					}
				}
				
				foreach (AST.Attribute attribute in section.Attributes) {
					List<object> positionalArguments = new List<object>();
					foreach (AST.Expression positionalArgument in attribute.PositionalArguments) {
						positionalArguments.Add(ConvertAttributeArgument(positionalArgument));
					}
					Dictionary<string, object> namedArguments = new Dictionary<string, object>();
					foreach (AST.NamedArgumentExpression namedArgumentExpression in attribute.NamedArguments) {
						namedArguments.Add(namedArgumentExpression.Name, ConvertAttributeArgument(namedArgumentExpression.Expression));
					}
					result.Add(new DefaultAttribute(new AttributeReturnType(context, attribute.Name),
					                                target, positionalArguments, namedArguments)
					           {
					           	CompilationUnit = cu,
					           	Region = GetRegion(attribute.StartLocation, attribute.EndLocation)
					           });
				}
			}
			return result;
		}
		
		static object ConvertAttributeArgument(AST.Expression expression)
		{
			AST.PrimitiveExpression pe = expression as AST.PrimitiveExpression;
			if (pe != null)
				return pe.Value;
			else
				return null;
		}
		
		public override object VisitAttributeSection(ICSharpCode.NRefactory.Ast.AttributeSection attributeSection, object data)
		{
			if (GetCurrentClass() == null) {
				ClassFinder cf = new ClassFinder(new DefaultClass(cu, "DummyClass"), attributeSection.StartLocation.Line, attributeSection.StartLocation.Column);
				cu.Attributes.AddRange(VisitAttributes(new[] { attributeSection }, cf));
			}
			return null;
		}
		
		string PrependCurrentNamespace(string name)
		{
			if (string.IsNullOrEmpty(currentNamespace.NamespaceName))
				return name;
			else
				return currentNamespace.NamespaceName + "." + name;
		}
		
		public override object VisitNamespaceDeclaration(AST.NamespaceDeclaration namespaceDeclaration, object data)
		{
			DefaultUsingScope oldNamespace = currentNamespace;
			foreach (string name in namespaceDeclaration.Name.Split('.')) {
				currentNamespace = new DefaultUsingScope {
					Parent = currentNamespace,
					NamespaceName = PrependCurrentNamespace(name),
				};
				currentNamespace.Parent.ChildScopes.Add(currentNamespace);
			}
			object ret = namespaceDeclaration.AcceptChildren(this, data);
			currentNamespace = oldNamespace;
			return ret;
		}
		
		ClassType TranslateClassType(AST.ClassType type)
		{
			switch (type) {
				case AST.ClassType.Enum:
					return ClassType.Enum;
				case AST.ClassType.Interface:
					return ClassType.Interface;
				case AST.ClassType.Struct:
					return ClassType.Struct;
				case AST.ClassType.Module:
					return ClassType.Module;
				default:
					return ClassType.Class;
			}
		}
		
		static DomRegion GetRegion(RefParser.Location start, RefParser.Location end)
		{
			return DomRegion.FromLocation(start, end);
		}
		
		public override object VisitTypeDeclaration(AST.TypeDeclaration typeDeclaration, object data)
		{
			DomRegion region = GetRegion(typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(typeDeclaration.BodyStartLocation, typeDeclaration.EndLocation);
			
			DefaultClass c = new DefaultClass(cu, TranslateClassType(typeDeclaration.Type), ConvertTypeModifier(typeDeclaration.Modifier), region, GetCurrentClass());
			if (c.IsStatic) {
				// static classes are also abstract and sealed at the same time
				c.Modifiers |= ModifierEnum.Abstract | ModifierEnum.Sealed;
			}
			c.BodyRegion = bodyRegion;
			ConvertAttributes(typeDeclaration, c);
			c.Documentation = GetDocumentation(region.BeginLine, typeDeclaration.Attributes);
			
			DefaultClass outerClass = GetCurrentClass();
			if (outerClass != null) {
				outerClass.InnerClasses.Add(c);
				c.FullyQualifiedName = outerClass.FullyQualifiedName + '.' + typeDeclaration.Name;
			} else {
				c.FullyQualifiedName = PrependCurrentNamespace(typeDeclaration.Name);
				cu.Classes.Add(c);
			}
			c.UsingScope = currentNamespace;
			currentClass.Push(c);
			
			ConvertTemplates(outerClass, typeDeclaration.Templates, c); // resolve constrains in context of the class
			// templates must be converted before base types because base types may refer to generic types
			
			if (c.ClassType != ClassType.Enum && typeDeclaration.BaseTypes != null) {
				foreach (AST.TypeReference type in typeDeclaration.BaseTypes) {
					IReturnType rt = CreateReturnType(type, null, TypeVisitor.ReturnTypeOptions.BaseTypeReference);
					if (rt != null) {
						c.BaseTypes.Add(rt);
					}
				}
			}
			
			object ret = typeDeclaration.AcceptChildren(this, data);
			currentClass.Pop();
			
			if (c.ClassType == ClassType.Module) {
				foreach (DefaultField f in c.Fields) {
					f.Modifiers |= ModifierEnum.Static;
				}
				foreach (DefaultMethod m in c.Methods) {
					m.Modifiers |= ModifierEnum.Static;
				}
				foreach (DefaultProperty p in c.Properties) {
					p.Modifiers |= ModifierEnum.Static;
				}
				foreach (DefaultEvent e in c.Events) {
					e.Modifiers |= ModifierEnum.Static;
				}
			}
			
			return ret;
		}
		
		void ConvertTemplates(DefaultClass outerClass, IList<AST.TemplateDefinition> templateList, DefaultClass c)
		{
			int outerClassTypeParameterCount = outerClass != null ? outerClass.TypeParameters.Count : 0;
			if (templateList.Count == 0 && outerClassTypeParameterCount == 0) {
				c.TypeParameters = DefaultTypeParameter.EmptyTypeParameterList;
			} else {
				Debug.Assert(c.TypeParameters.Count == 0);
				
				int index = 0;
				if (outerClassTypeParameterCount > 0) {
					foreach (DefaultTypeParameter outerTypeParamter in outerClass.TypeParameters) {
						DefaultTypeParameter p = new DefaultTypeParameter(c, outerTypeParamter.Name, index++);
						p.HasConstructableConstraint = outerTypeParamter.HasConstructableConstraint;
						p.HasReferenceTypeConstraint = outerTypeParamter.HasReferenceTypeConstraint;
						p.HasValueTypeConstraint = outerTypeParamter.HasValueTypeConstraint;
						p.Attributes.AddRange(outerTypeParamter.Attributes);
						p.Constraints.AddRange(outerTypeParamter.Constraints);
						c.TypeParameters.Add(p);
					}
				}
				
				foreach (AST.TemplateDefinition template in templateList) {
					c.TypeParameters.Add(new DefaultTypeParameter(c, template.Name, index++));
				}
				// converting the constraints requires that the type parameters are already present
				for (int i = 0; i < templateList.Count; i++) {
					ConvertConstraints(templateList[i], (DefaultTypeParameter)c.TypeParameters[i + outerClassTypeParameterCount]);
				}
			}
		}
		
		void ConvertTemplates(List<AST.TemplateDefinition> templateList, DefaultMethod m)
		{
			int index = 0;
			if (templateList.Count == 0) {
				m.TypeParameters = DefaultTypeParameter.EmptyTypeParameterList;
			} else {
				Debug.Assert(m.TypeParameters.Count == 0);
				foreach (AST.TemplateDefinition template in templateList) {
					m.TypeParameters.Add(new DefaultTypeParameter(m, template.Name, index++));
				}
				// converting the constraints requires that the type parameters are already present
				for (int i = 0; i < templateList.Count; i++) {
					ConvertConstraints(templateList[i], (DefaultTypeParameter)m.TypeParameters[i]);
				}
			}
		}
		
		void ConvertConstraints(AST.TemplateDefinition template, DefaultTypeParameter typeParameter)
		{
			foreach (AST.TypeReference typeRef in template.Bases) {
				if (typeRef == AST.TypeReference.NewConstraint) {
					typeParameter.HasConstructableConstraint = true;
				} else if (typeRef == AST.TypeReference.ClassConstraint) {
					typeParameter.HasReferenceTypeConstraint = true;
				} else if (typeRef == AST.TypeReference.StructConstraint) {
					typeParameter.HasValueTypeConstraint = true;
				} else {
					IReturnType rt = CreateReturnType(typeRef, typeParameter.Method, TypeVisitor.ReturnTypeOptions.None);
					if (rt != null) {
						typeParameter.Constraints.Add(rt);
					}
				}
			}
		}
		
		public override object VisitDelegateDeclaration(AST.DelegateDeclaration delegateDeclaration, object data)
		{
			DomRegion region = GetRegion(delegateDeclaration.StartLocation, delegateDeclaration.EndLocation);
			DefaultClass c = new DefaultClass(cu, ClassType.Delegate, ConvertTypeModifier(delegateDeclaration.Modifier), region, GetCurrentClass());
			c.Documentation = GetDocumentation(region.BeginLine, delegateDeclaration.Attributes);
			ConvertAttributes(delegateDeclaration, c);
			CreateDelegate(c, delegateDeclaration.Name, delegateDeclaration.ReturnType,
			               delegateDeclaration.Templates, delegateDeclaration.Parameters);
			return c;
		}
		
		void CreateDelegate(DefaultClass c, string name, AST.TypeReference returnType, IList<AST.TemplateDefinition> templates, IList<AST.ParameterDeclarationExpression> parameters)
		{
			c.BaseTypes.Add(c.ProjectContent.SystemTypes.MulticastDelegate);
			DefaultClass outerClass = GetCurrentClass();
			if (outerClass != null) {
				outerClass.InnerClasses.Add(c);
				c.FullyQualifiedName = outerClass.FullyQualifiedName + '.' + name;
			} else {
				c.FullyQualifiedName = PrependCurrentNamespace(name);
				cu.Classes.Add(c);
			}
			c.UsingScope = currentNamespace;
			currentClass.Push(c); // necessary for CreateReturnType
			ConvertTemplates(outerClass, templates, c);
			
			List<IParameter> p = new List<IParameter>();
			if (parameters != null) {
				foreach (AST.ParameterDeclarationExpression param in parameters) {
					p.Add(CreateParameter(param));
				}
			}
			AnonymousMethodReturnType.AddDefaultDelegateMethod(c, CreateReturnType(returnType), p);
			
			currentClass.Pop();
		}
		
		IParameter CreateParameter(AST.ParameterDeclarationExpression par)
		{
			return CreateParameter(par, null);
		}
		
		IParameter CreateParameter(AST.ParameterDeclarationExpression par, IMethod method)
		{
			return CreateParameter(par, method, GetCurrentClass(), cu);
		}
		
		internal static IParameter CreateParameter(AST.ParameterDeclarationExpression par, IMethod method, IClass currentClass, ICompilationUnit cu)
		{
			IReturnType parType = CreateReturnType(par.TypeReference, method, currentClass, cu, TypeVisitor.ReturnTypeOptions.None);
			DefaultParameter p = new DefaultParameter(par.ParameterName, parType, GetRegion(par.StartLocation, par.EndLocation));
			p.Modifiers = (ParameterModifiers)par.ParamModifier;
			return p;
		}
		
		public override object VisitMethodDeclaration(AST.MethodDeclaration methodDeclaration, object data)
		{
			DomRegion region     = GetRegion(methodDeclaration.StartLocation, methodDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(methodDeclaration.EndLocation, methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : RefParser.Location.Empty);
			DefaultClass currentClass = GetCurrentClass();
			
			DefaultMethod method = new DefaultMethod(methodDeclaration.Name, null, ConvertModifier(methodDeclaration.Modifier), region, bodyRegion, currentClass);
			method.Documentation = GetDocumentation(region.BeginLine, methodDeclaration.Attributes);
			ConvertTemplates(methodDeclaration.Templates, method);
			method.ReturnType = CreateReturnType(methodDeclaration.TypeReference, method, TypeVisitor.ReturnTypeOptions.None);
			ConvertAttributes(methodDeclaration, method);
			method.IsExtensionMethod = methodDeclaration.IsExtensionMethod
				|| method.Attributes.Any(att => att.AttributeType != null && att.AttributeType.FullyQualifiedName == "System.Runtime.CompilerServices.ExtensionAttribute");
			if (methodDeclaration.Parameters.Count > 0) {
				foreach (AST.ParameterDeclarationExpression par in methodDeclaration.Parameters) {
					method.Parameters.Add(CreateParameter(par, method));
				}
			} else {
				method.Parameters = DefaultParameter.EmptyParameterList;
			}
			if (methodDeclaration.HandlesClause.Count > 0) {
				foreach (string handlesClause in methodDeclaration.HandlesClause) {
					if (handlesClause.ToLowerInvariant().StartsWith("me."))
						method.HandlesClauses.Add(handlesClause.Substring(3));
					else if (handlesClause.ToLowerInvariant().StartsWith("mybase."))
						method.HandlesClauses.Add(handlesClause.Substring(7));
					else
						method.HandlesClauses.Add(handlesClause);
				}
			} else {
				method.HandlesClauses = EmptyList<string>.Instance;
			}

			AddInterfaceImplementations(method, methodDeclaration);
			
			currentClass.Methods.Add(method);
			return null;
		}
		
		public override object VisitDeclareDeclaration(AST.DeclareDeclaration declareDeclaration, object data)
		{
			DefaultClass currentClass = GetCurrentClass();
			
			DomRegion region = GetRegion(declareDeclaration.StartLocation, declareDeclaration.EndLocation);
			DefaultMethod method = new DefaultMethod(declareDeclaration.Name, null, ConvertModifier(declareDeclaration.Modifier), region, DomRegion.Empty, currentClass);
			method.Documentation = GetDocumentation(region.BeginLine, declareDeclaration.Attributes);
			method.Modifiers |= ModifierEnum.Extern | ModifierEnum.Static;
			
			method.ReturnType = CreateReturnType(declareDeclaration.TypeReference, method, TypeVisitor.ReturnTypeOptions.None);
			ConvertAttributes(declareDeclaration, method);
			
			foreach (AST.ParameterDeclarationExpression par in declareDeclaration.Parameters) {
				method.Parameters.Add(CreateParameter(par, method));
			}
			
			currentClass.Methods.Add(method);
			return null;
		}
		
		public override object VisitOperatorDeclaration(AST.OperatorDeclaration operatorDeclaration, object data)
		{
			DefaultClass c  = GetCurrentClass();
			DomRegion region     = GetRegion(operatorDeclaration.StartLocation, operatorDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(operatorDeclaration.EndLocation, operatorDeclaration.Body != null ? operatorDeclaration.Body.EndLocation : RefParser.Location.Empty);
			
			DefaultMethod method = new DefaultMethod(operatorDeclaration.Name, CreateReturnType(operatorDeclaration.TypeReference), ConvertModifier(operatorDeclaration.Modifier), region, bodyRegion, c);
			method.Documentation = GetDocumentation(region.BeginLine, operatorDeclaration.Attributes);
			ConvertAttributes(operatorDeclaration, method);
			if(operatorDeclaration.Parameters != null)
			{
				foreach (AST.ParameterDeclarationExpression par in operatorDeclaration.Parameters) {
					method.Parameters.Add(CreateParameter(par, method));
				}
			}
			AddInterfaceImplementations(method, operatorDeclaration);
			c.Methods.Add(method);
			return null;
		}
		
		public override object VisitConstructorDeclaration(AST.ConstructorDeclaration constructorDeclaration, object data)
		{
			DomRegion region     = GetRegion(constructorDeclaration.StartLocation, constructorDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : RefParser.Location.Empty);
			DefaultClass c = GetCurrentClass();
			
			Constructor constructor = new Constructor(ConvertModifier(constructorDeclaration.Modifier), region, bodyRegion, GetCurrentClass());
			constructor.Documentation = GetDocumentation(region.BeginLine, constructorDeclaration.Attributes);
			ConvertAttributes(constructorDeclaration, constructor);
			if (constructorDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in constructorDeclaration.Parameters) {
					constructor.Parameters.Add(CreateParameter(par));
				}
			}
			
			if (constructor.Modifiers.HasFlag(ModifierEnum.Static))
				constructor.Modifiers = ConvertModifier(constructorDeclaration.Modifier, ModifierEnum.None);

			c.Methods.Add(constructor);
			return null;
		}
		
		public override object VisitDestructorDeclaration(AST.DestructorDeclaration destructorDeclaration, object data)
		{
			DomRegion region     = GetRegion(destructorDeclaration.StartLocation, destructorDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : RefParser.Location.Empty);
			
			DefaultClass c = GetCurrentClass();
			
			Destructor destructor = new Destructor(region, bodyRegion, c);
			ConvertAttributes(destructorDeclaration, destructor);
			c.Methods.Add(destructor);
			return null;
		}
		
		bool IsVisualBasic {
			get {
				return cu.ProjectContent.Language == LanguageProperties.VBNet;
			}
		}
		
		public override object VisitFieldDeclaration(AST.FieldDeclaration fieldDeclaration, object data)
		{
			DomRegion region = GetRegion(fieldDeclaration.StartLocation, fieldDeclaration.EndLocation);
			DefaultClass c = GetCurrentClass();
			ModifierEnum modifier = ConvertModifier(fieldDeclaration.Modifier,
			                                        (c.ClassType == ClassType.Struct && this.IsVisualBasic)
			                                        ? ModifierEnum.Public : ModifierEnum.Private);
			string doku = GetDocumentation(region.BeginLine, fieldDeclaration.Attributes);
			if (currentClass.Count > 0) {
				for (int i = 0; i < fieldDeclaration.Fields.Count; ++i) {
					AST.VariableDeclaration field = (AST.VariableDeclaration)fieldDeclaration.Fields[i];
					
					IReturnType retType;
					if (c.ClassType == ClassType.Enum) {
						retType = c.DefaultReturnType;
					} else {
						retType = CreateReturnType(fieldDeclaration.GetTypeForField(i));
						if (!field.FixedArrayInitialization.IsNull)
							retType = new ArrayReturnType(cu.ProjectContent, retType, 1);
					}
					DefaultField f = new DefaultField(retType, field.Name, modifier, region, c);
					ConvertAttributes(fieldDeclaration, f);
					f.Documentation = doku;
					if (c.ClassType == ClassType.Enum) {
						f.Modifiers = ModifierEnum.Const | ModifierEnum.Public;
					}
					
					c.Fields.Add(f);
				}
			}
			return null;
		}
		
		public override object VisitPropertyDeclaration(AST.PropertyDeclaration propertyDeclaration, object data)
		{
			DomRegion region     = GetRegion(propertyDeclaration.StartLocation, propertyDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(propertyDeclaration.BodyStart,     propertyDeclaration.BodyEnd);
			
			IReturnType type = CreateReturnType(propertyDeclaration.TypeReference);
			DefaultClass c = GetCurrentClass();
			
			DefaultProperty property = new DefaultProperty(propertyDeclaration.Name, type, ConvertModifier(propertyDeclaration.Modifier), region, bodyRegion, GetCurrentClass());
			if (propertyDeclaration.HasGetRegion) {
				property.GetterRegion = GetRegion(propertyDeclaration.GetRegion.StartLocation, propertyDeclaration.GetRegion.EndLocation);
				property.CanGet = true;
				property.GetterModifiers = ConvertModifier(propertyDeclaration.GetRegion.Modifier, ModifierEnum.None);
			}
			if (propertyDeclaration.HasSetRegion) {
				property.SetterRegion = GetRegion(propertyDeclaration.SetRegion.StartLocation, propertyDeclaration.SetRegion.EndLocation);
				property.CanSet = true;
				property.SetterModifiers = ConvertModifier(propertyDeclaration.SetRegion.Modifier, ModifierEnum.None);
			}
			property.Documentation = GetDocumentation(region.BeginLine, propertyDeclaration.Attributes);
			ConvertAttributes(propertyDeclaration, property);
			
			property.IsIndexer = propertyDeclaration.IsIndexer;
			
			if (propertyDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in propertyDeclaration.Parameters) {
					property.Parameters.Add(CreateParameter(par));
				}
			}
			// If an IndexerNameAttribute is specified, use the specified name
			// for the indexer instead of the default name.
			IAttribute indexerNameAttribute = property.Attributes.LastOrDefault(this.IsIndexerNameAttribute);
			if (indexerNameAttribute != null && indexerNameAttribute.PositionalArguments.Count > 0) {
				string name = indexerNameAttribute.PositionalArguments[0] as string;
				if (!String.IsNullOrEmpty(name)) {
					property.FullyQualifiedName = String.Concat(property.DeclaringType.FullyQualifiedName, ".", name);
				}
			}
			
			AddInterfaceImplementations(property, propertyDeclaration);
			c.Properties.Add(property);
			return null;
		}
		
		bool IsIndexerNameAttribute(IAttribute att)
		{
			if (att == null || att.AttributeType == null)
				return false;
			string indexerNameAttributeFullName = typeof(System.Runtime.CompilerServices.IndexerNameAttribute).FullName;
			IClass indexerNameAttributeClass = this.Cu.ProjectContent.GetClass(indexerNameAttributeFullName, 0, LanguageProperties.CSharp, GetClassOptions.Default | GetClassOptions.ExactMatch);
			if (indexerNameAttributeClass == null) {
				return String.Equals(att.AttributeType.FullyQualifiedName, indexerNameAttributeFullName, StringComparison.Ordinal);
			}
			return att.AttributeType.Equals(indexerNameAttributeClass.DefaultReturnType);
		}
		
		public override object VisitEventDeclaration(AST.EventDeclaration eventDeclaration, object data)
		{
			DomRegion region     = GetRegion(eventDeclaration.StartLocation, eventDeclaration.EndLocation);
			DomRegion bodyRegion = GetRegion(eventDeclaration.BodyStart,     eventDeclaration.BodyEnd);
			DefaultClass c = GetCurrentClass();
			
			IReturnType type;
			if (eventDeclaration.TypeReference.IsNull) {
				DefaultClass del = new DefaultClass(cu, ClassType.Delegate,
				                                    ConvertModifier(eventDeclaration.Modifier),
				                                    region, c);
				del.Modifiers |= ModifierEnum.Synthetic;
				CreateDelegate(del, eventDeclaration.Name + "EventHandler",
				               new AST.TypeReference("System.Void", true),
				               new AST.TemplateDefinition[0],
				               eventDeclaration.Parameters);
				type = del.DefaultReturnType;
			} else {
				type = CreateReturnType(eventDeclaration.TypeReference);
			}
			DefaultEvent e = new DefaultEvent(eventDeclaration.Name, type, ConvertModifier(eventDeclaration.Modifier), region, bodyRegion, c);
			ConvertAttributes(eventDeclaration, e);
			AddInterfaceImplementations(e, eventDeclaration);
			c.Events.Add(e);
			
			e.Documentation = GetDocumentation(region.BeginLine, eventDeclaration.Attributes);
			if (eventDeclaration.HasAddRegion) {
				e.AddMethod = new DefaultMethod(e.DeclaringType, "add_" + e.Name) {
					Parameters = { new DefaultParameter("value", e.ReturnType, DomRegion.Empty) },
					Region = GetRegion(eventDeclaration.AddRegion.StartLocation, eventDeclaration.AddRegion.EndLocation),
					BodyRegion = GetRegion(eventDeclaration.AddRegion.Block.StartLocation, eventDeclaration.AddRegion.Block.EndLocation)
				};
			}
			if (eventDeclaration.HasRemoveRegion) {
				e.RemoveMethod = new DefaultMethod(e.DeclaringType, "remove_" + e.Name) {
					Parameters = { new DefaultParameter("value", e.ReturnType, DomRegion.Empty) },
					Region = GetRegion(eventDeclaration.RemoveRegion.StartLocation, eventDeclaration.RemoveRegion.EndLocation),
					BodyRegion = GetRegion(eventDeclaration.RemoveRegion.Block.StartLocation, eventDeclaration.RemoveRegion.Block.EndLocation)
				};
			}
			return null;
		}

		void AddInterfaceImplementations(AbstractMember member, AST.MemberNode memberNode)
		{
			member.InterfaceImplementations.AddRange(
				memberNode.InterfaceImplementations
				.Select(x => new ExplicitInterfaceImplementation(CreateReturnType(x.InterfaceType), x.MemberName))
			);
			if (!IsVisualBasic && member.InterfaceImplementations.Any()) {
				member.Modifiers = ConvertModifier(memberNode.Modifier, ModifierEnum.None);
			}
		}
		
		IReturnType CreateReturnType(AST.TypeReference reference, IMethod method, TypeVisitor.ReturnTypeOptions options)
		{
			return CreateReturnType(reference, method, GetCurrentClass(), cu, options);
		}
		
		static IReturnType CreateReturnType(AST.TypeReference reference, IMethod method, IClass currentClass, ICompilationUnit cu, TypeVisitor.ReturnTypeOptions options)
		{
			if (currentClass == null) {
				return TypeVisitor.CreateReturnType(reference, new DefaultClass(cu, "___DummyClass"), method, 1, 1, cu.ProjectContent, options | TypeVisitor.ReturnTypeOptions.Lazy);
			} else {
				return TypeVisitor.CreateReturnType(reference, currentClass, method, currentClass.Region.BeginLine + 1, 1, cu.ProjectContent, options | TypeVisitor.ReturnTypeOptions.Lazy);
			}
		}
		
		IReturnType CreateReturnType(AST.TypeReference reference)
		{
			return CreateReturnType(reference, null, TypeVisitor.ReturnTypeOptions.None);
		}
	}
}
