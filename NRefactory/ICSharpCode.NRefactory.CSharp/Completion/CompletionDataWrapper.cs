// 
// CompletionDataWrapper.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public class CompletionDataWrapper
	{
		CSharpCompletionEngine completion;
		List<ICompletionData> result = new List<ICompletionData> ();
		
		public List<ICompletionData> Result {
			get {
				return result;
			}
		}
		
		ICompletionDataFactory Factory {
			get {
				return completion.factory;
			}
		}

		internal bool AnonymousDelegateAdded {
			get;
			set;
		}
		
		public CompletionDataWrapper (CSharpCompletionEngine completion)
		{
			this.completion = completion;
		}
		
		public void Add (ICompletionData data)
		{
			result.Add (data);
		}


		public ICompletionData AddCustom (string displayText, string description = null, string completionText = null)
		{
			var literalCompletionData = Factory.CreateLiteralCompletionData(displayText, description, completionText);
			result.Add(literalCompletionData);
			return literalCompletionData;
		}
		
		HashSet<string> usedNamespaces = new HashSet<string> ();

		bool IsAccessible(MemberLookup lookup, INamespace ns)
		{
			if (ns.Types.Any (t => lookup.IsAccessible (t, false)))
				return true;
			foreach (var child in ns.ChildNamespaces)
				if (IsAccessible (lookup, child))
					return true;
			return false;
		}
		
		public void AddNamespace (MemberLookup lookup, INamespace ns)
		{
			if (usedNamespaces.Contains (ns.Name))
				return;
			if (!IsAccessible (lookup, ns)) {
				usedNamespaces.Add (ns.Name);
				return;
			}
			usedNamespaces.Add (ns.Name);
			result.Add (Factory.CreateNamespaceCompletionData (ns));
		}

		public void AddAlias(string alias)
		{
			result.Add (Factory.CreateLiteralCompletionData (alias));
		}

		Dictionary<string, ICompletionData> typeDisplayText = new Dictionary<string, ICompletionData> ();
		Dictionary<IType, ICompletionData> addedTypes = new Dictionary<IType, ICompletionData> ();

		public ICompletionData AddConstructors(IType type, bool showFullName, bool isInAttributeContext = false)
		{
			return InternalAddType(type, showFullName, isInAttributeContext, true);
		}

		public ICompletionData AddType(IType type, bool showFullName, bool isInAttributeContext = false)
		{
			return InternalAddType(type, showFullName, isInAttributeContext, false);
		}

		ICompletionData InternalAddType(IType type, bool showFullName, bool isInAttributeContext, bool addConstrurs)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (type.Name == "Void" && type.Namespace == "System" || type.Kind == TypeKind.Unknown)
				return null;
			if (addedTypes.ContainsKey (type))
				return addedTypes[type];
			usedNamespaces.Add(type.Name);
			var def = type.GetDefinition();
			if (def != null && def.ParentAssembly != completion.ctx.CurrentAssembly) {
				switch (completion.EditorBrowsableBehavior) {
					case EditorBrowsableBehavior.Ignore:
						break;
					case EditorBrowsableBehavior.Normal:
						var state = def.GetEditorBrowsableState();
						if (state != System.ComponentModel.EditorBrowsableState.Always)
							return null;
						break;
					case EditorBrowsableBehavior.IncludeAdvanced:
						if (!def.IsBrowsable())
							return null;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			ICompletionData usedType;
			var data = Factory.CreateTypeCompletionData(type, showFullName, isInAttributeContext, addConstrurs);
			var text = data.DisplayText;
			if (typeDisplayText.TryGetValue(text, out usedType)) {
				usedType.AddOverload(data);
				return usedType;
			} 
			typeDisplayText [text] = data;
			result.Add(data);
			addedTypes[type] = data;
			return data;
		}

		Dictionary<string, List<ICompletionData>> data = new Dictionary<string, List<ICompletionData>> ();
		
		public ICompletionData AddVariable(IVariable variable)
		{
			if (data.ContainsKey(variable.Name))
				return null;
			data [variable.Name] = new List<ICompletionData>();
			var cd = Factory.CreateVariableCompletionData(variable);
			result.Add(cd);
			return cd;
		}
		
		public ICompletionData AddNamedParameterVariable(IVariable variable)
		{
			var name = variable.Name + ":";
			if (data.ContainsKey(name))
				return null;
			data [name] = new List<ICompletionData>();
			
			var cd = Factory.CreateVariableCompletionData(variable);
			cd.CompletionText += ":";
			cd.DisplayText += ":";
			result.Add(cd);
			return cd;
		}
		
		public void AddTypeParameter (ITypeParameter variable)
		{
			if (data.ContainsKey (variable.Name))
				return;
			data [variable.Name] = new List<ICompletionData> ();
			result.Add (Factory.CreateVariableCompletionData (variable));
		}

		public void AddTypeImport(ITypeDefinition type, bool useFullName, bool addForTypeCreation)
		{
			result.Add(Factory.CreateImportCompletionData(type, useFullName, addForTypeCreation));
		}

		public ICompletionData AddMember (IMember member)
		{
			var newData = Factory.CreateEntityCompletionData (member);
			
			if (member.ParentAssembly != completion.ctx.CurrentAssembly) {
				switch (completion.EditorBrowsableBehavior) {
					case EditorBrowsableBehavior.Ignore:
						break;
					case EditorBrowsableBehavior.Normal:
						var state = member.GetEditorBrowsableState();
						if (state != System.ComponentModel.EditorBrowsableState.Always)
							return null;
						break;
					case EditorBrowsableBehavior.IncludeAdvanced:
						if (!member.IsBrowsable())
							return null;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			string memberKey = newData.DisplayText;
			if (memberKey == null)
				return null;

			newData.CompletionCategory = GetCompletionCategory (member.DeclaringTypeDefinition);

			List<ICompletionData> existingData;
			data.TryGetValue (memberKey, out existingData);
			if (existingData != null) {
				if (member.SymbolKind == SymbolKind.Field || member.SymbolKind == SymbolKind.Property || member.SymbolKind == SymbolKind.Event)
					return null;
				var a = member as IEntity;
				foreach (var d in existingData) {
					if (!(d is IEntityCompletionData))
						continue;
					var b = ((IEntityCompletionData)d).Entity;
					if (a == null || b == null || a.SymbolKind == b.SymbolKind) {
						d.AddOverload (newData);
						return d;
					} 
				}
				if (newData != null) {
					result.Add (newData);
					data [memberKey].Add (newData);
				}
			} else {
				result.Add (newData);
				data [memberKey] = new List<ICompletionData> ();
				data [memberKey].Add (newData);
			}
			return newData;
		}
		
		internal CompletionCategory GetCompletionCategory (IType type)
		{
			if (type == null)
				return null;
			if (!completionCategories.ContainsKey (type))
				completionCategories [type] = new TypeCompletionCategory (type);
			return completionCategories [type];
		}
		
		Dictionary<IType, CompletionCategory> completionCategories = new Dictionary<IType, CompletionCategory> ();
		class TypeCompletionCategory : CompletionCategory
		{
			public IType Type {
				get;
				private set;
			}
			
			public TypeCompletionCategory (IType type) : base (type.FullName, null)
			{
				this.Type = type;
			}
			
			public override int CompareTo (CompletionCategory other)
			{
				var compareCategory = other as TypeCompletionCategory;
				if (compareCategory == null)
					return -1;
				int result;
				if (Type.ReflectionName == compareCategory.Type.ReflectionName) {
					result = 0;
				} else if (Type.GetAllBaseTypes().Any(t => t.ReflectionName == compareCategory.Type.ReflectionName)) {
					result = -1;
				} else if (compareCategory.Type.GetAllBaseTypes().Any(t => t.ReflectionName == Type.ReflectionName)) {
					result = 1;
				} else {
					var d = Type.GetDefinition ();
					var ct = compareCategory.Type.GetDefinition();
					if (ct.IsStatic && d.IsStatic) {
						result = d.FullName.CompareTo (ct.FullName);
					} else if (d.IsStatic) {
						result = 1;
					}else if (ct.IsStatic) {
						result = -1;
					} else {
						result = 0;
					}
				}
				return result;
			}
		}
		HashSet<IType> addedEnums = new HashSet<IType> ();
		public ICompletionData AddEnumMembers (IType resolvedType, CSharpResolver state)
		{
			if (addedEnums.Contains (resolvedType))
				return null;
			addedEnums.Add (resolvedType);
			var result = AddType(resolvedType, true);
			foreach (var field in resolvedType.GetFields ()) {
				if (field.IsPublic && (field.IsConst || field.IsStatic)) {
					Result.Add(Factory.CreateMemberCompletionData(resolvedType, field));
				}
			}
			return result;
		}
		HashSet<string> anonymousSignatures = new HashSet<string> ();

		public bool HasAnonymousDelegateAdded(string signature)
		{
			return anonymousSignatures.Contains(signature); 
		}

		public void AddAnonymousDelegateAdded(string signature)
		{
			anonymousSignatures.Add(signature); 
		}
	}
}


