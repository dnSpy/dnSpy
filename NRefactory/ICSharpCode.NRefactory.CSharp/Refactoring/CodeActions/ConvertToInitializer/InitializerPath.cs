//
// InitializerPath.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	class InitializerPath
	{
		InitializerPath(object anchor)
		{
			this.anchor = anchor;
			MemberPath = new List<IMember>();
		}

		public InitializerPath(IVariable target) : this((object)target)
		{
		}

		public InitializerPath(IMember target) : this((object)target)
		{
		}

		public static InitializerPath FromResolveResult(ResolveResult resolveResult)
		{
			InitializerPath initializerPath = null;
			var memberPath = new List<IMember>();
			var currentResolveResult = resolveResult;
			do {
				if (currentResolveResult is MemberResolveResult) {
					var memberResolveResult = (MemberResolveResult)currentResolveResult;
					memberPath.Add(memberResolveResult.Member);
					currentResolveResult = memberResolveResult.TargetResult;
				} else if (currentResolveResult is LocalResolveResult) {
					var localResolveResult = (LocalResolveResult)currentResolveResult;
					memberPath.Reverse();
					initializerPath = new InitializerPath(localResolveResult.Variable) {
						MemberPath = memberPath
					};
					break;
				} else if (currentResolveResult is ThisResolveResult) {
					break;
				} else {
					return null;
				}

			} while (currentResolveResult != null);

			if (initializerPath == null) {
				// This path is rooted at a member
				memberPath.Reverse();
				initializerPath = new InitializerPath(memberPath [0]) {
					MemberPath = memberPath.Skip(1).ToList()
				};
			}
			return initializerPath;
		}

		public InitializerPath GetParentPath()
		{
			if (MemberPath.Count < 1)
				throw new InvalidOperationException("Cannot get the parent path of a path that does not contain any members.");
			return new InitializerPath(anchor) {
				MemberPath = MemberPath.Take(MemberPath.Count - 1).ToList()
			};
		}

		public bool IsSubPath(InitializerPath other)
		{
			if (!other.anchor.Equals(anchor))
				return false;
			if (MemberPath.Count <= other.MemberPath.Count)
				return false;
			for (int i = 0; i < other.MemberPath.Count; i++) {
				if (MemberPath [i] != other.MemberPath [i])
					return false;
			}
			return true;
		}

		public int Level { get { return MemberPath.Count + 1; } }

		object anchor;

		public IVariable VariableRoot {
			get { return anchor as IVariable; }
		}

		public IMember MemberRoot {
			get { return anchor as IMember; }
		}

		public string RootName {
			get {
				if (anchor is IMember)
					return (anchor as IMember).Name;
				else
					return (anchor as IVariable).Name;
			}
		}

		public IList<IMember> MemberPath { get; private set; }

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(InitializerPath))
				return false;

			var other = (InitializerPath)obj;

			if (!object.Equals(anchor, other.anchor))
				return false;

			if (MemberPath.Count != other.MemberPath.Count)
				return false;

			for (int i = 0; i < MemberPath.Count; i++) {
				if (!object.Equals(MemberPath [i], other.MemberPath [i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hash = anchor.GetHashCode();
			foreach (var member in MemberPath)
				hash ^= member.GetHashCode();
			return hash;
		}

		public static bool operator==(InitializerPath left, InitializerPath right)
		{
			return object.Equals(left, right);
		}

		public static bool operator!=(InitializerPath left, InitializerPath right)
		{
			return !object.Equals(left, right);
		}

		public override string ToString()
		{
			if (MemberPath.Count > 0)
				return string.Format("[InitializerPath: {0}.{1}]", RootName,
				                     string.Join(".", MemberPath.Select<IMember, string>(member => member.Name)));
			else
				return string.Format("[InitializerPath: {0}]", RootName);
		}
	}

}

