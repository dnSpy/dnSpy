// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.SharpDevelop.Dom
{
	#region ResolveResult
	/// <summary>
	/// The base class of all resolve results.
	/// This class is used whenever an expression is not one of the special expressions
	/// (having their own ResolveResult class).
	/// The ResolveResult specified the location where Resolve was called (Class+Member)
	/// and the type of the resolved expression.
	/// </summary>
	public class ResolveResult : AbstractFreezable, ICloneable
	{
		IClass callingClass;
		IMember callingMember;
		IReturnType resolvedType;
		
		public ResolveResult(IClass callingClass, IMember callingMember, IReturnType resolvedType)
		{
			this.callingClass = callingClass;
			this.callingMember = callingMember;
			this.resolvedType = resolvedType;
		}
		
		public virtual bool IsValid {
			get { return true; }
		}
		
		/// <summary>
		/// Gets the class that contained the expression used to get this ResolveResult.
		/// Can be null when the class is unknown.
		/// </summary>
		public IClass CallingClass {
			get { return callingClass; }
		}
		
		/// <summary>
		/// Gets the member (method or property in <see cref="CallingClass"/>) that contained the
		/// expression used to get this ResolveResult.
		/// Can be null when the expression was not inside a member or the member is unknown.
		/// </summary>
		public IMember CallingMember {
			get { return callingMember; }
		}
		
		/// <summary>
		/// Gets the type of the resolved expression.
		/// Can be null when the type cannot be represented by a IReturnType (e.g. when the
		/// expression was a namespace name).
		/// </summary>
		public IReturnType ResolvedType {
			get { return resolvedType; }
			set {
				CheckBeforeMutation();
				resolvedType = value;
			}
		}
		
		public virtual ResolveResult Clone()
		{
			return new ResolveResult(callingClass, callingMember, resolvedType);
		}
		
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		
		bool showAllNamespacesContentsInCC = false;
		/// <summary>
		/// Gets code completion data for this ResolveResult.
		/// </summary>
		/// <param name="projectContent"></param>
		/// <param name="showItemsFromAllNamespaces">If true, items (e.g. extension methods) from all namespaces are returned, regardless current imports. Default is false.</param>
		/// <returns></returns>
		public List<ICompletionEntry> GetCompletionData(IProjectContent projectContent, bool showItemsFromAllNamespaces)
		{
			// Little hack - store value in a property to pass it to GetCompletionData(LanguageProperties language, bool showStatic)
			// Otherwise we would have to add it as a parameter to GetCompletionData(IProjectContent projectContent),
			// which would change signature in classes overriding this method as well.
			this.showAllNamespacesContentsInCC = showItemsFromAllNamespaces;
			var result = GetCompletionData(projectContent);
			this.showAllNamespacesContentsInCC = false;
			return result;
		}
		
		public virtual List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			return GetCompletionData(projectContent.Language, false);
		}
		
		protected List<ICompletionEntry> GetCompletionData(LanguageProperties language, bool showStatic)
		{
			if (resolvedType == null) return null;
			List<ICompletionEntry> res = new List<ICompletionEntry>();
			
			foreach (IMember m in MemberLookupHelper.GetAccessibleMembers(resolvedType, callingClass, language)) {
				if (language.ShowMember(m, showStatic))
					res.Add(m);
			}
			
			if (!showStatic && callingClass != null) {
				AddExtensions(language, res.Add, callingClass, resolvedType, this.showAllNamespacesContentsInCC);
			}
			
			return res;
		}
		
		/// <summary>
		/// Adds extension methods to <paramref name="res"/>.
		/// </summary>
		public static void AddExtensions(LanguageProperties language, Action<IMethodOrProperty> methodFound, IClass callingClass, IReturnType resolvedType, bool searchInAllNamespaces = false)
		{
			if (language == null)
				throw new ArgumentNullException("language");
			if (methodFound == null)
				throw new ArgumentNullException("methodFound");
			if (resolvedType == null)
				throw new ArgumentNullException("resolvedType");
			if (callingClass == null)
				throw new ArgumentNullException("callingClass");
			
			// convert resolvedType into direct type to speed up the IsApplicable lookups
			resolvedType = resolvedType.GetDirectReturnType();
			
			foreach (IMethodOrProperty mp in CtrlSpaceResolveHelper.FindAllExtensions(language, callingClass, searchInAllNamespaces)) {
				TryAddExtension(language, methodFound, mp, resolvedType);
			}
		}
		
		static void TryAddExtension(LanguageProperties language, Action<IMethodOrProperty> methodFound, IMethodOrProperty ext, IReturnType resolvedType)
		{
			// now add the extension method if it fits the type
			if (MemberLookupHelper.IsApplicable(resolvedType, ext.Parameters[0].ReturnType, ext as IMethod)) {
				IMethod method = ext as IMethod;
				if (method != null && method.TypeParameters.Count > 0) {
					IReturnType[] typeArguments = new IReturnType[method.TypeParameters.Count];
					MemberLookupHelper.InferTypeArgument(method.Parameters[0].ReturnType, resolvedType, typeArguments);
					for (int i = 0; i < typeArguments.Length; i++) {
						if (typeArguments[i] != null) {
							ext = (IMethod)ext.CreateSpecializedMember();
							ext.ReturnType = ConstructedReturnType.TranslateType(ext.ReturnType, typeArguments, true);
							for (int j = 0; j < ext.Parameters.Count; ++j) {
								ext.Parameters[j].ReturnType = ConstructedReturnType.TranslateType(ext.Parameters[j].ReturnType, typeArguments, true);
							}
							break;
						}
					}
				}
				methodFound(ext);
			}
		}
		
		public virtual FilePosition GetDefinitionPosition()
		{
			// this is only possible on some subclasses of ResolveResult
			return FilePosition.Empty;
		}
		
		/// <summary>
		/// Gets if this ResolveResult represents a reference to the specified entity.
		/// </summary>
		public virtual bool IsReferenceTo(IEntity entity)
		{
			return false;
		}
	}
	#endregion
	
	#region MixedResolveResult
	/// <summary>
	/// The MixedResolveResult is used when an expression can have multiple meanings, for example
	/// "Size" in a class deriving from "Control".
	/// </summary>
	public class MixedResolveResult : ResolveResult
	{
		ResolveResult primaryResult, secondaryResult;

		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			primaryResult.Freeze();
			secondaryResult.Freeze();
		}
		
		public ResolveResult PrimaryResult {
			get {
				return primaryResult;
			}
		}
		
		public IEnumerable<ResolveResult> Results {
			get {
				yield return primaryResult;
				yield return secondaryResult;
			}
		}
		
		public TypeResolveResult TypeResult {
			get {
				if (primaryResult is TypeResolveResult)
					return (TypeResolveResult)primaryResult;
				if (secondaryResult is TypeResolveResult)
					return (TypeResolveResult)secondaryResult;
				return null;
			}
		}
		
		public MixedResolveResult(ResolveResult primaryResult, ResolveResult secondaryResult)
			: base(primaryResult.CallingClass, primaryResult.CallingMember, primaryResult.ResolvedType)
		{
			if (primaryResult == null)
				throw new ArgumentNullException("primaryResult");
			if (secondaryResult == null)
				throw new ArgumentNullException("secondaryResult");
			this.primaryResult = primaryResult;
			this.secondaryResult = secondaryResult;
		}
		
		public override FilePosition GetDefinitionPosition()
		{
			return primaryResult.GetDefinitionPosition();
		}
		
		public override List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			List<ICompletionEntry> result = primaryResult.GetCompletionData(projectContent);
			List<ICompletionEntry> result2 = secondaryResult.GetCompletionData(projectContent);
			if (result == null)  return result2;
			if (result2 == null) return result;
			foreach (ICompletionEntry o in result2) {
				if (!result.Contains(o))
					result.Add(o);
			}
			return result;
		}
		
		public override ResolveResult Clone()
		{
			return new MixedResolveResult(primaryResult.Clone(), secondaryResult.Clone());
		}
		
		public override bool IsReferenceTo(IEntity entity)
		{
			return primaryResult.IsReferenceTo(entity) || secondaryResult.IsReferenceTo(entity);
		}
	}
	#endregion
	
	#region LocalResolveResult
	/// <summary>
	/// The LocalResolveResult is used when an expression was a simple local variable
	/// or parameter.
	/// </summary>
	/// <remarks>
	/// For fields in the current class, a MemberResolveResult is used, so "e" is not always
	/// a LocalResolveResult.
	/// </remarks>
	public class LocalResolveResult : ResolveResult
	{
		IField field;
		
		public LocalResolveResult(IMember callingMember, IField field)
			: base(callingMember.DeclaringType, callingMember, field.ReturnType)
		{
			if (callingMember == null)
				throw new ArgumentNullException("callingMember");
			if (field == null)
				throw new ArgumentNullException("field");
			this.field = field;
			if (!field.IsParameter && !field.IsLocalVariable) {
				throw new ArgumentException("the field must either be a local variable-field or a parameter-field");
			}
		}
		
		public LocalResolveResult(IMember callingMember, IParameter parameter)
			: this(callingMember, new DefaultField.ParameterField(parameter.ReturnType, parameter.Name, parameter.Region, callingMember.DeclaringType))
		{
		}
		
		public override ResolveResult Clone()
		{
			return new LocalResolveResult(this.CallingMember, this.Field);
		}
		
		/// <summary>
		/// Gets the field representing the local variable.
		/// </summary>
		public IField Field {
			get { return field; }
		}
		
		/// <summary>
		/// Gets if the variable is a parameter (true) or a local variable (false).
		/// </summary>
		public bool IsParameter {
			get { return field.IsParameter; }
		}
		
		/// <summary>
		/// Gets the name of the parameter/local variable.
		/// </summary>
		public string VariableName {
			get { return field.Name; }
		}
		
		/// <summary>
		/// Gets th position where the parameter/local variable is declared.
		/// </summary>
		public DomRegion VariableDefinitionRegion {
			get { return field.Region; }
		}
		
		public override FilePosition GetDefinitionPosition()
		{
			ICompilationUnit cu = this.CallingClass.CompilationUnit;
			if (cu == null) {
				return FilePosition.Empty;
			}
			if (cu.FileName == null || cu.FileName.Length == 0) {
				return FilePosition.Empty;
			}
			DomRegion reg = field.Region;
			if (!reg.IsEmpty) {
				return new FilePosition(cu.FileName, reg.BeginLine, reg.BeginColumn);
			} else {
				LoggingService.Warn("GetDefinitionPosition: field.Region is empty");
				return new FilePosition(cu.FileName);
			}
		}
		
		public override bool IsReferenceTo(IEntity entity)
		{
			IField f = entity as IField;
			if (f != null && (f.IsLocalVariable || f.IsParameter)) {
				return field.Region.BeginLine == f.Region.BeginLine
					&& field.Region.BeginColumn == f.Region.BeginColumn;
			}
			return base.IsReferenceTo(entity);
		}
	}
	#endregion
	
	#region NamespaceResolveResult
	/// <summary>
	/// The NamespaceResolveResult is used when an expression was the name of a namespace.
	/// <see cref="ResolveResult.ResolvedType"/> is always null on a NamespaceReturnType.
	/// </summary>
	/// <example>
	/// Example expressions:
	/// "System"
	/// "System.Windows.Forms"
	/// "using Win = System.Windows;  Win.Forms"
	/// </example>
	public class NamespaceResolveResult : ResolveResult
	{
		string name;
		
		public NamespaceResolveResult(IClass callingClass, IMember callingMember, string name)
			: base(callingClass, callingMember, null)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.name = name;
		}
		
		/// <summary>
		/// Gets the name of the namespace.
		/// </summary>
		public string Name {
			get {
				return name;
			}
		}
		
		public override List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			return projectContent.GetNamespaceContents(name);
		}
		
		public override ResolveResult Clone()
		{
			return new NamespaceResolveResult(this.CallingClass, this.CallingMember, this.Name);
		}
	}
	#endregion
	
	#region IntegerLiteralResolveResult
	/// <summary>
	/// The IntegerLiteralResolveResult is used when an expression was an integer literal.
	/// It is a normal ResolveResult with a type of "int", but does not provide
	/// any code completion data.
	/// </summary>
	public class IntegerLiteralResolveResult : ResolveResult
	{
		public IntegerLiteralResolveResult(IClass callingClass, IMember callingMember, IReturnType systemInt32)
			: base(callingClass, callingMember, systemInt32)
		{
		}
		
		public override List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			return null;
		}
		
		public override ResolveResult Clone()
		{
			return new IntegerLiteralResolveResult(this.CallingClass, this.CallingMember, this.ResolvedType);
		}
	}
	#endregion
	
	#region TypeResolveResult
	/// <summary>
	/// The TypeResolveResult is used when an expression was the name of a type.
	/// This resolve result makes code completion show the static members instead
	/// of the instance members.
	/// </summary>
	/// <example>
	/// Example expressions:
	/// "System.EventArgs"
	/// "using System;  EventArgs"
	/// </example>
	public class TypeResolveResult : ResolveResult
	{
		IClass resolvedClass;
		
		public TypeResolveResult(IClass callingClass, IMember callingMember, IClass resolvedClass)
			: base(callingClass, callingMember, resolvedClass.DefaultReturnType)
		{
			this.resolvedClass = resolvedClass;
		}
		
		public TypeResolveResult(IClass callingClass, IMember callingMember, IReturnType resolvedType, IClass resolvedClass)
			: base(callingClass, callingMember, resolvedType)
		{
			this.resolvedClass = resolvedClass;
		}
		
		public TypeResolveResult(IClass callingClass, IMember callingMember, IReturnType resolvedType)
			: base(callingClass, callingMember, resolvedType)
		{
			this.resolvedClass = resolvedType.GetUnderlyingClass();
		}
		
		public override ResolveResult Clone()
		{
			return new TypeResolveResult(this.CallingClass, this.CallingMember, this.ResolvedType, this.ResolvedClass);
		}
		
		/// <summary>
		/// Gets the class corresponding to the resolved type.
		/// This property can be null when the type has no class (for example a type parameter).
		/// </summary>
		public IClass ResolvedClass {
			get {
				return resolvedClass;
			}
		}
		
		public override List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			List<ICompletionEntry> ar = GetCompletionData(projectContent.Language, true);
			if (resolvedClass != null) {
				ar.AddRange(resolvedClass.GetCompoundClass().GetAccessibleTypes(CallingClass));
			}
			return ar;
		}
		
		public override FilePosition GetDefinitionPosition()
		{
			if (resolvedClass == null) {
				return FilePosition.Empty;
			}
			ICompilationUnit cu = resolvedClass.CompilationUnit;
			if (cu == null || cu.FileName == null || cu.FileName.Length == 0) {
				return FilePosition.Empty;
			}
			DomRegion reg = resolvedClass.Region;
			if (!reg.IsEmpty)
				return new FilePosition(cu.FileName, reg.BeginLine, reg.BeginColumn);
			else
				return new FilePosition(cu.FileName);
		}
		
		public override bool IsReferenceTo(IEntity entity)
		{
			IClass c = entity as IClass;
			return c != null && resolvedClass != null
				&& resolvedClass.FullyQualifiedName == c.FullyQualifiedName
				&& resolvedClass.TypeParameters.Count == c.TypeParameters.Count;
		}
	}
	#endregion
	
	#region MemberResolveResult
	/// <summary>
	/// The MemberResolveResult is used when an expression was a member
	/// (field, property, event or method call).
	/// </summary>
	/// <example>
	/// Example expressions:
	/// "(any expression).fieldName"
	/// "(any expression).eventName"
	/// "(any expression).propertyName"
	/// "(any expression).methodName(arguments)" (methods only when method parameters are part of expression)
	/// "using System;  EventArgs.Empty"
	/// "fieldName" (when fieldName is a field in the current class)
	/// "new System.Windows.Forms.Button()" (constructors are also methods)
	/// "SomeMethod()" (when SomeMethod is a method in the current class)
	/// "System.Console.WriteLine(text)"
	/// </example>
	public class MemberResolveResult : ResolveResult
	{
		IMember resolvedMember;
		
		public MemberResolveResult(IClass callingClass, IMember callingMember, IMember resolvedMember)
			: base(callingClass, callingMember, resolvedMember.ReturnType)
		{
			if (resolvedMember == null)
				throw new ArgumentNullException("resolvedMember");
			this.resolvedMember = resolvedMember;
		}
		
		public override ResolveResult Clone()
		{
			return new MemberResolveResult(this.CallingClass, this.CallingMember, this.ResolvedMember) {
				IsExtensionMethodCall = IsExtensionMethodCall
			};
		}
		
		bool isExtensionMethodCall;
		
		public bool IsExtensionMethodCall {
			get { return isExtensionMethodCall; }
			set {
				CheckBeforeMutation();
				isExtensionMethodCall = value;
			}
		}
		
		/// <summary>
		/// Gets the member that was resolved.
		/// </summary>
		public IMember ResolvedMember {
			get { return resolvedMember; }
		}
		
		public override FilePosition GetDefinitionPosition()
		{
			return GetDefinitionPosition(resolvedMember);
		}
		
		internal static FilePosition GetDefinitionPosition(IMember resolvedMember)
		{
			IClass declaringType = resolvedMember.DeclaringType;
			if (declaringType == null) {
				return FilePosition.Empty;
			}
			ICompilationUnit cu = declaringType.CompilationUnit;
			if (cu == null) {
				return FilePosition.Empty;
			}
			if (cu.FileName == null || cu.FileName.Length == 0) {
				return FilePosition.Empty;
			}
			DomRegion reg = resolvedMember.Region;
			if (!reg.IsEmpty)
				return new FilePosition(cu.FileName, reg.BeginLine, reg.BeginColumn);
			else
				return new FilePosition(cu.FileName);
		}
		
		public override bool IsReferenceTo(IEntity entity)
		{
			IClass c = entity as IClass;
			if (c != null) {
				IMethod m = resolvedMember as IMethod;
				return m != null && m.IsConstructor
					&& m.DeclaringType.FullyQualifiedName == c.FullyQualifiedName
					&& m.DeclaringType.TypeParameters.Count == c.TypeParameters.Count;
			} else {
				return MemberLookupHelper.IsSimilarMember(resolvedMember, entity as IMember);
			}
		}
	}
	#endregion
	
	#region MethodResolveResult
	public class MethodGroup : AbstractFreezable, IList<IMethod>
	{
		IList<IMethod> innerList;
		bool isExtensionMethodGroup;
		
		public MethodGroup() : this(new List<IMethod>())
		{
		}
		
		public MethodGroup(IList<IMethod> innerList)
		{
			if (innerList == null)
				throw new ArgumentNullException("innerList");
			this.innerList = innerList;
		}
		
		public bool IsExtensionMethodGroup {
			get { return isExtensionMethodGroup; }
			set {
				CheckBeforeMutation();
				isExtensionMethodGroup = value;
			}
		}
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			innerList = FreezeList(innerList);
		}
		
		public IMethod this[int index] {
			get { return innerList[index]; }
			set { innerList[index] = value; }
		}
		
		public int Count {
			get { return innerList.Count; }
		}
		
		public bool IsReadOnly {
			get { return innerList.IsReadOnly; }
		}
		
		public int IndexOf(IMethod item)
		{
			return innerList.IndexOf(item);
		}
		
		public void Insert(int index, IMethod item)
		{
			innerList.Insert(index, item);
		}
		
		public void RemoveAt(int index)
		{
			innerList.RemoveAt(index);
		}
		
		public void Add(IMethod item)
		{
			innerList.Add(item);
		}
		
		public void Clear()
		{
			innerList.Clear();
		}
		
		public bool Contains(IMethod item)
		{
			return innerList.Contains(item);
		}
		
		public void CopyTo(IMethod[] array, int arrayIndex)
		{
			innerList.CopyTo(array, arrayIndex);
		}
		
		public bool Remove(IMethod item)
		{
			return innerList.Remove(item);
		}
		
		public IEnumerator<IMethod> GetEnumerator()
		{
			return innerList.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
	
	/// <summary>
	/// The MethodResolveResult is used when an expression was the name of a method,
	/// but there were no parameters specified so the exact overload could not be found.
	/// <see cref="ResolveResult.ResolvedType"/> is always null on a MethodReturnType.
	/// </summary>
	/// <example>
	/// Example expressions:
	/// "System.Console.WriteLine"
	/// "a.Add"      (where a is List&lt;string&gt;)
	/// "SomeMethod" (when SomeMethod is a method in the current class)
	/// </example>
	public class MethodGroupResolveResult : ResolveResult
	{
		string name;
		IReturnType containingType;
		IList<MethodGroup> possibleMethods;
		
		public bool IsVBNetAddressOf { get; set; }
		
		public MethodGroupResolveResult(IClass callingClass, IMember callingMember, IReturnType containingType, string name)
			: base(callingClass, callingMember, null)
		{
			if (containingType == null)
				throw new ArgumentNullException("containingType");
			if (name == null)
				throw new ArgumentNullException("name");
			this.containingType = containingType;
			this.name = name;
			this.ResolvedType = new MethodGroupReturnType();
		}
		
		public MethodGroupResolveResult(IClass callingClass, IMember callingMember, IReturnType containingType, string name,
		                                IList<MethodGroup> possibleMethods)
			: base(callingClass, callingMember, null)
		{
			if (containingType == null)
				throw new ArgumentNullException("containingType");
			if (name == null)
				throw new ArgumentNullException("name");
			if (possibleMethods == null)
				throw new ArgumentNullException("possibleMethods");
			this.containingType = containingType;
			this.name = name;
			this.possibleMethods = possibleMethods;
			this.ResolvedType = new MethodGroupReturnType();
		}
		
		public MethodGroupResolveResult(IClass callingClass, IMember callingMember, IReturnType containingType, string name,
		                                IList<MethodGroup> possibleMethods, bool isVBNet, bool isAddressOf)
			: this(callingClass, callingMember, containingType, name, possibleMethods)
		{
			IMethod parameterlessMethod = possibleMethods.SelectMany(list => list).FirstOrDefault(m => !m.Parameters.Any());;
			if (isVBNet && !isAddressOf && parameterlessMethod != null)
				this.ResolvedType = parameterlessMethod.ReturnType;
		}
		
		public override ResolveResult Clone()
		{
			return new MethodGroupResolveResult(this.CallingClass, this.CallingMember, this.ContainingType, this.Name, this.Methods);
		}
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			if (possibleMethods != null) {
				possibleMethods = FreezeList(possibleMethods);
			}
		}
		
		/// <summary>
		/// Gets the name of the method.
		/// </summary>
		public string Name {
			get { return name; }
		}
		
		/// <summary>
		/// Gets the class that contains the method.
		/// This property cannot be null.
		/// </summary>
		public IReturnType ContainingType {
			get { return containingType; }
		}
		
		/// <summary>
		/// The list of possible methods.
		/// </summary>
		public IList<MethodGroup> Methods {
			get {
				if (possibleMethods == null) {
					possibleMethods = FreezeList(
						new MethodGroup[] {
							new MethodGroup(
								containingType.GetMethods().FindAll((IMethod m) => m.Name == this.name)
							)
						});
				}
				return possibleMethods;
			}
		}
		
		public IMethod GetMethodIfSingleOverload()
		{
			if (this.Methods.Count > 0 && this.Methods[0].Count == 1)
				return this.Methods[0][0];
			else
				return null;
		}
		
		public IMethod GetMethodWithEmptyParameterList()
		{
			if (this.Methods.Count > 0 && !IsVBNetAddressOf) {
				return this.Methods
					.SelectMany(group => group.Select(item => item))
					.FirstOrDefault(i => i.Parameters.Count == 0);
			}
			
			return null;
		}
		
		public override FilePosition GetDefinitionPosition()
		{
			IMethod m = GetMethodIfSingleOverload();
			IMethod m2 = GetMethodWithEmptyParameterList();
			if (m != null)
				return MemberResolveResult.GetDefinitionPosition(m);
			else if (m2 != null)
				return MemberResolveResult.GetDefinitionPosition(m2);
			else
				return base.GetDefinitionPosition();
		}
		
		public override bool IsReferenceTo(IEntity entity)
		{
			return MemberLookupHelper.IsSimilarMember(GetMethodIfSingleOverload(), entity as IMember);
		}
	}
	#endregion
	
	#region VBBaseOrThisReferenceInConstructorResolveResult
	/// <summary>
	/// Is used for "MyBase" or "Me" in VB constructors to show "New" in the completion list.
	/// </summary>
	public class VBBaseOrThisReferenceInConstructorResolveResult : ResolveResult
	{
		public VBBaseOrThisReferenceInConstructorResolveResult(IClass callingClass, IMember callingMember, IReturnType referencedType)
			: base(callingClass, callingMember, referencedType)
		{
		}
		
		public override List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			List<ICompletionEntry> res = base.GetCompletionData(projectContent);
			foreach (IMethod m in this.ResolvedType.GetMethods()) {
				if (m.IsConstructor && !m.IsStatic && m.IsAccessible(this.CallingClass, true))
					res.Add(m);
			}
			return res;
		}
		
		public override ResolveResult Clone()
		{
			return new VBBaseOrThisReferenceInConstructorResolveResult(this.CallingClass, this.CallingMember, this.ResolvedType);
		}
	}
	#endregion
	
	#region BaseResolveResult
	/// <summary>
	/// Is used for "base"/"MyBase" expression.
	/// The completion list always shows protected members.
	/// </summary>
	public class BaseResolveResult : ResolveResult
	{
		public BaseResolveResult(IClass callingClass, IMember callingMember, IReturnType baseClassType)
			: base(callingClass, callingMember, baseClassType)
		{
		}
		
		public override List<ICompletionEntry> GetCompletionData(IProjectContent projectContent)
		{
			if (this.ResolvedType == null) return null;
			List<ICompletionEntry> res = new List<ICompletionEntry>();
			
			foreach (IMember m in MemberLookupHelper.GetAccessibleMembers(this.ResolvedType, this.CallingClass, projectContent.Language, true)) {
				if (projectContent.Language.ShowMember(m, false))
					res.Add(m);
			}
			
			if (this.CallingClass != null) {
				AddExtensions(projectContent.Language, res.Add, this.CallingClass, this.ResolvedType);
			}
			
			return res;
		}
		
		public override ResolveResult Clone()
		{
			return new BaseResolveResult(this.CallingClass, this.CallingMember, this.ResolvedType);
		}
	}
	#endregion
	
	#region DelegateCallResolveResult
	/// <summary>
	/// Is used for calls to delegates/events.
	/// </summary>
	public class DelegateCallResolveResult : ResolveResult
	{
		IMethod delegateInvokeMethod;
		ResolveResult targetRR;
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			delegateInvokeMethod.Freeze();
			targetRR.Freeze();
		}
		
		public DelegateCallResolveResult(ResolveResult targetRR, IMethod delegateInvokeMethod)
			: base(targetRR.CallingClass, targetRR.CallingMember, delegateInvokeMethod.ReturnType)
		{
			this.targetRR = targetRR;
			this.delegateInvokeMethod = delegateInvokeMethod;
		}
		
		/// <summary>
		/// Gets the Invoke() method of the delegate.
		/// </summary>
		public IMethod DelegateInvokeMethod {
			get { return delegateInvokeMethod; }
		}
		
		/// <summary>
		/// Gets the type of the delegate.
		/// </summary>
		public IReturnType DelegateType {
			get { return targetRR.ResolvedType; }
		}
		
		/// <summary>
		/// Gets the resolve result referring to the delegate.
		/// </summary>
		public ResolveResult Target {
			get { return targetRR; }
		}
		
		public override ResolveResult Clone()
		{
			return new DelegateCallResolveResult(targetRR, delegateInvokeMethod);
		}
		
		public override FilePosition GetDefinitionPosition()
		{
			return targetRR.GetDefinitionPosition();
		}
		
		public override bool IsReferenceTo(ICSharpCode.SharpDevelop.Dom.IEntity entity)
		{
			return targetRR.IsReferenceTo(entity);
		}
	}
	#endregion
	
	#region UnknownIdentifierResolveResult
	/// <summary>
	/// Used for unknown identifiers.
	/// </summary>
	public class UnknownIdentifierResolveResult : ResolveResult
	{
		string identifier;
		
		public UnknownIdentifierResolveResult(IClass callingClass, IMember callingMember, string identifier)
			: base(callingClass, callingMember, null)
		{
			this.identifier = identifier;
		}
		
		public string Identifier {
			get { return identifier; }
		}
		
		public override bool IsValid {
			get { return false; }
		}
		
		public override ResolveResult Clone()
		{
			return new UnknownIdentifierResolveResult(this.CallingClass, this.CallingMember, this.Identifier);
		}
	}
	#endregion
	
	#region UnknownMethodResolveResult
	/// <summary>
	/// Used for calls to unknown methods.
	/// </summary>
	public class UnknownMethodResolveResult : ResolveResult
	{
		string callName;
		bool isStaticContext;
		List<IReturnType> arguments;
		IReturnType target;
		
		public UnknownMethodResolveResult(IClass callingClass, IMember callingMember, IReturnType target, string callName, bool isStaticContext, List<IReturnType> arguments)
			: base(callingClass, callingMember, null)
		{
			this.target = target == null ? (callingClass == null ? null : callingClass.DefaultReturnType) : target;
			this.callName = callName;
			this.arguments = arguments;
			this.isStaticContext = isStaticContext;
		}
		
		public bool IsStaticContext {
			get { return isStaticContext; }
		}
		
		public string CallName {
			get { return callName; }
		}
		
		public IReturnType Target {
			get { return target; }
		}
		
		public List<IReturnType> Arguments {
			get { return arguments; }
		}
		
		public override bool IsValid {
			get { return false; }
		}
		
		public override ResolveResult Clone()
		{
			return new UnknownMethodResolveResult(this.CallingClass, this.CallingMember, this.target, this.callName, this.isStaticContext, this.arguments);
		}
	}
	#endregion
	
	#region UnknownConstructorCallResolveResult
	/// <summary>
	/// Used for constructor calls on unknown types.
	/// </summary>
	public class UnknownConstructorCallResolveResult : ResolveResult
	{
		string typeName;
		
		public UnknownConstructorCallResolveResult(IClass callingClass, IMember callingMember, string typeName)
			: base(callingClass, callingMember, null)
		{
			this.typeName = typeName;
		}
		
		public string TypeName {
			get { return typeName; }
		}
		
		public override bool IsValid {
			get { return false; }
		}
		
		public override ResolveResult Clone()
		{
			return new UnknownConstructorCallResolveResult(this.CallingClass, this.CallingMember, this.TypeName);
		}
	}
	#endregion
}
