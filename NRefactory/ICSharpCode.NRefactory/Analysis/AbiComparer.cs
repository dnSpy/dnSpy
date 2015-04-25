//
// ABIComparer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.Analysis
{
	/// <summary>
	/// Used to check the compatibility state of two compilations.
	/// </summary>
	public enum AbiCompatibility
	{
		/// <summary>
		/// The ABI is equal
		/// </summary>
		Equal,

		/// <summary>
		/// Some items got added, but the ABI remains to be compatible
		/// </summary>
		Bigger,

		/// <summary>
		/// The ABI has changed
		/// </summary>
		Incompatible
	}

	[Serializable]
	public sealed class AbiEventArgs : EventArgs
	{
		public string Message { get; set; }

		public AbiEventArgs(string message)
		{
			this.Message = message;
		}
	}

	/// <summary>
	/// The Abi comparer checks the public API of two compilation and determines the compatibility state.
	/// </summary>
	public class AbiComparer
	{
		public bool StopOnIncompatibility {
			get; 
			set;
		}
		void CheckContstraints(IType otype, ITypeParameter p1, ITypeParameter p2, ref AbiCompatibility compatibility)
		{
			if (p1.DirectBaseTypes.Count () != p2.DirectBaseTypes.Count () ||
			    p1.HasReferenceTypeConstraint != p2.HasReferenceTypeConstraint ||
			    p1.HasValueTypeConstraint != p2.HasValueTypeConstraint ||
			    p1.HasDefaultConstructorConstraint != p2.HasDefaultConstructorConstraint) {
				OnIncompatibilityFound (new AbiEventArgs (string.Format (TranslateString ("Type parameter constraints of type {0} have changed."), otype.FullName)));
				compatibility = AbiCompatibility.Incompatible;
			}
		}

		void CheckContstraints(IMethod omethod, ITypeParameter p1, ITypeParameter p2, ref AbiCompatibility compatibility)
		{
			if (p1.DirectBaseTypes.Count () != p2.DirectBaseTypes.Count () ||
			    p1.HasReferenceTypeConstraint != p2.HasReferenceTypeConstraint ||
			    p1.HasValueTypeConstraint != p2.HasValueTypeConstraint ||
			    p1.HasDefaultConstructorConstraint != p2.HasDefaultConstructorConstraint) {
				OnIncompatibilityFound (new AbiEventArgs (string.Format (TranslateString ("Type parameter constraints of method {0} have changed."), omethod.FullName)));
				compatibility = AbiCompatibility.Incompatible;
			}
		}

		void CheckTypes (ITypeDefinition oType, ITypeDefinition nType, ref AbiCompatibility compatibility)
		{
			int oldMemberCount = 0;
			Predicate<IUnresolvedMember> pred = null;
			if (oType.Kind == TypeKind.Class || oType.Kind == TypeKind.Struct)
				pred = m => (m.IsPublic || m.IsProtected) && !m.IsOverride && !m.IsSynthetic;

			for (int i = 0; i < oType.TypeParameterCount; i++) {
				CheckContstraints (oType, oType.TypeParameters[i], nType.TypeParameters[i], ref compatibility);
				if (compatibility == AbiCompatibility.Incompatible && StopOnIncompatibility)
					return;
			}

			foreach (var member in oType.GetMembers (pred, GetMemberOptions.IgnoreInheritedMembers)) {
				var newMember = nType.GetMembers (m => member.UnresolvedMember.Name == m.Name && m.IsPublic == member.IsPublic && m.IsProtected == member.IsProtected);
				var equalMember = newMember.FirstOrDefault (m => SignatureComparer.Ordinal.Equals (member, m));
				if (equalMember == null) {
					compatibility = AbiCompatibility.Incompatible;
					if (StopOnIncompatibility)
						return;
					continue;
				}
				var om = member as IMethod;
				if (om != null) {
					for (int i = 0; i < om.TypeParameters.Count; i++) {
						CheckContstraints (om, om.TypeParameters[i], ((IMethod)equalMember).TypeParameters[i], ref compatibility);
						if (compatibility == AbiCompatibility.Incompatible && StopOnIncompatibility)
							return;
					}
				}

				oldMemberCount++;
			}
			if (compatibility == AbiCompatibility.Bigger && oType.Kind != TypeKind.Interface)
				return;
			if (oldMemberCount != nType.GetMembers (pred, GetMemberOptions.IgnoreInheritedMembers).Count ()) {
				if (oType.Kind == TypeKind.Interface) {
					OnIncompatibilityFound (new AbiEventArgs (string.Format (TranslateString ("Interafce {0} has changed."), oType.FullName)));
					compatibility = AbiCompatibility.Incompatible;
				} else {
					if (compatibility == AbiCompatibility.Equal)
						compatibility = AbiCompatibility.Bigger;
				}
			}
		}

		void CheckNamespace(INamespace oNs, INamespace nNs, ref AbiCompatibility compatibility)
		{
			foreach (var type in oNs.Types) {
				if (!type.IsPublic && !type.IsProtected)
					continue;
				var newType = nNs.GetTypeDefinition (type.Name, type.TypeParameterCount);
				if (newType == null) {
					OnIncompatibilityFound (new AbiEventArgs (string.Format (TranslateString ("Type definition {0} is missing."), type.FullName)));
					compatibility = AbiCompatibility.Incompatible;
					if (StopOnIncompatibility) 
						return;
					continue;
				}
				CheckTypes (type, newType, ref compatibility);
				if (compatibility == AbiCompatibility.Incompatible && StopOnIncompatibility)
					return;
			}

			if (compatibility == AbiCompatibility.Bigger)
				return;
			foreach (var type in nNs.Types) {
				if (!type.IsPublic && !type.IsProtected)
					continue;
				if (oNs.GetTypeDefinition (type.Name, type.TypeParameterCount) == null) {
					if (compatibility == AbiCompatibility.Equal)
						compatibility = AbiCompatibility.Bigger;
					return;
				}
			}
		}

		static bool ContainsPublicTypes(INamespace testNs)
		{
			var stack = new Stack<INamespace> ();
			stack.Push (testNs);
			while (stack.Count > 0) {
				var ns = stack.Pop ();
				if (ns.Types.Any (t => t.IsPublic))
					return true;
				foreach (var child in ns.ChildNamespaces)
					stack.Push (child);
			}
			return false;
		}

		/// <summary>
		/// Check the specified oldProject and newProject if they're compatible.
		/// </summary>
		/// <param name="oldProject">Old project.</param>
		/// <param name="newProject">New project.</param>
		public AbiCompatibility Check (ICompilation oldProject, ICompilation newProject)
		{
			var oldStack = new Stack<INamespace> ();
			var newStack = new Stack<INamespace> ();
			oldStack.Push (oldProject.MainAssembly.RootNamespace);
			newStack.Push (newProject.MainAssembly.RootNamespace);

			AbiCompatibility compatibility = AbiCompatibility.Equal;
			while (oldStack.Count > 0) {
				var oNs = oldStack.Pop ();
				var nNs = newStack.Pop ();

				CheckNamespace (oNs, nNs, ref compatibility);
				if (compatibility == AbiCompatibility.Incompatible && StopOnIncompatibility)
					return AbiCompatibility.Incompatible;
				foreach (var child in oNs.ChildNamespaces) {
					var newChild = nNs.GetChildNamespace (child.Name);
					if (newChild == null) {
						OnIncompatibilityFound (new AbiEventArgs (string.Format (TranslateString ("Namespace {0} is missing."), child.FullName)));
						if (StopOnIncompatibility)
							return AbiCompatibility.Incompatible;
						continue;
					}
					oldStack.Push (child);
					newStack.Push (newChild);
				}

				// check if namespaces are added
				if (compatibility != AbiCompatibility.Bigger) {
					foreach (var child in nNs.ChildNamespaces) {
						if (oNs.GetChildNamespace (child.Name) == null) {
							if (compatibility == AbiCompatibility.Equal && ContainsPublicTypes (child))
								compatibility = AbiCompatibility.Bigger;
							break;
						}
					}
				}
			}
			return compatibility;
		}

		public virtual string TranslateString(string str)
		{
			return str;
		}

		public event EventHandler<AbiEventArgs> IncompatibilityFound;

		protected virtual void OnIncompatibilityFound(AbiEventArgs e)
		{
			var handler = IncompatibilityFound;
			if (handler != null)
				handler(this, e);
		}
	}
}

