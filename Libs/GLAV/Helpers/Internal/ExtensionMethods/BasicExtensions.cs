
namespace GLAV.Helpers.Internal.Extensions;
internal static class BasicExtensions
{
    public static void ReplaceFirst<T>(this T[] array, T target, T replacement)
    {
        int index = Array.IndexOf(array, target);
        if (index != -1)
        {
            array[index] = replacement;
        }
    }
}