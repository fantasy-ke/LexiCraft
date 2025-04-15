using System;

namespace ZService.Analyzers.Attribute
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class IgnoreFilterAttribute : System.Attribute
	{
	}
}
