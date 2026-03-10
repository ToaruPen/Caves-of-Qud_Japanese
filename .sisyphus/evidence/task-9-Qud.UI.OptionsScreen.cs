using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiUnity.Common.Extensions;
using FuzzySharp;
using FuzzySharp.Extractor;
using UnityEngine;
using XRL;
using XRL.CharacterBuilds.UI;
using XRL.UI;
using XRL.UI.Framework;

namespace Qud.UI;

[UIView("ModernOptionsMenu", false, false, false, null, null, false, 0, false, NavCategory = "Menu", UICanvas = "_manuallyshown", UICanvasHost = 1)]
public class OptionsScreen : SingletonWindowBase<OptionsScreen>
{
	public class NavContext : NavigationContext
	{
		public bool editMode;
	}

	public FrameworkScroller hotkeyBar;

	public FrameworkScroller optionsScroller;

	public FrameworkScroller categoryScroller;

	public FrameworkSearchInput searchInput;

	public RectTransform safeArea;

	public EmbarkBuilderModuleBackButton backButton;

	public TaskCompletionSource<bool> completionSource;

	public NavContext globalContext = new NavContext();

	public ScrollContext<NavigationContext> topHorizNav = new ScrollContext<NavigationContext>();

	public ScrollContext<NavigationContext> midHorizNav = new ScrollContext<NavigationContext>();

	public ScrollContext<NavigationContext> vertNav = new ScrollContext<NavigationContext>();

	public List<OptionsDataRow> menuItems = new List<OptionsDataRow>();

	public List<OptionsDataRow> filteredMenuItems = new List<OptionsDataRow>();

	public Dictionary<string, bool> categoryExpanded = new Dictionary<string, bool>();

	public ScrollChildContext advancedOptionsScrollProxy = new ScrollChildContext();

	public OptionsRow advancedOptionsCheckboxRow;

	public OptionsCheckboxRow advancedOptionsCheck;

	public float lastWidth;

	public float breakpointBackButtonWidth;

	public bool wasInScroller;

	public NavigationContext lastContext;

	private ScrollViewCalcs _scrollCalc = new ScrollViewCalcs();

	private bool SelectFirst = true;

	public static readonly MenuOption COLLAPSE_ALL = new MenuOption
	{
		InputCommand = "V Negative",
		Description = "Collapse All",
		disabled = false
	};

	public static readonly MenuOption EXPAND_ALL = new MenuOption
	{
		InputCommand = "V Positive",
		Description = "Expand All",
		disabled = false
	};

	public static readonly MenuOption HELP_TEXT = new MenuOption
	{
		InputCommand = "CmdHelp",
		Description = "Help",
		disabled = false
	};

	public static List<MenuOption> defaultMenuOptions = new List<MenuOption>
	{
		new MenuOption
		{
			InputCommand = "NavigationXYAxis",
			Description = "navigate",
			disabled = true
		},
		COLLAPSE_ALL,
		EXPAND_ALL,
		new MenuOption
		{
			InputCommand = "Accept",
			Description = "Select",
			disabled = true
		},
		HELP_TEXT
	};

	public string searchText;

	private OptionsCategoryRow searcher = new OptionsCategoryRow();

	public FrameworkDataElement lastSelectedElement;

	public bool breakBackButton => lastWidth <= breakpointBackButtonWidth;

	public static MenuOption BACK_BUTTON => EmbarkBuilderOverlayWindow.BackMenuOption;

	public async Task<bool> OptionsMenu()
	{
		searchText = "";
		GameManager.Instance.PushGameView("ModernOptionsMenu");
		menuItems = Options.OptionsByCategory.SelectMany(delegate(KeyValuePair<string, List<GameOption>> kv)
		{
			OptionsCategoryRow element = new OptionsCategoryRow
			{
				Id = kv.Key,
				Title = kv.Key,
				CategoryId = kv.Key,
				SearchWords = kv.Key
			};
			categoryExpanded.TryAdd(kv.Key, value: true);
			return ((IEnumerable<GameOption>)kv.Value).Select((Func<GameOption, OptionsDataRow>)delegate(GameOption option)
			{
				OptionsDataRow optionsDataRow = null;
				if (option.Type == "Checkbox")
				{
					return new OptionsCheckboxRow(option);
				}
				if (option.Type == "Button")
				{
					return new OptionsMenuButtonRow(option);
				}
				if (option.Type == "Slider")
				{
					return new OptionsSliderRow(option);
				}
				return (option.Type == "Combo" || option.Type == "BigCombo") ? ((OptionsDataRow<string>)new OptionsComboBoxRow(option)) : ((OptionsDataRow<string>)new OptionsUnknownTypeRow(option));
			}).Prepend(element);
		}).ToList();
		int num = menuItems.FindIndex((OptionsDataRow row) => row.Id == "OptionShowAdvancedOptions");
		if (num >= 0)
		{
			advancedOptionsCheck = menuItems[num] as OptionsCheckboxRow;
			menuItems.RemoveAt(num);
		}
		FilterItems();
		completionSource?.TrySetCanceled();
		completionSource = new TaskCompletionSource<bool>();
		await The.UiContext;
		ControlManager.ResetInput();
		Show();
		bool info = await completionSource.Task;
		DisableNavContext();
		await The.UiContext;
		Hide();
		GameManager.Instance.PopGameView(bHard: true);
		return info;
	}

	public void FilterItems()
	{
		filteredMenuItems.Clear();
		if (string.IsNullOrWhiteSpace(searchText))
		{
			Dictionary<string, OptionsCategoryRow> dictionary = menuItems.OfType<OptionsCategoryRow>().ToDictionary((OptionsCategoryRow i) => i.CategoryId);
			{
				foreach (IGrouping<string, OptionsDataRow> item in from s in menuItems
					where s != null && !(s is OptionsCategoryRow) && s.IsEnabled != null && s.IsEnabled()
					group s by s.CategoryId)
				{
					categoryExpanded[item.Key] = true;
					filteredMenuItems.Add(dictionary[item.Key]);
					foreach (OptionsDataRow item2 in item)
					{
						if (!(item2 is OptionsCategoryRow))
						{
							filteredMenuItems.Add(item2);
						}
					}
				}
				return;
			}
		}
		IEnumerable<ExtractedResult<OptionsDataRow>> source = Process.ExtractTop(searcher, menuItems, (OptionsDataRow i) => i.SearchWords.ToLower(), null, menuItems.Count, 50);
		Dictionary<string, OptionsCategoryRow> dictionary2 = menuItems.OfType<OptionsCategoryRow>().ToDictionary((OptionsCategoryRow i) => i.CategoryId);
		foreach (IGrouping<string, ExtractedResult<OptionsDataRow>> item3 in from s in source
			where !(s.Value is OptionsCategoryRow)
			group s by s.Value.CategoryId)
		{
			categoryExpanded[item3.Key] = true;
			filteredMenuItems.Add(dictionary2[item3.Key]);
			foreach (ExtractedResult<OptionsDataRow> item4 in item3)
			{
				if (!(item4.Value is OptionsCategoryRow))
				{
					filteredMenuItems.Add(item4.Value);
				}
			}
		}
	}

	public void Exit()
	{
		completionSource?.TrySetResult(result: false);
	}

	public void Update()
	{
		if (globalContext.IsActive())
		{
			bool flag = NavigationController.instance.activeContext?.IsInside(optionsScroller.GetNavigationContext()) ?? false;
			float width = base.rectTransform.rect.width;
			CheckRequirements();
			if (flag != wasInScroller || lastWidth != width)
			{
				wasInScroller = flag;
				lastWidth = width;
				backButton.gameObject.SetActive(!breakBackButton);
				safeArea.offsetMin = new Vector2(breakBackButton ? 10 : 150, safeArea.offsetMin.y);
				safeArea.offsetMax = new Vector2(breakBackButton ? (-10) : (-150), safeArea.offsetMax.y);
				UpdateMenuBars();
			}
			else if (lastContext != NavigationController.instance.activeContext)
			{
				lastContext = NavigationController.instance.activeContext;
				UpdateMenuBars();
			}
		}
	}

	protected void SetAllExpanded(bool state)
	{
		OptionsDataRow currentSelection = lastSelectedElement as OptionsDataRow;
		string[] array = categoryExpanded.Keys.ToArray();
		foreach (string key in array)
		{
			categoryExpanded[key] = state;
		}
		Show();
		if (!state)
		{
			currentSelection = GetMenuItems().FirstOrDefault((OptionsDataRow row) => row.CategoryId == currentSelection?.CategoryId);
		}
		int index = Math.Max(0, GetMenuItems().IndexOf(currentSelection));
		optionsScroller.scrollContext.SelectIndex(index);
	}

	protected void CheckRequirements()
	{
		if (!globalContext.editMode && Options.ShouldCheckRequirements())
		{
			float contentTop = ScrollViewCalcs.GetScrollViewCalcs(optionsScroller.scrollRect.transform as RectTransform, _scrollCalc).contentTop;
			MetricsManager.LogEditorInfo("Requirements Rescroll: " + _scrollCalc);
			OptionsDataRow item = lastSelectedElement as OptionsDataRow;
			FilterItems();
			Show();
			ScrollViewCalcs.GetScrollViewCalcs(optionsScroller.scrollRect.transform as RectTransform, _scrollCalc);
			MetricsManager.LogEditorInfo("Requirements Reshow: " + _scrollCalc);
			_scrollCalc.ScrollToContentTop(contentTop);
			ScrollViewCalcs.GetScrollViewCalcs(optionsScroller.scrollRect.transform as RectTransform, _scrollCalc);
			MetricsManager.LogEditorInfo("Requirements PostScroll: " + _scrollCalc);
			int index = Math.Max(0, GetMenuItems().IndexOf(item));
			optionsScroller.scrollContext.SelectIndex(index);
			ScrollViewCalcs.GetScrollViewCalcs(optionsScroller.scrollRect.transform as RectTransform, _scrollCalc);
			MetricsManager.LogEditorInfo("Requirements PostSelect: " + _scrollCalc);
		}
	}

	public void ExpandAll()
	{
		SetAllExpanded(state: true);
	}

	public void CollapseAll()
	{
		SetAllExpanded(state: false);
	}

	public void SetupContext()
	{
		globalContext.buttonHandlers = new Dictionary<InputButtonTypes, Action>();
		globalContext.buttonHandlers.Set(InputButtonTypes.CancelButton, XRL.UI.Framework.Event.Helpers.Handle(Exit));
		globalContext.commandHandlers = new Dictionary<string, Action>
		{
			{
				"CmdFilter",
				XRL.UI.Framework.Event.Helpers.Handle(searchInput.EnterAndOpen)
			},
			{
				"V Positive",
				XRL.UI.Framework.Event.Helpers.Handle(ExpandAll)
			},
			{
				"V Negative",
				XRL.UI.Framework.Event.Helpers.Handle(CollapseAll)
			},
			{
				"CmdHelp",
				XRL.UI.Framework.Event.Helpers.Handle(delegate
				{
					HandleMenuOption(HELP_TEXT);
				})
			}
		};
		midHorizNav.SetAxis(InputAxisTypes.NavigationXAxis);
		midHorizNav.contexts.Clear();
		midHorizNav.contexts.Add(backButton.navigationContext);
		midHorizNav.contexts.Add(categoryScroller.GetNavigationContext());
		midHorizNav.contexts.Add(vertNav);
		vertNav.SetAxis(InputAxisTypes.NavigationYAxis);
		vertNav.contexts.Clear();
		topHorizNav.SetAxis(InputAxisTypes.NavigationXAxis);
		topHorizNav.contexts.Clear();
		topHorizNav.contexts.Add(searchInput.context);
		if (advancedOptionsCheck != null)
		{
			advancedOptionsScrollProxy.index = 0;
			advancedOptionsCheckboxRow.SetupContexts(advancedOptionsScrollProxy);
			topHorizNav.contexts.Add(advancedOptionsScrollProxy);
		}
		vertNav.contexts.Add(topHorizNav);
		searchInput.context.inputText = searchText ?? "";
		searchInput.OnSearchTextChange.RemoveListener(OnSearchTextChange);
		searchInput.OnSearchTextChange.AddListener(OnSearchTextChange);
		vertNav.contexts.Add(optionsScroller.GetNavigationContext());
		midHorizNav.Setup();
		midHorizNav.parentContext = globalContext;
		optionsScroller.scrollContext.wraps = false;
		vertNav.wraps = false;
		midHorizNav.wraps = false;
	}

	public void OnSearchTextChange(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			searchText = "";
		}
		else
		{
			searcher.SearchWords = (searchText = text);
		}
		FilterItems();
		Show();
	}

	public void UpdateMenuBars()
	{
		if (lastSelectedElement is OptionsDataRow optionsDataRow && !string.IsNullOrEmpty(optionsDataRow.HelpText))
		{
			hotkeyBar.BeforeShow(null, NavigationController.GetMenuOptions(defaultMenuOptions));
		}
		else
		{
			hotkeyBar.BeforeShow(null, NavigationController.GetMenuOptions(defaultMenuOptions.Take(defaultMenuOptions.Count - 1)));
		}
		hotkeyBar.GetNavigationContext().disabled = true;
		hotkeyBar.onSelected.RemoveAllListeners();
		hotkeyBar.onSelected.AddListener(HandleMenuOption);
	}

	public async void HandleMenuOption(FrameworkDataElement data)
	{
		if (data == COLLAPSE_ALL)
		{
			CollapseAll();
		}
		else if (data == EXPAND_ALL)
		{
			ExpandAll();
		}
		else
		{
			if (data != HELP_TEXT)
			{
				return;
			}
			OptionsDataRow optionSelected = lastSelectedElement as OptionsDataRow;
			if (!string.IsNullOrEmpty(optionSelected?.HelpText))
			{
				await NavigationController.instance.SuspendContextWhile(() => Popup.NewPopupMessageAsync(optionSelected.HelpText, PopupMessage.AnyKey, null, "Option: " + optionSelected.Title));
			}
		}
	}

	public IEnumerable<OptionsDataRow> GetMenuItems()
	{
		foreach (OptionsDataRow filteredMenuItem in filteredMenuItems)
		{
			if (filteredMenuItem is OptionsCategoryRow optionsCategoryRow)
			{
				optionsCategoryRow.categoryExpanded = categoryExpanded[optionsCategoryRow.Id];
				yield return filteredMenuItem;
			}
			else if (categoryExpanded[filteredMenuItem.CategoryId] && filteredMenuItem.IsEnabled())
			{
				if (filteredMenuItem is OptionsCheckboxRow optionsCheckboxRow)
				{
					optionsCheckboxRow.Value = Options.GetOption(optionsCheckboxRow.Id) == "Yes";
				}
				yield return filteredMenuItem;
			}
		}
	}

	public IEnumerable<OptionsDataRow> GetCategoryItems()
	{
		foreach (OptionsDataRow filteredMenuItem in filteredMenuItems)
		{
			if (filteredMenuItem is OptionsCategoryRow)
			{
				yield return filteredMenuItem;
			}
		}
	}

	public override void Show()
	{
		base.Show();
		backButton?.gameObject.SetActive(!breakBackButton);
		if (backButton.navigationContext == null)
		{
			backButton.Awake();
		}
		backButton.navigationContext.buttonHandlers = new Dictionary<InputButtonTypes, Action>();
		backButton.navigationContext.buttonHandlers.Set(InputButtonTypes.AcceptButton, XRL.UI.Framework.Event.Helpers.Handle(Exit));
		optionsScroller.scrollContext.wraps = false;
		optionsScroller.BeforeShow(null, GetMenuItems());
		categoryScroller.scrollContext.wraps = true;
		categoryScroller.BeforeShow(null, GetCategoryItems());
		if (SelectFirst)
		{
			SelectFirst = false;
			optionsScroller.scrollContext.selectedPosition = 0;
		}
		optionsScroller.onSelected.RemoveAllListeners();
		optionsScroller.onSelected.AddListener(HandleSelect);
		optionsScroller.onHighlight.RemoveAllListeners();
		optionsScroller.onHighlight.AddListener(HandleHighlight);
		categoryScroller.onSelected.RemoveAllListeners();
		categoryScroller.onSelected.AddListener(HandleSelectLeft);
		categoryScroller.onHighlight.RemoveAllListeners();
		categoryScroller.onHighlight.AddListener(HandleHighlightLeft);
		UpdateMenuBars();
		if (advancedOptionsCheck != null)
		{
			advancedOptionsCheckboxRow.setData(advancedOptionsCheck);
			advancedOptionsCheckboxRow.gameObject.SetActive(value: true);
		}
		else
		{
			advancedOptionsCheckboxRow.gameObject.SetActive(value: false);
		}
		SetupContext();
		EnableNavContext();
	}

	public async void HandleSelect(FrameworkDataElement element)
	{
		if (element is OptionsCategoryRow optionsCategoryRow)
		{
			MetricsManager.LogEditorInfo("Handle Category select " + optionsCategoryRow.CategoryId);
			categoryExpanded[optionsCategoryRow.CategoryId] = !categoryExpanded[optionsCategoryRow.CategoryId];
			Show();
			optionsScroller.scrollContext.SelectIndex(GetMenuItems().ToList().FindIndex((OptionsDataRow s) => s == element));
		}
		if (element is OptionsMenuButtonRow optionsMenuButtonRow && optionsMenuButtonRow.OnClick.Invoke(null, null) is Task<bool> task)
		{
			await task;
		}
		Show();
	}

	public void HandleHighlight(FrameworkDataElement element)
	{
		lastSelectedElement = element;
		string catId = null;
		if (element is OptionsDataRow optionsDataRow)
		{
			catId = optionsDataRow.CategoryId;
		}
		if (catId != null)
		{
			FrameworkScroller frameworkScroller = categoryScroller;
			int selectedPosition = (categoryScroller.scrollContext.selectedPosition = GetCategoryItems().ToList().FindIndex((OptionsDataRow s) => s?.CategoryId == catId));
			frameworkScroller.selectedPosition = selectedPosition;
		}
		UpdateMenuBars();
	}

	public void HandleSelectLeft(FrameworkDataElement element)
	{
		OptionsCategoryRow cat = element as OptionsCategoryRow;
		if (cat != null)
		{
			if (!categoryExpanded[cat.CategoryId])
			{
				categoryExpanded[cat.CategoryId] = true;
				Show();
			}
			int num = GetMenuItems().ToList().FindIndex((OptionsDataRow s) => (s as OptionsCategoryRow)?.CategoryId == cat.CategoryId);
			optionsScroller.scrollContext.SelectIndex(num);
			optionsScroller.selectedPosition = num;
			optionsScroller.ScrollSelectedToTop();
		}
	}

	public void HandleHighlightLeft(FrameworkDataElement element)
	{
	}

	public override void Hide()
	{
		base.Hide();
		DisableNavContext();
		base.gameObject.SetActive(value: false);
		ControlManager.ResetInput();
	}

	public void EnableNavContext()
	{
		globalContext.disabled = false;
		optionsScroller.GetNavigationContext().ActivateAndEnable();
	}

	public void DisableNavContext(bool deactivate = true)
	{
		if (deactivate)
		{
			NavigationContext activeContext = NavigationController.instance.activeContext;
			if (activeContext != null && activeContext.IsInside(globalContext))
			{
				NavigationController.instance.activeContext = null;
			}
		}
		globalContext.disabled = true;
	}
}
