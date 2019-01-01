/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Method debug info
	/// </summary>
	public sealed class MethodDebugInfo {
		/// <summary>
		/// Compiler name (<see cref="PredefinedCompilerNames"/>) or null
		/// </summary>
		public string CompilerName { get; }

		/// <summary>
		/// Decompiler options version number
		/// </summary>
		public int DecompilerSettingsVersion { get; }

		/// <summary>
		/// Gets the state machine kind
		/// </summary>
		public StateMachineKind StateMachineKind { get; }

		/// <summary>
		/// Gets the method
		/// </summary>
		public MethodDef Method { get; }

		/// <summary>
		/// Gets the kickoff method or null
		/// </summary>
		public MethodDef KickoffMethod { get; }

		/// <summary>
		/// Gets the parameters. There could be missing parameters, in which case use <see cref="Method"/>. This array isn't sorted.
		/// </summary>
		public SourceParameter[] Parameters { get; }

		/// <summary>
		/// Gets all statements, sorted by <see cref="ILSpan.Start"/>
		/// </summary>
		public SourceStatement[] Statements { get; }

		/// <summary>
		/// Gets async info or null if none
		/// </summary>
		public AsyncMethodDebugInfo AsyncInfo { get; }

		/// <summary>
		/// Gets the root scope
		/// </summary>
		public MethodDebugScope Scope { get; }

		/// <summary>
		/// Method span or the default value (position 0, length 0) if it's not known
		/// </summary>
		public TextSpan Span { get; }

		/// <summary>
		/// true if <see cref="Span"/> is a valid method span
		/// </summary>
		public bool HasSpan => Span.Start != 0 && Span.End != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="compilerName">Compiler name (<see cref="PredefinedCompilerNames"/>) or null</param>
		/// <param name="decompilerSettingsVersion">Decompiler settings version number. This version number should get incremented when the settings change.</param>
		/// <param name="stateMachineKind">State machine kind</param>
		/// <param name="method">Method</param>
		/// <param name="kickoffMethod">Kickoff method or null</param>
		/// <param name="parameters">Parameters or null</param>
		/// <param name="statements">Statements</param>
		/// <param name="scope">Root scope</param>
		/// <param name="methodSpan">Method span or null to calculate it from <paramref name="statements"/></param>
		/// <param name="asyncMethodDebugInfo">Async info or null</param>
		public MethodDebugInfo(string compilerName, int decompilerSettingsVersion, StateMachineKind stateMachineKind, MethodDef method, MethodDef kickoffMethod, SourceParameter[] parameters, SourceStatement[] statements, MethodDebugScope scope, TextSpan? methodSpan, AsyncMethodDebugInfo asyncMethodDebugInfo) {
			if (statements == null)
				throw new ArgumentNullException(nameof(statements));
			CompilerName = compilerName;
			Method = method ?? throw new ArgumentNullException(nameof(method));
			KickoffMethod = kickoffMethod;
			Parameters = parameters ?? Array.Empty<SourceParameter>();
			if (statements.Length > 1)
				Array.Sort(statements, SourceStatement.SpanStartComparer);
			DecompilerSettingsVersion = decompilerSettingsVersion;
			Statements = statements;
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
			Span = methodSpan ?? CalculateMethodSpan(statements) ?? new TextSpan(0, 0);
			AsyncInfo = asyncMethodDebugInfo;
		}

		static TextSpan? CalculateMethodSpan(SourceStatement[] statements) {
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (var statement in statements) {
				if (min > statement.TextSpan.Start)
					min = statement.TextSpan.Start;
				if (max < statement.TextSpan.End)
					max = statement.TextSpan.End;
			}
			return min <= max ? TextSpan.FromBounds(min, max) : (TextSpan?)null;
		}

		/// <summary>
		/// Gets a <see cref="SourceStatement"/>
		/// </summary>
		/// <param name="lineStart">Offset of start of line</param>
		/// <param name="lineEnd">Offset of end of line</param>
		/// <param name="textPosition">Position in text document</param>
		/// <returns></returns>
		public SourceStatement? GetSourceStatementByTextOffset(int lineStart, int lineEnd, int textPosition) {
			if (lineStart >= Span.End || lineEnd < Span.Start)
				return null;

			SourceStatement? intersection = null;
			foreach (var statement in Statements) {
				if (statement.TextSpan.Start <= textPosition) {
					if (textPosition < statement.TextSpan.End)
						return statement;
					if (textPosition == statement.TextSpan.End) {
						// If it matches more than one statement, pick the smallest one. More specifically,
						// use the first statement if they're identical; that way we use the smallest
						// IL offset since Statements is sorted by IL offset.
						if (intersection == null || statement.TextSpan.Start > intersection.Value.TextSpan.Start)
							intersection = statement;
					}
				}
			}
			if (intersection != null)
				return intersection;

			var list = new List<SourceStatement>();
			foreach (var statement in Statements) {
				if (lineStart < statement.TextSpan.End && lineEnd > statement.TextSpan.Start)
					list.Add(statement);
			}
			list.Sort((a, b) => {
				var d = Math.Abs(a.TextSpan.Start - textPosition) - Math.Abs(b.TextSpan.Start - textPosition);
				if (d != 0)
					return d;
				return (int)(a.ILSpan.Start - b.ILSpan.Start);
			});
			if (list.Count > 0)
				return list[0];
			return null;
		}

		/// <summary>
		/// Gets a <see cref="SourceStatement"/>
		/// </summary>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		public SourceStatement? GetSourceStatementByCodeOffset(uint ilOffset) {
			foreach (var statement in Statements) {
				if (statement.ILSpan.Start <= ilOffset && ilOffset < statement.ILSpan.End)
					return statement;
			}
			return null;
		}
	}

	/// <summary>
	/// State machine kind
	/// </summary>
	public enum StateMachineKind {
		/// <summary>
		/// Not a state machine
		/// </summary>
		None,

		/// <summary>
		/// Iterator method state machine
		/// </summary>
		IteratorMethod,

		/// <summary>
		/// Async method state machine
		/// </summary>
		AsyncMethod,
	}
}
