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
using System.Collections.ObjectModel;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// .NET method body
	/// </summary>
	public abstract class DmdMethodBody {
		/// <summary>
		/// Gets the token of the locals signature
		/// </summary>
		public abstract int LocalSignatureMetadataToken { get; }

		/// <summary>
		/// Gets all locals
		/// </summary>
		public abstract ReadOnlyCollection<DmdLocalVariableInfo> LocalVariables { get; }

		/// <summary>
		/// Gets max stack size
		/// </summary>
		public abstract int MaxStackSize { get; }

		/// <summary>
		/// true if locals are automatically initialized
		/// </summary>
		public abstract bool InitLocals { get; }

		/// <summary>
		/// Gets the IL bytes
		/// </summary>
		/// <returns></returns>
		public abstract byte[] GetILAsByteArray();

		/// <summary>
		/// Gets the exception clauses
		/// </summary>
		public abstract ReadOnlyCollection<DmdExceptionHandlingClause> ExceptionHandlingClauses { get; }

		/// <summary>
		/// Gets the generic type arguments that were used to create this method body
		/// </summary>
		public abstract ReadOnlyCollection<DmdType> GenericTypeArguments { get; }

		/// <summary>
		/// Gets the generic method arguments that were used to create this method body
		/// </summary>
		public abstract ReadOnlyCollection<DmdType> GenericMethodArguments { get; }
	}

	/// <summary>
	/// Local variable info
	/// </summary>
	public sealed class DmdLocalVariableInfo {
		/// <summary>
		/// Gets the type of the local
		/// </summary>
		public DmdType LocalType { get; }

		/// <summary>
		/// true if it's a pinned local
		/// </summary>
		public bool IsPinned { get; }

		/// <summary>
		/// Index of the local in the locals signature
		/// </summary>
		public int LocalIndex { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="localType">Type of the local</param>
		/// <param name="localIndex">Index of local</param>
		/// <param name="isPinned">True if it's a pinned local</param>
		public DmdLocalVariableInfo(DmdType localType, int localIndex, bool isPinned) {
			if (localIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(localIndex));
			LocalType = localType ?? throw new ArgumentNullException(nameof(localType));
			LocalIndex = localIndex;
			IsPinned = isPinned;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (IsPinned)
				return LocalType.ToString() + " (" + LocalIndex.ToString() + ") (pinned)";
			return LocalType.ToString() + " (" + LocalIndex.ToString() + ")";
		}
	}

	/// <summary>
	/// Exception clause kind
	/// </summary>
	[Flags]
	public enum DmdExceptionHandlingClauseOptions {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Clause				= 0,
		Filter				= 1,
		Finally				= 2,
		Fault				= 4,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Exception clause
	/// </summary>
	public sealed class DmdExceptionHandlingClause {
		/// <summary>
		/// Gets the clause kind
		/// </summary>
		public DmdExceptionHandlingClauseOptions Flags { get; }

		/// <summary>
		/// Try offset
		/// </summary>
		public int TryOffset { get; }

		/// <summary>
		/// Try length
		/// </summary>
		public int TryLength { get; }

		/// <summary>
		/// Handler offset
		/// </summary>
		public int HandlerOffset { get; }

		/// <summary>
		/// Handler length
		/// </summary>
		public int HandlerLength { get; }

		/// <summary>
		/// Filter offset
		/// </summary>
		public int FilterOffset {
			get {
				if (Flags != DmdExceptionHandlingClauseOptions.Filter)
					throw new InvalidOperationException();
				return filterOffset;
			}
		}
		readonly int filterOffset;

		/// <summary>
		/// Catch type
		/// </summary>
		public DmdType? CatchType {
			get {
				if (Flags != DmdExceptionHandlingClauseOptions.Clause)
					throw new InvalidOperationException();
				return catchType;
			}
		}
		readonly DmdType? catchType;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <param name="tryOffset">Try offset</param>
		/// <param name="tryLength">Try length</param>
		/// <param name="handlerOffset">Handler offset</param>
		/// <param name="handlerLength">Handler length</param>
		/// <param name="filterOffset">Filter offset</param>
		/// <param name="catchType">Catch type</param>
		public DmdExceptionHandlingClause(DmdExceptionHandlingClauseOptions flags, int tryOffset, int tryLength, int handlerOffset, int handlerLength, int filterOffset, DmdType? catchType) {
			Flags = flags;
			TryOffset = tryOffset;
			TryLength = tryLength;
			HandlerOffset = handlerOffset;
			HandlerLength = handlerLength;
			this.filterOffset = filterOffset;
			this.catchType = catchType;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			switch (Flags) {
			case DmdExceptionHandlingClauseOptions.Clause:
				return $"Flags={Flags}, TryOffset={TryOffset}, TryLength={TryLength}, HandlerOffset={HandlerOffset}, HandlerLength={HandlerLength}, CatchType={CatchType}";
			case DmdExceptionHandlingClauseOptions.Filter:
				return $"Flags={Flags}, TryOffset={TryOffset}, TryLength={TryLength}, HandlerOffset={HandlerOffset}, HandlerLength={HandlerLength}, FilterOffset={FilterOffset}";
			case DmdExceptionHandlingClauseOptions.Finally:
			case DmdExceptionHandlingClauseOptions.Fault:
			default:
				return $"Flags={Flags}, TryOffset={TryOffset}, TryLength={TryLength}, HandlerOffset={HandlerOffset}, HandlerLength={HandlerLength}";
			}
		}
	}
}
