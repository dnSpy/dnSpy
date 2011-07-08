// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.VB
{
	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	[Export(typeof(Language))]
	public class VBLanguage : Language
	{
		Predicate<IAstTransform> transformAbortCondition = null;
		bool showAllMembers = false;
		
		public VBLanguage()
		{
		}
		
		public override string Name {
			get { return "VB"; }
		}
		
		public override string FileExtension {
			get { return ".vb"; }
		}
		
		public override string ProjectFileExtension {
			get { return ".vbproj"; }
		}
		
		public override void WriteCommentLine(ITextOutput output, string comment)
		{
			output.WriteLine("' " + comment);
		}
		
		public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(method.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddMethod(method);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}
		
		public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}
		
		public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(field.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddField(field);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}
		
		public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}
		
		public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}
		
		public override bool ShowMember(MemberReference member)
		{
			return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilationOptions().DecompilerSettings);
		}
		
		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationOptions options)
		{
			astBuilder.RunTransformations(transformAbortCondition);
			if (options.DecompilerSettings.ShowXmlDocumentation)
				AddXmlDocTransform.Run(astBuilder.CompilationUnit);
			var unit = astBuilder.CompilationUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}
		
		AstBuilder CreateAstBuilder(DecompilationOptions options, ModuleDefinition currentModule = null, TypeDefinition currentType = null, bool isSingleMember = false)
		{
			if (currentModule == null)
				currentModule = currentType.Module;
			DecompilerSettings settings = options.DecompilerSettings;
			if (isSingleMember) {
				settings = settings.Clone();
				settings.UsingDeclarations = false;
			}
			return new AstBuilder(
				new DecompilerContext(currentModule) {
					CancellationToken = options.CancellationToken,
					CurrentType = currentType,
					Settings = settings
				});
		}
		
		public override string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
		{
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;
			var astType = AstBuilder
				.ConvertType(type, typeAttributes, options)
				.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);

			StringWriter w = new StringWriter();
			// TODO
//			if (type.IsByReference) {
//				ParameterDefinition pd = typeAttributes as ParameterDefinition;
//				if (pd != null && (!pd.IsIn && pd.IsOut))
//					w.Write("out ");
//				else
//					w.Write("ref ");
//
//				if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
//					((ComposedType)astType).PointerRank--;
//			}
			
			astType.AcceptVisitor(new OutputVisitor(w, new VBFormattingOptions()), null);
			return w.ToString();
		}
	}
	
	public class ILSpyEnvironmentProvider : IEnvironmentProvider
	{
		public string RootNamespace {
			get {
				return "";
			}
		}
		
		public string GetTypeNameForAttribute(ICSharpCode.NRefactory.CSharp.Attribute attribute)
		{
			return attribute.Type.Annotations
				.OfType<Mono.Cecil.MemberReference>()
				.First()
				.FullName;
		}
		
		public ClassType GetClassTypeForAstType(ICSharpCode.NRefactory.CSharp.AstType type)
		{
			var definition = type.Annotations.OfType<TypeReference>().First().ResolveOrThrow();
			
			if (definition.IsClass)
				return ClassType.Class;
			if (definition.IsInterface)
				return ClassType.Interface;
			if (definition.IsEnum)
				return ClassType.Enum;
			if (definition.IsFunctionPointer)
				return ClassType.Delegate;
			if (definition.IsValueType)
				return ClassType.Struct;
			
			return ClassType.Module;
		}
		
		public IType ResolveExpression(ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			throw new NotImplementedException();
		}
	}
}
