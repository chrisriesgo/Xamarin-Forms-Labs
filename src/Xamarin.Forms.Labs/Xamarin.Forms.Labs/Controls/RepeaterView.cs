﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Xamarin.Forms.Labs.Controls
{
    public class RepeaterView<T> : StackLayout
    {
        public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create<RepeaterView<T>, DataTemplate>(p => p.ItemTemplate, default(DataTemplate));
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create<RepeaterView<T>, ObservableCollection<T>>(p => p.ItemsSource, new ObservableCollection<T>(), BindingMode.OneWay, null, ItemsChanged);
        public static BindableProperty ItemClickCommandProperty =
            BindableProperty.Create<RepeaterView<T>, ICommand>(x => x.ItemClickCommand, null);
        public static readonly BindableProperty TemplateSelectorProperty =
            BindableProperty.Create<RepeaterView<T>, TemplateSelector>(x => x.TemplateSelector, default(TemplateSelector));

        public delegate void RepeaterViewItemAddedEventHandler(object sender, RepeaterViewItemAddedEventArgs args);
        public event RepeaterViewItemAddedEventHandler ItemCreated;

        public RepeaterView()
        {
            Spacing = 0;
        }

        public ObservableCollection<T> ItemsSource
        {
            get { return (ObservableCollection<T>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public TemplateSelector TemplateSelector
        {
            get { return (TemplateSelector)GetValue(TemplateSelectorProperty); }
            set { SetValue(TemplateSelectorProperty, value); }
        }
        public ICommand ItemClickCommand
        {
            get { return (ICommand)this.GetValue(ItemClickCommandProperty); }
            set { SetValue(ItemClickCommandProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gives codebehind a chance to play with the 
        /// newly created view object :D
        /// </summary>
        /// <param name="view">The visual view object</param>
        /// <param name="model">The item being added</param>
        protected virtual void NotifyItemAdded(View view, T model)
        {
            if (ItemCreated != null)
            {
                ItemCreated(this, new RepeaterViewItemAddedEventArgs(view, model));
            }
        }
        /// <summary>
        /// Select a datatemplate dynamically
        /// Prefer the TemplateSelector then the DataTemplate
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual DataTemplate GetTemplateFor(Type type)
        {
            DataTemplate retTemplate = null;
            if (TemplateSelector != null)
                retTemplate = TemplateSelector.TemplateFor(type);
            return retTemplate ?? ItemTemplate;
        }

        /// <summary>
        /// Creates a view based on the items type
        /// While we do have T, T could very well be
        /// a common superclass or an interface by 
        /// using the items actual type we support
        /// both inheritance base polymorphism
        /// and shape based polymorphism
        /// </summary>
        /// <param name="item"></param>
        /// <returns>A View that has been initialized with item as it's BindingContext</returns>
        protected virtual View ViewFor(T item)
        {
            var template = GetTemplateFor(item.GetType());
            var content = template.CreateContent();
            var view = ((ViewCell)content).View;
            view.BindingContext = item;
            view.GestureRecognizers.Add(new TapGestureRecognizer { Command = ItemClickCommand, CommandParameter = item });
            return view;
        }

        /// <summary>
        /// Reset the collection of bound objects
        /// Remove the old collection changed eventhandler (if any)
        /// Create new cells for each new item
        /// </summary>
        /// <param name="bindable">The control</param>
        /// <param name="oldValue">Previous bound collection</param>
        /// <param name="newValue">New bound collection</param>
        private static void ItemsChanged(BindableObject bindable, ObservableCollection<T> oldValue, ObservableCollection<T> newValue)
        {
            var control = bindable as RepeaterView<T>;
            if (control == null)
                throw new Exception("Invalid bindable object passed to ReapterView::ItemsChanged expected a ReapterView<T> received a " + bindable.GetType().Name);
            if (oldValue != null)
                oldValue.CollectionChanged -= control.ItemsSource_CollectionChanged;

            control.ItemsSource.CollectionChanged += control.ItemsSource_CollectionChanged;
            control.Children.Clear();

            foreach (var item in newValue)
            {
                var view = control.ViewFor(item);
                control.Children.Add(view);
                control.NotifyItemAdded(view, item);
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Update visible controls based on changes in the bound collection
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e"></param>
        void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Children.Clear();
            }
            else
            {
                if (e.OldItems != null)
                {
                    Children.RemoveAt(e.OldStartingIndex);
                }

                if (e.NewItems != null)
                {
                    foreach (T item in e.NewItems)
                    {
                        var view = ViewFor(item);
                        Children.Insert(ItemsSource.IndexOf(item), view);
                        NotifyItemAdded(view, item);
                    }
                }
            }
            UpdateChildrenLayout();
            InvalidateLayout();

        }
    }

    public class RepeaterViewItemAddedEventArgs : EventArgs
    {
        public RepeaterViewItemAddedEventArgs(View view, object model)
        {
            View = view;
            Model = model;
        }

        public View View { get; set; }
        public object Model { get; set; }
    }
}
