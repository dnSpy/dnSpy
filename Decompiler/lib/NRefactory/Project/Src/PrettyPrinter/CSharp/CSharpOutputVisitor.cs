// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public sealed class CSharpOutputVisitor : NodeTrackingAstVisitor, IOutputAstVisitor
	{
		Errors                errors             = new Errors();
		CSharpOutputFormatter outputFormatter;
		PrettyPrintOptions    prettyPrintOptions = new PrettyPrintOptions();
		
		public string Text {
			get {
				return outputFormatter.Text;
			}
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		AbstractPrettyPrintOptions IOutputAstVisitor.Options {
			get { return prettyPrintOptions; }
		}
		
		public PrettyPrintOptions Options {
			get { return prettyPrintOptions; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				prettyPrintOptions = value;
			}
		}
		
		public IOutputFormatter OutputFormatter {
			get {
				return outputFormatter;
			}
		}
		
		public CSharpOutputVisitor()
		{
			outputFormatter = new CSharpOutputFormatter(prettyPrintOptions);
		}
		
		public event Action<INode> BeforeNodeVisit;
		public event Action<INode> AfterNodeVisit;
		
		protected override void BeginVisit(INode node)
		{
			if (BeforeNodeVisit != null) {
				BeforeNodeVisit(node);
			}
			base.BeginVisit(node);
		}
		
		protected override void EndVisit(INode node)
		{
			base.EndVisit(node);
			if (AfterNodeVisit != null) {
				AfterNodeVisit(node);
			}
		}
		
		void Error(INode node, string message)
		{
			outputFormatter.PrintText(" // ERROR: " + message + Environment.NewLine);
			errors.Error(node.StartLocation.Line, node.StartLocation.Column, message);
		}
		
		void NotSupported(INode node)
		{
			Error(node, "Not supported in C#: " + node.GetType().Name);
		}
		
		#region ICSharpCode.NRefactory.Parser.IASTVisitor interface implementation
		public override object TrackedVisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			compilationUnit.AcceptChildren(this, data);
			outputFormatter.EndFile();
			return null;
		}
		
		/// <summary>
		/// Converts type name to primitive type name. Returns typeString if typeString is not
		/// a primitive type.
		/// </summary>
		static string ConvertTypeString(string typeString)
		{
			string primitiveType;
			if (TypeReference.PrimitiveTypesCSharpReverse.TryGetValue(typeString, out primitiveType))
				return primitiveType;
			else
				return typeString;
		}
		
		void PrintTemplates(List<TemplateDefinition> templates)
		{
			if (templates.Count == 0) return;
			outputFormatter.PrintToken(Tokens.LessThan);
			for (int i = 0; i < templates.Count; i++) {
				if (i > 0) PrintFormattedComma();
				if (templates[i].VarianceModifier == VarianceModifier.Contravariant) {
					outputFormatter.PrintToken(Tokens.In);
					outputFormatter.Space();
				} else if (templates[i].VarianceModifier == VarianceModifier.Covariant) {
					outputFormatter.PrintToken(Tokens.Out);
					outputFormatter.Space();
				}
				outputFormatter.PrintIdentifier(templates[i].Name);
			}
			outputFormatter.PrintToken(Tokens.GreaterThan);
		}
		
		public override object TrackedVisitTypeReference(TypeReference typeReference, object data)
		{
			if (typeReference == TypeReference.ClassConstraint) {
				outputFormatter.PrintToken(Tokens.Class);
			} else if (typeReference == TypeReference.StructConstraint) {
				outputFormatter.PrintToken(Tokens.Struct);
			} else if (typeReference == TypeReference.NewConstraint) {
				outputFormatter.PrintToken(Tokens.New);
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			} else {
				PrintTypeReferenceWithoutArray(typeReference);
				if (typeReference.IsArrayType) {
					PrintArrayRank(typeReference.RankSpecifier, 0);
				}
			}
			return null;
		}
		
		void PrintArrayRank(int[] rankSpecifier, int startRankIndex)
		{
			for (int i = startRankIndex; i < rankSpecifier.Length; ++i) {
				outputFormatter.PrintToken(Tokens.OpenSquareBracket);
				bool outputSpace = this.prettyPrintOptions.SpacesWithinBrackets && rankSpecifier[i] > 0;
				if (outputSpace) {
					outputFormatter.Space();
				}
				for (int j = 0; j < rankSpecifier[i]; ++j) {
					outputFormatter.PrintToken(Tokens.Comma);
				}
				if (outputSpace) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			}
		}
		
		void PrintTypeReferenceWithoutArray(TypeReference typeReference)
		{
			if (typeReference.IsGlobal) {
				outputFormatter.PrintText("global::");
			}
			bool printGenerics = true;
			if (typeReference.IsKeyword) {
				if (typeReference.Type == "System.Nullable"
				    && typeReference.GenericTypes != null
				    && typeReference.GenericTypes.Count == 1)
				{
					TrackVisit(typeReference.GenericTypes[0], null);
					outputFormatter.PrintText("?");
					printGenerics = false;
				} else {
					outputFormatter.PrintText(ConvertTypeString(typeReference.Type));
				}
			} else {
				outputFormatter.PrintIdentifier(typeReference.Type);
			}
			if (printGenerics && typeReference.GenericTypes != null && typeReference.GenericTypes.Count > 0) {
				outputFormatter.PrintToken(Tokens.LessThan);
				AppendCommaSeparatedList(typeReference.GenericTypes);
				outputFormatter.PrintToken(Tokens.GreaterThan);
			}
			for (int i = 0; i < typeReference.PointerNestingLevel; ++i) {
				outputFormatter.PrintToken(Tokens.Times);
			}
		}
		
		public override object TrackedVisitInnerClassTypeReference(InnerClassTypeReference innerClassTypeReference, object data)
		{
			TrackVisit(innerClassTypeReference.BaseType, data);
			outputFormatter.PrintToken(Tokens.Dot);
			return VisitTypeReference((TypeReference)innerClassTypeReference, data);
		}
		
		#region Global scope
		void VisitAttributes(ICollection attributes, object data)
		{
			if (attributes == null || attributes.Count <= 0) {
				return;
			}
			foreach (AttributeSection section in attributes) {
				TrackVisit(section, data);
			}
		}
		void PrintFormattedComma()
		{
			if (this.prettyPrintOptions.SpacesBeforeComma) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Comma);
			if (this.prettyPrintOptions.SpacesAfterComma) {
				outputFormatter.Space();
			}
		}
		void PrintFormattedCommaAndNewLine()
		{
			if (this.prettyPrintOptions.SpacesBeforeComma) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Comma);
			outputFormatter.NewLine();
			outputFormatter.Indent();
		}
		
		public override object TrackedVisitAttributeSection(AttributeSection attributeSection, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			if (this.prettyPrintOptions.SpacesWithinBrackets) {
				outputFormatter.Space();
			}
			if (!string.IsNullOrEmpty(attributeSection.AttributeTarget)) {
				outputFormatter.PrintText(attributeSection.AttributeTarget);
				outputFormatter.PrintToken(Tokens.Colon);
				outputFormatter.Space();
			}
			Debug.Assert(attributeSection.Attributes != null);
			for (int j = 0; j < attributeSection.Attributes.Count; ++j) {
				TrackVisit((INode)attributeSection.Attributes[j], data);
				if (j + 1 < attributeSection.Attributes.Count) {
					PrintFormattedComma();
				}
			}
			if (this.prettyPrintOptions.SpacesWithinBrackets) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object TrackedVisitAttribute(ICSharpCode.NRefactory.Ast.Attribute attribute, object data)
		{
			outputFormatter.PrintIdentifier(attribute.Name);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinMethodCallParentheses) {
				outputFormatter.Space();
			}
			this.AppendCommaSeparatedList(attribute.PositionalArguments);
			
			if (attribute.NamedArguments != null && attribute.NamedArguments.Count > 0) {
				if (attribute.PositionalArguments.Count > 0) {
					PrintFormattedComma();
				}
				for (int i = 0; i < attribute.NamedArguments.Count; ++i) {
					NamedArgumentExpression nae = attribute.NamedArguments[i];
					outputFormatter.PrintIdentifier(nae.Name);
					if (prettyPrintOptions.AroundAssignmentParentheses)
						outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Assign);
					if (prettyPrintOptions.AroundAssignmentParentheses)
						outputFormatter.Space();
					nae.Expression.AcceptVisitor(this, data);
					if (i + 1 < attribute.NamedArguments.Count) {
						PrintFormattedComma();
					}
				}
			}
			if (this.prettyPrintOptions.WithinMethodCallParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			outputFormatter.PrintIdentifier(namedArgumentExpression.Name);
			outputFormatter.PrintToken(Tokens.Colon);
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(namedArgumentExpression.Expression, data);
			return null;
		}
		
		public override object TrackedVisitUsing(Using @using, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Using);
			outputFormatter.Space();
			
			outputFormatter.PrintIdentifier(@using.Name);
			
			if (@using.IsAlias) {
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Assign);
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				TrackVisit(@using.Alias, data);
			}
			
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object TrackedVisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			foreach (Using u in usingDeclaration.Usings) {
				TrackVisit(u, data);
			}
			return null;
		}
		
		public override object TrackedVisitExternAliasDirective(ExternAliasDirective externAliasDirective, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintText("extern alias ");
			outputFormatter.PrintIdentifier(externAliasDirective.Name);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object TrackedVisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Namespace);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(namespaceDeclaration.Name);
			
			outputFormatter.BeginBrace(this.prettyPrintOptions.NamespaceBraceStyle, this.prettyPrintOptions.IndentNamespaceBody);
			
			namespaceDeclaration.AcceptChildren(this, data);
			
			outputFormatter.EndBrace(this.prettyPrintOptions.IndentNamespaceBody);
			
			return null;
		}
		
		
		void OutputEnumMembers(TypeDeclaration typeDeclaration, object data)
		{
			for (int i = 0; i < typeDeclaration.Children.Count; i++) {
				FieldDeclaration fieldDeclaration = typeDeclaration.Children[i] as FieldDeclaration;
				if (fieldDeclaration == null) {
					// not a field?
					TrackVisit(typeDeclaration.Children[i], data);
					continue;
				}
				BeginVisit(fieldDeclaration);
				VariableDeclaration f = (VariableDeclaration)fieldDeclaration.Fields[0];
				VisitAttributes(fieldDeclaration.Attributes, data);
				outputFormatter.Indent();
				outputFormatter.PrintIdentifier(f.Name);
				if (f.Initializer != null && !f.Initializer.IsNull) {
					if (this.prettyPrintOptions.AroundAssignmentParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Assign);
					if (this.prettyPrintOptions.AroundAssignmentParentheses) {
						outputFormatter.Space();
					}
					TrackVisit(f.Initializer, data);
				}
				if (i < typeDeclaration.Children.Count - 1) {
					outputFormatter.PrintToken(Tokens.Comma);
				}
				outputFormatter.NewLine();
				EndVisit(fieldDeclaration);
			}
		}
		
		TypeDeclaration currentType = null;
		
		public override object TrackedVisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			VisitAttributes(typeDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(typeDeclaration.Modifier);
			switch (typeDeclaration.Type) {
				case ClassType.Enum:
					outputFormatter.PrintToken(Tokens.Enum);
					break;
				case ClassType.Interface:
					outputFormatter.PrintToken(Tokens.Interface);
					break;
				case ClassType.Struct:
					outputFormatter.PrintToken(Tokens.Struct);
					break;
				default:
					outputFormatter.PrintToken(Tokens.Class);
					break;
			}
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(typeDeclaration.Name);
			
			PrintTemplates(typeDeclaration.Templates);
			
			if (typeDeclaration.BaseTypes != null && typeDeclaration.BaseTypes.Count > 0) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Colon);
				outputFormatter.Space();
				for (int i = 0; i < typeDeclaration.BaseTypes.Count; ++i) {
					if (i > 0) {
						PrintFormattedComma();
					}
					TrackVisit(typeDeclaration.BaseTypes[i], data);
				}
			}
			
			foreach (TemplateDefinition templateDefinition in typeDeclaration.Templates) {
				TrackVisit(templateDefinition, data);
			}
			
			switch (typeDeclaration.Type) {
				case ClassType.Enum:
					outputFormatter.BeginBrace(this.prettyPrintOptions.EnumBraceStyle, this.prettyPrintOptions.IndentEnumBody);
					break;
				case ClassType.Interface:
					outputFormatter.BeginBrace(this.prettyPrintOptions.InterfaceBraceStyle, this.prettyPrintOptions.IndentInterfaceBody);
					break;
				case ClassType.Struct:
					outputFormatter.BeginBrace(this.prettyPrintOptions.StructBraceStyle, this.prettyPrintOptions.IndentStructBody);
					break;
				default:
					outputFormatter.BeginBrace(this.prettyPrintOptions.ClassBraceStyle, this.prettyPrintOptions.IndentClassBody);
					break;
			}
			
			TypeDeclaration oldType = currentType;
			currentType = typeDeclaration;
			if (typeDeclaration.Type == ClassType.Enum) {
				OutputEnumMembers(typeDeclaration, data);
			} else {
				typeDeclaration.AcceptChildren(this, data);
			}
			currentType = oldType;
			switch (typeDeclaration.Type) {
				case ClassType.Enum:
					outputFormatter.EndBrace(this.prettyPrintOptions.IndentEnumBody);
					break;
				case ClassType.Interface:
					outputFormatter.EndBrace(this.prettyPrintOptions.IndentInterfaceBody);
					break;
				case ClassType.Struct:
					outputFormatter.EndBrace(this.prettyPrintOptions.IndentStructBody);
					break;
				default:
					outputFormatter.EndBrace(this.prettyPrintOptions.IndentCaseBody);
					break;
			}
			
			return null;
		}
		
		public override object TrackedVisitTemplateDefinition(TemplateDefinition templateDefinition, object data)
		{
			if (templateDefinition.Bases.Count == 0)
				return null;
			
			outputFormatter.Space();
			outputFormatter.PrintText("where");
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(templateDefinition.Name);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Colon);
			outputFormatter.Space();
			
			for (int i = 0; i < templateDefinition.Bases.Count; ++i) {
				TrackVisit(templateDefinition.Bases[i], data);
				if (i + 1 < templateDefinition.Bases.Count) {
					PrintFormattedComma();
				}
			}
			return null;
		}
		
		public override object TrackedVisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			VisitAttributes(delegateDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(delegateDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Delegate);
			outputFormatter.Space();
			TrackVisit(delegateDeclaration.ReturnType, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(delegateDeclaration.Name);
			PrintTemplates(delegateDeclaration.Templates);
			if (prettyPrintOptions.BeforeDelegateDeclarationParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			bool withinParentheses = this.prettyPrintOptions.WithinMethodDeclarationParentheses && delegateDeclaration.Parameters.Any ();
			if (withinParentheses) {
				outputFormatter.Space();
			}
			AppendCommaSeparatedList(delegateDeclaration.Parameters);
			if (withinParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			foreach (TemplateDefinition templateDefinition in delegateDeclaration.Templates) {
				TrackVisit(templateDefinition, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object TrackedVisitOptionDeclaration(OptionDeclaration optionDeclaration, object data)
		{
			if (((optionDeclaration.OptionType == OptionType.Explicit || optionDeclaration.OptionType == OptionType.Strict)
			     && optionDeclaration.OptionValue == true) || optionDeclaration.OptionType == OptionType.Infer)
			{
				// Explicit On/Strict On is what C# does, do not report an error
			} else {
				NotSupported(optionDeclaration);
			}
			return null;
		}
		#endregion
		
		#region Type level
		public override object TrackedVisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			if (!fieldDeclaration.TypeReference.IsNull) {
				VisitAttributes(fieldDeclaration.Attributes, data);
				outputFormatter.Indent();
				OutputModifier(fieldDeclaration.Modifier);
				TrackVisit(fieldDeclaration.TypeReference, data);
				outputFormatter.Space();
				AppendCommaSeparatedList(fieldDeclaration.Fields);
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				for (int i = 0; i < fieldDeclaration.Fields.Count; i++) {
					VisitAttributes(fieldDeclaration.Attributes, data);
					outputFormatter.Indent();
					OutputModifier(fieldDeclaration.Modifier);
					TrackVisit(fieldDeclaration.GetTypeForField(i), data);
					outputFormatter.Space();
					TrackVisit(fieldDeclaration.Fields[i], data);
					outputFormatter.PrintToken(Tokens.Semicolon);
					outputFormatter.NewLine();
				}
			}
			return null;
		}
		
		public override object TrackedVisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			outputFormatter.PrintIdentifier(variableDeclaration.Name);
			if (!variableDeclaration.FixedArrayInitialization.IsNull) {
				outputFormatter.PrintToken(Tokens.OpenSquareBracket);
				TrackVisit(variableDeclaration.FixedArrayInitialization, data);
				outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			}
			if (!variableDeclaration.Initializer.IsNull) {
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Assign);
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				TrackVisit(variableDeclaration.Initializer, data);
			}
			return null;
		}
		
		public override object TrackedVisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			VisitAttributes(propertyDeclaration.Attributes, data);
			outputFormatter.Indent();
			propertyDeclaration.Modifier &= ~Modifiers.ReadOnly;
			OutputModifier(propertyDeclaration.Modifier);
			TrackVisit(propertyDeclaration.TypeReference, data);
			outputFormatter.Space();
			if (propertyDeclaration.InterfaceImplementations.Count > 0) {
				TrackVisit(propertyDeclaration.InterfaceImplementations[0].InterfaceType, data);
				outputFormatter.PrintToken(Tokens.Dot);
			}
			if (propertyDeclaration.IsIndexer) {
				outputFormatter.PrintToken(Tokens.This);
				
				outputFormatter.PrintToken(Tokens.OpenSquareBracket);
				if (this.prettyPrintOptions.SpacesWithinBrackets) {
					outputFormatter.Space();
				}
				AppendCommaSeparatedList(propertyDeclaration.Parameters);
				if (this.prettyPrintOptions.SpacesWithinBrackets) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			} else
				outputFormatter.PrintIdentifier(propertyDeclaration.Name);
			
			OutputGetAndSetRegion(propertyDeclaration.GetRegion, propertyDeclaration.SetRegion);
			
			return null;
		}
		
		void OutputGetAndSetRegion(PropertyGetRegion getRegion, PropertySetRegion setRegion)
		{
			BraceStyle braceStyle = this.prettyPrintOptions.PropertyBraceStyle;
			
			if (getRegion.Block.IsNull && setRegion.Block.IsNull && getRegion.Attributes.Count == 0 && setRegion.Attributes.Count == 0
			    && (braceStyle == BraceStyle.EndOfLine || braceStyle == BraceStyle.EndOfLineWithoutSpace))
			{
				if (braceStyle == BraceStyle.EndOfLine)
					outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				// automatic property / abstract property:
				// output in a single line: "string Text { get; set; }"
				if (!getRegion.IsNull) {
					outputFormatter.Space();
					OutputModifier(getRegion.Modifier);
					outputFormatter.PrintText("get;");
				}
				if (!setRegion.IsNull) {
					outputFormatter.Space();
					OutputModifier(setRegion.Modifier);
					outputFormatter.PrintText("set;");
				}
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
				outputFormatter.NewLine();
			} else {
				outputFormatter.BeginBrace(braceStyle, this.prettyPrintOptions.IndentPropertyBody);
				TrackVisit(getRegion, null);
				TrackVisit(setRegion, null);
				outputFormatter.EndBrace(this.prettyPrintOptions.IndentPropertyBody);
			}
		}
		
		public override object TrackedVisitPropertyGetRegion(PropertyGetRegion propertyGetRegion, object data)
		{
			this.VisitAttributes(propertyGetRegion.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(propertyGetRegion.Modifier);
			outputFormatter.PrintText("get");
			if (prettyPrintOptions.AllowPropertyGetBlockInline) {
				OutputBlockAllowInline(propertyGetRegion.Block, prettyPrintOptions.PropertyGetBraceStyle);
			} else {
				OutputBlock(propertyGetRegion.Block, prettyPrintOptions.PropertyGetBraceStyle);
			}
			return null;
		}
		
		public override object TrackedVisitPropertySetRegion(PropertySetRegion propertySetRegion, object data)
		{
			this.VisitAttributes(propertySetRegion.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(propertySetRegion.Modifier);
			outputFormatter.PrintText("set");
			if (prettyPrintOptions.AllowPropertySetBlockInline) {
				OutputBlockAllowInline(propertySetRegion.Block, prettyPrintOptions.PropertySetBraceStyle);
			} else {
				OutputBlock(propertySetRegion.Block, prettyPrintOptions.PropertySetBraceStyle);
			}
			return null;
		}
		
		public override object TrackedVisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			VisitAttributes(eventDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(eventDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Event);
			outputFormatter.Space();
			TrackVisit(eventDeclaration.TypeReference, data);
			outputFormatter.Space();
			
			if (eventDeclaration.InterfaceImplementations.Count > 0) {
				TrackVisit(eventDeclaration.InterfaceImplementations[0].InterfaceType, data);
				outputFormatter.PrintToken(Tokens.Dot);
			}
			
			outputFormatter.PrintIdentifier(eventDeclaration.Name);
			
			if (!eventDeclaration.Initializer.IsNull) {
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Assign);
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				TrackVisit(eventDeclaration.Initializer, data);
			}
			
			if (eventDeclaration.AddRegion.IsNull && eventDeclaration.RemoveRegion.IsNull) {
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.BeginBrace(this.prettyPrintOptions.EventBraceStyle, this.prettyPrintOptions.IndentEventBody);
				TrackVisit(eventDeclaration.AddRegion, data);
				TrackVisit(eventDeclaration.RemoveRegion, data);
				outputFormatter.EndBrace(this.prettyPrintOptions.IndentEventBody);
			}
			return null;
		}
		
		public override object TrackedVisitEventAddRegion(EventAddRegion eventAddRegion, object data)
		{
			VisitAttributes(eventAddRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintText("add");
			if (prettyPrintOptions.AllowEventAddBlockInline) {
				OutputBlockAllowInline(eventAddRegion.Block, prettyPrintOptions.EventAddBraceStyle);
			} else {
				OutputBlock(eventAddRegion.Block, prettyPrintOptions.EventAddBraceStyle);
			}
			return null;
		}
		
		public override object TrackedVisitEventRemoveRegion(EventRemoveRegion eventRemoveRegion, object data)
		{
			VisitAttributes(eventRemoveRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintText("remove");
			if (prettyPrintOptions.AllowEventRemoveBlockInline) {
				OutputBlockAllowInline(eventRemoveRegion.Block, prettyPrintOptions.EventRemoveBraceStyle);
			} else {
				OutputBlock(eventRemoveRegion.Block, prettyPrintOptions.EventRemoveBraceStyle);
			}
			return null;
		}
		
		public override object TrackedVisitEventRaiseRegion(EventRaiseRegion eventRaiseRegion, object data)
		{
			// VB.NET only
			NotSupported(eventRaiseRegion);
			return null;
		}
		
		public override object TrackedVisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			VisitAttributes(parameterDeclarationExpression.Attributes, data);
			OutputModifier(parameterDeclarationExpression.ParamModifier, parameterDeclarationExpression);
			if (!parameterDeclarationExpression.TypeReference.IsNull) {
				TrackVisit(parameterDeclarationExpression.TypeReference, data);
				outputFormatter.Space();
			}
			outputFormatter.PrintIdentifier(parameterDeclarationExpression.ParameterName);
			if (!parameterDeclarationExpression.DefaultValue.IsNull) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Assign);
				outputFormatter.Space();
				TrackVisit(parameterDeclarationExpression.DefaultValue, data);
			}
			return null;
		}
		
		public override object TrackedVisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			VisitAttributes(methodDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(methodDeclaration.Modifier);
			TrackVisit(methodDeclaration.TypeReference, data);
			outputFormatter.Space();
			if (methodDeclaration.InterfaceImplementations.Count > 0) {
				TrackVisit(methodDeclaration.InterfaceImplementations[0].InterfaceType, data);
				outputFormatter.PrintToken(Tokens.Dot);
			}
			if (methodDeclaration.HandlesClause.Count > 0) {
				Error(methodDeclaration, "Handles clauses are not supported in C#");
			}
			outputFormatter.PrintIdentifier(methodDeclaration.Name);
			
			PrintMethodDeclaration(methodDeclaration);
			return null;
		}
		
		void PrintMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			PrintTemplates(methodDeclaration.Templates);
			if (prettyPrintOptions.BeforeMethodDeclarationParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			bool withinParentheses = this.prettyPrintOptions.WithinMethodDeclarationParentheses && methodDeclaration.Parameters.Any ();
			if (withinParentheses) {
				outputFormatter.Space();
			}
			if (methodDeclaration.IsExtensionMethod) {
				outputFormatter.PrintToken(Tokens.This);
				outputFormatter.Space();
			}
			AppendCommaSeparatedList(methodDeclaration.Parameters);
			if (withinParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			foreach (TemplateDefinition templateDefinition in methodDeclaration.Templates) {
				TrackVisit(templateDefinition, null);
			}
			OutputBlock(methodDeclaration.Body, this.prettyPrintOptions.MethodBraceStyle);
		}
		
		public override object TrackedVisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			VisitAttributes(operatorDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(operatorDeclaration.Modifier);
			
			if (operatorDeclaration.IsConversionOperator) {
				if (operatorDeclaration.ConversionType == ConversionType.Implicit) {
					outputFormatter.PrintToken(Tokens.Implicit);
				} else {
					outputFormatter.PrintToken(Tokens.Explicit);
				}
			} else {
				TrackVisit(operatorDeclaration.TypeReference, data);
			}
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Operator);
			outputFormatter.Space();
			
			if (operatorDeclaration.IsConversionOperator) {
				TrackVisit(operatorDeclaration.TypeReference, data);
			} else {
				switch (operatorDeclaration.OverloadableOperator) {
					case OverloadableOperatorType.Add:
					case OverloadableOperatorType.UnaryPlus:
						outputFormatter.PrintToken(Tokens.Plus);
						break;
					case OverloadableOperatorType.BitNot:
						outputFormatter.PrintToken(Tokens.BitwiseComplement);
						break;
					case OverloadableOperatorType.BitwiseAnd:
						outputFormatter.PrintToken(Tokens.BitwiseAnd);
						break;
					case OverloadableOperatorType.BitwiseOr:
						outputFormatter.PrintToken(Tokens.BitwiseOr);
						break;
					case OverloadableOperatorType.Concat:
						outputFormatter.PrintToken(Tokens.Plus);
						break;
					case OverloadableOperatorType.Decrement:
						outputFormatter.PrintToken(Tokens.Decrement);
						break;
					case OverloadableOperatorType.Divide:
					case OverloadableOperatorType.DivideInteger:
						outputFormatter.PrintToken(Tokens.Div);
						break;
					case OverloadableOperatorType.Equality:
						outputFormatter.PrintToken(Tokens.Equal);
						break;
					case OverloadableOperatorType.ExclusiveOr:
						outputFormatter.PrintToken(Tokens.Xor);
						break;
					case OverloadableOperatorType.GreaterThan:
						outputFormatter.PrintToken(Tokens.GreaterThan);
						break;
					case OverloadableOperatorType.GreaterThanOrEqual:
						outputFormatter.PrintToken(Tokens.GreaterEqual);
						break;
					case OverloadableOperatorType.Increment:
						outputFormatter.PrintToken(Tokens.Increment);
						break;
					case OverloadableOperatorType.InEquality:
						outputFormatter.PrintToken(Tokens.NotEqual);
						break;
					case OverloadableOperatorType.IsTrue:
						outputFormatter.PrintToken(Tokens.True);
						break;
					case OverloadableOperatorType.IsFalse:
						outputFormatter.PrintToken(Tokens.False);
						break;
					case OverloadableOperatorType.LessThan:
						outputFormatter.PrintToken(Tokens.LessThan);
						break;
					case OverloadableOperatorType.LessThanOrEqual:
						outputFormatter.PrintToken(Tokens.LessEqual);
						break;
					case OverloadableOperatorType.Like:
						outputFormatter.PrintText("Like");
						break;
					case OverloadableOperatorType.Modulus:
						outputFormatter.PrintToken(Tokens.Mod);
						break;
					case OverloadableOperatorType.Multiply:
						outputFormatter.PrintToken(Tokens.Times);
						break;
					case OverloadableOperatorType.Not:
						outputFormatter.PrintToken(Tokens.Not);
						break;
					case OverloadableOperatorType.Power:
						outputFormatter.PrintText("Power");
						break;
					case OverloadableOperatorType.ShiftLeft:
						outputFormatter.PrintToken(Tokens.ShiftLeft);
						break;
					case OverloadableOperatorType.ShiftRight:
						outputFormatter.PrintToken(Tokens.GreaterThan);
						outputFormatter.PrintToken(Tokens.GreaterThan);
						break;
					case OverloadableOperatorType.UnaryMinus:
					case OverloadableOperatorType.Subtract:
						outputFormatter.PrintToken(Tokens.Minus);
						break;
					default:
						Error(operatorDeclaration, operatorDeclaration.OverloadableOperator.ToString() + " is not supported as overloadable operator");
						break;
				}
			}
			
			PrintMethodDeclaration(operatorDeclaration);
			return null;
		}
		
		public override object TrackedVisitInterfaceImplementation(InterfaceImplementation interfaceImplementation, object data)
		{
			throw new InvalidOperationException();
		}
		
		public override object TrackedVisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			VisitAttributes(constructorDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(constructorDeclaration.Modifier);
			if (currentType != null) {
				outputFormatter.PrintIdentifier(currentType.Name);
			} else {
				outputFormatter.PrintIdentifier(constructorDeclaration.Name);
			}
			if (prettyPrintOptions.BeforeConstructorDeclarationParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			bool withinParentheses = this.prettyPrintOptions.WithinMethodDeclarationParentheses && constructorDeclaration.Parameters.Any ();
			if (withinParentheses) {
				outputFormatter.Space();
			}
			AppendCommaSeparatedList(constructorDeclaration.Parameters);
			if (withinParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			TrackVisit(constructorDeclaration.ConstructorInitializer, data);
			OutputBlock(constructorDeclaration.Body, this.prettyPrintOptions.ConstructorBraceStyle);
			return null;
		}
		
		public override object TrackedVisitConstructorInitializer(ConstructorInitializer constructorInitializer, object data)
		{
			if (constructorInitializer.IsNull) {
				return null;
			}
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Colon);
			outputFormatter.Space();
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base) {
				outputFormatter.PrintToken(Tokens.Base);
			} else {
				outputFormatter.PrintToken(Tokens.This);
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinMethodCallParentheses) {
				outputFormatter.Space();
			}
			AppendCommaSeparatedList(constructorInitializer.Arguments);
			if (this.prettyPrintOptions.WithinMethodCallParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			VisitAttributes(destructorDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(destructorDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.BitwiseComplement);
			if (currentType != null)
				outputFormatter.PrintIdentifier(currentType.Name);
			else
				outputFormatter.PrintIdentifier(destructorDeclaration.Name);
			if (prettyPrintOptions.BeforeConstructorDeclarationParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			OutputBlock(destructorDeclaration.Body, this.prettyPrintOptions.DestructorBraceStyle);
			return null;
		}
		
		public override object TrackedVisitDeclareDeclaration(DeclareDeclaration declareDeclaration, object data)
		{
			NotSupported(declareDeclaration);
			return null;
		}
		#endregion
		
		#region Statements
		
		void OutputBlock(BlockStatement blockStatement, BraceStyle braceStyle)
		{
			OutputBlock(blockStatement, braceStyle, true);
		}
		void OutputBlock(BlockStatement blockStatement, BraceStyle braceStyle, bool emitEndingNewLine)
		{
			BeginVisit(blockStatement);
			if (blockStatement.IsNull) {
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.BeginBrace(braceStyle, this.prettyPrintOptions.IndentBlocks);
				foreach (Statement stmt in blockStatement.Children) {
					outputFormatter.Indent();
					if (stmt is BlockStatement) {
						TrackVisit(stmt, BraceStyle.EndOfLine);
					} else {
						TrackVisit(stmt, null);
					}
					if (!outputFormatter.LastCharacterIsNewLine)
						outputFormatter.NewLine();
				}
				outputFormatter.EndBrace (this.prettyPrintOptions.IndentBlocks, emitEndingNewLine);
			}
			EndVisit(blockStatement);
		}
		
		void OutputBlockAllowInline(BlockStatement blockStatement, BraceStyle braceStyle)
		{
			OutputBlockAllowInline(blockStatement, braceStyle, true);
		}
		
		void OutputBlockAllowInline(BlockStatement blockStatement, BraceStyle braceStyle, bool useNewLine)
		{
			if (!blockStatement.IsNull
			    && (
			    	blockStatement.Children.Count == 0
			    	|| blockStatement.Children.Count == 1
			    	&& (blockStatement.Children[0] is ExpressionStatement
			    	    || blockStatement.Children[0] is ReturnStatement
			    	   )))
			{
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				outputFormatter.Space();
				if (blockStatement.Children.Count != 0) {
					bool doIndent  = outputFormatter.DoIndent;
					bool doNewLine = outputFormatter.DoNewLine;
					outputFormatter.DoIndent  = false;
					outputFormatter.DoNewLine = false;
					
					TrackVisit(blockStatement.Children[0], null);
					
					outputFormatter.DoIndent  = doIndent;
					outputFormatter.DoNewLine = doNewLine;
					
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
				if (useNewLine) {
					outputFormatter.NewLine();
				}
			} else {
				OutputBlock(blockStatement, braceStyle, useNewLine);
			}
		}
		
		public override object TrackedVisitBlockStatement(BlockStatement blockStatement, object data)
		{
			if (outputFormatter.TextLength == 0) {
				// we are outputting only a code block:
				// do not output braces, just the block's contents
				foreach (Statement stmt in blockStatement.Children) {
					outputFormatter.Indent();
					TrackVisit(stmt, null);
					if (!outputFormatter.LastCharacterIsNewLine)
						outputFormatter.NewLine();
				}
				return null;
			}
			
			if (data is BraceStyle)
				OutputBlock(blockStatement, (BraceStyle)data);
			else
				OutputBlock(blockStatement, BraceStyle.NextLine);
			return null;
		}
		
		public override object TrackedVisitAddHandlerStatement(AddHandlerStatement addHandlerStatement, object data)
		{
			TrackVisit(addHandlerStatement.EventExpression, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.PlusAssign);
			outputFormatter.Space();
			TrackVisit(addHandlerStatement.HandlerExpression, data);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitRemoveHandlerStatement(RemoveHandlerStatement removeHandlerStatement, object data)
		{
			TrackVisit(removeHandlerStatement.EventExpression, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.MinusAssign);
			outputFormatter.Space();
			TrackVisit(removeHandlerStatement.HandlerExpression, data);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitRaiseEventStatement(RaiseEventStatement raiseEventStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.If);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			outputFormatter.PrintIdentifier(raiseEventStatement.EventName);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.NotEqual);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Null);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			outputFormatter.BeginBrace(BraceStyle.EndOfLine, this.prettyPrintOptions.IndentBlocks);
			
			outputFormatter.Indent();
			outputFormatter.PrintIdentifier(raiseEventStatement.EventName);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			this.AppendCommaSeparatedList(raiseEventStatement.Arguments);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.PrintToken(Tokens.Semicolon);
			
			outputFormatter.NewLine();
			outputFormatter.EndBrace(this.prettyPrintOptions.IndentBlocks);
			
			return null;
		}
		
		public override object TrackedVisitEraseStatement(EraseStatement eraseStatement, object data)
		{
			for (int i = 0; i < eraseStatement.Expressions.Count; i++) {
				if (i > 0) {
					outputFormatter.NewLine();
					outputFormatter.Indent();
				}
				TrackVisit(eraseStatement.Expressions[i], data);
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Assign);
				if (this.prettyPrintOptions.AroundAssignmentParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Null);
				outputFormatter.PrintToken(Tokens.Semicolon);
			}
			return null;
		}
		
		public override object TrackedVisitErrorStatement(ErrorStatement errorStatement, object data)
		{
			NotSupported(errorStatement);
			return null;
		}
		
		public override object TrackedVisitOnErrorStatement(OnErrorStatement onErrorStatement, object data)
		{
			NotSupported(onErrorStatement);
			return null;
		}
		
		public override object TrackedVisitReDimStatement(ReDimStatement reDimStatement, object data)
		{
			if (!reDimStatement.IsPreserve) {
				NotSupported(reDimStatement);
				return null;
			}
			foreach (InvocationExpression ie in reDimStatement.ReDimClauses) {
				outputFormatter.PrintText("Array.Resize(ref ");
				ie.TargetObject.AcceptVisitor(this, data);
				outputFormatter.PrintText(", ");
				for (int i = 0; i < ie.Arguments.Count; i++) {
					if (i > 0) outputFormatter.PrintText(", ");
					Expression.AddInteger(ie.Arguments[i], 1).AcceptVisitor(this, data);
				}
				outputFormatter.PrintText(")");
				outputFormatter.PrintToken(Tokens.Semicolon);
			}
			return null;
		}
		
		public override object TrackedVisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			TrackVisit(expressionStatement.Expression, data);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitLocalVariableDeclaration(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			TypeReference type = localVariableDeclaration.GetTypeForVariable(0);
			for (int i = 1; i < localVariableDeclaration.Variables.Count; ++i) {
				if (localVariableDeclaration.GetTypeForVariable(i) != type)
					return TrackedVisitLocalVariableDeclarationSeparateTypes(localVariableDeclaration, data);
			}
			// all variables have the same type
			OutputModifier(localVariableDeclaration.Modifier);
			TrackVisit(type ?? new TypeReference("System.Object", true), data);
			outputFormatter.Space();
			AppendCommaSeparatedList(localVariableDeclaration.Variables);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		object TrackedVisitLocalVariableDeclarationSeparateTypes(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			for (int i = 0; i < localVariableDeclaration.Variables.Count; ++i) {
				VariableDeclaration v = (VariableDeclaration)localVariableDeclaration.Variables[i];
				if (i > 0) {
					outputFormatter.NewLine();
					outputFormatter.Indent();
				}
				OutputModifier(localVariableDeclaration.Modifier);
				TrackVisit(localVariableDeclaration.GetTypeForVariable(i) ?? new TypeReference("System.Object", true), data);
				outputFormatter.Space();
				TrackVisit(v, data);
				outputFormatter.PrintToken(Tokens.Semicolon);
			}
			return null;
		}
		
		public override object TrackedVisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitYieldStatement(YieldStatement yieldStatement, object data)
		{
			Debug.Assert(yieldStatement != null);
			Debug.Assert(yieldStatement.Statement != null);
			outputFormatter.PrintText("yield");
			outputFormatter.Space();
			TrackVisit(yieldStatement.Statement, data);
			return null;
		}
		
		public override object TrackedVisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Return);
			if (!returnStatement.Expression.IsNull) {
				outputFormatter.Space();
				TrackVisit(returnStatement.Expression, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.If);
			if (this.prettyPrintOptions.IfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinIfParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(ifElseStatement.Condition, data);
			if (this.prettyPrintOptions.WithinIfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			PrintIfSection(ifElseStatement.TrueStatement);
			bool wasBlock = false;
			if (ifElseStatement.TrueStatement != null && ifElseStatement.TrueStatement.Count > 0)
				wasBlock = ifElseStatement.TrueStatement.Last () is BlockStatement;
			
			foreach (ElseIfSection elseIfSection in ifElseStatement.ElseIfSections) {
				TrackVisit(elseIfSection, data);
				wasBlock = elseIfSection.EmbeddedStatement is BlockStatement;
			}
			
			if (ifElseStatement.HasElseStatements) {
				if (prettyPrintOptions.PlaceElseOnNewLine || (prettyPrintOptions.PlaceNonBlockElseOnNewLine && !wasBlock)) {
					outputFormatter.NewLine();
					outputFormatter.Indent();
				} else {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Else);
				PrintIfSection(ifElseStatement.FalseStatement);
			}
			
			return null;
		}
		
		void PrintIfSection(List<Statement> statements)
		{
			if (statements.Count == 1 && (statements[0] is BlockStatement)) {
				OutputBlock((BlockStatement)statements[0],
				            prettyPrintOptions.StatementBraceStyle,
				            prettyPrintOptions.PlaceElseOnNewLine);
				return;
			}
			/*			if (statements.Count != 1 || !(statements[0] is BlockStatement)) {
				outputFormatter.Space();
			}*/
			if (statements.Count != 1) {
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			} else {
				outputFormatter.NewLine ();
				outputFormatter.IndentationLevel++;
				outputFormatter.Indent ();
			}
			
			foreach (Statement stmt in statements) {
				TrackVisit(stmt, prettyPrintOptions.StatementBraceStyle);
			}
			
			if (statements.Count == 1) {
				outputFormatter.IndentationLevel--;
			} else {
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			}
			/*			if (statements.Count != 1 || !(statements[0] is BlockStatement)) {
				outputFormatter.Space();
			}*/
		}
		
		public override object TrackedVisitElseIfSection(ElseIfSection elseIfSection, object data)
		{
			if (prettyPrintOptions.PlaceElseOnNewLine) {
				outputFormatter.Indent();
			} else {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Else);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.If);
			if (prettyPrintOptions.IfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinIfParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(elseIfSection.Condition, data);
			if (this.prettyPrintOptions.WithinIfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			WriteEmbeddedStatement(elseIfSection.EmbeddedStatement, prettyPrintOptions.IfElseBraceForcement, prettyPrintOptions.StatementBraceStyle, false);
			
			return null;
		}
		
		public override object TrackedVisitForStatement(ForStatement forStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.For);
			if (this.prettyPrintOptions.ForParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinForParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.DoIndent = false;
			outputFormatter.DoNewLine = false;
			outputFormatter.EmitSemicolon = false;
			for (int i = 0; i < forStatement.Initializers.Count; ++i) {
				INode node = (INode)forStatement.Initializers[i];
				TrackVisit(node, data);
				if (i + 1 < forStatement.Initializers.Count) {
					outputFormatter.PrintToken(Tokens.Comma);
				}
			}
			outputFormatter.EmitSemicolon = true;
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.EmitSemicolon = false;
			if (!forStatement.Condition.IsNull) {
				if (this.prettyPrintOptions.SpacesAfterSemicolon) {
					outputFormatter.Space();
				}
				TrackVisit(forStatement.Condition, data);
			}
			outputFormatter.EmitSemicolon = true;
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.EmitSemicolon = false;
			if (forStatement.Iterator != null && forStatement.Iterator.Count > 0) {
				if (this.prettyPrintOptions.SpacesAfterSemicolon) {
					outputFormatter.Space();
				}
				
				for (int i = 0; i < forStatement.Iterator.Count; ++i) {
					INode node = (INode)forStatement.Iterator[i];
					TrackVisit(node, data);
					if (i + 1 < forStatement.Iterator.Count) {
						outputFormatter.PrintToken(Tokens.Comma);
					}
				}
			}
			if (this.prettyPrintOptions.WithinForParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.EmitSemicolon = true;
			outputFormatter.DoNewLine     = true;
			outputFormatter.DoIndent      = true;
			
			WriteEmbeddedStatement(forStatement.EmbeddedStatement, prettyPrintOptions.ForBraceForcement, prettyPrintOptions.StatementBraceStyle, true);
			
			return null;
		}
		
		void WriteEmbeddedStatement (Statement statement)
		{
			WriteEmbeddedStatement (statement, true);
		}
		
		void WriteEmbeddedStatement (Statement statement, bool emitEndingNewLine)
		{
			if (statement is BlockStatement) {
				OutputBlock((BlockStatement)statement, prettyPrintOptions.StatementBraceStyle, emitEndingNewLine);
			} else {
				++outputFormatter.IndentationLevel;
				outputFormatter.NewLine();
				outputFormatter.Indent ();
				TrackVisit(statement, null);
				--outputFormatter.IndentationLevel;
			}
		}
		
		void WriteEmbeddedStatement (Statement statement, BraceForcement forcement, BraceStyle braceStyle, bool emitEndingNewLine)
		{
			if (statement is BlockStatement) {
				BlockStatement block = (BlockStatement)statement;
				switch (forcement) {
					case BraceForcement.RemoveBraces:
						if (block.Children.Count == 1) {
							++outputFormatter.IndentationLevel;
							outputFormatter.NewLine();
							outputFormatter.Indent ();
							TrackVisit(block.Children[0], null);
							--outputFormatter.IndentationLevel;
						} else  {
							goto default;
						}
						break;
					case BraceForcement.RemoveBracesForSingleLine:
						goto case BraceForcement.RemoveBraces;
					default:
						OutputBlock((BlockStatement)statement, prettyPrintOptions.StatementBraceStyle, emitEndingNewLine);
						break;
				}
			} else {
				switch (forcement) {
					case BraceForcement.AddBraces:
						BlockStatement blockStatement = new BlockStatement ();
						blockStatement.AddChild (statement);
						OutputBlock(blockStatement, braceStyle, true);
						break;
					default:
						WriteEmbeddedStatement (statement, emitEndingNewLine);
						break;
				}
			}
		}
		
		public override object TrackedVisitLabelStatement(LabelStatement labelStatement, object data)
		{
			outputFormatter.PrintIdentifier(labelStatement.Label);
			outputFormatter.PrintToken(Tokens.Colon);
			return null;
		}
		
		public override object TrackedVisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Goto);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(gotoStatement.Label);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Switch);
			if (this.prettyPrintOptions.SwitchParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinSwitchParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(switchStatement.SwitchExpression, data);
			if (this.prettyPrintOptions.WithinSwitchParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			outputFormatter.NewLine();
			if (prettyPrintOptions.IndentSwitchBody)
				++outputFormatter.IndentationLevel;
			
			foreach (SwitchSection section in switchStatement.SwitchSections) {
				TrackVisit(section, data);
			}
			
			if (prettyPrintOptions.IndentSwitchBody)
				--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			return null;
		}
		
		public override object TrackedVisitSwitchSection(SwitchSection switchSection, object data)
		{
			foreach (CaseLabel label in switchSection.SwitchLabels) {
				TrackVisit(label, data);
			}
			int standardIndentLevel = outputFormatter.IndentationLevel;
			if (prettyPrintOptions.IndentCaseBody)
				++outputFormatter.IndentationLevel;
			for (int i = 0; i < switchSection.Children.Count; i++) {
				Statement stmt = switchSection.Children[i] as Statement;
				int oldIndent = outputFormatter.IndentationLevel;
				if (i == switchSection.Children.Count - 1) {
					if (prettyPrintOptions.IndentBreakStatements)
						outputFormatter.IndentationLevel = standardIndentLevel + 1;
					else
						outputFormatter.IndentationLevel = standardIndentLevel;
				}
				outputFormatter.Indent();
				TrackVisit(stmt, data);
				outputFormatter.NewLine();
				outputFormatter.IndentationLevel = oldIndent;
			}
			
			if (prettyPrintOptions.IndentCaseBody)
				--outputFormatter.IndentationLevel;
			return null;
		}
		
		public override object TrackedVisitCaseLabel(CaseLabel caseLabel, object data)
		{
			outputFormatter.Indent();
			if (caseLabel.IsDefault) {
				outputFormatter.PrintToken(Tokens.Default);
			} else {
				outputFormatter.PrintToken(Tokens.Case);
				outputFormatter.Space();
				if (caseLabel.BinaryOperatorType != BinaryOperatorType.None) {
					Error(caseLabel, String.Format("Case labels with binary operators are unsupported : {0}", caseLabel.BinaryOperatorType));
				}
				TrackVisit(caseLabel.Label, data);
			}
			outputFormatter.PrintToken(Tokens.Colon);
			if (!caseLabel.ToExpression.IsNull) {
				PrimitiveExpression pl = caseLabel.Label as PrimitiveExpression;
				PrimitiveExpression pt = caseLabel.ToExpression as PrimitiveExpression;
				if (pl != null && pt != null && pl.Value is int && pt.Value is int) {
					int plv = (int)pl.Value;
					int prv = (int)pt.Value;
					if (plv < prv && plv + 12 > prv) {
						for (int i = plv + 1; i <= prv; i++) {
							outputFormatter.NewLine();
							outputFormatter.Indent();
							outputFormatter.PrintToken(Tokens.Case);
							outputFormatter.Space();
							outputFormatter.PrintText(i.ToString(NumberFormatInfo.InvariantInfo));
							outputFormatter.PrintToken(Tokens.Colon);
						}
					} else {
						outputFormatter.PrintText(" // TODO: to ");
						TrackVisit(caseLabel.ToExpression, data);
					}
				} else {
					outputFormatter.PrintText(" // TODO: to ");
					TrackVisit(caseLabel.ToExpression, data);
				}
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public override object TrackedVisitBreakStatement(BreakStatement breakStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Break);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitStopStatement(StopStatement stopStatement, object data)
		{
			outputFormatter.PrintText("System.Diagnostics.Debugger.Break()");
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitResumeStatement(ResumeStatement resumeStatement, object data)
		{
			NotSupported(resumeStatement);
			return null;
		}
		
		public override object TrackedVisitEndStatement(EndStatement endStatement, object data)
		{
			outputFormatter.PrintText("System.Environment.Exit(0)");
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Continue);
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Goto);
			outputFormatter.Space();
			if (gotoCaseStatement.IsDefaultCase) {
				outputFormatter.PrintToken(Tokens.Default);
			} else {
				outputFormatter.PrintToken(Tokens.Case);
				outputFormatter.Space();
				TrackVisit(gotoCaseStatement.Expression, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		void PrintLoopCheck(DoLoopStatement doLoopStatement)
		{
			outputFormatter.PrintToken(Tokens.While);
			if (this.prettyPrintOptions.WhileParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinWhileParentheses) {
				outputFormatter.Space();
			}
			
			if (doLoopStatement.ConditionType == ConditionType.Until) {
				outputFormatter.PrintToken(Tokens.Not);
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
			}
			
			if (doLoopStatement.Condition.IsNull) {
				outputFormatter.PrintToken(Tokens.True);
			} else {
				TrackVisit(doLoopStatement.Condition, null);
			}
			
			if (doLoopStatement.ConditionType == ConditionType.Until) {
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			if (this.prettyPrintOptions.WithinWhileParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
		}
		
		public override object TrackedVisitDoLoopStatement(DoLoopStatement doLoopStatement, object data)
		{
			if (doLoopStatement.ConditionPosition == ConditionPosition.None) {
				Error(doLoopStatement, String.Format("Unknown condition position for loop : {0}.", doLoopStatement));
			}
			
			if (doLoopStatement.ConditionPosition == ConditionPosition.Start) {
				PrintLoopCheck(doLoopStatement);
			} else {
				outputFormatter.PrintToken(Tokens.Do);
			}
			
			WriteEmbeddedStatement(doLoopStatement.EmbeddedStatement, prettyPrintOptions.WhileBraceForcement, prettyPrintOptions.StatementBraceStyle, prettyPrintOptions.PlaceWhileOnNewLine);
			
			if (doLoopStatement.ConditionPosition == ConditionPosition.End) {
				if (prettyPrintOptions.PlaceWhileOnNewLine) {
					outputFormatter.Indent();
				} else {
					outputFormatter.Space();
				}
				PrintLoopCheck(doLoopStatement);
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			}
			
			return null;
		}
		
		public override object TrackedVisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Foreach);
			if (this.prettyPrintOptions.ForeachParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinForEachParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(foreachStatement.TypeReference, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(foreachStatement.VariableName);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.In);
			outputFormatter.Space();
			TrackVisit(foreachStatement.Expression, data);
			if (this.prettyPrintOptions.WithinForEachParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			WriteEmbeddedStatement(foreachStatement.EmbeddedStatement, prettyPrintOptions.ForEachBraceForcement, prettyPrintOptions.StatementBraceStyle, true);
			
			return null;
		}
		
		public override object TrackedVisitLockStatement(LockStatement lockStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Lock);
			if (this.prettyPrintOptions.LockParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinLockParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(lockStatement.LockExpression, data);
			if (this.prettyPrintOptions.WithinLockParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			WriteEmbeddedStatement(lockStatement.EmbeddedStatement);
			
			return null;
		}
		
		public override object TrackedVisitUsingStatement(UsingStatement usingStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Using);
			if (this.prettyPrintOptions.UsingParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (prettyPrintOptions.WithinUsingParentheses) {
				outputFormatter.Space();
			}
			PrintStatementInline(usingStatement.ResourceAcquisition, data);
			if (prettyPrintOptions.WithinUsingParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			WriteEmbeddedStatement(usingStatement.EmbeddedStatement, prettyPrintOptions.UsingBraceForcement, prettyPrintOptions.StatementBraceStyle, true);
			
			return null;
		}
		
		void PrintStatementInline(Statement statement, object data)
		{
			outputFormatter.DoIndent = false;
			outputFormatter.DoNewLine = false;
			outputFormatter.EmitSemicolon = false;
			TrackVisit(statement, data);
			outputFormatter.DoIndent = true;
			outputFormatter.DoNewLine = true;
			outputFormatter.EmitSemicolon = true;
		}
		
		public override object TrackedVisitWithStatement(WithStatement withStatement, object data)
		{
			NotSupported(withStatement);
			return null;
		}
		
		public override object TrackedVisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Try);
			
			WriteEmbeddedStatement (tryCatchStatement.StatementBlock, prettyPrintOptions.PlaceCatchOnNewLine);
			for (int i = 0 ; i < tryCatchStatement.CatchClauses.Count; i++) {
				TrackVisit(tryCatchStatement.CatchClauses[i], i == tryCatchStatement.CatchClauses.Count - 1);
			}
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				if (prettyPrintOptions.PlaceFinallyOnNewLine) {
					//				if (!prettyPrintOptions.PlaceCatchOnNewLine)
					//					outputFormatter.NewLine ();
					outputFormatter.Indent();
				} else {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.Finally);
				WriteEmbeddedStatement(tryCatchStatement.FinallyBlock);
			}
			
			return null;
		}
		
		public override object TrackedVisitCatchClause(CatchClause catchClause, object data)
		{
			if (prettyPrintOptions.PlaceCatchOnNewLine) {
				outputFormatter.Indent();
			} else {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Catch);
			
			if (!catchClause.TypeReference.IsNull) {
				if (this.prettyPrintOptions.CatchParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				if (this.prettyPrintOptions.WithinCatchParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintIdentifier(catchClause.TypeReference.Type);
				if (catchClause.VariableName.Length > 0) {
					outputFormatter.Space();
					outputFormatter.PrintIdentifier(catchClause.VariableName);
				}
				if (this.prettyPrintOptions.WithinCatchParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			WriteEmbeddedStatement(catchClause.StatementBlock, ((bool)data) ? prettyPrintOptions.PlaceFinallyOnNewLine : prettyPrintOptions.PlaceCatchOnNewLine);
			return null;
		}
		
		public override object TrackedVisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Throw);
			if (!throwStatement.Expression.IsNull) {
				outputFormatter.Space();
				TrackVisit(throwStatement.Expression, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			return null;
		}
		
		public override object TrackedVisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Fixed);
			if (this.prettyPrintOptions.FixedParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinCheckedExpressionParantheses) {
				outputFormatter.Space();
			}
			PrintStatementInline(fixedStatement.PointerDeclaration, data);
			if (this.prettyPrintOptions.WithinCheckedExpressionParantheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			WriteEmbeddedStatement(fixedStatement.EmbeddedStatement, prettyPrintOptions.FixedBraceForcement, prettyPrintOptions.StatementBraceStyle, true);
			return null;
		}
		
		public override object TrackedVisitUnsafeStatement(UnsafeStatement unsafeStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Unsafe);
			WriteEmbeddedStatement(unsafeStatement.Block);
			return null;
		}
		
		public override object TrackedVisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Checked);
			WriteEmbeddedStatement(checkedStatement.Block);
			return null;
		}
		
		public override object TrackedVisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Unchecked);
			WriteEmbeddedStatement(uncheckedStatement.Block);
			return null;
		}
		
		public override object TrackedVisitExitStatement(ExitStatement exitStatement, object data)
		{
			if (exitStatement.ExitType == ExitType.Function || exitStatement.ExitType == ExitType.Sub || exitStatement.ExitType == ExitType.Property) {
				outputFormatter.PrintToken(Tokens.Return);
				outputFormatter.PrintToken(Tokens.Semicolon);
			} else {
				outputFormatter.PrintToken(Tokens.Break);
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.PrintText(" // TODO: might not be correct. Was : Exit " + exitStatement.ExitType);
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public override object TrackedVisitForNextStatement(ForNextStatement forNextStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.For);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinForParentheses) {
				outputFormatter.Space();
			}
			if (forNextStatement.LoopVariableExpression.IsNull) {
				if (!forNextStatement.TypeReference.IsNull) {
					TrackVisit(forNextStatement.TypeReference, data);
					outputFormatter.Space();
				}
				outputFormatter.PrintIdentifier(forNextStatement.VariableName);
			} else {
				TrackVisit(forNextStatement.LoopVariableExpression, data);
			}
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Assign);
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(forNextStatement.Start, data);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.Space();
			if (forNextStatement.LoopVariableExpression.IsNull)
				outputFormatter.PrintIdentifier(forNextStatement.VariableName);
			else
				TrackVisit(forNextStatement.LoopVariableExpression, data);
			outputFormatter.Space();
			Expression stepExpr = forNextStatement.Step;
			while (stepExpr is ParenthesizedExpression)
				stepExpr = ((ParenthesizedExpression)stepExpr).Expression;
			PrimitiveExpression pe = stepExpr as PrimitiveExpression;
			if ((pe == null || !(pe.Value is int) || ((int)pe.Value) >= 0)
			    && !(stepExpr is UnaryOperatorExpression))
				outputFormatter.PrintToken(Tokens.LessEqual);
			else {
				if (stepExpr is UnaryOperatorExpression && ((UnaryOperatorExpression)stepExpr).Op == UnaryOperatorType.Plus)
					outputFormatter.PrintToken(Tokens.LessEqual);
				else
					outputFormatter.PrintToken(Tokens.GreaterEqual);
			}
			outputFormatter.Space();
			TrackVisit(forNextStatement.End, data);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.Space();
			if (forNextStatement.LoopVariableExpression.IsNull)
				outputFormatter.PrintIdentifier(forNextStatement.VariableName);
			else
				TrackVisit(forNextStatement.LoopVariableExpression, data);
			if (forNextStatement.Step.IsNull) {
				outputFormatter.PrintToken(Tokens.Increment);
			} else {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.PlusAssign);
				outputFormatter.Space();
				TrackVisit(forNextStatement.Step, data);
			}
			if (this.prettyPrintOptions.WithinForParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			WriteEmbeddedStatement(forNextStatement.EmbeddedStatement, prettyPrintOptions.ForBraceForcement, prettyPrintOptions.StatementBraceStyle, true);
			return null;
		}
		#endregion
		
		#region Expressions
		public override object TrackedVisitClassReferenceExpression(ClassReferenceExpression classReferenceExpression, object data)
		{
			NotSupported(classReferenceExpression);
			return null;
		}
		
		static string ConvertCharLiteral(char ch)
		{
			if (ch == '\'') return "\\'";
			return ConvertChar(ch);
		}
		
		static string ConvertChar(char ch)
		{
			switch (ch) {
				case '\\':
					return "\\\\";
				case '\0':
					return "\\0";
				case '\a':
					return "\\a";
				case '\b':
					return "\\b";
				case '\f':
					return "\\f";
				case '\n':
					return "\\n";
				case '\r':
					return "\\r";
				case '\t':
					return "\\t";
				case '\v':
					return "\\v";
				default:
					if (char.IsControl(ch)) {
						return "\\u" + ((int)ch).ToString("x4");
					} else {
						return ch.ToString();
					}
			}
		}
		
		static string ConvertString(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in str) {
				if (ch == '"')
					sb.Append("\\\"");
				else
					sb.Append(ConvertChar(ch));
			}
			return sb.ToString();
		}
		
		public override object TrackedVisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			outputFormatter.PrintText(ToCSharpString(primitiveExpression));
			return null;
		}
		
		internal static string ToCSharpString(PrimitiveExpression primitiveExpression)
		{
			if (primitiveExpression.Value == null) {
				return "null";
			}
			
			object val = primitiveExpression.Value;
			
			if (val is bool) {
				if ((bool)val) {
					return "true";
				} else {
					return "false";
				}
			}
			
			if (val is string) {
				return "\"" + ConvertString(val.ToString()) + "\"";
			}
			
			if (val is char) {
				return "'" + ConvertCharLiteral((char)val) + "'";
			}
			
			if (val is decimal) {
				return ((decimal)val).ToString(NumberFormatInfo.InvariantInfo) + "m";
			}
			
			if (val is float) {
				return ((float)val).ToString(NumberFormatInfo.InvariantInfo) + "f";
			}
			
			if (val is double) {
				string text = ((double)val).ToString(NumberFormatInfo.InvariantInfo);
				if (text.IndexOf('.') < 0 && text.IndexOf('E') < 0)
					return text + ".0";
				else
					return text;
			}
			
			if (val is IFormattable) {
				StringBuilder b = new StringBuilder();
				if (primitiveExpression.LiteralFormat == LiteralFormat.HexadecimalNumber) {
					b.Append("0x");
					b.Append(((IFormattable)val).ToString("x", NumberFormatInfo.InvariantInfo));
				} else {
					b.Append(((IFormattable)val).ToString(null, NumberFormatInfo.InvariantInfo));
				}
				if (val is uint || val is ulong) {
					b.Append("u");
				}
				if (val is long || val is ulong) {
					b.Append("L");
				}
				return b.ToString();
			} else {
				return val.ToString();
			}
		}
		
		static bool IsNullLiteralExpression(Expression expr)
		{
			PrimitiveExpression pe = expr as PrimitiveExpression;
			if (pe == null) return false;
			return pe.Value == null;
		}
		
		public override object TrackedVisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			// VB-operators that require special representation:
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.ReferenceEquality:
				case BinaryOperatorType.ReferenceInequality:
					if (IsNullLiteralExpression(binaryOperatorExpression.Left) || IsNullLiteralExpression(binaryOperatorExpression.Right)) {
						// prefer a == null to object.ReferenceEquals(a, null)
						break;
					}
					
					if (binaryOperatorExpression.Op == BinaryOperatorType.ReferenceInequality)
						outputFormatter.PrintToken(Tokens.Not);
					outputFormatter.PrintText("object.ReferenceEquals");
					if (prettyPrintOptions.BeforeMethodCallParentheses) {
						outputFormatter.Space();
					}
					
					outputFormatter.PrintToken(Tokens.OpenParenthesis);
					TrackVisit(binaryOperatorExpression.Left, data);
					PrintFormattedComma();
					TrackVisit(binaryOperatorExpression.Right, data);
					outputFormatter.PrintToken(Tokens.CloseParenthesis);
					return null;
				case BinaryOperatorType.Power:
					outputFormatter.PrintText("Math.Pow");
					if (prettyPrintOptions.BeforeMethodCallParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.OpenParenthesis);
					if (this.prettyPrintOptions.WithinMethodCallParentheses) {
						outputFormatter.Space();
					}
					TrackVisit(binaryOperatorExpression.Left, data);
					PrintFormattedComma();
					TrackVisit(binaryOperatorExpression.Right, data);
					if (this.prettyPrintOptions.WithinMethodCallParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.CloseParenthesis);
					return null;
				case BinaryOperatorType.DictionaryAccess:
					TrackVisit(binaryOperatorExpression.Left, data);
					outputFormatter.PrintToken(Tokens.OpenSquareBracket);
					TrackVisit(binaryOperatorExpression.Right, data);
					outputFormatter.PrintToken(Tokens.CloseSquareBracket);
					return null;
			}
			TrackVisit(binaryOperatorExpression.Left, data);
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Concat: // translate Concatenation to +
					if (prettyPrintOptions.AroundAdditiveOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Plus);
					if (prettyPrintOptions.AroundAdditiveOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.Subtract:
					if (prettyPrintOptions.AroundAdditiveOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Minus);
					if (prettyPrintOptions.AroundAdditiveOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.Multiply:
					if (prettyPrintOptions.AroundMultiplicativeOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Times);
					if (prettyPrintOptions.AroundMultiplicativeOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.DivideInteger:
					if (prettyPrintOptions.AroundMultiplicativeOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Div);
					if (prettyPrintOptions.AroundMultiplicativeOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.Modulus:
					if (prettyPrintOptions.AroundMultiplicativeOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Mod);
					if (prettyPrintOptions.AroundMultiplicativeOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.ShiftLeft:
					if (prettyPrintOptions.AroundShiftOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.ShiftLeft);
					if (prettyPrintOptions.AroundShiftOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.ShiftRight:
					if (prettyPrintOptions.AroundShiftOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.GreaterThan);
					outputFormatter.PrintToken(Tokens.GreaterThan);
					if (prettyPrintOptions.AroundShiftOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.BitwiseAnd:
					if (prettyPrintOptions.AroundBitwiseOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.BitwiseAnd);
					if (prettyPrintOptions.AroundBitwiseOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.BitwiseOr:
					if (prettyPrintOptions.AroundBitwiseOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.BitwiseOr);
					if (prettyPrintOptions.AroundBitwiseOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.ExclusiveOr:
					if (prettyPrintOptions.AroundBitwiseOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Xor);
					if (prettyPrintOptions.AroundBitwiseOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.LogicalAnd:
					if (prettyPrintOptions.AroundLogicalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.LogicalAnd);
					if (prettyPrintOptions.AroundLogicalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.LogicalOr:
					if (prettyPrintOptions.AroundLogicalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.LogicalOr);
					if (prettyPrintOptions.AroundLogicalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.ReferenceEquality:
					if (prettyPrintOptions.AroundEqualityOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.Equal);
					if (prettyPrintOptions.AroundEqualityOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.GreaterThan:
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.GreaterThan);
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.GreaterEqual);
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.InEquality:
				case BinaryOperatorType.ReferenceInequality:
					if (prettyPrintOptions.AroundEqualityOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.NotEqual);
					if (prettyPrintOptions.AroundEqualityOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.LessThan:
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.LessThan);
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.LessThanOrEqual:
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.LessEqual);
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
				case BinaryOperatorType.NullCoalescing:
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.DoubleQuestion);
					if (prettyPrintOptions.AroundRelationalOperatorParentheses) {
						outputFormatter.Space();
					}
					break;
					
				default:
					Error(binaryOperatorExpression, String.Format("Unknown binary operator {0}", binaryOperatorExpression.Op));
					return null;
			}
			TrackVisit(binaryOperatorExpression.Right, data);
			return null;
		}
		
		public override object TrackedVisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(parenthesizedExpression.Expression, data);
			if (this.prettyPrintOptions.WithinParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			TrackVisit(invocationExpression.TargetObject, data);
			
			if (prettyPrintOptions.BeforeMethodCallParentheses) {
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinMethodCallParentheses) {
				outputFormatter.Space();
			}
			AppendCommaSeparatedList(invocationExpression.Arguments);
			if (this.prettyPrintOptions.WithinMethodCallParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			outputFormatter.PrintIdentifier(identifierExpression.Identifier);
			PrintTypeArgumentList(identifierExpression.TypeArguments);
			return null;
		}
		
		void PrintTypeArgumentList(List<TypeReference> typeArguments)
		{
			if (typeArguments != null && typeArguments.Count > 0) {
				outputFormatter.PrintToken(Tokens.LessThan);
				AppendCommaSeparatedList(typeArguments);
				outputFormatter.PrintToken(Tokens.GreaterThan);
			}
		}
		
		public override object TrackedVisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			TrackVisit(typeReferenceExpression.TypeReference, data);
			return null;
		}
		
		public override object TrackedVisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.BitNot:
					outputFormatter.PrintToken(Tokens.BitwiseComplement);
					break;
				case UnaryOperatorType.Decrement:
					outputFormatter.PrintToken(Tokens.Decrement);
					break;
				case UnaryOperatorType.Increment:
					outputFormatter.PrintToken(Tokens.Increment);
					break;
				case UnaryOperatorType.Minus:
					outputFormatter.PrintToken(Tokens.Minus);
					break;
				case UnaryOperatorType.Not:
					outputFormatter.PrintToken(Tokens.Not);
					break;
				case UnaryOperatorType.Plus:
					outputFormatter.PrintToken(Tokens.Plus);
					break;
				case UnaryOperatorType.PostDecrement:
					TrackVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintToken(Tokens.Decrement);
					return null;
				case UnaryOperatorType.PostIncrement:
					TrackVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintToken(Tokens.Increment);
					return null;
				case UnaryOperatorType.Dereference:
					outputFormatter.PrintToken(Tokens.Times);
					break;
				case UnaryOperatorType.AddressOf:
					outputFormatter.PrintToken(Tokens.BitwiseAnd);
					break;
				default:
					Error(unaryOperatorExpression, String.Format("Unknown unary operator {0}", unaryOperatorExpression.Op));
					return null;
			}
			TrackVisit(unaryOperatorExpression.Expression, data);
			return null;
		}
		
		public override object TrackedVisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			TrackVisit(assignmentExpression.Left, data);
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			switch (assignmentExpression.Op) {
				case AssignmentOperatorType.Assign:
					outputFormatter.PrintToken(Tokens.Assign);
					break;
				case AssignmentOperatorType.Add:
				case AssignmentOperatorType.ConcatString:
					outputFormatter.PrintToken(Tokens.PlusAssign);
					break;
				case AssignmentOperatorType.Subtract:
					outputFormatter.PrintToken(Tokens.MinusAssign);
					break;
				case AssignmentOperatorType.Multiply:
					outputFormatter.PrintToken(Tokens.TimesAssign);
					break;
				case AssignmentOperatorType.Divide:
				case AssignmentOperatorType.DivideInteger:
					outputFormatter.PrintToken(Tokens.DivAssign);
					break;
				case AssignmentOperatorType.ShiftLeft:
					outputFormatter.PrintToken(Tokens.ShiftLeftAssign);
					break;
				case AssignmentOperatorType.ShiftRight:
					outputFormatter.PrintToken(Tokens.GreaterThan);
					outputFormatter.PrintToken(Tokens.GreaterEqual);
					break;
				case AssignmentOperatorType.ExclusiveOr:
					outputFormatter.PrintToken(Tokens.XorAssign);
					break;
				case AssignmentOperatorType.Modulus:
					outputFormatter.PrintToken(Tokens.ModAssign);
					break;
				case AssignmentOperatorType.BitwiseAnd:
					outputFormatter.PrintToken(Tokens.BitwiseAndAssign);
					break;
				case AssignmentOperatorType.BitwiseOr:
					outputFormatter.PrintToken(Tokens.BitwiseOrAssign);
					break;
				case AssignmentOperatorType.Power:
					outputFormatter.PrintToken(Tokens.Assign);
					if (this.prettyPrintOptions.AroundAssignmentParentheses) {
						outputFormatter.Space();
					}
					VisitBinaryOperatorExpression(new BinaryOperatorExpression(assignmentExpression.Left,
					                                                           BinaryOperatorType.Power,
					                                                           assignmentExpression.Right), data);
					return null;
				default:
					Error(assignmentExpression, String.Format("Unknown assignment operator {0}", assignmentExpression.Op));
					return null;
			}
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(assignmentExpression.Right, data);
			return null;
		}
		
		public override object TrackedVisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Sizeof);
			if (prettyPrintOptions.SizeOfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinSizeOfParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(sizeOfExpression.TypeReference, data);
			if (this.prettyPrintOptions.WithinSizeOfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Typeof);
			if (prettyPrintOptions.TypeOfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinTypeOfParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(typeOfExpression.TypeReference, data);
			if (this.prettyPrintOptions.WithinTypeOfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Default);
			if (prettyPrintOptions.TypeOfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinTypeOfParentheses) {
				outputFormatter.Space();
			}
			TrackVisit(defaultValueExpression.TypeReference, data);
			if (this.prettyPrintOptions.WithinTypeOfParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitTypeOfIsExpression(TypeOfIsExpression typeOfIsExpression, object data)
		{
			TrackVisit(typeOfIsExpression.Expression, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Is);
			outputFormatter.Space();
			TrackVisit(typeOfIsExpression.TypeReference, data);
			return null;
		}
		
		public override object TrackedVisitAddressOfExpression(AddressOfExpression addressOfExpression, object data)
		{
			// C# 2.0 can reference methods directly
			return TrackVisit(addressOfExpression.Expression, data);
		}
		
		public override object TrackedVisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Delegate);
			
			if (anonymousMethodExpression.Parameters.Count > 0 || anonymousMethodExpression.HasParameterList) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				bool withinParentheses = this.prettyPrintOptions.WithinMethodDeclarationParentheses && anonymousMethodExpression.Parameters.Any ();
				if (withinParentheses) {
					outputFormatter.Space();
				}
				AppendCommaSeparatedList(anonymousMethodExpression.Parameters);
				if (withinParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			OutputBlockAllowInline(anonymousMethodExpression.Body, this.prettyPrintOptions.AnonymousMethodBraceStyle, false);
			return null;
		}
		
		public override object TrackedVisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			if (lambdaExpression.Parameters.Count == 1 && lambdaExpression.Parameters[0].TypeReference.IsNull) {
				// short syntax
				outputFormatter.PrintIdentifier(lambdaExpression.Parameters[0].ParameterName);
			} else {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				if (this.prettyPrintOptions.WithinParentheses) {
					outputFormatter.Space();
				}
				AppendCommaSeparatedList(lambdaExpression.Parameters);
				if (this.prettyPrintOptions.WithinParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.LambdaArrow);
			if (!lambdaExpression.ExpressionBody.IsNull) {
				outputFormatter.Space();
				TrackVisit(lambdaExpression.ExpressionBody, null);
			}
			if (!lambdaExpression.StatementBody.IsNull && lambdaExpression.StatementBody is BlockStatement) {
				OutputBlockAllowInline(lambdaExpression.StatementBody as BlockStatement, this.prettyPrintOptions.MethodBraceStyle, false);
			}
			return null;
		}
		
		public override object TrackedVisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Checked);
			if (prettyPrintOptions.CheckedParentheses) {
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinCheckedExpressionParantheses) {
				outputFormatter.Space();
			}
			TrackVisit(checkedExpression.Expression, data);
			if (this.prettyPrintOptions.WithinCheckedExpressionParantheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Unchecked);
			if (prettyPrintOptions.UncheckedParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (this.prettyPrintOptions.WithinCheckedExpressionParantheses) {
				outputFormatter.Space();
			}
			TrackVisit(uncheckedExpression.Expression, data);
			if (this.prettyPrintOptions.WithinCheckedExpressionParantheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object TrackedVisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			Expression target = pointerReferenceExpression.TargetObject;
			
			if (target is BinaryOperatorExpression || target is CastExpression) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
			}
			TrackVisit(target, data);
			if (target is BinaryOperatorExpression || target is CastExpression) {
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			outputFormatter.PrintToken(Tokens.Pointer);
			outputFormatter.PrintIdentifier(pointerReferenceExpression.MemberName);
			PrintTypeArgumentList(pointerReferenceExpression.TypeArguments);
			
			return null;
		}
		
		public override object TrackedVisitCastExpression(CastExpression castExpression, object data)
		{
			if (castExpression.CastType == CastType.TryCast) {
				TrackVisit(castExpression.Expression, data);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				TrackVisit(castExpression.CastTo, data);
			} else {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				if (prettyPrintOptions.WithinCastParentheses) {
					outputFormatter.Space();
				}
				TrackVisit(castExpression.CastTo, data);
				if (prettyPrintOptions.WithinCastParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
				if (this.prettyPrintOptions.SpacesAfterTypecast) {
					outputFormatter.Space();
				}
				TrackVisit(castExpression.Expression, data);
			}
			return null;
		}
		
		public override object TrackedVisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Stackalloc);
			outputFormatter.Space();
			TrackVisit(stackAllocExpression.TypeReference, data);
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			if (this.prettyPrintOptions.SpacesWithinBrackets) {
				outputFormatter.Space();
			}
			TrackVisit(stackAllocExpression.Expression, data);
			if (this.prettyPrintOptions.SpacesWithinBrackets) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			return null;
		}
		
		public override object TrackedVisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			TrackVisit(indexerExpression.TargetObject, data);
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			if (this.prettyPrintOptions.SpacesWithinBrackets) {
				outputFormatter.Space();
			}
			AppendCommaSeparatedList(indexerExpression.Indexes);
			if (this.prettyPrintOptions.SpacesWithinBrackets) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			return null;
		}
		
		public override object TrackedVisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.This);
			return null;
		}
		
		public override object TrackedVisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data) {
			outputFormatter.PrintToken(Tokens.Base);
			return null;
		}
		
		public override object TrackedVisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.New);
			if (!objectCreateExpression.CreateType.IsNull) {
				outputFormatter.Space();
				TrackVisit(objectCreateExpression.CreateType, data);
			}
			if (objectCreateExpression.Parameters.Count > 0 || objectCreateExpression.ObjectInitializer.IsNull) {
				if (prettyPrintOptions.NewParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				if (this.prettyPrintOptions.WithinMethodCallParentheses) {
					outputFormatter.Space();
				}
				AppendCommaSeparatedList(objectCreateExpression.Parameters);
				if (this.prettyPrintOptions.WithinMethodCallParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			if (!objectCreateExpression.ObjectInitializer.IsNull) {
				outputFormatter.Space();
				TrackVisit(objectCreateExpression.ObjectInitializer, data);
			}
			return null;
		}
		
		public override object TrackedVisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.New);
			if (arrayCreateExpression.IsImplicitlyTyped) {
				outputFormatter.PrintToken(Tokens.OpenSquareBracket);
				outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			} else {
				outputFormatter.Space();
				PrintTypeReferenceWithoutArray(arrayCreateExpression.CreateType);
				
				if (arrayCreateExpression.Arguments.Count > 0) {
					outputFormatter.PrintToken(Tokens.OpenSquareBracket);
					bool outputSpace = this.prettyPrintOptions.SpacesWithinBrackets && arrayCreateExpression.Arguments.Count > 0;
					if (outputSpace) {
						outputFormatter.Space();
					}
					for (int i = 0; i < arrayCreateExpression.Arguments.Count; ++i) {
						if (i > 0) PrintFormattedComma();
						TrackVisit(arrayCreateExpression.Arguments[i], data);
					}
					if (outputSpace) {
						outputFormatter.Space();
					}
					outputFormatter.PrintToken(Tokens.CloseSquareBracket);
					PrintArrayRank(arrayCreateExpression.CreateType.RankSpecifier, 1);
				} else {
					PrintArrayRank(arrayCreateExpression.CreateType.RankSpecifier, 0);
				}
			}
			
			if (!arrayCreateExpression.ArrayInitializer.IsNull) {
				outputFormatter.Space();
				TrackVisit(arrayCreateExpression.ArrayInitializer, data);
			}
			return null;
		}
		
		public override object TrackedVisitMemberInitializerExpression(MemberInitializerExpression memberInitializerExpression, object data)
		{
			outputFormatter.PrintIdentifier(memberInitializerExpression.Name);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Assign);
			outputFormatter.Space();
			TrackVisit(memberInitializerExpression.Expression, data);
			return null;
		}
		
		public override object TrackedVisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			Expression target = memberReferenceExpression.TargetObject;
			
			if (target is BinaryOperatorExpression || target is CastExpression) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				if (this.prettyPrintOptions.WithinMethodCallParentheses) {
					outputFormatter.Space();
				}
			}
			TrackVisit(target, data);
			if (target is BinaryOperatorExpression || target is CastExpression) {
				if (this.prettyPrintOptions.WithinMethodCallParentheses) {
					outputFormatter.Space();
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			outputFormatter.PrintToken(Tokens.Dot);
			outputFormatter.PrintIdentifier(memberReferenceExpression.MemberName);
			PrintTypeArgumentList(memberReferenceExpression.TypeArguments);
			return null;
		}
		
		public override object TrackedVisitDirectionExpression(DirectionExpression directionExpression, object data)
		{
			switch (directionExpression.FieldDirection) {
				case FieldDirection.Out:
					outputFormatter.PrintToken(Tokens.Out);
					outputFormatter.Space();
					break;
				case FieldDirection.Ref:
					outputFormatter.PrintToken(Tokens.Ref);
					outputFormatter.Space();
					break;
			}
			TrackVisit(directionExpression.Expression, data);
			return null;
		}
		
		public override object TrackedVisitCollectionInitializerExpression(CollectionInitializerExpression arrayInitializerExpression, object data)
		{
			outputFormatter.PrintToken (Tokens.OpenCurlyBrace);
			if (arrayInitializerExpression.CreateExpressions.Count == 1) {
				outputFormatter.Space ();
			} else {
				outputFormatter.IndentationLevel++;
				outputFormatter.NewLine ();
				outputFormatter.Indent ();
			}
			var createExprs = arrayInitializerExpression.CreateExpressions;
			for (int i = 0; i < createExprs.Count; i++) {
				if (i > 0) {
					PrintFormattedCommaAndNewLine();
				}
				NamedArgumentExpression nae = createExprs[i] as NamedArgumentExpression;
				if (nae != null) {
					outputFormatter.PrintIdentifier(nae.Name);
					if (prettyPrintOptions.AroundAssignmentParentheses)
						outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Assign);
					if (prettyPrintOptions.AroundAssignmentParentheses)
						outputFormatter.Space();
					nae.Expression.AcceptVisitor(this, data);
				} else {
					createExprs[i].AcceptVisitor(this, data);
				}
			}
			if (arrayInitializerExpression.CreateExpressions.Count == 1) {
				outputFormatter.Space ();
			} else {
				outputFormatter.IndentationLevel--;
				outputFormatter.NewLine();
				outputFormatter.Indent();
			}
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			return null;
		}
		
		public override object TrackedVisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			if (conditionalExpression.Condition is ConditionalExpression) {
				TrackedVisitParenthesizedExpression(new ParenthesizedExpression(conditionalExpression.Condition), data);
			} else {
				TrackVisit(conditionalExpression.Condition, data);
			}
			if (this.prettyPrintOptions.ConditionalOperatorBeforeConditionSpace) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Question);
			if (this.prettyPrintOptions.ConditionalOperatorAfterConditionSpace) {
				outputFormatter.Space();
			}
			TrackVisit(conditionalExpression.TrueExpression, data);
			if (this.prettyPrintOptions.ConditionalOperatorBeforeSeparatorSpace) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Colon);
			if (this.prettyPrintOptions.ConditionalOperatorAfterSeparatorSpace) {
				outputFormatter.Space();
			}
			TrackVisit(conditionalExpression.FalseExpression, data);
			return null;
		}
		
		#endregion
		#endregion
		
		void OutputModifier(ParameterModifiers modifier, INode node)
		{
			if ((modifier & ParameterModifiers.Ref) == ParameterModifiers.Ref) {
				outputFormatter.PrintToken(Tokens.Ref);
				outputFormatter.Space();
			} else if ((modifier & ParameterModifiers.Out) == ParameterModifiers.Out) {
				outputFormatter.PrintToken(Tokens.Out);
				outputFormatter.Space();
			}
			if ((modifier & ParameterModifiers.Params) == ParameterModifiers.Params) {
				outputFormatter.PrintToken(Tokens.Params);
				outputFormatter.Space();
			}
		}
		
		void OutputModifier(Modifiers modifier)
		{
			ArrayList tokenList = new ArrayList();
			if ((modifier & Modifiers.Unsafe) != 0) {
				tokenList.Add(Tokens.Unsafe);
			}
			if ((modifier & Modifiers.Public) != 0) {
				tokenList.Add(Tokens.Public);
			}
			if ((modifier & Modifiers.Private) != 0) {
				tokenList.Add(Tokens.Private);
			}
			if ((modifier & Modifiers.Protected) != 0) {
				tokenList.Add(Tokens.Protected);
			}
			if ((modifier & Modifiers.Static) != 0) {
				tokenList.Add(Tokens.Static);
			}
			if ((modifier & Modifiers.Internal) != 0) {
				tokenList.Add(Tokens.Internal);
			}
			if ((modifier & Modifiers.Override) != 0) {
				tokenList.Add(Tokens.Override);
			}
			if ((modifier & Modifiers.Abstract) != 0) {
				tokenList.Add(Tokens.Abstract);
			}
			if ((modifier & Modifiers.Virtual) != 0) {
				tokenList.Add(Tokens.Virtual);
			}
			if ((modifier & Modifiers.New) != 0) {
				tokenList.Add(Tokens.New);
			}
			if ((modifier & Modifiers.Sealed) != 0) {
				tokenList.Add(Tokens.Sealed);
			}
			if ((modifier & Modifiers.Extern) != 0) {
				tokenList.Add(Tokens.Extern);
			}
			if ((modifier & Modifiers.Const) != 0) {
				tokenList.Add(Tokens.Const);
			}
			if ((modifier & Modifiers.ReadOnly) != 0) {
				tokenList.Add(Tokens.Readonly);
			}
			if ((modifier & Modifiers.Volatile) != 0) {
				tokenList.Add(Tokens.Volatile);
			}
			if ((modifier & Modifiers.Fixed) != 0) {
				tokenList.Add(Tokens.Fixed);
			}
			outputFormatter.PrintTokenList(tokenList);
			
			if ((modifier & Modifiers.Partial) != 0) {
				outputFormatter.PrintText("partial ");
			}
		}
		
		object TrackVisit(INode node, object data)
		{
			return node.AcceptVisitor(this, data);
		}
		
		/// <summary>
		/// Resets the output formatter, sets Text to string.Empty.
		/// </summary>
		public void Reset ()
		{
			outputFormatter.Reset ();
		}
		
		public void AppendCommaSeparatedList<T>(ICollection<T> list) where T : class, INode
		{
			AppendCommaSeparatedList(list, false);
		}
		
		public void AppendCommaSeparatedList<T>(ICollection<T> list, bool alwaysBreakLine) where T : class, INode
		{
			if (list != null) {
				int i = 0;
				foreach (T node in list) {
					node.AcceptVisitor(this, null);
					if (i + 1 < list.Count) {
						if (alwaysBreakLine || (i + 1) % 10 == 0) {
							PrintFormattedCommaAndNewLine();
						} else {
							PrintFormattedComma();
						}
					}
					i++;
				}
			}
		}
		
		public override object TrackedVisitQueryExpression(QueryExpression queryExpression, object data)
		{
			if (queryExpression.IsQueryContinuation) {
				queryExpression.FromClause.Sources.First().AcceptVisitor(this, data);
			}
			outputFormatter.IndentationLevel++;
			if (queryExpression.IsQueryContinuation) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Into);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier(queryExpression.FromClause.Sources.First().Identifier);
			} else {
				queryExpression.FromClause.AcceptVisitor(this, data);
			}
			queryExpression.MiddleClauses.ForEach(PrintClause);
			PrintClause(queryExpression.SelectOrGroupClause);
			outputFormatter.IndentationLevel--;
			return null;
		}
		
		void PrintClause(QueryExpressionClause clause)
		{
			if (!clause.IsNull) {
				outputFormatter.NewLine();
				outputFormatter.Indent();
				clause.AcceptVisitor(this, null);
			}
		}
		
		public override object TrackedVisitQueryExpressionFromClause(QueryExpressionFromClause fromClause, object data)
		{
			CollectionRangeVariable variable = fromClause.Sources.Single();
			outputFormatter.PrintToken(Tokens.From);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(variable.Identifier);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.In);
			outputFormatter.Space();
			variable.Expression.AcceptVisitor(this, data);
			return null;
		}
		
		public override object TrackedVisitQueryExpressionJoinClause(QueryExpressionJoinClause joinClause, object data)
		{
			outputFormatter.PrintToken(Tokens.Join);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(joinClause.Source.Identifier);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.In);
			outputFormatter.Space();
			joinClause.Source.Expression.AcceptVisitor(this, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.On);
			outputFormatter.Space();
			joinClause.OnExpression.AcceptVisitor(this, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Equals);
			outputFormatter.Space();
			joinClause.EqualsExpression.AcceptVisitor(this, data);
			if (!string.IsNullOrEmpty(joinClause.IntoIdentifier)) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Into);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier(joinClause.IntoIdentifier);
			}
			return null;
		}
		
		public override object TrackedVisitQueryExpressionLetClause(QueryExpressionLetClause letClause, object data)
		{
			outputFormatter.PrintToken(Tokens.Let);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(letClause.Identifier);
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			outputFormatter.PrintToken(Tokens.Assign);
			if (this.prettyPrintOptions.AroundAssignmentParentheses) {
				outputFormatter.Space();
			}
			return letClause.Expression.AcceptVisitor(this, data);
		}
		
		public override object TrackedVisitQueryExpressionGroupClause(QueryExpressionGroupClause groupClause, object data)
		{
			outputFormatter.PrintToken(Tokens.Group);
			outputFormatter.Space();
			groupClause.Projection.AcceptVisitor(this, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.By);
			outputFormatter.Space();
			return groupClause.GroupBy.AcceptVisitor(this, data);
		}
		
		public override object TrackedVisitQueryExpressionOrderClause(QueryExpressionOrderClause queryExpressionOrderClause, object data)
		{
			outputFormatter.PrintToken(Tokens.Orderby);
			outputFormatter.Space();
			AppendCommaSeparatedList(queryExpressionOrderClause.Orderings);
			return null;
		}
		
		public override object TrackedVisitQueryExpressionOrdering(QueryExpressionOrdering ordering, object data)
		{
			ordering.Criteria.AcceptVisitor(this, data);
			if (ordering.Direction == QueryExpressionOrderingDirection.Ascending) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Ascending);
			} else if (ordering.Direction == QueryExpressionOrderingDirection.Descending) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Descending);
			}
			return null;
		}
		
		public override object TrackedVisitQueryExpressionSelectClause(QueryExpressionSelectClause selectClause, object data)
		{
			outputFormatter.PrintToken(Tokens.Select);
			outputFormatter.Space();
			return selectClause.Projection.AcceptVisitor(this, data);
		}
		
		public override object TrackedVisitQueryExpressionWhereClause(QueryExpressionWhereClause whereClause, object data)
		{
			outputFormatter.PrintToken(Tokens.Where);
			outputFormatter.Space();
			return whereClause.Condition.AcceptVisitor(this, data);
		}
	}
}
