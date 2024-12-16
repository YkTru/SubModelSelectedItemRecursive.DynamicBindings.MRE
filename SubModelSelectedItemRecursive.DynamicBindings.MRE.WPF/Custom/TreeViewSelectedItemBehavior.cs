using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace SubModelSelectedItemRecursive.DynamicBindings.MRE.WPF.Custom
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
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
            }

            base.OnDetaching();
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem = e.NewValue;
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewSelectedItemBehavior behavior && behavior.AssociatedObject != null)
            {
                var treeView = behavior.AssociatedObject;
                var newSelectedItem = e.NewValue;

                if (newSelectedItem != null)
                {
                    SelectAndFocusTreeViewItem(treeView, newSelectedItem);

                    // Re-focus the tree view itself to ensure proper UI rendering
                    treeView.Focus();
                }
            }
        }

        private static void SelectAndFocusTreeViewItem(TreeView treeView, object itemToSelect)
        {
            if (treeView == null || itemToSelect == null)
                return;

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

            if (treeViewItem.DataContext == itemToSelect)
            {
                treeViewItem.IsSelected = true;
                treeViewItem.Focus(); // Set focus to ensure the blue background
                treeViewItem.BringIntoView();
                return true;
            }

            treeViewItem.IsExpanded = true; // Ensure children are loaded
            treeViewItem.ApplyTemplate();

            var itemsPresenter = (ItemsPresenter)treeViewItem.Template.FindName("ItemsHost", treeViewItem);
            if (itemsPresenter != null)
            {
                itemsPresenter.ApplyTemplate();
            }

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
