// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Debugger.Interop.CorPublish
{
	public static partial class CorPublishExtensionMethods
	{
		public static ICorPublishEnum Clone(this CorpubPublishClass instance)
		{
			ICorPublishEnum ppEnum;
			instance.__Clone(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static ICorPublishAppDomainEnum EnumAppDomains(this CorpubPublishClass instance)
		{
			ICorPublishAppDomainEnum ppEnum;
			instance.__EnumAppDomains(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static ICorPublishProcessEnum EnumProcesses(this CorpubPublishClass instance, COR_PUB_ENUMPROCESS Type)
		{
			ICorPublishProcessEnum ppIEnum;
			instance.__EnumProcesses(Type, out ppIEnum);
			ProcessOutParameter(ppIEnum);
			return ppIEnum;
		}
		
		public static uint GetCount(this CorpubPublishClass instance)
		{
			uint pcelt;
			instance.__GetCount(out pcelt);
			return pcelt;
		}
		
		public static void GetDisplayName(this CorpubPublishClass instance, uint cchName, out uint pcchName, StringBuilder szName)
		{
			instance.__GetDisplayName(cchName, out pcchName, szName);
		}
		
		public static uint GetID(this CorpubPublishClass instance)
		{
			uint puId;
			instance.__GetID(out puId);
			return puId;
		}
		
		public static void GetName(this CorpubPublishClass instance, uint cchName, out uint pcchName, StringBuilder szName)
		{
			instance.__GetName(cchName, out pcchName, szName);
		}
		
		public static ICorPublishProcess GetProcess(this CorpubPublishClass instance, uint pid)
		{
			ICorPublishProcess ppProcess;
			instance.__GetProcess(pid, out ppProcess);
			ProcessOutParameter(ppProcess);
			return ppProcess;
		}
		
		public static uint GetProcessID(this CorpubPublishClass instance)
		{
			uint pid;
			instance.__GetProcessID(out pid);
			return pid;
		}
		
		public static ICorPublishEnum ICorPublishAppDomainEnum_Clone(this CorpubPublishClass instance)
		{
			ICorPublishEnum ppEnum;
			instance.__ICorPublishAppDomainEnum_Clone(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static uint ICorPublishAppDomainEnum_GetCount(this CorpubPublishClass instance)
		{
			uint pcelt;
			instance.__ICorPublishAppDomainEnum_GetCount(out pcelt);
			return pcelt;
		}
		
		public static void ICorPublishAppDomainEnum_Reset(this CorpubPublishClass instance)
		{
			instance.__ICorPublishAppDomainEnum_Reset();
		}
		
		public static void ICorPublishAppDomainEnum_Skip(this CorpubPublishClass instance, uint celt)
		{
			instance.__ICorPublishAppDomainEnum_Skip(celt);
		}
		
		public static int IsManaged(this CorpubPublishClass instance)
		{
			int pbManaged;
			instance.__IsManaged(out pbManaged);
			return pbManaged;
		}
		
		public static void Next(this CorpubPublishClass instance, uint celt, out ICorPublishAppDomain objects, out uint pceltFetched)
		{
			instance.__Next(celt, out objects, out pceltFetched);
			ProcessOutParameter(objects);
		}
		
		public static void Next(this CorpubPublishClass instance, uint celt, out ICorPublishProcess objects, out uint pceltFetched)
		{
			instance.__Next(celt, out objects, out pceltFetched);
			ProcessOutParameter(objects);
		}
		
		public static void Reset(this CorpubPublishClass instance)
		{
			instance.__Reset();
		}
		
		public static void Skip(this CorpubPublishClass instance, uint celt)
		{
			instance.__Skip(celt);
		}
		
		public static ICorPublishProcessEnum EnumProcesses(this ICorPublish instance, COR_PUB_ENUMPROCESS Type)
		{
			ICorPublishProcessEnum ppIEnum;
			instance.__EnumProcesses(Type, out ppIEnum);
			ProcessOutParameter(ppIEnum);
			return ppIEnum;
		}
		
		public static ICorPublishProcess GetProcess(this ICorPublish instance, uint pid)
		{
			ICorPublishProcess ppProcess;
			instance.__GetProcess(pid, out ppProcess);
			ProcessOutParameter(ppProcess);
			return ppProcess;
		}
		
		public static uint GetID(this ICorPublishAppDomain instance)
		{
			uint puId;
			instance.__GetID(out puId);
			return puId;
		}
		
		public static void GetName(this ICorPublishAppDomain instance, uint cchName, out uint pcchName, StringBuilder szName)
		{
			instance.__GetName(cchName, out pcchName, szName);
		}
		
		public static void Skip(this ICorPublishAppDomainEnum instance, uint celt)
		{
			instance.__Skip(celt);
		}
		
		public static void Reset(this ICorPublishAppDomainEnum instance)
		{
			instance.__Reset();
		}
		
		public static ICorPublishEnum Clone(this ICorPublishAppDomainEnum instance)
		{
			ICorPublishEnum ppEnum;
			instance.__Clone(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static uint GetCount(this ICorPublishAppDomainEnum instance)
		{
			uint pcelt;
			instance.__GetCount(out pcelt);
			return pcelt;
		}
		
		public static void Next(this ICorPublishAppDomainEnum instance, uint celt, out ICorPublishAppDomain objects, out uint pceltFetched)
		{
			instance.__Next(celt, out objects, out pceltFetched);
			ProcessOutParameter(objects);
		}
		
		public static void Skip(this ICorPublishEnum instance, uint celt)
		{
			instance.__Skip(celt);
		}
		
		public static void Reset(this ICorPublishEnum instance)
		{
			instance.__Reset();
		}
		
		public static ICorPublishEnum Clone(this ICorPublishEnum instance)
		{
			ICorPublishEnum ppEnum;
			instance.__Clone(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static uint GetCount(this ICorPublishEnum instance)
		{
			uint pcelt;
			instance.__GetCount(out pcelt);
			return pcelt;
		}
		
		public static int IsManaged(this ICorPublishProcess instance)
		{
			int pbManaged;
			instance.__IsManaged(out pbManaged);
			return pbManaged;
		}
		
		public static ICorPublishAppDomainEnum EnumAppDomains(this ICorPublishProcess instance)
		{
			ICorPublishAppDomainEnum ppEnum;
			instance.__EnumAppDomains(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static uint GetProcessID(this ICorPublishProcess instance)
		{
			uint pid;
			instance.__GetProcessID(out pid);
			return pid;
		}
		
		public static void GetDisplayName(this ICorPublishProcess instance, uint cchName, out uint pcchName, StringBuilder szName)
		{
			instance.__GetDisplayName(cchName, out pcchName, szName);
		}
		
		public static void Skip(this ICorPublishProcessEnum instance, uint celt)
		{
			instance.__Skip(celt);
		}
		
		public static void Reset(this ICorPublishProcessEnum instance)
		{
			instance.__Reset();
		}
		
		public static ICorPublishEnum Clone(this ICorPublishProcessEnum instance)
		{
			ICorPublishEnum ppEnum;
			instance.__Clone(out ppEnum);
			ProcessOutParameter(ppEnum);
			return ppEnum;
		}
		
		public static uint GetCount(this ICorPublishProcessEnum instance)
		{
			uint pcelt;
			instance.__GetCount(out pcelt);
			return pcelt;
		}
		
		public static void Next(this ICorPublishProcessEnum instance, uint celt, out ICorPublishProcess objects, out uint pceltFetched)
		{
			instance.__Next(celt, out objects, out pceltFetched);
			ProcessOutParameter(objects);
		}
		
	}
}
