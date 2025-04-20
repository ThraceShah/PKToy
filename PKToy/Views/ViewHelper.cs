namespace PKToy.Views;
static class ViewHelper
{
    internal static T New<T>() where T : new()
    {
        return new T();
    }
}