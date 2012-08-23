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
	class CompletionDataWrapper
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
		
		public CompletionDataWrapper (CSharpCompletionEngine completion)
		{
			this.completion = completion;
		}
		
		public void Add (ICompletionData data)
		{
			result.Add (data);
		}


		public void AddCustom (string displayText, string description = null, string completionText = null)
		{
			result.Add (Factory.CreateLiteralCompletionData (displayText, description, completionText));
		}
		
		HashSet<string> usedNamespaces = new HashSet<string> ();
		
		public void AddNamespace (INamespace ns)
		{
			if (usedNamespaces.Contains (ns.Name))
				return;
			usedNamespaces.Add (ns.Name);
			result.Add (Factory.CreateNamespaceCompletionData (ns));
		}

		public void AddAlias(string alias)
		{
			result.Add (Factory.CreateLiteralCompletionData (alias));
		}


		HashSet<string> usedTypes = new HashSet<string> ();

		public ICompletionData AddType(IType type, string shortType)
		{
			if (type == null || string.IsNullOrEmpty(shortType) || usedTypes.Contains(shortType))
				return null;
			if (type.Name == "Void" && type.Namespace == "System")
				return null;

			var def = type.GetDefinition ();
			if (def != null && def.ParentAssembly != completion.ctx.CurrentAssembly && !def.IsBrowsable ())
				return null;

			usedTypes.Add(shortType);
			var iCompletionData = Factory.CreateTypeCompletionData(type, shortType);
			result.Add(iCompletionData);
			return iCompletionData;
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

		public ICompletionData AddMember (IMember member)
		{
			var newData = Factory.CreateEntityCompletionData (member);
			
			if (member.ParentAssembly != completion.ctx.CurrentAssembly && !member.IsBrowsable ())
				return null;

			string memberKey = newData.DisplayText;
			if (memberKey == null)
				return null;

			if (member is IMember) {
				newData.CompletionCategory = GetCompletionCategory (member.DeclaringTypeDefinition);
			}
			List<ICompletionData> existingData;
			data.TryGetValue (memberKey, out existingData);
			
			if (existingData != null) {
				var a = member as IEntity;
				foreach (var d in existingData) {
					if (!(d is IEntityCompletionData))
						continue;
					var b = ((IEntityCompletionData)d).Entity;
					if (a == null || b == null || a.EntityType == b.EntityType) {
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
				
				if (Type.ReflectionName == compareCategory.Type.ReflectionName)
					return 0;
				
				if (Type.GetAllBaseTypes ().Any (t => t.ReflectionName == compareCategory.Type.ReflectionName))
					return -1;
				return 1;
			}
		}
		HashSet<IType> addedEnums = new HashSet<IType> ();
		public void AddEnumMembers (IType resolvedType, CSharpResolver state, string typeString)
		{
			if (addedEnums.Contains (resolvedType))
				return;
			addedEnums.Add (resolvedType);
			if (typeString.Contains(".")) {
				AddType(resolvedType, typeString);
			}
			foreach (var field in resolvedType.GetFields ()) {
				if (field.IsPublic && (field.IsConst || field.IsStatic)) {
					Result.Add(Factory.CreateEntityCompletionData(
						field,
						typeString + "." + field.Name
						)
					           );
				}
			}
		}
	}
}


