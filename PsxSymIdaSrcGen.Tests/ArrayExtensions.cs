namespace PsxSymIdaSrcGen.Tests;

public static class ArrayExtensions // TODO move to library
{
    public static void ForEach<T>(this IEnumerable<T> array, Action<T> action)
    {
        foreach (var item in array)
        {
            action(item);
        }
    }

    public static IEnumerable<T[]> Split<T>(this T[] array, IEnumerable<int> indices)
    {
        var split = indices as int[] ?? indices.ToArray();

        if (split.Any(s => s < 0 || s > array.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(indices));
        }

        var start = 0;

        foreach (var index in split)
        {
            yield return array[start..index];

            start = index;
        }

        yield return array[start..];
    }
}