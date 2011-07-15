// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;

namespace ILSpy.BamlDecompiler
{
	/// <summary>
	/// Represents an event registration of a XAML code-behind class.
	/// </summary>
	sealed class EventRegistration
	{
		public string EventName, MethodName;
		public TypeDefinition AttachSourceType;
		public bool IsAttached;
	}
	
	/// <summary>
	/// Decompiles event and name mappings of XAML code-behind classes.
	/// </summary>
	sealed class ConnectMethodDecompiler
	{
		AssemblyDefinition assembly;
		
		public ConnectMethodDecompiler(AssemblyDefinition assembly)
		{
			this.assembly = assembly;
		}
		
		public Dictionary<int, EventRegistration[]> DecompileEventMappings(string fullTypeName)
		{
			var result = new Dictionary<int, EventRegistration[]>();
			TypeDefinition type = this.assembly.MainModule.GetType(fullTypeName);
			
			if (type == null)
				return result;
			
			MethodDefinition def = null;
			
			foreach (var method in type.Methods) {
				if (method.Name == "System.Windows.Markup.IComponentConnector.Connect") {
					def = method;
					break;
				}
			}
			
			if (def == null)
				return result;
			
			// decompile method and optimize the switch
			ILBlock ilMethod = new ILBlock();
			ILAstBuilder astBuilder = new ILAstBuilder();
			ILAstOptimizer optimizer = new ILAstOptimizer();
			var context = new DecompilerContext(type.Module) { CurrentMethod = def, CurrentType = type };
			ilMethod.Body = astBuilder.Build(def, true, context);
			optimizer.Optimize(context, ilMethod, ILAstOptimizationStep.RemoveRedundantCode3);
			
			ILSwitch ilSwitch = ilMethod.Body.OfType<ILSwitch>().FirstOrDefault();
			ILCondition condition = ilMethod.Body.OfType<ILCondition>().FirstOrDefault();
			
			if (ilSwitch != null) {
				foreach (var caseBlock in ilSwitch.CaseBlocks) {
					if (caseBlock.Values == null)
						continue;
					var events = FindEvents(caseBlock);
					foreach (int id in caseBlock.Values)
						result.Add(id, events);
				}
			} else if (condition != null) {
				result.Add(1, FindEvents(condition.FalseBlock));
			}
			
			return result;
		}

		EventRegistration[] FindEvents(ILBlock block)
		{
			var events = new List<EventRegistration>();
			
			foreach (var node in block.Body) {
				var expr = node as ILExpression;
				string eventName, handlerName;
				TypeDefinition attachSource;
				if (IsAddEvent(expr, out eventName, out handlerName))
					events.Add(new EventRegistration {
					           	EventName = eventName,
					           	MethodName = handlerName
					           });
				else if (IsAddAttachedEvent(expr, out eventName, out handlerName, out attachSource))
					events.Add(new EventRegistration {
					           	EventName = eventName,
					           	MethodName = handlerName,
					           	AttachSourceType = attachSource,
					           	IsAttached = true
					           });
			}
			
			return events.ToArray();
		}
		
		bool IsAddAttachedEvent(ILExpression expr, out string eventName, out string handlerName, out TypeDefinition attachSource)
		{
			eventName = "";
			handlerName = "";
			attachSource = null;
			
			if (expr == null || !(expr.Code == ILCode.Callvirt || expr.Code == ILCode.Call))
				return false;
			
			if (expr.Operand is MethodReference && expr.Arguments.Count == 3) {
				var addMethod = expr.Operand as MethodReference;
				if (addMethod.Name != "AddHandler" || addMethod.Parameters.Count != 2)
					return false;
				var arg = expr.Arguments[1];
				if (arg.Code != ILCode.Ldsfld || arg.Arguments.Any() || !(arg.Operand is FieldReference))
					return false;
				FieldReference fldRef = (FieldReference)arg.Operand;
				attachSource = fldRef.DeclaringType.Resolve();
				eventName = fldRef.Name;
				if (eventName.EndsWith("Event") && eventName.Length > "Event".Length)
					eventName = eventName.Remove(eventName.Length - "Event".Length);
				var arg1 = expr.Arguments[2];
				if (arg1.Code != ILCode.Newobj)
					return false;
				var arg2 = arg1.Arguments[1];
				if (arg2.Code != ILCode.Ldftn && arg2.Code != ILCode.Ldvirtftn)
					return false;
				if (arg2.Operand is MethodReference) {
					var m = arg2.Operand as MethodReference;
					handlerName = m.Name;
					return true;
				}
			}
			
			return false;
		}
		
		bool IsAddEvent(ILExpression expr, out string eventName, out string handlerName)
		{
			eventName = "";
			handlerName = "";
			
			if (expr == null || !(expr.Code == ILCode.Callvirt || expr.Code == ILCode.Call))
				return false;
			
			if (expr.Operand is MethodReference && expr.Arguments.Count == 2) {
				var addMethod = expr.Operand as MethodReference;
				if (addMethod.Name.StartsWith("add_") && addMethod.Parameters.Count == 1)
					eventName = addMethod.Name.Substring("add_".Length);
				var arg = expr.Arguments[1];
				if (arg.Code != ILCode.Newobj || arg.Arguments.Count != 2)
					return false;
				var arg1 = arg.Arguments[1];
				if (arg1.Code != ILCode.Ldftn && arg1.Code != ILCode.Ldvirtftn)
					return false;
				if (arg1.Operand is MethodReference) {
					var m = arg1.Operand as MethodReference;
					handlerName = m.Name;
					return true;
				}
			}
			
			return false;
		}
	}
}
