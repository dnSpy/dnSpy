// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Debugger.Interop.CorDebug
{
	public static partial class CorDebugExtensionMethods
	{
		const int EnumerateBufferSize = 16;
		
		static void ProcessOutParameter(object parameter)
		{
			TrackedComObjects.ProcessOutParameter(parameter);
		}
		
		// ICorDebugArrayValue
		
		public static unsafe uint[] GetDimensions(this ICorDebugArrayValue corArray)
		{
			uint[] dimensions = new uint[corArray.GetRank()];
			fixed(uint* pDimensions = dimensions)
				corArray.GetDimensions((uint)dimensions.Length, new IntPtr(pDimensions));
			return dimensions;
		}
		
		public static unsafe uint[] GetBaseIndicies(this ICorDebugArrayValue corArray)
		{
			uint[] baseIndicies = new uint[corArray.GetRank()];
			fixed(uint* pBaseIndicies = baseIndicies)
				corArray.GetBaseIndicies((uint)baseIndicies.Length, new IntPtr(pBaseIndicies));
			return baseIndicies;
		}
		
		public static unsafe ICorDebugValue GetElement(this ICorDebugArrayValue corArray, uint[] indices)
		{
			fixed(uint* pIndices = indices)
				return corArray.GetElement((uint)indices.Length, new IntPtr(pIndices));
		}
		
		public static unsafe ICorDebugValue GetElement(this ICorDebugArrayValue corArray, int[] indices)
		{
			fixed(int* pIndices = indices)
				return corArray.GetElement((uint)indices.Length, new IntPtr(pIndices));
		}
		
		// ICorDebugClass2
		
		public static ICorDebugType GetParameterizedType(this ICorDebugClass2 corClass, uint elementType, ICorDebugType[] ppTypeArgs)
		{
			return corClass.GetParameterizedType(elementType, (uint)ppTypeArgs.Length, ppTypeArgs);
		}
		
		// ICorDebugCode
		
		public static unsafe byte[] GetCode(this ICorDebugCode corCode)
		{
			byte[] code = new byte[corCode.GetSize()];
			fixed(byte* pCode = code)
				corCode.GetCode(0, (uint)code.Length, (uint)code.Length, new IntPtr(pCode));
			return code;
		}
		
		// ICorDebugEnum
		
		public static IEnumerable<ICorDebugFrame> GetEnumerator(this ICorDebugFrameEnum corEnum)
		{
			corEnum.Reset();
			while (true) {
				ICorDebugFrame[] corFrames = new ICorDebugFrame[EnumerateBufferSize];
				uint fetched = corEnum.Next(EnumerateBufferSize, corFrames);
				if (fetched == 0)
					yield break;
				for(int i = 0; i < fetched; i++)
					yield return corFrames[i];
			}
		}
		
		public static ICorDebugFrame Next(this ICorDebugFrameEnum corEnum)
		{
			ICorDebugFrame[] corFrames = new ICorDebugFrame[] { null };
			uint framesFetched = corEnum.Next(1, corFrames);
			return corFrames[0];
		}
		
		public static IEnumerable<ICorDebugChain> GetEnumerator(this ICorDebugChainEnum corEnum)
		{
			corEnum.Reset();
			while (true) {
				ICorDebugChain[] corChains = new ICorDebugChain[EnumerateBufferSize];
				uint fetched = corEnum.Next(EnumerateBufferSize, corChains);
				if (fetched == 0)
					yield break;
				for(int i = 0; i < fetched; i++)
					yield return corChains[i];
			}
		}
			
		public static ICorDebugChain Next(this ICorDebugChainEnum corChainEnum)
		{
			ICorDebugChain[] corChains = new ICorDebugChain[] { null };
			uint chainsFetched = corChainEnum.Next(1, corChains);
			return corChains[0];
		}
		
		// ICorDebugGenericValue
		
		public static unsafe Byte[] GetRawValue(this ICorDebugGenericValue corGenVal)
		{
			byte[] retValue = new byte[(int)corGenVal.GetSize()];
			fixed(byte* pRetValue = retValue)
				corGenVal.GetValue(new IntPtr(pRetValue));
			return retValue;
		}
		
		public static unsafe void SetRawValue(this ICorDebugGenericValue corGenVal, byte[] value)
		{
			if (corGenVal.GetSize() != value.Length)
				throw new ArgumentException("Incorrect length");
			fixed(byte* pValue = value)
				corGenVal.SetValue(new IntPtr(pValue));
		}
		
		public static unsafe object GetValue(this ICorDebugGenericValue corGenVal, Type type)
		{
			object retValue;
			byte[] value = new byte[(int)corGenVal.GetSize()];
			fixed(byte* pValue = value) {
				corGenVal.GetValue(new IntPtr(pValue));
				switch(type.FullName) {
					case "System.Boolean": retValue = *((System.Boolean*)pValue); break;
					case "System.Char":    retValue = *((System.Char*)   pValue); break;
					case "System.SByte":   retValue = *((System.SByte*)  pValue); break;
					case "System.Byte":    retValue = *((System.Byte*)   pValue); break;
					case "System.Int16":   retValue = *((System.Int16*)  pValue); break;
					case "System.UInt16":  retValue = *((System.UInt16*) pValue); break;
					case "System.Int32":   retValue = *((System.Int32*)  pValue); break;
					case "System.UInt32":  retValue = *((System.UInt32*) pValue); break;
					case "System.Int64":   retValue = *((System.Int64*)  pValue); break;
					case "System.UInt64":  retValue = *((System.UInt64*) pValue); break;
					case "System.Single":  retValue = *((System.Single*) pValue); break;
					case "System.Double":  retValue = *((System.Double*) pValue); break;
					case "System.IntPtr":  retValue = *((System.IntPtr*) pValue); break;
					case "System.UIntPtr": retValue = *((System.UIntPtr*)pValue); break;
					default: throw new NotSupportedException();
				}
			}
			return retValue;
		}
		
		public static unsafe void SetValue(this ICorDebugGenericValue corGenVal, object value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			byte[] val = new byte[(int)corGenVal.GetSize()];
			fixed(byte* pValue = val) {
				switch(value.GetType().FullName) {
					case "System.Boolean": *((System.Boolean*)pValue) = (System.Boolean)value; break;
					case "System.Char":    *((System.Char*)   pValue) = (System.Char)   value; break;
					case "System.SByte":   *((System.SByte*)  pValue) = (System.SByte)  value; break;
					case "System.Byte":    *((System.Byte*)   pValue) = (System.Byte)   value; break;
					case "System.Int16":   *((System.Int16*)  pValue) = (System.Int16)  value; break;
					case "System.UInt16":  *((System.UInt16*) pValue) = (System.UInt16) value; break;
					case "System.Int32":   *((System.Int32*)  pValue) = (System.Int32)  value; break;
					case "System.UInt32":  *((System.UInt32*) pValue) = (System.UInt32) value; break;
					case "System.Int64":   *((System.Int64*)  pValue) = (System.Int64)  value; break;
					case "System.UInt64":  *((System.UInt64*) pValue) = (System.UInt64) value; break;
					case "System.Single":  *((System.Single*) pValue) = (System.Single) value; break;
					case "System.Double":  *((System.Double*) pValue) = (System.Double) value; break;
					case "System.IntPtr":  *((System.IntPtr*) pValue) = (System.IntPtr) value; break;
					case "System.UIntPtr": *((System.UIntPtr*)pValue) = (System.UIntPtr)value; break;
					default: throw new NotSupportedException();
				}
				corGenVal.SetValue(new IntPtr(pValue));
			}
		}
		
		// ICorDebugModule
		
		public static string GetName(this ICorDebugModule corModule)
		{
			// The 'out' parameter returns the size of the needed buffer as in other functions
			return Util.GetString(corModule.GetName, 256, true);
		}
		
		// ICorDebugProcess
		
		public static bool HasQueuedCallbacks(this ICorDebugProcess corProcess)
		{
			return corProcess.HasQueuedCallbacks(null) != 0;
		}
		
		// ICorDebugStepper
		
		public static unsafe void StepRange(this ICorDebugStepper corStepper, bool bStepIn, int[] ranges)
		{
			fixed(int* pRanges = ranges)
				corStepper.StepRange(bStepIn?1:0, (IntPtr)pRanges, (uint)ranges.Length / 2);
		}
		
		// ICorDebugStringValue
		
		public static string GetString(this ICorDebugStringValue corString)
		{
			uint length = corString.GetLength();
			return Util.GetString(corString.GetString, length, false);
		}
		
		// ICorDebugTypeEnum
		
		public static IEnumerable<ICorDebugType> GetEnumerator(this ICorDebugTypeEnum corTypeEnum)
		{
			corTypeEnum.Reset();
			while (true) {
				ICorDebugType corType = corTypeEnum.Next();
				if (corType != null) {
					yield return corType;
				} else {
					break;
				}
			}
		}
		
		public static ICorDebugType Next(this ICorDebugTypeEnum corTypeEnum)
		{
			ICorDebugType[] corTypes = new ICorDebugType[1];
			uint typesFetched = corTypeEnum.Next(1, corTypes);
			if (typesFetched == 0) {
				return null;
			} else {
				return corTypes[0];
			}
		}
		
		public static List<ICorDebugType> ToList(this ICorDebugTypeEnum corTypeEnum)
		{
			return new List<ICorDebugType>(corTypeEnum.GetEnumerator());
		}
	}
}
