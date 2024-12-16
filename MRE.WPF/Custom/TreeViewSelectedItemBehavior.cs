using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace MRE.WPF.Custom
{
    public class TreeViewSelectedItemBehavior : Behavior<TreeView>
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(TreeViewSelectedItemBehavior),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
                Console.WriteLine("TreeViewSelectedItemBehavior attached.");
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
                Console.WriteLine("TreeViewSelectedItemBehavior detached.");
            }

            base.OnDetaching();
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem = e.NewValue;
            Console.WriteLine($"TreeView SelectedItem changed to: {SelectedItem}");
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewSelectedItemBehavior behavior && behavior.AssociatedObject != null)
            {
                var treeView = behavior.AssociatedObject;
                var newSelectedItem = e.NewValue;

                Console.WriteLine($"DependencyProperty SelectedItem changed to: {newSelectedItem}");

                if (newSelectedItem != null)
                {
                    SelectAndFocusTreeViewItem(treeView, newSelectedItem);
                    treeView.Focus();
                }
            }
        }

        private static void SelectAndFocusTreeViewItem(TreeView treeView, object itemToSelect)
        {
            if (treeView == null || itemToSelect == null)
                return;

            Console.WriteLine($"Attempting to select item: {itemToSelect}");

            foreach (var item in treeView.Items)
            {
                if (SelectTreeViewItemRecursive(treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem,
                        itemToSelect))
                {
                    break;
                }
            }
        }

        private static bool SelectTreeViewItemRecursive(TreeViewItem treeViewItem, object itemToSelect)
        {
            if (treeViewItem == null)
                return false;

            Console.WriteLine($"Checking TreeViewItem: {treeViewItem.DataContext}");

            if (treeViewItem.DataContext == itemToSelect)
            {
                Console.WriteLine($"TreeViewItem matched and selected: {itemToSelect}");
                treeViewItem.IsSelected = true;
                treeViewItem.Focus();
                treeViewItem.BringIntoView();
                return true;
            }

            treeViewItem.IsExpanded = true;
            treeViewItem.ApplyTemplate();

            var itemsHostPanel = (Panel)VisualTreeHelper.GetChild(treeViewItem, 0);
            if (itemsHostPanel != null)
            {
                for (int i = 0; i < treeViewItem.Items.Count; i++)
                {
                    var childItem = (TreeViewItem)treeViewItem.ItemContainerGenerator.ContainerFromIndex(i);
                    if (SelectTreeViewItemRecursive(childItem, itemToSelect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
