﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POEApi.Infrastructure;
using POEApi.Model;
using Procurement.Interfaces;
using Procurement.ViewModel;
using Procurement.ViewModel.Filters;

namespace Procurement.Controls
{
    public abstract class AbstractStashTabControl : UserControl, IStashControl
    {
        public static readonly DependencyProperty FiltersProperty =
            DependencyProperty.Register("Filters", typeof(IEnumerable<IFilter>), typeof(StashTabControl), null);

        /// <summary>
        /// Dictionary to tie together items and their view models, this is searched and the viewModels
        /// IsItemInFilter is updated
        /// </summary>
        public readonly Dictionary<Item, ItemDisplayViewModel> StashByLocation = new Dictionary<Item, ItemDisplayViewModel>();

        public bool Ready;
        public TabType TabType;

        public AbstractStashTabControl()
        {
            
        }

        public AbstractStashTabControl(int tabNumber)
        {
            TabNumber = tabNumber;
            Stash = ApplicationState.Stash[ApplicationState.CurrentLeague].GetItemsByTab(TabNumber);
            
            Loaded += Control_Loaded;
        }

        public List<Item> Stash { get; set; }

        public virtual void RefreshTab(string accountName)
        {
            var stash = ApplicationState.Stash[ApplicationState.CurrentLeague];
            stash.RefreshTab(ApplicationState.Model, ApplicationState.CurrentLeague, TabNumber, accountName);
        }

        public int TabNumber { get; set; }

        public int FilterResults { get; set; }

        public List<IFilter> Filters
        {
            get { return (List<IFilter>) GetValue(FiltersProperty); }
            set { SetValue(FiltersProperty, value); }
        }

        public void SetPremiumTabBorderColour()
        {
            var tab = ApplicationState.Stash[ApplicationState.CurrentLeague].Tabs[TabNumber];

            if (Border == null || tab.Colour == null)
            {
                return;
            }

            var color = tab.Colour.WpfColor;

            Border.BorderBrush = new SolidColorBrush(color);
        }

        public abstract Border Border { get; }

        public virtual void ForceUpdate()
        {
            FilterResults = !Filters.Any() ? -1 : 0;

            foreach (var item in StashByLocation)
            {
                if (Search(item.Key))
                {
                    item.Value.IsItemInFilter = true;
                    FilterResults++;
                }
                else
                {
                    item.Value.IsItemInFilter = false;
                }
            }

            UpdateLayout();
        }

        public void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (Ready)
            {
                return;
            }

            Refresh();
        }

        private void UpdateStashByLocation()
        {
            StashByLocation.Clear();

            foreach (var item in Stash)
            {
                var entry = StashByLocation.Keys.FirstOrDefault(x => x.X == item.X
                                                                     && x.Y == item.Y);

                if (entry != null)
                {
                    continue;
                }

                StashByLocation.Add(item, getImage(item));
            }
        }

        private ItemDisplayViewModel getImage(Item item)
        {
            return new ItemDisplayViewModel(item);
        }

        public virtual void Refresh()
        {
            Stash = ApplicationState.Stash[ApplicationState.CurrentLeague].GetItemsByTab(TabNumber);
            TabType = GetTabType();

            UpdateStashByLocation();
        }

        private TabType GetTabType()
        {
            try
            {
                return ApplicationState.Stash[ApplicationState.CurrentLeague].Tabs[TabNumber].Type;
            }
            catch (Exception ex)
            {
                //Todo: This should be injected.
                Logger.Log("Error in StashControl.GetTabType: " + ex);
                return TabType.Normal;
            }
        }

        private bool Search(Item item)
        {
            if (!Filters.Any())
            {
                return false;
            }

            return Filters.All(filter => filter.Applicable(item));
        }
    }
}