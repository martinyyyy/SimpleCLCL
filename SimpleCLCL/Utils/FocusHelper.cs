using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpleCLCL.Utils
{
    class FocusHelper
    {
        public static void FocusFirstItem(ListBox listBox)
        {
            listBox.SelectedIndex = 0;

            listBox.UpdateLayout(); // Pre-generates item containers
            var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            scrollViewer.ScrollToVerticalOffset(0);

            ListBoxItem listBoxItem = GetCurrentListboxItem(listBox);
            if (listBoxItem != null)
            {
                listBox.ScrollIntoView(listBox.Items[0]);
                listBoxItem.Focus();
            }
        }
        
        public static ListBoxItem GetCurrentListboxItem(ListBox listBox)
        {
            return (ListBoxItem)listBox
                .ItemContainerGenerator
                .ContainerFromItem(listBox.SelectedItem);
        }

        public static TChild FindVisualChild<TChild>(DependencyObject obj) where TChild : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is TChild dependencyObject)
                {
                    return dependencyObject;
                }

                var childOfChild = FindVisualChild<TChild>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }


    }
}
