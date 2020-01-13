﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.ListViewBaseTests
{
	[TestClass]
	public class Given_ListViewBase
	{
#if !NETFX_CORE
		[TestMethod]
		public void When_MultiSelectedItem()
		{
			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = null,
				Template = new ControlTemplate(() => new ItemsPresenter()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" },
					new Border { Name = "b2" }
				}
			};

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));
			Assert.IsNotNull(panel.FindName("b2"));

			SUT.SelectionMode = ListViewSelectionMode.Multiple;

			SUT.OnItemClicked(0);

			Assert.AreEqual(1, SUT.SelectedItems.Count);

			SUT.OnItemClicked(1);

			Assert.AreEqual(2, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_SingleSelectedItem_Event()
		{
			var panel = new StackPanel();

			var item = new Border { Name = "b1" };
			var SUT = new ListViewBase()
			{
				Style = null,
				Template = new ControlTemplate(() => new ItemsPresenter()),
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Items = {
					item
				}
			};

			var model = new MyModel
			{
				SelectedItem = (object)null
			};

			SUT.SetBinding(
				Selector.SelectedItemProperty,
				new Binding()
				{
					Path = "SelectedItem",
					Source = model,
					Mode = BindingMode.TwoWay
				}
			);

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));

			var selectionChanged = 0;

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged++;
				Assert.AreEqual(item, SUT.SelectedItem);

				// In windows, when programmatically changed, the bindings are updated *after*
				// the event is raised, but *before* when the SelectedItem is changed from the UI.
				Assert.IsNull(model.SelectedItem);
			};

			SUT.SelectedIndex = 0;

			Assert.AreEqual(item, model.SelectedItem);

			Assert.IsNotNull(SUT.SelectedItem);
			Assert.AreEqual(1, selectionChanged);
			Assert.AreEqual(1, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_ResetItemsSource()
		{
			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = null,
				Template = new ControlTemplate(() => new ItemsPresenter()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ItemsSource = new int[] { 1, 2, 3 };
			SUT.OnItemClicked(0);

			SUT.ItemsSource = null;
		}
#endif

		[TestMethod]
		public void When_SelectionChanged_Changes_Selection()
		{
			var list = new ListView()
			{
				Style = null,
				ItemContainerStyle = BuildBasicContainerStyle(),
			};
			list.ItemsSource = Enumerable.Range(0, 20);

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			Assert.AreEqual(14, list.SelectedItem);
			Assert.AreEqual(14, list.SelectedIndex);

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				var l = sender as ListViewBase;
				l.SelectedItem = 14;
			}
		}

		[TestMethod]
		public void When_SelectionChanged_Changes_Selection_Repeated()
		{
			var list = new ListView()
			{
				Style = null,
				ItemContainerStyle = BuildBasicContainerStyle(),
			};
			list.ItemsSource = Enumerable.Range(0, 20);
			var callbackCount = 0;

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			Assert.AreEqual(14, list.SelectedItem);
			Assert.AreEqual(14, list.SelectedIndex);
			Assert.AreEqual(8, callbackCount); //Unlike eg TextBox.TextChanged there is no guard on reentrant modification

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				callbackCount++;
				var l = sender as ListViewBase;
				var selected = (int)l.SelectedItem;
				if (selected < 14)
				{
					selected++;
					l.SelectedItem = selected;
				}
			}
		}

		[TestMethod]
		public void When_Single_SelectionChanged_And_SelectorItem_IsSelected_Changed()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};
			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) => {
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);
			Assert.AreEqual(0, selectionChanged.Count);

			SUT.SelectedItem = source[0];

			Assert.AreEqual(source[0], SUT.SelectedValue);
			Assert.AreEqual(1, selectionChanged.Count);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.AreEqual(0, selectionChanged[0].RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);

			source[1].IsSelected = true;

			Assert.AreEqual(source[1], SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);
			Assert.AreEqual(source[1], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().RemovedItems.Count);
			Assert.IsFalse(source[0].IsSelected);

			source[2].IsSelected = true;

			Assert.AreEqual(source[2], SUT.SelectedItem);
			Assert.AreEqual(5, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().RemovedItems.Count);
			Assert.IsTrue(source[2].IsSelected);
			Assert.IsFalse(source[1].IsSelected);
			Assert.IsFalse(source[0].IsSelected);

			source[2].IsSelected = false;

			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(6, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().RemovedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().AddedItems.Count);
			Assert.IsFalse(source[0].IsSelected);
			Assert.IsFalse(source[1].IsSelected);
			Assert.IsFalse(source[2].IsSelected);
		}

		[TestMethod]
		public void When_Multi_SelectionChanged_And_SelectorItem_IsSelected_Changed()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) => {
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);
			Assert.AreEqual(0, selectionChanged.Count);

			SUT.SelectedItem = source[0];

			Assert.IsNull(SUT.SelectedValue);
			Assert.AreEqual(1, selectionChanged.Count);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.AreEqual(0, selectionChanged[0].RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);

			source[1].IsSelected = true;

			Assert.AreEqual(source[0], SUT.SelectedItem);
			Assert.AreEqual(2, selectionChanged.Count);
			Assert.AreEqual(source[1], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);
			Assert.IsTrue(source[1].IsSelected);

			source[2].IsSelected = true;

			Assert.AreEqual(source[0], SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);
			Assert.IsTrue(source[1].IsSelected);
			Assert.IsTrue(source[2].IsSelected);

			source[2].IsSelected = false;

			Assert.AreEqual(4, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().RemovedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().AddedItems.Count);
			Assert.IsTrue(source[0].IsSelected);
			Assert.IsTrue(source[1].IsSelected);
			Assert.IsFalse(source[2].IsSelected);
		}

		[TestMethod]
		public void When_Single_IsSelected_Changed_And_String_Items()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) => {
				selectionChanged.Add(e);
			};

			var source = Enumerable.Range(0, 10).Select(v => v.ToString()).ToArray();
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedItem);

			SUT.SelectedItem = "1";

			Assert.AreEqual(1, selectionChanged.Count);

			if (SUT.ContainerFromIndex(2) is ListViewItem s1)
			{
				s1.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(1, SUT.SelectedItems.Count);
			Assert.AreEqual("2", SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);
		}

		[TestMethod]
		public void When_Multi_IsSelected_Changed_And_String_Items()
		{
			var SUT = new ListView()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) => {
				selectionChanged.Add(e);
			};

			var source = Enumerable.Range(0, 10).Select(v => v.ToString()).ToArray();
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedItem);

			SUT.SelectedItem = "1";

			Assert.AreEqual(1, selectionChanged.Count);

			if (SUT.ContainerFromIndex(2) is ListViewItem s1)
			{
				s1.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(2, SUT.SelectedItems.Count);
			Assert.AreEqual("1", SUT.SelectedItem);
			Assert.AreEqual(2, selectionChanged.Count);

			if (SUT.ContainerFromIndex(3) is ListViewItem s2)
			{
				s2.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(3, SUT.SelectedItems.Count);
			Assert.AreEqual("1", SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);

			if (SUT.ContainerFromIndex(2) is ListViewItem s3)
			{
				s3.IsSelected = false;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(2, SUT.SelectedItems.Count);
		}

		private Style BuildBasicContainerStyle() =>
			new Style(typeof(Windows.UI.Xaml.Controls.ListViewItem))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ContentPresenter().Apply(p => {
								p.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding(){ Path = "ContentTemplate", RelativeSource = RelativeSource.TemplatedParent });
								p.SetBinding(ContentPresenter.ContentProperty, new Binding(){ Path = "Content", RelativeSource = RelativeSource.TemplatedParent });
							})
						)
					)
				}
			};
	}

	public class MyModel
	{
		public object SelectedItem { get; set; }
	}

	public class MyItemsControl : ItemsControl
	{
		public int OnItemsChangedCallCount { get; private set; }

		protected override void OnItemsChanged(object e)
		{
			OnItemsChangedCallCount++;
			base.OnItemsChanged(e);
		}
	}
}
