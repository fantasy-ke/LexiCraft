namespace ZAnalyzers.Core.Attribute
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)] 
	public class RouteAttribute(string route) : System.Attribute
	{
		public readonly string Route = route;
	}
}
