/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace dnSpy.Debugger.Exceptions {
	sealed class EXCEPTION_INFO {	// See msdbg.h or https://msdn.microsoft.com/en-us/library/vstudio/bb161797%28v=vs.140%29.aspx
		public string Name { get; }
		public uint Code { get; }
		public ExceptionState State { get; }

		public EXCEPTION_INFO(string name, uint code, ExceptionState state) {
			Name = name;
			Code = code;
			State = state;
		}

		public override string ToString() {
			if ((State & ExceptionState.EXCEPTION_CODE_SUPPORTED) != 0) {
				if ((State & ExceptionState.EXCEPTION_CODE_DISPLAY_IN_HEX) != 0)
					return string.Format("0x{0:X8} {1}", Code, Name);
				return string.Format("{0} {1}", Code, Name);
			}
			return Name;
		}
	}

	interface IDefaultExceptionSettings {
		IEnumerable<ExceptionInfo> ExceptionInfos { get; }
	}

	[Export(typeof(IDefaultExceptionSettings))]
	sealed class DefaultExceptionSettings : IDefaultExceptionSettings {
		/*
		static void Dump() {
			var sb = new StringBuilder();
			using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0_Config\AD7Metrics\Exception\{449EC4CC-30D2-4032-9256-EE18EB41B62B}\Common Language Runtime Exceptions")) {
				foreach (var sk1 in key.GetSubKeyNames()) {
					using (var key1 = key.OpenSubKey(sk1)) {
						foreach (var sk2 in key1.GetSubKeyNames()) {
							using (var key2 = key1.OpenSubKey(sk2)) {
								int code = (int)key2.GetValue("Code");
								var state = (ExceptionState)(int)key2.GetValue("State");
								sb.AppendLine(string.Format("			new EXCEPTION_INFO(\"{0}\", 0x{1:X8}, {2}),", sk2, code, ToString(state)));
							}
						}
					}
				}
			}
		}
		static string ToString(ExceptionState f) {
			var sb = new StringBuilder();
			if (f == 0) return "ExceptionState.EXCEPTION_NONE";
			if ((f & ExceptionState.EXCEPTION_STOP_FIRST_CHANCE) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_FIRST_CHANCE");
			if ((f & ExceptionState.EXCEPTION_STOP_SECOND_CHANCE) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_SECOND_CHANCE");
			if ((f & ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE");
			if ((f & ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT");
			if ((f & ExceptionState.EXCEPTION_CANNOT_BE_CONTINUED) != 0) Append(sb, "ExceptionState.EXCEPTION_CANNOT_BE_CONTINUED");
			if ((f & ExceptionState.EXCEPTION_CODE_SUPPORTED) != 0) Append(sb, "ExceptionState.EXCEPTION_CODE_SUPPORTED");
			if ((f & ExceptionState.EXCEPTION_CODE_DISPLAY_IN_HEX) != 0) Append(sb, "ExceptionState.EXCEPTION_CODE_DISPLAY_IN_HEX");
			if ((f & ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED) != 0) Append(sb, "ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED");
			if ((f & ExceptionState.EXCEPTION_MANAGED_DEBUG_ASSISTANT) != 0) Append(sb, "ExceptionState.EXCEPTION_MANAGED_DEBUG_ASSISTANT");
			if ((f & ExceptionState.EXCEPTION_STOP_FIRST_CHANCE_USE_PARENT) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_FIRST_CHANCE_USE_PARENT");
			if ((f & ExceptionState.EXCEPTION_STOP_SECOND_CHANCE_USE_PARENT) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_SECOND_CHANCE_USE_PARENT");
			if ((f & ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE_USE_PARENT) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE_USE_PARENT");
			if ((f & ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT_USE_PARENT) != 0) Append(sb, "ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT_USE_PARENT");
			return sb.ToString();
		}
		static void Append(StringBuilder sb, string s) {
			if (sb.Length > 0)
				sb.Append(" | ");
			sb.Append(s);
		}
		*/
		static readonly EXCEPTION_INFO[] DotNetExceptionInfos = new EXCEPTION_INFO[] {
			new EXCEPTION_INFO("Microsoft.JScript.JScriptException", 0x00000000, ExceptionState.EXCEPTION_STOP_FIRST_CHANCE_USE_PARENT | ExceptionState.EXCEPTION_STOP_SECOND_CHANCE_USE_PARENT),
			new EXCEPTION_INFO("System.AccessViolationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.AggregateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.AppDomainUnloadedException", 0x00000000, ExceptionState.EXCEPTION_NONE),
			new EXCEPTION_INFO("System.ApplicationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ArgumentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ArgumentNullException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ArgumentOutOfRangeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ArithmeticException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ArrayTypeMismatchException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.BadImageFormatException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.CannotUnloadAppDomainException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ContextMarshalException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DataMisalignedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DivideByZeroException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DllNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DuplicateWaitObjectException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.EntryPointNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Exception", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ExecutionEngineException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.FieldAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.FormatException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IndexOutOfRangeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.InsufficientMemoryException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.InvalidCastException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.InvalidOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.InvalidProgramException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.InvalidTimeZoneException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.MemberAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.MethodAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.MissingFieldException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.MissingMemberException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.MissingMethodException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.MulticastNotSupportedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.NotCancelableException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.NotFiniteNumberException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.NotImplementedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.NotSupportedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.NullReferenceException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ObjectDisposedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.OperationCanceledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.OutOfMemoryException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.OverflowException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.PlatformNotSupportedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.RankException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.StackOverflowException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.SystemException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.TimeoutException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.TimeZoneNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.TypeAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.TypeInitializationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.TypeLoadException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.TypeUnloadedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.UnauthorizedAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.UriFormatException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Collections.Generic.KeyNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.InvalidEnumArgumentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.LicenseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.WarningException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Win32Exception", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Composition.ChangeRejectedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Composition.CompositionContractMismatchException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Composition.CompositionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Composition.ImportCardinalityMismatchException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Composition.Primitives.ComposablePartException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.DataAnnotations.ValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Design.CheckoutException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Design.ExceptionCollection", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ComponentModel.Design.Serialization.CodeDomSerializerException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.ConfigurationErrorsException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.ConfigurationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.SettingsPropertyCannotBeSetForAnonymousUserException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.SettingsPropertyIsReadOnlyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.SettingsPropertyNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.SettingsPropertyWrongTypeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.Install.InstallException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Configuration.Provider.ProviderException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.ConstraintException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.DataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.DBConcurrencyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.DeletedRowInaccessibleException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.DuplicateNameException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.EvaluateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.InRowChangingEventException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.InvalidConstraintException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.InvalidExpressionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.MissingPrimaryKeyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.NoNullAllowedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.OperationAbortedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.ReadOnlyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.RowNotInTableException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.StrongTypingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SyntaxErrorException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.TypedDataSetGeneratorException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.VersionNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.Common.DbException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.Linq.ChangeConflictException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.Linq.DuplicateKeyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.Odbc.OdbcException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.OleDb.OleDbException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.OracleClient.OracleException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SqlClient.SqlException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SqlTypes.SqlException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SqlTypes.SqlNotFilledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SqlTypes.SqlNullValueException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SqlTypes.SqlTruncateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Data.SqlTypes.SqlTypeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Deployment.Application.DependentPlatformMissingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Deployment.Application.DeploymentDownloadException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Deployment.Application.DeploymentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Deployment.Application.InvalidDeploymentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Deployment.Application.TrustNotGrantedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Diagnostics.Eventing.Reader.EventLogException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Diagnostics.Eventing.Reader.EventLogInvalidDataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Diagnostics.Eventing.Reader.EventLogNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Diagnostics.Eventing.Reader.EventLogProviderDisabledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Diagnostics.Eventing.Reader.EventLogReadingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.DirectoryServicesCOMException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.MultipleMatchesException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.NoMatchingPrincipalException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.PasswordException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.PrincipalException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.PrincipalExistsException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.PrincipalOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.AccountManagement.PrincipalServerDownException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectExistsException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.ActiveDirectory.ActiveDirectoryServerDownException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.ActiveDirectory.ForestTrustCollisionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.ActiveDirectory.SyncFromAllServersOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.BerConversionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.DirectoryException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.DirectoryOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.DsmlInvalidDocumentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.ErrorResponseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.LdapException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.DirectoryServices.Protocols.TlsOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Drawing.Printing.InvalidPrinterException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.EnterpriseServices.RegistrationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.EnterpriseServices.ServicedComponentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.CardSpaceException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.IdentityValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.PolicyValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.ServiceBusyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.ServiceNotStartedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.StsCommunicationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.UnsupportedPolicyOptionsException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.UntrustedRecipientException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Selectors.UserCancellationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Tokens.SecurityTokenException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IdentityModel.Tokens.SecurityTokenValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.DirectoryNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.DriveNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.EndOfStreamException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.FileFormatException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.FileLoadException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.FileNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.InternalBufferOverflowException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.InvalidDataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.IOException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.PathTooLongException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.PipeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.IsolatedStorage.IsolatedStorageException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.Log.ReservationNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.IO.Log.SequenceFullException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.InstanceNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.InstrumentationBaseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.InstrumentationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.ManagementException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ActionPreferenceStopException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ApplicationFailedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ArgumentTransformationMetadataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.CmdletInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.CmdletProviderInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.CommandNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.DriveNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ExtendedTypeSystemException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.GetValueException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.GetValueInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.HaltCommandException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.IncompleteParseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ItemNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.MetadataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.MethodException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.MethodInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ParameterBindingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ParentContainsErrorRecordException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ParseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ParsingMetadataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PipelineClosedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PipelineStoppedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ProviderInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ProviderNameAmbiguousException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ProviderNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSArgumentException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSArgumentNullException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSArgumentOutOfRangeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSInvalidCastException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSInvalidOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSNotImplementedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSNotSupportedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSObjectDisposedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.PSSecurityException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.RedirectedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.RemoteException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.RuntimeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ScriptCallDepthException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ScriptRequiresException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.SessionStateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.SessionStateOverflowException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.SessionStateUnauthorizedAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.SetValueException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.SetValueInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.ValidationMetadataException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.WildcardPatternException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Host.HostException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Host.PromptingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Runspaces.InvalidPipelineStateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Runspaces.InvalidRunspaceStateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Runspaces.PSConsoleLoadException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Runspaces.PSSnapInException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Runspaces.RunspaceConfigurationAttributeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Automation.Runspaces.RunspaceConfigurationTypeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Management.Instrumentation.WmiProviderInstallationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Messaging.MessageQueueException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.CookieException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.HttpListenerException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.ProtocolViolationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.WebException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.Mail.SmtpException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.Mail.SmtpFailedRecipientException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.Mail.SmtpFailedRecipientsException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.NetworkInformation.NetworkInformationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.NetworkInformation.PingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.PeerToPeer.PeerToPeerException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Net.Sockets.SocketException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Printing.PrintCommitAttributesException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Printing.PrintingCanceledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Printing.PrintJobException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Printing.PrintQueueException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Printing.PrintServerException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Printing.PrintSystemException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.AmbiguousMatchException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.InvalidFilterCriteriaException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.MissingMetadataException", 0x00000000, ExceptionState.EXCEPTION_STOP_FIRST_CHANCE | ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.MissingRuntimeArtifactException", 0x00000000, ExceptionState.EXCEPTION_STOP_FIRST_CHANCE | ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.ReflectionTypeLoadException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.TargetException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.TargetInvocationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Reflection.TargetParameterCountException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Resources.MissingManifestResourceException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Resources.MissingSatelliteAssemblyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.COMException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.ComObjectInUseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.ExternalException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.InvalidComObjectException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.InvalidOleVariantTypeException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.MarshalDirectiveException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.SafeArrayRankMismatchException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.SafeArrayTypeMismatchException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.InteropServices.SEHException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Remoting.RemotingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Remoting.RemotingTimeoutException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Remoting.ServerException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Remoting.MetadataServices.SUDSGeneratorException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Remoting.MetadataServices.SUDSParserException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Serialization.InvalidDataContractException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Runtime.Serialization.SerializationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.HostProtectionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.SecurityException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.VerificationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.XmlSyntaxException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.AccessControl.PrivilegeNotHeldException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.Authentication.AuthenticationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.Authentication.InvalidCredentialException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.Cryptography.CryptographicException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.Cryptography.CryptographicUnexpectedOperationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.Policy.PolicyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.Principal.IdentityNotMappedExceptionn", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Security.RightsManagement.RightsManagementException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.ActionNotSupportedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.AddressAccessDeniedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.AddressAlreadyInUseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.ChannelTerminatedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.CommunicationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.CommunicationObjectAbortedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.CommunicationObjectFaultedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.EndpointNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.FaultException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.FaultException`1", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.InvalidMessageContractException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.MessageHeaderException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.MsmqException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.MsmqPoisonMessageException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.PoisonMessageException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.ProtocolException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.QuotaExceededException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.ServerTooBusyException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.ServiceActivationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Channels.InvalidChannelBindingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Channels.PnrpPeerResolver+PnrpException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Dispatcher.FilterInvalidBodyAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Dispatcher.InvalidBodyAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Dispatcher.MessageFilterException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Dispatcher.MultipleFilterMatchesException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Dispatcher.NavigatorInvalidBodyAccessException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Dispatcher.XPathNavigatorException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Security.ExpiredSecurityTokenException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Security.MessageSecurityException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Security.SecurityAccessDeniedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceModel.Security.SecurityNegotiationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.ServiceProcess.TimeoutException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Text.DecoderFallbackException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Text.EncoderFallbackException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.AbandonedMutexException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.LockRecursionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.SemaphoreFullException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.SynchronizationLockException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.Tasks.TaskCanceledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.ThreadAbortException", 0x00000000, ExceptionState.EXCEPTION_NONE),
			new EXCEPTION_INFO("System.Threading.ThreadInterruptedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.ThreadStartException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.ThreadStateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Threading.WaitHandleCannotBeOpenedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Transactions.TransactionAbortedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Transactions.TransactionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Transactions.TransactionInDoubtException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Transactions.TransactionManagerCommunicationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Transactions.TransactionPromotionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.HttpCompileException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.HttpException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.HttpParseException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.HttpRequestValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.HttpUnhandledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Caching.DatabaseNotEnabledForNotificationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Caching.TableNotEnabledForNotificationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Management.SqlExecutionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Security.MembershipCreateUserException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Security.MembershipPasswordException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Services.Protocols.SoapException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.Services.Protocols.SoapHeaderException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Web.UI.ViewStateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.ResourceReferenceKeyNotFoundException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Automation.ElementNotAvailableException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Automation.ElementNotEnabledException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Automation.NoClickablePointException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Automation.ProxyAssemblyNotLoadedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Controls.PrintDialogException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Forms.AxHost+InvalidActiveXStateException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Markup.XamlParseException", 0x00000000, ExceptionState.EXCEPTION_STOP_FIRST_CHANCE | ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Media.InvalidWmpVersionException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Media.Animation.AnimationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Xps.XpsException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Xps.XpsPackagingException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Xps.XpsSerializationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Windows.Xps.XpsWriterException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Activities.EventDeliveryFailedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Activities.WorkflowAuthorizationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Activities.Rules.RuleEvaluationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Activities.Rules.RuleEvaluationIncompatibleTypesException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Activities.Rules.RuleException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Activities.Rules.RuleSetValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.ComponentModel.WorkflowTerminatedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.ComponentModel.Compiler.WorkflowValidationFailedException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.ComponentModel.Serialization.WorkflowMarkupSerializationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Runtime.WorkflowOwnershipException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Runtime.Hosting.PersistenceException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Workflow.Runtime.Tracking.TrackingProfileDeserializationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.XmlException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.Schema.XmlSchemaException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.Schema.XmlSchemaInferenceException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.Schema.XmlSchemaValidationException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.XPath.XPathException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.Xsl.XsltCompileException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
			new EXCEPTION_INFO("System.Xml.Xsl.XsltException", 0x00000000, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE | ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT | ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED),
		};

		DefaultExceptionSettings() {
		}

		public IEnumerable<ExceptionInfo> ExceptionInfos => DotNetExceptionInfos.Select(a => new ExceptionInfo(ExceptionType.DotNet, a));
	}
}
