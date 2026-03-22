using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ConsoleLib.Console;
using Qud.API;
using UnityEngine;
using UnityEngine.UI;
using XRL;
using XRL.UI;
using XRL.UI.Framework;

namespace Qud.UI;

[UIView("MainMenu", false, false, false, null, null, false, 0, false, NavCategory = "Menu", UICanvas = "MainMenu", UICanvasHost = 1)]
public class MainMenu : SingletonWindowBase<MainMenu>, ControlManager.IControllerChangedEvent
{
	protected List<SaveInfoData> saves;

	public Image background;

	public CanvasGroup logoFader;

	public CanvasGroup leftFader;

	public RectTransform centerTransform;

	public VerticalLayoutGroup centerFlyoutGroup;

	public FrameworkScroller hotkeyBar;

	public FrameworkScroller leftScroller;

	public FrameworkScroller rightScroller;

	public TaskCompletionSource<SaveGameInfo> completionSource;

	public NavigationContext globalContext = new NavigationContext();

	public ScrollContext<NavigationContext> midHorizNav = new ScrollContext<NavigationContext>();

	public UITextSkin versionText;

	public GameObject[] backgrounds;

	public static List<MainMenuOptionData> LeftOptions = new List<MainMenuOptionData>
	{
		new MainMenuOptionData
		{
			Text = "New Game",
			Command = "Pick:New Game",
			Shortcut = UnityEngine.KeyCode.N
		},
		new MainMenuOptionData
		{
			Text = "Continue",
			Command = "Pick:Continue",
			Shortcut = UnityEngine.KeyCode.C
		},
		new MainMenuOptionData
		{
			Text = "Records",
			Command = "Pick:High Scores",
			Shortcut = UnityEngine.KeyCode.H
		},
		new MainMenuOptionData
		{
			Text = "Options",
			Command = "Pick:Options",
			Shortcut = UnityEngine.KeyCode.O
		},
		new MainMenuOptionData
		{
			Text = "Mods",
			Command = "Pick:Installed Mod Configuration",
			Alert = MainMenuOptionData.AlertMode.ModStatus
		}
	};

	public static List<MainMenuOptionData> RightOptions = new List<MainMenuOptionData>
	{
		new MainMenuOptionData
		{
			Text = "Redeem Code",
			Command = "Pick:Redeem Code"
		},
		new MainMenuOptionData
		{
			Text = "Modding Toolkit",
			Command = "Pick:Modding Utilities",
			Shortcut = UnityEngine.KeyCode.M
		},
		new MainMenuOptionData
		{
			Text = "Credits",
			Command = "Pick:Credits"
		},
		new MainMenuOptionData
		{
			Text = "Help",
			Command = "Pick:Help",
			Shortcut = UnityEngine.KeyCode.F1
		}
	};

	public static bool EnsureReselect = false;

	private bool SelectFirst = true;

	public bool FirstShow = true;

	public GameObject steamInputWarning;

	public static bool HasDromadDeluxeEntitlement = false;

	public RectTransform dromadDeluxeBadge;

	public bool wasInScroller;

	public void SetupContext()
	{
		globalContext.buttonHandlers = new Dictionary<InputButtonTypes, Action>();
		globalContext.buttonHandlers.Set(InputButtonTypes.CancelButton, XRL.UI.Framework.Event.Helpers.Handle(Quit));
		leftScroller.GetNavigationContext().axisHandlers[InputAxisTypes.NavigationXAxis] = delegate
		{
			rightScroller.GetNavigationContext().Activate();
		};
		rightScroller.GetNavigationContext().axisHandlers[InputAxisTypes.NavigationXAxis] = delegate
		{
			leftScroller.GetNavigationContext().Activate();
		};
	}

	public IEnumerator DoIntroTween(GameObject activeBackground)
	{
		float introTime = 0f;
		LeanTweenType EASE_TYPE = LeanTweenType.easeOutCubic;
		if (activeBackground != null)
		{
			activeBackground.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
			LeanTween.scale(activeBackground, new Vector3(1.01f, 1.01f, 1f), 6f).setEase(EASE_TYPE);
		}
		leftFader.alpha = 0f;
		logoFader.alpha = 0f;
		centerFlyoutGroup.padding.top = -290;
		centerTransform.localScale = new Vector3(0f, 1f, 1f);
		while (introTime < 1f)
		{
			introTime += Time.deltaTime;
			yield return null;
		}
		SoundManager.PlayUISound("Sounds/UI/ui_mainMenu_popUp", 1f, Combat: false, Interface: true);
		LeanTween.alpha(logoFader.gameObject, 1f, 2f).setEase(EASE_TYPE);
		LeanTween.alpha(leftFader.gameObject, 1f, 2f).setEase(EASE_TYPE);
		float flywideStart = 0f;
		while (centerTransform.localScale.x < 1f)
		{
			flywideStart += Time.deltaTime;
			centerTransform.localScale = new Vector3(Mathf.Lerp(0f, 1f, flywideStart * 5f), 1f, 1f);
			LayoutRebuilder.ForceRebuildLayoutImmediate(centerTransform);
			yield return null;
		}
		float flyoutStart = 0f;
		while (centerFlyoutGroup.padding.top < 0)
		{
			flyoutStart += Time.deltaTime;
			centerFlyoutGroup.padding.top = (int)Mathf.Lerp(-290f, 0f, Easing.SineEaseIn(flyoutStart));
			LayoutRebuilder.ForceRebuildLayoutImmediate(centerFlyoutGroup.gameObject.transform as RectTransform);
			yield return null;
		}
	}

	public override void Show()
	{
		TutorialManager.EndTutorial();
		versionText.SetText($"{XRLGame.MarketingVersion}{XRLGame.MarketingPostfix}\n{{{{K|build {XRLGame.CoreVersion}}}}}");
		base.Show();
		string option = Options.GetOption("OptionMainMenuBackground");
		GameObject gameObject = null;
		backgrounds[0].SetActive(option == "Modern");
		backgrounds[1].SetActive(option == "Classic");
		if (option == "Modern")
		{
			gameObject = backgrounds[0];
		}
		if (option == "Classic")
		{
			gameObject = backgrounds[1];
		}
		if (FirstShow)
		{
			FirstShow = false;
			StartCoroutine(DoIntroTween(gameObject));
		}
		else
		{
			gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			leftFader.alpha = 1f;
			logoFader.alpha = 1f;
			centerFlyoutGroup.padding.top = 0;
			centerTransform.localScale = new Vector3(1f, 1f, 1f);
		}
		LeftOptions[1].Enabled = SaveManagement.HasAnyGames();
		leftScroller.scrollContext.wraps = true;
		leftScroller.BeforeShow(null, LeftOptions);
		leftScroller.onSelected.RemoveAllListeners();
		leftScroller.onSelected.AddListener(SelectedInfo);
		leftScroller.scrollContext.parentContext = globalContext;
		rightScroller.scrollContext.wraps = true;
		rightScroller.BeforeShow(null, RightOptions);
		rightScroller.onSelected.RemoveAllListeners();
		rightScroller.onSelected.AddListener(SelectedInfo);
		rightScroller.scrollContext.parentContext = globalContext;
		if (SelectFirst)
		{
			SelectFirst = false;
			leftScroller.scrollContext.selectedPosition = 0;
			leftScroller.scrollContext.Activate();
		}
		SetupContext();
		EnableNavContext();
		UpdateMenuBars();
		TutorialManager.EndTutorial();
		if (File.Exists(DataManager.DLCPath("OSTEntitlement.txt")) && File.Exists(DataManager.DLCPath("PetPack1Entitlement.txt")))
		{
			HasDromadDeluxeEntitlement = true;
		}
	}

	public async void Reshow()
	{
		await The.UiContext;
		EnableNavContext();
		leftScroller.scrollContext.ActivateAndEnable();
	}

	public override void Hide()
	{
		base.Hide();
		DisableNavContext();
		base.gameObject.SetActive(value: false);
	}

	public void EnableNavContext()
	{
		globalContext.disabled = false;
		leftScroller.GetNavigationContext().ActivateAndEnable();
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
		leftScroller.GetNavigationContext().disabled = true;
	}

	public async void SelectedInfo(FrameworkDataElement data)
	{
		if (!(data is MainMenuOptionData mainMenuOptionData))
		{
			return;
		}
		if (mainMenuOptionData.Command == "Pick:Redeem Code")
		{
			try
			{
				string text = await Popup.AskStringAsync("Redeem a Code", "", 32);
				if (!string.IsNullOrEmpty(text))
				{
					CodeRedemptionManager.redeemNoProgress(text);
				}
				return;
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Code Redemption", x);
				return;
			}
		}
		if (mainMenuOptionData.Command == "Pick:Installed Mod Configuration")
		{
			try
			{
				await NavigationController.instance.SuspendContextWhile(SingletonWindowBase<ModManagerUI>.instance.ShowMenuAsync);
				return;
			}
			finally
			{
				UIManager.showWindow("MainMenu");
				Reshow();
			}
		}
		if (mainMenuOptionData.Command == "Pick:Modding Utilities")
		{
			try
			{
				await NavigationController.instance.SuspendContextWhile(SingletonWindowBase<ModToolkit>.instance.ShowMenuAsync);
				return;
			}
			finally
			{
				UIManager.showWindow("MainMenu");
			}
		}
		if (mainMenuOptionData.Command == "Pick:Achievements")
		{
			try
			{
				await NavigationController.instance.SuspendContextWhile(SingletonWindowBase<AchievementView>.instance.ShowMenuAsync);
				return;
			}
			finally
			{
				UIManager.showWindow("MainMenu");
			}
		}
		Keyboard.PushMouseEvent(mainMenuOptionData.Command);
	}

	public async Task<XRLGame> ContinueMenu()
	{
		SelectFirst = true;
		while (true)
		{
			completionSource?.TrySetCanceled();
			completionSource = new TaskCompletionSource<SaveGameInfo>();
			await The.UiContext;
			ControlManager.ResetInput();
			SelectFirst = true;
			Show();
			SaveGameInfo info = await completionSource.Task;
			DisableNavContext();
			await The.UiContext;
			if (info == null)
			{
				break;
			}
			try
			{
				if (info.json.SaveVersion < 395)
				{
					await Popup.ShowAsync("That save file looks like it's from an older save format revision (" + info.json.GameVersion + "). Sorry!\n\nYou can probably change to a previous branch in your game client and get it to load if you want to finish it off.");
				}
				else if (await info.TryRestoreModsAndLoadAsync())
				{
					Hide();
					return The.Game;
				}
			}
			catch (Exception ex)
			{
				MetricsManager.LogEditorError("Continue Menu", ex.ToString());
			}
		}
		Hide();
		return null;
	}

	public async void Quit()
	{
		if (await Popup.ShowYesNoAsync("Are you sure you want to quit?") == DialogResult.Yes)
		{
			GameManager.Instance.Quit();
		}
	}

	public void Exit()
	{
		MetricsManager.LogEditorInfo("Exiting continue screen");
		completionSource?.TrySetResult(null);
		ControlManager.ResetInput();
	}

	public async void HandleDelete()
	{
		if (NavigationController.instance.activeContext.IsInside(leftScroller.GetNavigationContext()))
		{
			SaveInfoData saveInfo = saves[leftScroller.selectedPosition];
			string title = "{{R|Delete " + saveInfo.SaveGame.Name + "}}";
			if ((await Popup.NewPopupMessageAsync("Are you sure you want to delete the save game for " + saveInfo.SaveGame.Name + "?", PopupMessage.AcceptCancelButton, null, title, null, 1)).command != "Cancel")
			{
				DisableNavContext();
				saveInfo.SaveGame.Delete();
				Show();
				await Popup.NewPopupMessageAsync("Game Deleted!", PopupMessage.AcceptButton);
			}
		}
	}

	public void UpdateMenuBars()
	{
		List<MenuOption> list = new List<MenuOption>();
		list.Add(new MenuOption
		{
			InputCommand = "NavigationXYAxis",
			Description = "navigate"
		});
		list.Add(new MenuOption
		{
			KeyDescription = ControlManager.getCommandInputDescription("Accept"),
			Description = "select"
		});
		list.Add(new MenuOption
		{
			KeyDescription = ControlManager.getCommandInputDescription("Cancel"),
			Description = "quit"
		});
		hotkeyBar.GetNavigationContext().disabled = true;
		hotkeyBar.BeforeShow(null, list);
	}

	public void Update()
	{
		if (HasDromadDeluxeEntitlement)
		{
			RectTransform obj = dromadDeluxeBadge;
			if (((object)obj == null || !obj.gameObject.activeSelf) && Options.GetOption("OptionShowCollectionBadges") == "Yes")
			{
				dromadDeluxeBadge.gameObject.SetActive(value: true);
				dromadDeluxeBadge.localScale = new Vector3(1f, 1f, 1f);
			}
		}
		RectTransform obj2 = dromadDeluxeBadge;
		if (((object)obj2 == null || !obj2.gameObject.activeSelf) && (!HasDromadDeluxeEntitlement || Options.GetOption("OptionShowCollectionBadges") != "Yes"))
		{
			dromadDeluxeBadge.gameObject.SetActive(value: false);
		}
		if (!Options.ModernUI)
		{
			Hide();
			return;
		}
		NavigationContext activeContext = NavigationController.instance.activeContext;
		bool num = (activeContext != null && activeContext.IsInside(leftScroller.GetNavigationContext())) || (NavigationController.instance.activeContext?.IsInside(rightScroller.GetNavigationContext()) ?? false);
		if (num != wasInScroller)
		{
			UpdateMenuBars();
		}
		if (num)
		{
			LeftOptions.ForEach(delegate(MainMenuOptionData o)
			{
				if (ControlManager.isHotkeyDown(o.Shortcut))
				{
					SelectedInfo(o);
				}
			});
			RightOptions.ForEach(delegate(MainMenuOptionData o)
			{
				if (ControlManager.isHotkeyDown(o.Shortcut))
				{
					SelectedInfo(o);
				}
			});
		}
		if ((bool)steamInputWarning && PlatformManager.Steam.Initialized && ControlManager.SuspectSteamInput && !steamInputWarning.activeInHierarchy)
		{
			steamInputWarning.SetActive(value: true);
		}
	}

	public void ControllerChanged()
	{
		UpdateMenuBars();
	}

	private void LateUpdate()
	{
		if (EnsureReselect)
		{
			EnsureReselect = false;
			EnableNavContext();
			leftScroller.scrollContext.ActivateAndEnable();
		}
	}
}
