using JetBrains.Annotations;
using ReactiveUI;
using System;
using System.Net;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels.ValueConverters
{
	[ExposeServices(typeof(IBindingTypeConverter))]
	[UsedImplicitly]
	public class StringToIPEndpointTypeConverter : IBindingTypeConverter, ISingletonDependency
	{
		public int GetAffinityForObjects(Type fromType, Type toType)
		{
			if (fromType == typeof(string) && toType == typeof(IPEndPoint))
			{
				return 11;
			}

			if (fromType == typeof(IPEndPoint) && toType == typeof(string))
			{
				return 11;
			}

			return 0;
		}

		public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
		{
			if (toType == typeof(IPEndPoint) && from is string str)
			{
				if (IPEndPoint.TryParse(str, out var ipe))
				{
					result = ipe;
					return true;
				}

				result = null;
				return false;
			}

			if (from is IPEndPoint fromIPEndPoint)
			{
				result = fromIPEndPoint.ToString();
			}
			else
			{
				result = string.Empty;
			}

			return true;
		}
	}
}
