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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Decompiler;
using Decompiler.Transforms;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Decompiler logic for C#.
	/// </summary>
	public class CSharpLanguage : Language
	{
		string name = "C#";
		bool showAllMembers;
		Predicate<IAstVisitor<object, object>> transformAbortCondition = null;
		
		public CSharpLanguage()
		{
		}
		
		#if DEBUG
		internal static IEnumerable<CSharpLanguage> GetDebugLanguages()
		{
			string lastTransformName = "no transforms";
			foreach (Type _transformType in TransformationPipeline.CreatePipeline(new DecompilerContext()).Select(v => v.GetType()).Distinct()) {
				Type transformType = _transformType; // copy for lambda
				yield return new CSharpLanguage {
					transformAbortCondition = v => transformType.IsInstanceOfType(v),
					name = "C# - " + lastTransformName,
					showAllMembers = true
				};
				lastTransformName = "after " + transformType.Name;
			}
			yield return new CSharpLanguage {
				name = "C# - " + lastTransformName,
				showAllMembers = true
			};
		}
		#endif
		
		public override string Name {
			get { return name; }
		}
		
		public override string FileExtension {
			get { return ".cs"; }
		}
		
		public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, method.DeclaringType);
			codeDomBuilder.AddMethod(method);
			codeDomBuilder.GenerateCode(output, transformAbortCondition);
		}
		
		public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, property.DeclaringType);
			codeDomBuilder.AddProperty(property);
			codeDomBuilder.GenerateCode(output, transformAbortCondition);
		}
		
		public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, field.DeclaringType);
			codeDomBuilder.AddField(field);
			codeDomBuilder.GenerateCode(output, transformAbortCondition);
		}
		
		public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, ev.DeclaringType);
			codeDomBuilder.AddEvent(ev);
			codeDomBuilder.GenerateCode(output, transformAbortCondition);
		}
		
		public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, type);
			codeDomBuilder.AddType(type);
			codeDomBuilder.GenerateCode(output, transformAbortCondition);
		}
		
		public override void DecompileAssembly(AssemblyDefinition assembly, string fileName, ITextOutput output, DecompilationOptions options)
		{
			if (options.FullDecompilation) {
				foreach (TypeDefinition type in assembly.MainModule.Types) {
					AstBuilder codeDomBuilder = CreateAstBuilder(options, type);
					codeDomBuilder.AddType(type);
					codeDomBuilder.GenerateCode(output, transformAbortCondition);
					output.WriteLine();
				}
			} else {
				base.DecompileAssembly(assembly, fileName, output, options);
			}
		}
		
		AstBuilder CreateAstBuilder(DecompilationOptions options, TypeDefinition currentType)
		{
			return new AstBuilder(
				new DecompilerContext {
					CancellationToken = options.CancellationToken,
					CurrentType = currentType
				});
		}
		
		public override string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes)
		{
			AstType astType = AstBuilder.ConvertType(type, typeAttributes);
			if (!includeNamespace) {
				var tre = new TypeReferenceExpression { Type = astType };
				tre.AcceptVisitor(new RemoveNamespaceFromType(), null);
				astType = tre.Type;
			}
			
			StringWriter w = new StringWriter();
			if (type.IsByReference) {
				ParameterDefinition pd = typeAttributes as ParameterDefinition;
				if (pd != null && (!pd.IsIn && pd.IsOut))
					w.Write("out ");
				else
					w.Write("ref ");
			}
			
			astType.AcceptVisitor(new OutputVisitor(w, new CSharpFormattingPolicy()), null);
			return w.ToString();
		}
		
		sealed class RemoveNamespaceFromType : DepthFirstAstVisitor<object, object>
		{
			public override object VisitMemberType(MemberType memberType, object data)
			{
				base.VisitMemberType(memberType, data);
				SimpleType st = memberType.Target as SimpleType;
				if (st != null && !st.TypeArguments.Any()) {
					var ta = memberType.TypeArguments.ToArray();
					memberType.TypeArguments = null;
					memberType.ReplaceWith(new SimpleType { Identifier = memberType.MemberName, TypeArguments = ta });
				}
				return null;
			}
		}
		
		public override bool ShowMember(MemberReference member)
		{
			if (showAllMembers) {
				return true;
			}
			MethodDefinition method = member as MethodDefinition;
			if (method != null && (method.IsGetter || method.IsSetter || method.IsAddOn || method.IsRemoveOn))
				return false;
			TypeDefinition type = member as TypeDefinition;
			if (type != null && type.Name.StartsWith("<>c__DisplayClass", StringComparison.Ordinal) && type.IsCompilerGenerated())
				return false;
			return true;
		}
	}
}
