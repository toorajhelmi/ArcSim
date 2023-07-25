using System;
namespace Research.ArcSim.Extensions
{
	public static class ListEx
	{
		public static T AddX<T>(this List<T> list, T item)
		{
			list.Add(item);
			return item;
		}
	}
}

