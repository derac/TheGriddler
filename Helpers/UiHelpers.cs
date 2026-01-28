using System.Windows;
using System.Windows.Media;

namespace TheGriddler.Helpers;

public static class UiHelpers
{
    public static T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
    {
        if (obj == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t)
                return t;
            
            if (FindVisualChild<T>(child) is T childOfChild)
                return childOfChild;
        }
        return null;
    }
}
