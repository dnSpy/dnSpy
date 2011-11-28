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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedEventFiredByTreeNode : AnalyzerSearchTreeNode
	{
		private readonly EventDefinition analyzedEvent;
		private readonly FieldDefinition eventBackingField;
		private readonly MethodDefinition eventFiringMethod;

		private ConcurrentDictionary<MethodDefinition, int> foundMethods;

		public AnalyzedEventFiredByTreeNode(EventDefinition analyzedEvent)
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");

			this.analyzedEvent = analyzedEvent;

			this.eventBackingField = GetBackingField(analyzedEvent);
			this.eventFiringMethod = analyzedEvent.EventType.Resolve().Methods.First(md => md.Name == "Invoke");
		}

		public override object Text
		{
			get { return "Raised By"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			foundMethods = new ConcurrentDictionary<MethodDefinition, int>();

			foreach (var child in FindReferencesInType(analyzedEvent.DeclaringType).OrderBy(n => n.Text)) {
				yield return child;
			}

			foundMethods = null;
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDefinition type)
		{
			// HACK: in lieu of proper flow analysis, I'm going to use a simple heuristic
			// If the method accesses the event's backing field, and calls invoke on a delegate 
			// with the same signature, then it is (most likely) raise the given event.

			foreach (MethodDefinition method in type.Methods) {
				bool readBackingField = false;
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					Code code = instr.OpCode.Code;
					if (code == Code.Ldfld || code == Code.Ldflda) {
						FieldReference fr = instr.Operand as FieldReference;
						if (fr != null && fr.Name == eventBackingField.Name && fr == eventBackingField) {
							readBackingField = true;
						}
					}
					if (readBackingField && (code == Code.Callvirt || code == Code.Call)) {
						MethodReference mr = instr.Operand as MethodReference;
						if (mr != null && mr.Name == eventFiringMethod.Name && mr.Resolve() == eventFiringMethod) {
							found = true;
							break;
						}
					}
				}

				method.Body = null;

				if (found) {
					MethodDefinition codeLocation = this.Language.GetOriginalCodeLocation(method) as MethodDefinition;
					if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
						var node = new AnalyzedMethodTreeNode(codeLocation);
						node.Language = this.Language;
						yield return node;
					}
				}
			}
		}

		private bool HasAlreadyBeenFound(MethodDefinition method)
		{
			return !foundMethods.TryAdd(method, 0);
		}

		// HACK: we should probably examine add/remove methods to determine this
		private static FieldDefinition GetBackingField(EventDefinition ev)
		{
			var fieldName = ev.Name;
			var vbStyleFieldName = fieldName + "Event";
			var fieldType = ev.EventType;

			foreach (var fd in ev.DeclaringType.Fields) {
				if (fd.Name == fieldName || fd.Name == vbStyleFieldName)
					if (fd.FieldType.FullName == fieldType.FullName)
						return fd;
			}

			return null;
		}


		public static bool CanShow(EventDefinition ev)
		{
			return GetBackingField(ev) != null;
		}
	}
}
