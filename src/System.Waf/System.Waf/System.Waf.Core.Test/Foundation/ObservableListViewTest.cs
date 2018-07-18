﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Waf.Foundation;

namespace Test.Waf.Foundation
{
    [TestClass]
    public class ObservableListViewTest
    {
        private ObservableCollection<string> originalList;
        private ObservableListView<string> observableListView;
        private NotifyCollectionChangedEventArgs eventArgs;
        private int countChangedCount;
        private int indexerChangedCount;

        [TestInitialize]
        public void Initialize()
        {
            originalList = new ObservableCollection<string>();
            observableListView = new ObservableListView<string>(originalList);

            NotifyCollectionChangedEventHandler collectionHandler = (sender, e) =>
            {
                eventArgs = e;
            };
            observableListView.CollectionChanged += collectionHandler;

            PropertyChangedEventHandler propertyHandler = (sender, e) =>
            {
                if (e.PropertyName == nameof(observableListView.Count))
                {
                    countChangedCount++;
                }
                else if (e.PropertyName == "Item[]")
                {
                    indexerChangedCount++;
                }
            };
            observableListView.PropertyChanged += propertyHandler;
        }

        [TestCleanup]
        public void Cleanup()
        {
            AssertNoEventsRaised(() =>
            {
                observableListView.Dispose();
                originalList.Add("disposed");
            });
        }

        [TestMethod]
        public void RelayEventsWithoutFilter()
        {
            originalList.Add("first");
            AssertElementAdded("first", 0, eventArgs);
            Assert.AreEqual(1, countChangedCount);
            Assert.AreEqual(1, indexerChangedCount);

            originalList.Add("second");
            AssertElementAdded("second", 1, eventArgs);

            originalList.Add("third");
            AssertElementAdded("third", 2, eventArgs);

            AssertNoEventsRaised(() => observableListView.Update());  // Collection has not changed.

            originalList.Move(0, 1);
            AssertElementMoved("second", 1, 0, eventArgs);

            countChangedCount = indexerChangedCount = 0;
            originalList.Clear();
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, eventArgs.Action);
            Assert.AreEqual(1, countChangedCount);
            Assert.AreEqual(1, indexerChangedCount);

            originalList.Insert(0, "1");
            AssertElementAdded("1", 0, eventArgs);

            originalList.Insert(1, "3");
            AssertElementAdded("3", 1, eventArgs);

            originalList.Insert(1, "2");
            AssertElementAdded("2", 1, eventArgs);

            countChangedCount = indexerChangedCount = 0;
            originalList.Remove("1");
            AssertElementRemoved("1", 0, eventArgs);
            Assert.AreEqual(1, countChangedCount);
            Assert.AreEqual(1, indexerChangedCount);

            originalList.RemoveAt(1);
            AssertElementRemoved("3", 1, eventArgs);
        }

        [TestMethod]
        public void RelayEventsWithFilter()
        {
            observableListView.Filter = item => item != "second" && item != "2";

            originalList.Add("first");
            AssertElementAdded("first", 0, eventArgs);
            Assert.AreEqual(1, countChangedCount);
            Assert.AreEqual(1, indexerChangedCount);

            AssertNoEventsRaised(() => originalList.Add("second"));

            originalList.Add("third");
            AssertElementAdded("third", 1, eventArgs);

            AssertNoEventsRaised(() => observableListView.Update());  // Collection has not changed.

            Assert.IsTrue(new[] { "first", "third" }.SequenceEqual(observableListView));

            countChangedCount = indexerChangedCount = 0;
            originalList.Clear();
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, eventArgs.Action);
            Assert.AreEqual(1, countChangedCount);
            Assert.AreEqual(1, indexerChangedCount);

            originalList.Insert(0, "1");
            AssertElementAdded("1", 0, eventArgs);

            originalList.Insert(1, "3");
            AssertElementAdded("3", 1, eventArgs);

            AssertNoEventsRaised(() => originalList.Insert(1, "2"));

            Assert.IsTrue(new[] { "1", "3" }.SequenceEqual(observableListView));

            countChangedCount = indexerChangedCount = 0;
            originalList.Remove("1");
            AssertElementRemoved("1", 0, eventArgs);
            Assert.AreEqual(1, countChangedCount);
            Assert.AreEqual(1, indexerChangedCount);

            originalList.RemoveAt(1);
            AssertElementRemoved("3", 0, eventArgs);  // Index 0 because "2" is hidden by filter
        }

        private static void AssertElementAdded<T>(T newItem, int newStartingIndex, NotifyCollectionChangedEventArgs eventArgs)
        {
            Assert.AreEqual(NotifyCollectionChangedAction.Add, eventArgs.Action);
            Assert.AreEqual(newItem, eventArgs.NewItems.Cast<T>().Single());
            Assert.AreEqual(newStartingIndex, eventArgs.NewStartingIndex);
            Assert.IsNull(eventArgs.OldItems);
            Assert.AreEqual(-1, eventArgs.OldStartingIndex);
        }

        private static void AssertElementRemoved<T>(T oldItem, int oldStartingIndex, NotifyCollectionChangedEventArgs eventArgs)
        {
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, eventArgs.Action);
            Assert.AreEqual(oldItem, eventArgs.OldItems.Cast<T>().Single());
            Assert.AreEqual(oldStartingIndex, eventArgs.OldStartingIndex);
            Assert.IsNull(eventArgs.NewItems);
            Assert.AreEqual(-1, eventArgs.NewStartingIndex);
        }

        private static void AssertElementMoved<T>(T item, int oldIndex, int newIndex, NotifyCollectionChangedEventArgs eventArgs)
        {
            Assert.AreEqual(NotifyCollectionChangedAction.Move, eventArgs.Action);
            Assert.AreEqual(item, eventArgs.NewItems.Cast<T>().Single());
            Assert.AreEqual(item, eventArgs.OldItems.Cast<T>().Single());
            Assert.AreEqual(oldIndex, eventArgs.OldStartingIndex);
            Assert.AreEqual(newIndex, eventArgs.NewStartingIndex);
        }

        private void AssertNoEventsRaised(Action action)
        {
            eventArgs = null;
            countChangedCount = indexerChangedCount = 0;

            action();

            Assert.IsNull(eventArgs);
            Assert.AreEqual(0, countChangedCount);
            Assert.AreEqual(0, indexerChangedCount);
        }
    }
}
