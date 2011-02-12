// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.SharpDevelop.Dom.Refactoring;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class LanguageProperties
	{
		/// <summary>
		/// A case-sensitive dummy language that returns false for all Supports.. properties,
		/// uses a dummy code generator and refactoring provider and returns null for CodeDomProvider.
		/// </summary>
		public readonly static LanguageProperties None = new LanguageProperties(StringComparer.Ordinal);
		
		/// <summary>
		/// C# 3.0 language properties.
		/// </summary>
		public readonly static LanguageProperties CSharp = new CSharpProperties();
		
		/// <summary>
		/// VB.Net 8 language properties.
		/// </summary>
		public readonly static LanguageProperties VBNet = new VBNetProperties();
		
		public LanguageProperties(StringComparer nameComparer)
		{
			this.nameComparer = nameComparer;
		}
		
		#region Language-specific service providers
		readonly StringComparer nameComparer;
		
		public StringComparer NameComparer {
			get {
				return nameComparer;
			}
		}
		
		public virtual CodeGenerator CodeGenerator {
			get {
				return CodeGenerator.DummyCodeGenerator;
			}
		}
		
		public virtual RefactoringProvider RefactoringProvider {
			get {
				return RefactoringProvider.DummyProvider;
			}
		}
		
		/// <summary>
		/// Gets the ambience for this language. Because the IAmbience interface is not thread-safe, every call
		/// creates a new instance.
		/// </summary>
		public virtual IAmbience GetAmbience()
		{
			return new CSharp.CSharpAmbience();
		}
		
		/// <summary>
		/// Gets the CodeDomProvider for this language. Can return null!
		/// </summary>
		public virtual System.CodeDom.Compiler.CodeDomProvider CodeDomProvider {
			get {
				return null;
			}
		}
		
		sealed class DummyCodeDomProvider : System.CodeDom.Compiler.CodeDomProvider
		{
			public static readonly DummyCodeDomProvider Instance = new DummyCodeDomProvider();
			
			[Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class.")]
			public override System.CodeDom.Compiler.ICodeGenerator CreateGenerator()
			{
				return null;
			}
			
			[Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
			public override System.CodeDom.Compiler.ICodeCompiler CreateCompiler()
			{
				return null;
			}
		}
		#endregion
		
		#region Supports...
		/// <summary>
		/// Gets if the language supports calling C# 3-style extension methods
		/// (first parameter = instance parameter)
		/// </summary>
		public virtual bool SupportsExtensionMethods {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if the language supports calling extension properties
		/// (first parameter = instance parameter)
		/// </summary>
		public virtual bool SupportsExtensionProperties {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if extension methods/properties are searched in imported classes (returns true) or if
		/// only the extensions from the current class, imported classes and imported modules are used
		/// (returns false). This property has no effect if the language doesn't support extension methods or properties.
		/// </summary>
		public virtual bool SearchExtensionsInClasses {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if namespaces are imported (i.e. Imports System, Dim a As Collections.ArrayList)
		/// </summary>
		public virtual bool ImportNamespaces {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if modules are imported with their namespace (i.e. Microsoft.VisualBasic.Randomize()).
		/// </summary>
		public virtual bool ImportModules {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if classes can be imported (i.e. Imports System.Math)
		/// </summary>
		public virtual bool CanImportClasses {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if the language allows partial classes where the partial modifier is not
		/// used on any part.
		/// </summary>
		public virtual bool ImplicitPartialClasses {
			get { return false; }
		}
		
		/// <summary>
		/// Allow invoking an object constructor outside of ExpressionContext.ObjectCreation.
		/// Used for Boo, which creates instances like this: 'self.Size = Size(10, 20)'
		/// </summary>
		public virtual bool AllowObjectConstructionOutsideContext {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if the language supports implicit interface implementations.
		/// </summary>
		public virtual bool SupportsImplicitInterfaceImplementation {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if the language enforces that explicit interface implementations are uncallable except through
		/// the interface itself.
		/// If this property is false, code generators may assume that multiple explicit interface implementations
		/// with conflicting return types are invalid unless they are renamed.
		/// </summary>
		public virtual bool ExplicitInterfaceImplementationIsPrivateScope {
			get { return false; }
		}
		
		/// <summary>
		/// Gets if events explicitly implementing an interface require add {} remove {} regions.
		/// </summary>
		public virtual bool RequiresAddRemoveRegionInExplicitInterfaceImplementation {
			get { return false; }
		}
		
		/// <summary>
		/// Gets the start token of an indexer expression in the language. Usually '[' or '('.
		/// </summary>
		public virtual string IndexerExpressionStartToken {
			get { return "["; }
		}
		
		public virtual TextFinder GetFindClassReferencesTextFinder(IClass c)
		{
			// when finding attribute references, also look for the short form of the name
			if (c.Name.Length > 9 && nameComparer.Equals(c.Name.Substring(c.Name.Length - 9), "Attribute")) {
				return new CombinedTextFinder(
					new WholeWordTextFinder(c.Name.Substring(0, c.Name.Length - 9), nameComparer),
					new WholeWordTextFinder(c.Name, nameComparer)
				);
			}
			return new WholeWordTextFinder(c.Name, nameComparer);
		}
		
		public virtual TextFinder GetFindMemberReferencesTextFinder(IMember member)
		{
			IProperty property = member as IProperty;
			if (property != null && property.IsIndexer) {
				return new IndexBeforeTextFinder(IndexerExpressionStartToken);
			} else {
				return new WholeWordTextFinder(member.Name, nameComparer);
			}
		}
		
		public virtual bool IsClassWithImplicitlyStaticMembers(IClass c)
		{
			return false;
		}
		#endregion
		
		#region Code-completion filters
		public virtual bool ShowInNamespaceCompletion(IClass c)
		{
			return true;
		}
		
		public virtual bool ShowMember(IMember member, bool showStatic)
		{
			IProperty property = member as IProperty;
			if (property != null && property.IsIndexer) {
				return false;
			}
			IMethod method = member as IMethod;
			if (method != null && (method.IsConstructor || method.IsOperator)) {
				return false;
			}
			return member.IsStatic == showStatic;
		}
		
		public virtual bool ShowMemberInOverrideCompletion(IMember member)
		{
			return true;
		}
		#endregion
		
		/// <summary>
		/// Generates the default imports statements a new application for this language should use.
		/// </summary>
		public virtual IUsing CreateDefaultImports(IProjectContent pc)
		{
			return null;
		}
		
		public override string ToString()
		{
			return "[" + base.ToString() + "]";
		}
		
		public static LanguageProperties GetLanguage(string language)
		{
			switch(language)
			{
				case "VBNet":
				case "VB":
					return LanguageProperties.VBNet;
				default:
					return LanguageProperties.CSharp;
			}
		}
		
		#region CSharpProperties
		internal sealed class CSharpProperties : LanguageProperties
		{
			public CSharpProperties() : base(StringComparer.Ordinal) {}
			
			public override RefactoringProvider RefactoringProvider {
				get {
					return NRefactoryRefactoringProvider.NRefactoryCSharpProviderInstance;
				}
			}
			
			public override CodeGenerator CodeGenerator {
				get {
					return CSharpCodeGenerator.Instance;
				}
			}
			
			public override System.CodeDom.Compiler.CodeDomProvider CodeDomProvider {
				get {
					return new Microsoft.CSharp.CSharpCodeProvider();
				}
			}
			
			public override bool SupportsImplicitInterfaceImplementation {
				get { return true; }
			}
			
			public override bool ExplicitInterfaceImplementationIsPrivateScope {
				get { return true; }
			}
			
			/// <summary>
			/// Gets if events explicitly implementing an interface require add {} remove {} regions.
			/// </summary>
			public override bool RequiresAddRemoveRegionInExplicitInterfaceImplementation {
				get { return true; }
			}
			
			public override bool SupportsExtensionMethods {
				get { return true; }
			}
			
			public override bool SearchExtensionsInClasses {
				get { return true; }
			}
			
			public override string ToString()
			{
				return "[LanguageProperties: C#]";
			}
			
			public override TextFinder GetFindMemberReferencesTextFinder(IMember member)
			{
				IMethod method = member as IMethod;
				if (method != null && method.IsConstructor) {
					return new CombinedTextFinder(
						new WholeWordTextFinder(member.DeclaringType.Name, this.NameComparer),
						new WholeWordTextFinder("this", this.NameComparer),
						new WholeWordTextFinder("base", this.NameComparer)
					);
				} else {
					return base.GetFindMemberReferencesTextFinder(member);
				}
			}
			
			public override bool ShowMember(IMember member, bool showStatic)
			{
				if (!base.ShowMember(member, showStatic))
					return false;
				// do not show 'Finalize' methods (they are not directly callable from C#)
				IMethod method = member as IMethod;
				if (method != null) {
					if (method.Name == "Finalize" && method.Parameters.Count == 0)
						return false;
				}
				return true;
			}
			
			public override bool ShowMemberInOverrideCompletion(IMember member)
			{
				IMethod method = member as IMethod;
				
				if (method != null) {
					if (method.Name == "Finalize" && method.Parameters.Count == 0)
						return false;
				}
				
				return base.ShowMemberInOverrideCompletion(member);
			}
		}
		#endregion
		
		#region VBNetProperties
		internal sealed class VBNetProperties : LanguageProperties
		{
			public VBNetProperties() : base(StringComparer.OrdinalIgnoreCase) {}
			
			public override bool ShowMember(IMember member, bool showStatic)
			{
				if (member is ArrayReturnType.ArrayIndexer) {
					return false;
				}
				IMethod method = member as IMethod;
				if (method != null && (method.IsConstructor || method.IsOperator)) {
					return false;
				}
				return member.IsStatic || !showStatic;
			}
			
			public override bool ImportNamespaces {
				get {
					return true;
				}
			}
			
			public override bool ImportModules {
				get {
					return true;
				}
			}
			
			public override bool CanImportClasses {
				get {
					return true;
				}
			}
			
			public override bool SupportsExtensionMethods {
				get { return true; }
			}
			
			public override bool SearchExtensionsInClasses {
				get { return true; }
			}
			
			public override bool IsClassWithImplicitlyStaticMembers(IClass c)
			{
				return c.ClassType == ClassType.Module;
			}
			
			public override bool ShowInNamespaceCompletion(IClass c)
			{
				foreach (IAttribute attr in c.Attributes) {
					if (attr.AttributeType.FullyQualifiedName == "Microsoft.VisualBasic.HideModuleNameAttribute")
						return false;
				}
				return base.ShowInNamespaceCompletion(c);
			}
			
			public override IUsing CreateDefaultImports(IProjectContent pc)
			{
				DefaultUsing u = new DefaultUsing(pc);
				u.Usings.Add("Microsoft.VisualBasic");
				u.Usings.Add("System");
				u.Usings.Add("System.Collections");
				u.Usings.Add("System.Collections.Generic");
				u.Usings.Add("System.Drawing");
				u.Usings.Add("System.Diagnostics");
				u.Usings.Add("System.Windows.Forms");
				return u;
			}
			
			public override RefactoringProvider RefactoringProvider {
				get {
					return NRefactoryRefactoringProvider.NRefactoryVBNetProviderInstance;
				}
			}
			
			public override CodeGenerator CodeGenerator {
				get {
					return VBNetCodeGenerator.Instance;
				}
			}
			
			public override System.CodeDom.Compiler.CodeDomProvider CodeDomProvider {
				get {
					return new Microsoft.VisualBasic.VBCodeProvider();
				}
			}
			
			public override IAmbience GetAmbience()
			{
				return new VBNet.VBNetAmbience();
			}
			
			public override string IndexerExpressionStartToken {
				get { return "("; }
			}
			
			public override string ToString()
			{
				return "[LanguageProperties: VB.NET]";
			}
		}
		#endregion
		
		#region Text Finder
		protected sealed class WholeWordTextFinder : TextFinder
		{
			readonly string searchedText;
			readonly bool caseInsensitive;
			
			public WholeWordTextFinder(string word, StringComparer nameComparer)
			{
				if (word == null) word = string.Empty;
				
				caseInsensitive = nameComparer.Equals("a", "A");
				if (caseInsensitive)
					this.searchedText = word.ToLowerInvariant();
				else
					this.searchedText = word;
			}
			
			public override string PrepareInputText(string inputText)
			{
				if (caseInsensitive)
					return inputText.ToLowerInvariant();
				else
					return inputText;
			}
			
			public override TextFinderMatch Find(string inputText, int startPosition)
			{
				if (searchedText.Length == 0)
					return TextFinderMatch.Empty;
				int pos = startPosition - 1;
				while ((pos = inputText.IndexOf(searchedText, pos + 1)) >= 0) {
					if (pos > 0 && char.IsLetterOrDigit(inputText, pos - 1)) {
						continue; // memberName is not a whole word (a.SomeName cannot reference Name)
					}
					if (pos < inputText.Length - searchedText.Length - 1
					    && char.IsLetterOrDigit(inputText, pos + searchedText.Length))
					{
						continue; // memberName is not a whole word (a.Name2 cannot reference Name)
					}
					return new TextFinderMatch(pos, searchedText.Length);
				}
				return TextFinderMatch.Empty;
			}
		}
		
		protected sealed class CombinedTextFinder : TextFinder
		{
			readonly TextFinder[] finders;
			
			public CombinedTextFinder(params TextFinder[] finders)
			{
				if (finders == null)
					throw new ArgumentNullException("finders");
				if (finders.Length == 0)
					throw new ArgumentException("finders.Length must be > 0");
				this.finders = finders;
			}
			
			public override string PrepareInputText(string inputText)
			{
				return finders[0].PrepareInputText(inputText);
			}
			
			public override TextFinderMatch Find(string inputText, int startPosition)
			{
				TextFinderMatch best = TextFinderMatch.Empty;
				foreach (TextFinder f in finders) {
					TextFinderMatch r = f.Find(inputText, startPosition);
					if (r.Position >= 0 && (best.Position < 0 || r.Position < best.Position)) {
						best = r;
					}
				}
				return best;
			}
		}
		
		protected sealed class IndexBeforeTextFinder : TextFinder
		{
			readonly string searchText;
			
			public IndexBeforeTextFinder(string searchText)
			{
				this.searchText = searchText;
			}
			
			public override TextFinderMatch Find(string inputText, int startPosition)
			{
				int pos = inputText.IndexOf(searchText, startPosition);
				if (pos >= 0) {
					return new TextFinderMatch(pos, searchText.Length, pos - 1);
				} else {
					return TextFinderMatch.Empty;
				}
			}
		}
		#endregion
	}
}
