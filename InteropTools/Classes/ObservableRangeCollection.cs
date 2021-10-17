﻿// Copyright 2015-2021 (c) Interop Tools Development Team
// This file is licensed to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace InteropTools
{
    /// <summary>
    ///     Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole
    ///     list is refreshed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        ///     Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class.
        /// </summary>
        public ObservableRangeCollection()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class that contains
        ///     elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">collection: The collection from which the elements are copied.</param>
        /// <exception cref="ArgumentNullException">The collection parameter cannot be null.</exception>
        public ObservableRangeCollection(IEnumerable<T> collection)
        : base(collection)
        {
        }

        /// <summary>
        ///     Adds the elements of the specified collection to the end of the ObservableCollection(Of T).
        /// </summary>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (T i in collection)
            {
                Items.Add(i);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ClearList()
        {
            Items.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        ///     Removes the first occurence of each item in the specified collection from ObservableCollection(Of T).
        /// </summary>
        public void RemoveRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (T i in collection)
            {
                Items.Remove(i);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        ///     Clears the current collection and replaces it with the specified item.
        /// </summary>
        public void Replace(T item)
        {
            ReplaceRange(new[] { item });
        }

        /// <summary>
        ///     Clears the current collection and replaces it with the specified collection.
        /// </summary>
        public void ReplaceRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            Items.Clear();

            foreach (T i in collection)
            {
                Items.Add(i);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}