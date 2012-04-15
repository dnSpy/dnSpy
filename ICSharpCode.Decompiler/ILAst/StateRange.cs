// Copyright (c) 2012 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	struct Interval
	{
		public readonly int Start, End;
		
		public Interval(int start, int end)
		{
			Debug.Assert(start <= end || (start == 0 && end == -1));
			this.Start = start;
			this.End = end;
		}
		
		public override string ToString()
		{
			return string.Format("({0} to {1})", Start, End);
		}
	}
	
	class StateRange
	{
		readonly List<Interval> data = new List<Interval>();
		
		public StateRange()
		{
		}
		
		public StateRange(int start, int end)
		{
			this.data.Add(new Interval(start, end));
		}
		
		public bool IsEmpty {
			get { return data.Count == 0; }
		}
		
		public bool Contains(int val)
		{
			foreach (Interval v in data) {
				if (v.Start <= val && val <= v.End)
					return true;
			}
			return false;
		}
		
		public void UnionWith(StateRange other)
		{
			data.AddRange(other.data);
		}
		
		/// <summary>
		/// Unions this state range with (other intersect (minVal to maxVal))
		/// </summary>
		public void UnionWith(StateRange other, int minVal, int maxVal)
		{
			foreach (Interval v in other.data) {
				int start = Math.Max(v.Start, minVal);
				int end = Math.Min(v.End, maxVal);
				if (start <= end)
					data.Add(new Interval(start, end));
			}
		}
		
		/// <summary>
		/// Merges overlapping interval ranges.
		/// </summary>
		public void Simplify()
		{
			if (data.Count < 2)
				return;
			data.Sort((a, b) => a.Start.CompareTo(b.Start));
			Interval prev = data[0];
			int prevIndex = 0;
			for (int i = 1; i < data.Count; i++) {
				Interval next = data[i];
				Debug.Assert(prev.Start <= next.Start);
				if (next.Start <= prev.End + 1) { // intervals overlapping or touching
					prev = new Interval(prev.Start, Math.Max(prev.End, next.End));
					data[prevIndex] = prev;
					data[i] = new Interval(0, -1); // mark as deleted
				} else {
					prev = next;
					prevIndex = i;
				}
			}
			data.RemoveAll(i => i.Start > i.End); // remove all entries that were marked as deleted
		}
		
		public override string ToString()
		{
			return string.Join(",", data);
		}
		
		public Interval ToEnclosingInterval()
		{
			if (data.Count == 0)
				throw new SymbolicAnalysisFailedException();
			return new Interval(data[0].Start, data[data.Count - 1].End);
		}
	}
	
	enum StateRangeAnalysisMode
	{
		IteratorMoveNext,
		IteratorDispose,
		AsyncMoveNext
	}
	
	class StateRangeAnalysis
	{
		readonly StateRangeAnalysisMode mode;
		readonly FieldDefinition stateField;
		internal DefaultDictionary<ILNode, StateRange> ranges;
		SymbolicEvaluationContext evalContext;
		
		internal Dictionary<MethodDefinition, Interval> finallyMethodToStateInterval; // used only for IteratorDispose
		
		/// <summary>
		/// Initializes the state range logic:
		/// Clears 'ranges' and sets 'ranges[entryPoint]' to the full range (int.MinValue to int.MaxValue)
		/// </summary>
		public StateRangeAnalysis(ILNode entryPoint, StateRangeAnalysisMode mode, FieldDefinition stateField)
		{
			this.mode = mode;
			this.stateField = stateField;
			if (mode == StateRangeAnalysisMode.IteratorDispose) {
				finallyMethodToStateInterval = new Dictionary<MethodDefinition, Interval>();
			}
			
			ranges = new DefaultDictionary<ILNode, StateRange>(n => new StateRange());
			ranges[entryPoint] = new StateRange(int.MinValue, int.MaxValue);
			evalContext = new SymbolicEvaluationContext(stateField);
		}
		
		public int AssignStateRanges(List<ILNode> body, int bodyLength)
		{
			if (bodyLength == 0)
				return 0;
			for (int i = 0; i < bodyLength; i++) {
				StateRange nodeRange = ranges[body[i]];
				nodeRange.Simplify();
				
				ILLabel label = body[i] as ILLabel;
				if (label != null) {
					ranges[body[i + 1]].UnionWith(nodeRange);
					continue;
				}
				
				ILTryCatchBlock tryFinally = body[i] as ILTryCatchBlock;
				if (tryFinally != null) {
					if (mode == StateRangeAnalysisMode.IteratorDispose) {
						if (tryFinally.CatchBlocks.Count != 0 || tryFinally.FaultBlock != null || tryFinally.FinallyBlock == null)
							throw new SymbolicAnalysisFailedException();
						ranges[tryFinally.TryBlock].UnionWith(nodeRange);
						if (tryFinally.TryBlock.Body.Count != 0) {
							ranges[tryFinally.TryBlock.Body[0]].UnionWith(nodeRange);
							AssignStateRanges(tryFinally.TryBlock.Body, tryFinally.TryBlock.Body.Count);
						}
						continue;
					} else if (mode == StateRangeAnalysisMode.AsyncMoveNext) {
						return i;
					} else {
						throw new SymbolicAnalysisFailedException();
					}
				}
				
				ILExpression expr = body[i] as ILExpression;
				if (expr == null)
					throw new SymbolicAnalysisFailedException();
				switch (expr.Code) {
					case ILCode.Switch:
						{
							SymbolicValue val = evalContext.Eval(expr.Arguments[0]);
							if (val.Type != SymbolicValueType.State)
								goto default;
							ILLabel[] targetLabels = (ILLabel[])expr.Operand;
							for (int j = 0; j < targetLabels.Length; j++) {
								int state = j - val.Constant;
								ranges[targetLabels[j]].UnionWith(nodeRange, state, state);
							}
							StateRange nextRange = ranges[body[i + 1]];
							nextRange.UnionWith(nodeRange, int.MinValue, -1 - val.Constant);
							nextRange.UnionWith(nodeRange, targetLabels.Length - val.Constant, int.MaxValue);
							break;
						}
					case ILCode.Br:
					case ILCode.Leave:
						ranges[(ILLabel)expr.Operand].UnionWith(nodeRange);
						break;
					case ILCode.Brtrue:
						{
							SymbolicValue val = evalContext.Eval(expr.Arguments[0]);
							if (val.Type == SymbolicValueType.StateEquals) {
								ranges[(ILLabel)expr.Operand].UnionWith(nodeRange, val.Constant, val.Constant);
								StateRange nextRange = ranges[body[i + 1]];
								nextRange.UnionWith(nodeRange, int.MinValue, val.Constant - 1);
								nextRange.UnionWith(nodeRange, val.Constant + 1, int.MaxValue);
								break;
							} else if (val.Type == SymbolicValueType.StateInEquals) {
								ranges[body[i + 1]].UnionWith(nodeRange, val.Constant, val.Constant);
								StateRange targetRange = ranges[(ILLabel)expr.Operand];
								targetRange.UnionWith(nodeRange, int.MinValue, val.Constant - 1);
								targetRange.UnionWith(nodeRange, val.Constant + 1, int.MaxValue);
								break;
							} else {
								goto default;
							}
						}
					case ILCode.Nop:
						ranges[body[i + 1]].UnionWith(nodeRange);
						break;
					case ILCode.Ret:
						break;
					case ILCode.Stloc:
						{
							SymbolicValue val = evalContext.Eval(expr.Arguments[0]);
							if (val.Type == SymbolicValueType.State && val.Constant == 0) {
								evalContext.AddStateVariable((ILVariable)expr.Operand);
								goto case ILCode.Nop;
							} else {
								goto default;
							}
						}
					case ILCode.Call:
						// in some cases (e.g. foreach over array) the C# compiler produces a finally method outside of try-finally blocks
						if (mode == StateRangeAnalysisMode.IteratorDispose) {
							MethodDefinition mdef = (expr.Operand as MethodReference).ResolveWithinSameModule();
							if (mdef == null || finallyMethodToStateInterval.ContainsKey(mdef))
								throw new SymbolicAnalysisFailedException();
							finallyMethodToStateInterval.Add(mdef, nodeRange.ToEnclosingInterval());
							break;
						} else {
							goto default;
						}
					default:
						if (mode == StateRangeAnalysisMode.IteratorDispose) {
							throw new SymbolicAnalysisFailedException();
						} else {
							return i;
						}
				}
			}
			return bodyLength;
		}
		
		public void EnsureLabelAtPos(List<ILNode> body, ref int pos, ref int bodyLength)
		{
			if (pos > 0 && body[pos - 1] is ILLabel) {
				pos--;
			} else {
				// ensure that the first element at body[pos] is a label:
				ILLabel newLabel = new ILLabel();
				newLabel.Name = "YieldReturnEntryPoint";
				ranges[newLabel] = ranges[body[pos]]; // give the label the range of the instruction at body[pos]
				body.Insert(pos, newLabel);
				bodyLength++;
			}
		}
		
		public LabelRangeMapping CreateLabelRangeMapping(List<ILNode> body, int pos, int bodyLength)
		{
			LabelRangeMapping result = new LabelRangeMapping();
			CreateLabelRangeMapping(body, pos, bodyLength, result, false);
			return result;
		}
		
		void CreateLabelRangeMapping(List<ILNode> body, int pos, int bodyLength, LabelRangeMapping result, bool onlyInitialLabels)
		{
			for (int i = pos; i < bodyLength; i++) {
				ILLabel label = body[i] as ILLabel;
				if (label != null) {
					result.Add(new KeyValuePair<ILLabel, StateRange>(label, ranges[label]));
				} else {
					ILTryCatchBlock tryCatchBlock = body[i] as ILTryCatchBlock;
					if (tryCatchBlock != null) {
						CreateLabelRangeMapping(tryCatchBlock.TryBlock.Body, 0, tryCatchBlock.TryBlock.Body.Count, result, true);
					} else if (onlyInitialLabels) {
						break;
					}
				}
			}
		}
	}
	
	class LabelRangeMapping : List<KeyValuePair<ILLabel, StateRange>> {}
}
