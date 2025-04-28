namespace ZAnalyzers.Core.Attribute
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class FilterAttribute : System.Attribute
	{
		public Type[] Types { get; set; }

		public FilterAttribute(params Type[] types)
		{
			Types = types;
		}
	}
}
