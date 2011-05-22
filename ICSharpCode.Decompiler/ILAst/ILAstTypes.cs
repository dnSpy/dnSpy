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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CSharp;
using Cecil = Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public abstract class ILNode
	{
		public IEnumerable<T> GetSelfAndChildrenRecursive<T>(Func<T, bool> predicate = null) where T: ILNode
		{
			List<T> result = new List<T>(16);
			AccumulateSelfAndChildrenRecursive(result, predicate);
			return result;
		}
		
		void AccumulateSelfAndChildrenRecursive<T>(List<T> list, Func<T, bool> predicate) where T:ILNode
		{
			T thisAsT = this as T;
			if (thisAsT != null && (predicate == null || predicate(thisAsT)))
				list.Add(thisAsT);
			foreach (ILNode node in this.GetChildren()) {
				if (node != null)
					node.AccumulateSelfAndChildrenRecursive(list, predicate);
			}
		}
		
		public virtual IEnumerable<ILNode> GetChildren()
		{
			yield break;
		}
		
		public override string ToString()
		{
			StringWriter w = new StringWriter();
			WriteTo(new PlainTextOutput(w));
			return w.ToString().Replace("\r\n", "; ");
		}
		
		public abstract void WriteTo(ITextOutput output);
	}
	
	public class ILBlock: ILNode
	{
		public ILExpression EntryGoto;
		
		public List<ILNode> Body;
		
		public ILBlock(params ILNode[] body)
		{
			this.Body = new List<ILNode>(body);
		}
		
		public ILBlock(List<ILNode> body)
		{
			this.Body = body;
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.EntryGoto != null)
				yield return this.EntryGoto;
			foreach(ILNode child in this.Body) {
				yield return child;
			}
		}
		
		public override void WriteTo(ITextOutput output)
		{
			foreach(ILNode child in this.GetChildren()) {
				child.WriteTo(output);
				output.WriteLine();
			}
		}
	}
	
	public class ILBasicBlock: ILNode
	{
		/// <remarks> Body has to start with a label and end with unconditional control flow </remarks>
		public List<ILNode> Body = new List<ILNode>();
		
		public override IEnumerable<ILNode> GetChildren()
		{
			return this.Body;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			foreach(ILNode child in this.GetChildren()) {
				child.WriteTo(output);
				output.WriteLine();
			}
		}
	}
	
	public class ILLabel: ILNode
	{
		public string Name;

		public override void WriteTo(ITextOutput output)
		{
			output.WriteDefinition(Name + ":", this);
		}
	}
	
	public class ILTryCatchBlock: ILNode
	{
		public class CatchBlock: ILBlock
		{
			public TypeReference ExceptionType;
			public ILVariable ExceptionVariable;
			
			public override void WriteTo(ITextOutput output)
			{
				output.Write("catch ");
				output.WriteReference(ExceptionType.FullName, ExceptionType);
				if (ExceptionVariable != null) {
					output.Write(' ');
					output.Write(ExceptionVariable.Name);
				}
				output.WriteLine(" {");
				output.Indent();
				base.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
			}
		}
		
		public ILBlock          TryBlock;
		public List<CatchBlock> CatchBlocks;
		public ILBlock          FinallyBlock;
		public ILBlock          FaultBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.TryBlock != null)
				yield return this.TryBlock;
			foreach (var catchBlock in this.CatchBlocks) {
				yield return catchBlock;
			}
			if (this.FaultBlock != null)
				yield return this.FaultBlock;
			if (this.FinallyBlock != null)
				yield return this.FinallyBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.WriteLine(".try {");
			output.Indent();
			TryBlock.WriteTo(output);
			output.Unindent();
			output.WriteLine("}");
			foreach (CatchBlock block in CatchBlocks) {
				block.WriteTo(output);
			}
			if (FaultBlock != null) {
				output.WriteLine("fault {");
				output.Indent();
				FaultBlock.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
			}
			if (FinallyBlock != null) {
				output.WriteLine("finally {");
				output.Indent();
				FinallyBlock.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
			}
		}
	}
	
	public class ILVariable
	{
		public string Name;
		public bool   IsGenerated;
		public TypeReference Type;
		public VariableDefinition OriginalVariable;
		public ParameterDefinition OriginalParameter;
		
		public bool IsPinned {
			get { return OriginalVariable != null && OriginalVariable.IsPinned; }
		}
		
		public bool IsParameter {
			get { return OriginalParameter != null; }
		}
		
		public override string ToString()
		{
			return Name;
		}
	}
	
	public class ILRange
	{
		public int From;
		public int To;   // Exlusive
		
		public override string ToString()
		{
			return string.Format("{0}-{1}", From.ToString("X"), To.ToString("X"));
		}
		
		public static List<ILRange> OrderAndJoint(IEnumerable<ILRange> input)
		{
			if (input == null)
				throw new ArgumentNullException("Input is null!");
			
			List<ILRange> ranges = input.Where(r => r != null).OrderBy(r => r.From).ToList();
			for (int i = 0; i < ranges.Count - 1;) {
				ILRange curr = ranges[i];
				ILRange next = ranges[i + 1];
				// Merge consequtive ranges if they intersect
				if (curr.From <= next.From && next.From <= curr.To) {
					curr.To = Math.Max(curr.To, next.To);
					ranges.RemoveAt(i + 1);
				} else {
					i++;
				}
			}
			return ranges;
		}
		
		public static IEnumerable<ILRange> Invert(IEnumerable<ILRange> input, int codeSize)
		{
			if (input == null)
				throw new ArgumentNullException("Input is null!");
			
			if (codeSize <= 0)
				throw new ArgumentException("Code size must be grater than 0");
			
			var ordered = OrderAndJoint(input);
			if (ordered.Count == 0) {
				yield return new ILRange() { From = 0, To = codeSize };
			} else {
				// Gap before the first element
				if (ordered.First().From != 0)
					yield return new ILRange() { From = 0, To = ordered.First().From };
				
				// Gaps between elements
				for (int i = 0; i < ordered.Count - 1; i++)
					yield return new ILRange() { From = ordered[i].To, To = ordered[i + 1].From };
				
				// Gap after the last element
				Debug.Assert(ordered.Last().To <= codeSize);
				if (ordered.Last().To != codeSize)
					yield return new ILRange() { From = ordered.Last().To, To = codeSize };
			}
		}
	}
	
	public class ILExpressionPrefix
	{
		public readonly ILCode Code;
		public readonly object Operand;
		
		public ILExpressionPrefix(ILCode code, object operand = null)
		{
			this.Code = code;
			this.Operand = operand;
		}
	}
	
	public class ILExpression : ILNode
	{
		public ILCode Code { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; set; }
		public ILExpressionPrefix[] Prefixes { get; set; }
		// Mapping to the original instructions (useful for debugging)
		public List<ILRange> ILRanges { get; set; }
		
		public TypeReference ExpectedType { get; set; }
		public TypeReference InferredType { get; set; }
		
		public static readonly object AnyOperand = new object();
		
		public ILExpression(ILCode code, object operand, List<ILExpression> args)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
			this.ILRanges  = new List<ILRange>(1);
		}
		
		public ILExpression(ILCode code, object operand, params ILExpression[] args)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
			this.ILRanges  = new List<ILRange>(1);
		}
		
		public void AddPrefix(ILExpressionPrefix prefix)
		{
			ILExpressionPrefix[] arr = this.Prefixes;
			if (arr == null)
				arr = new ILExpressionPrefix[1];
			else
				Array.Resize(ref arr, arr.Length + 1);
			arr[arr.Length - 1] = prefix;
			this.Prefixes = arr;
		}
		
		public ILExpressionPrefix GetPrefix(ILCode code)
		{
			var prefixes = this.Prefixes;
			if (prefixes != null) {
				foreach (ILExpressionPrefix p in prefixes) {
					if (p.Code == code)
						return p;
				}
			}
			return null;
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			return Arguments;
		}
		
		public bool IsBranch()
		{
			return this.Operand is ILLabel || this.Operand is ILLabel[];
		}
		
		public IEnumerable<ILLabel> GetBranchTargets()
		{
			if (this.Operand is ILLabel) {
				return new ILLabel[] { (ILLabel)this.Operand };
			} else if (this.Operand is ILLabel[]) {
				return (ILLabel[])this.Operand;
			} else {
				return new ILLabel[] { };
			}
		}
		
		public override void WriteTo(ITextOutput output)
		{
			if (Operand is ILVariable && ((ILVariable)Operand).IsGenerated) {
				if (Code == ILCode.Stloc && this.InferredType == null) {
					output.Write(((ILVariable)Operand).Name);
					output.Write(" = ");
					Arguments.First().WriteTo(output);
					return;
				} else if (Code == ILCode.Ldloc) {
					output.Write(((ILVariable)Operand).Name);
					if (this.InferredType != null) {
						output.Write(':');
						this.InferredType.WriteTo(output, ILNameSyntax.ShortTypeName);
						if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
							output.Write("[exp:");
							this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
							output.Write(']');
						}
					}
					return;
				}
			}
			
			if (this.Prefixes != null) {
				foreach (var prefix in this.Prefixes) {
					output.Write(prefix.Code.GetName());
					output.Write(". ");
				}
			}
			
			output.Write(Code.GetName());
			if (this.InferredType != null) {
				output.Write(':');
				this.InferredType.WriteTo(output, ILNameSyntax.ShortTypeName);
				if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
					output.Write("[exp:");
					this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
					output.Write(']');
				}
			} else if (this.ExpectedType != null) {
				output.Write("[exp:");
				this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
				output.Write(']');
			}
			output.Write('(');
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel) {
					output.WriteReference(((ILLabel)Operand).Name, Operand);
				} else if (Operand is ILLabel[]) {
					ILLabel[] labels = (ILLabel[])Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (i > 0)
							output.Write(", ");
						output.WriteReference(labels[i].Name, labels[i]);
					}
				} else if (Operand is MethodReference) {
					MethodReference method = (MethodReference)Operand;
					if (method.DeclaringType != null) {
						method.DeclaringType.WriteTo(output, ILNameSyntax.ShortTypeName);
						output.Write("::");
					}
					output.WriteReference(method.Name, method);
				} else if (Operand is FieldReference) {
					FieldReference field = (FieldReference)Operand;
					field.DeclaringType.WriteTo(output, ILNameSyntax.ShortTypeName);
					output.Write("::");
					output.WriteReference(field.Name, field);
				} else {
					DisassemblerHelpers.WriteOperand(output, Operand);
				}
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) output.Write(", ");
				arg.WriteTo(output);
				first = false;
			}
			output.Write(')');
		}
	}
	
	public class ILWhileLoop : ILNode
	{
		public ILExpression Condition;
		public ILBlock      BodyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			if (this.BodyBlock != null)
				yield return this.BodyBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.WriteLine("");
			output.Write("loop (");
			if (this.Condition != null)
				this.Condition.WriteTo(output);
			output.WriteLine(") {");
			output.Indent();
			this.BodyBlock.WriteTo(output);
			output.Unindent();
			output.WriteLine("}");
		}
	}
	
	public class ILCondition : ILNode
	{
		public ILExpression Condition;
		public ILBlock TrueBlock;   // Branch was taken
		public ILBlock FalseBlock;  // Fall-though
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			if (this.TrueBlock != null)
				yield return this.TrueBlock;
			if (this.FalseBlock != null)
				yield return this.FalseBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.Write("if (");
			Condition.WriteTo(output);
			output.WriteLine(") {");
			output.Indent();
			TrueBlock.WriteTo(output);
			output.Unindent();
			output.Write("}");
			if (FalseBlock != null) {
				output.WriteLine(" else {");
				output.Indent();
				FalseBlock.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
			}
		}
	}
	
	public class ILSwitch: ILNode
	{
		public class CaseBlock: ILBlock
		{
			public List<int> Values;  // null for the default case
			
			public override void WriteTo(ITextOutput output)
			{
				if (this.Values != null) {
					foreach (int i in this.Values) {
						output.WriteLine("case {0}:", i);
					}
				} else {
					output.WriteLine("default:");
				}
				output.Indent();
				base.WriteTo(output);
				output.Unindent();
			}
		}
		
		public ILExpression Condition;
		public List<CaseBlock> CaseBlocks = new List<CaseBlock>();
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			foreach (ILBlock caseBlock in this.CaseBlocks) {
				yield return caseBlock;
			}
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.Write("switch (");
			Condition.WriteTo(output);
			output.WriteLine(") {");
			output.Indent();
			foreach (CaseBlock caseBlock in this.CaseBlocks) {
				caseBlock.WriteTo(output);
			}
			output.Unindent();
			output.WriteLine("}");
		}
	}
	
	public class ILFixedStatement : ILNode
	{
		public List<ILExpression> Initializers = new List<ILExpression>();
		public ILBlock      BodyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			foreach (ILExpression initializer in this.Initializers)
				yield return initializer;
			if (this.BodyBlock != null)
				yield return this.BodyBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.Write("fixed (");
			for (int i = 0; i < this.Initializers.Count; i++) {
				if (i > 0)
					output.Write(", ");
				this.Initializers[i].WriteTo(output);
			}
			output.WriteLine(") {");
			output.Indent();
			this.BodyBlock.WriteTo(output);
			output.Unindent();
			output.WriteLine("}");
		}
	}
}