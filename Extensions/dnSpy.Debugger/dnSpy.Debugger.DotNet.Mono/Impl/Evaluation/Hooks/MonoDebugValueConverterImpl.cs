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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation.Hooks {
	sealed class MonoDebugValueConverterImpl : IMonoDebugValueConverter {
		readonly DbgMonoDebugInternalRuntimeImpl runtime;

		public MonoDebugValueConverterImpl(DbgMonoDebugInternalRuntimeImpl runtime) =>
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

		bool TryGetUInt32(object? value, out uint result) {
			if (value is DbgDotNetValue dnValue) {
				var rawValue = dnValue.GetRawValue();
				if (rawValue.HasRawValue)
					value = rawValue.RawValue;
			}
			if (value is not null) {
				if (value is bool) {
					result = (bool)value ? 1U : 0;
					return true;
				}
				if (value is char) {
					result = (char)value;
					return true;
				}
				if (value is byte) {
					result = (byte)value;
					return true;
				}
				if (value is sbyte) {
					result = (uint)(sbyte)value;
					return true;
				}
				if (value is short) {
					result = (uint)(short)value;
					return true;
				}
				if (value is ushort) {
					result = (ushort)value;
					return true;
				}
				if (value is int) {
					result = (uint)(int)value;
					return true;
				}
				if (value is uint) {
					result = (uint)value;
					return true;
				}
			}
			result = 0;
			return false;
		}

		Exception Invalid() => new InvalidOperationException();

		char IMonoDebugValueConverter.ToChar(object? value) {
			if (TryGetUInt32(value, out var result))
				return (char)result;
			throw Invalid();
		}

		int IMonoDebugValueConverter.ToInt32(object? value) {
			if (TryGetUInt32(value, out var result))
				return (int)result;
			throw Invalid();
		}

		unsafe char[]? IMonoDebugValueConverter.ToCharArray(object? value) {
			if (value is null)
				return null;

			if (value is char[] result)
				return result;

			if (value is DbgDotNetValue dnValue) {
				// Don't check its type, it's null and can be cast to char[]
				if (dnValue.IsNull)
					return null;
				var type = dnValue.Type;
				if (type.IsArray && type.GetElementType() == type.AppDomain.System_Char) {
					var addr = dnValue.GetRawAddressValue(onlyDataAddress: true);
					if (addr is not null) {
						ulong chars = addr.Value.Length / 2;
						if (chars <= int.MaxValue / 2) {
							if (chars == 0)
								return Array.Empty<char>();
							try {
								result = new char[(int)chars];
							}
							catch (OutOfMemoryException) {
								throw Invalid();
							}
							var process = type.AppDomain.Runtime.GetDebuggerRuntime().Process;
							fixed (void* p = result)
								process.ReadMemory(addr.Value.Address, (byte*)p, (int)chars * 2);
							return result;
						}
					}
				}
			}

			throw Invalid();
		}
	}
}
