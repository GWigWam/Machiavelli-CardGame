namespace GWigWam.Machiavelli.Core;
public static class RandomExtensions
{
    public static T RandomItem<T>(this IEnumerable<T> values)
        => values.ElementAt(Random.Shared.Next(values.Count()));

    public static T RemoveRandomItem<T>(this IList<T> list)
    {
        var item = list.RandomItem();
        list.Remove(item);
        return item;
    }

    public static IEnumerable<T> RemoveRandomItems<T>(this IList<T> list, int count)
    {
        while (count > 0 && list.Count > 0)
        {
            yield return list.RemoveRandomItem();
            count--;
        }
    }
}
