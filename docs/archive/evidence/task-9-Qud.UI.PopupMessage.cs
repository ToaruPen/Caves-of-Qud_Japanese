using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleLib.Console;
using Genkit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRL;
using XRL.UI;
using XRL.UI.Framework;

namespace Qud.UI;

[UIView("DynamicPopupMessage", false, false, false, "Conversation", null, false, 0, false, IgnoreForceFullscreen = true, OverlayMode = 1)]
[UIView("PopupMessage", false, false, false, "Conversation", "PopupMessage", false, 0, false, IgnoreForceFullscreen = true, UICanvasHost = 1, OverlayMode = 1)]
public class PopupMessage : WindowBase
{
	public UITextSkin Message;

	public Action<QudMenuItem> commandCallback;

	public Action<QudMenuItem> selectCallback;

	public QudTextMenuController controller;

	public ControlledTMPInputField inputBox;

	public IRenderableDisplayManager contextImage;

	public UIThreeColorProperties afterImage;

	public UITextSkin contextText;

	public GameObject contextFrame;

	public GameObject contextContainer;

	public GameObject TitleContainer;

	public UITextSkin Title;

	public static List<QudMenuItem> AnyKey = new List<QudMenuItem>();

	public static List<QudMenuItem> _CancelButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		}
	};

	public static List<QudMenuItem> _CopyButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|[C]}} {{y|Copy}}",
			command = "Copy",
			hotkey = "char:c"
		},
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		}
	};

	public static readonly QudMenuItem LookButton = new QudMenuItem
	{
		text = "{{W|[L]}} {{y|Look}}",
		command = "Look",
		hotkey = "char:l"
	};

	public static List<QudMenuItem> _SingleButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|[space]}} {{y|Continue}}",
			command = "Accept",
			hotkey = "Accept,Cancel"
		}
	};

	public static List<QudMenuItem> _YesNoButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|[y]}} {{y|Yes}}",
			command = "Yes",
			hotkey = "Y,Accept"
		},
		new QudMenuItem
		{
			text = "{{W|[n]}} {{y|No}}",
			command = "No",
			hotkey = "N,Cancel"
		}
	};

	public static List<QudMenuItem> _YesNoButtonJoystick = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{y|Yes}}",
			command = "Yes",
			hotkey = "Y,Accept"
		},
		new QudMenuItem
		{
			text = "{{y|No}}",
			command = "No",
			hotkey = "N,Cancel"
		}
	};

	public static List<QudMenuItem> _YesNoCancelButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|[y]}} {{y|Yes}}",
			command = "Yes",
			hotkey = "Y,Accept"
		},
		new QudMenuItem
		{
			text = "{{W|[n]}} {{y|No}}",
			command = "No",
			hotkey = "N,V Negative"
		},
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		}
	};

	public static List<QudMenuItem> _AcceptCancelButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{y|Accept}}",
			command = "keep",
			hotkey = "Accept"
		},
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		}
	};

	public static List<QudMenuItem> _AcceptCancelColorButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{y|Submit}}",
			command = "keep",
			hotkey = "Submit"
		},
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		},
		new QudMenuItem
		{
			text = "{{W|[F1]}} {{y|Color}}",
			command = "Color",
			hotkey = "CmdHelp"
		}
	};

	public static List<QudMenuItem> _SubmitCancelButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{y|Submit}}",
			command = "keep",
			hotkey = "Submit"
		},
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		}
	};

	public static List<QudMenuItem> _SubmitCancelColorButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{y|Submit}}",
			command = "keep",
			hotkey = "Submit"
		},
		new QudMenuItem
		{
			text = "{{W|[Esc]}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		},
		new QudMenuItem
		{
			text = "{{W|[F1]}} {{y|Color}}",
			command = "Color",
			hotkey = "CmdHelp"
		}
	};

	public static List<QudMenuItem> AcceptButton = new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|[Enter]}} {{y|Accept}}",
			command = "keep",
			hotkey = "Accept"
		}
	};

	public QudMenuBottomContext bottomContextController;

	public Action onHide;

	public CancellationToken cancellationToken;

	public bool wasPushed;

	public string WantsSpecificPrompt;

	public string RestrictChars;

	public VerticalLayoutGroup centringLayoutGroup;

	public RectTransform popupPositionTransform;

	public static Location2D LOCATION_AT_MOUSE_CURSOR = new Location2D(-9999, -9999);

	public string PopupID;

	public static string lastPopupID;

	public float holdTime;

	private int HideNextFrame;

	public static List<QudMenuItem> CancelButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} Cancel",
						command = "Cancel",
						hotkey = "Cancel"
					}
				};
			}
			return _CancelButton;
		}
	}

	public static List<QudMenuItem> CopyButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{y|Copy}}",
						command = "Copy",
						hotkey = "char:c"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
						command = "Cancel",
						hotkey = "Cancel"
					}
				};
			}
			return _CopyButton;
		}
	}

	public static List<QudMenuItem> SingleButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{W|press " + ControlManager.getCommandInputDescription("Accept", Options.ModernUI) + "}}",
						command = "Accept",
						hotkey = "Accept,Cancel"
					}
				};
			}
			return _SingleButton;
		}
	}

	public static List<QudMenuItem> YesNoButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return _YesNoButtonJoystick;
			}
			return _YesNoButton;
		}
	}

	public static List<QudMenuItem> YesNoCancelButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = ControlManager.getCommandInputFormatted("Accept") + " {{y|Yes}}",
						command = "Yes",
						hotkey = "Y,Accept"
					},
					new QudMenuItem
					{
						text = ControlManager.getCommandInputFormatted("V Negative") + " {{y|No}}",
						command = "No",
						hotkey = "N,V Negative"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + " Cancel}}",
						command = "Cancel",
						hotkey = "Cancel"
					}
				};
			}
			return _YesNoCancelButton;
		}
	}

	public static List<QudMenuItem> AcceptCancelButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Accept", Options.ModernUI) + "}} {{y|Accept}}",
						command = "keep",
						hotkey = "Accept"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
						command = "Cancel",
						hotkey = "Cancel"
					}
				};
			}
			return _AcceptCancelButton;
		}
	}

	public static List<QudMenuItem> AcceptCancelColorButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Submit", Options.ModernUI) + "}} {{y|Submit}}",
						command = "keep",
						hotkey = "Submit"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
						command = "Cancel",
						hotkey = "Cancel"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Help", Options.ModernUI) + "}} {{y|Color}}",
						command = "Color",
						hotkey = "CmdHelp"
					}
				};
			}
			return _AcceptCancelColorButton;
		}
	}

	public static List<QudMenuItem> SubmitCancelButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Submit", Options.ModernUI) + "}} {{y|Submit}}",
						command = "keep",
						hotkey = "Submit"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
						command = "Cancel",
						hotkey = "Cancel"
					}
				};
			}
			return _SubmitCancelButton;
		}
	}

	public static List<QudMenuItem> SubmitCancelHoldButton => new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "{{W|" + ControlManager.getCommandInputDescription("Submit", Options.ModernUI) + "}} {{y|Submit}}",
			command = "keep",
			hotkey = "Submit"
		},
		new QudMenuItem
		{
			text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		},
		new QudMenuItem
		{
			text = "{{W|" + ControlManager.getCommandInputDescription("CmdOptions", Options.ModernUI) + "}} {{y|Hold to Accept}}",
			command = "CmdOptions",
			hotkey = "CmdOptions"
		}
	};

	public static List<QudMenuItem> SubmitCancelColorButton
	{
		get
		{
			if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
			{
				return new List<QudMenuItem>
				{
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Submit", Options.ModernUI) + "}} {{y|Submit}}",
						command = "keep",
						hotkey = "Submit"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
						command = "Cancel",
						hotkey = "Cancel"
					},
					new QudMenuItem
					{
						text = "{{W|" + ControlManager.getCommandInputDescription("CmdSystem1", Options.ModernUI) + "}} {{y|Color}}",
						command = "Color",
						hotkey = "CmdHelp"
					}
				};
			}
			return _AcceptCancelColorButton;
		}
	}

	public static List<QudMenuItem> AcceptCancelTradeButton => new List<QudMenuItem>
	{
		new QudMenuItem
		{
			text = "[{{W|" + ControlManager.getCommandInputDescription("CmdStartTrade", Options.ModernUI) + "}}] {{y|Trade}}",
			command = "trade",
			hotkey = "CmdStartTrade"
		},
		new QudMenuItem
		{
			text = "{{W|" + ControlManager.getCommandInputDescription("Accept", Options.ModernUI) + "}} {{y|Accept}}",
			command = "keep",
			hotkey = "Accept"
		},
		new QudMenuItem
		{
			text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", Options.ModernUI) + "}} {{y|Cancel}}",
			command = "Cancel",
			hotkey = "Cancel"
		}
	};

	public override void Init()
	{
		controller.isCurrentWindow = base.isCurrentWindow;
		base.Init();
	}

	public void ShowPopup(string message, List<QudMenuItem> buttons, Action<QudMenuItem> commandCallback = null, List<QudMenuItem> items = null, Action<QudMenuItem> selectedItemCallback = null, string title = null, bool includeInput = false, string inputDefault = null, int DefaultSelected = 0, Action onHide = null, IRenderable contextRender = null, string contextTitle = null, IRenderable afterRender = null, bool showContextFrame = true, bool pushView = true, CancellationToken cancelToken = default(CancellationToken), bool askingNumber = false, string RestrictChars = "", string WantsSpecificPrompt = null, Location2D PopupLocation = null, string PopupID = null)
	{
		HideNextFrame = 0;
		if (this.onHide != null)
		{
			try
			{
				MetricsManager.LogError("ShowPopup::OnHide wasn't called! Calling it now");
				this.onHide();
				this.onHide = null;
			}
			catch (Exception x)
			{
				MetricsManager.LogException("PopupMessage::ShowPopup onHide sanity check", x);
			}
		}
		if (PopupLocation == null)
		{
			centringLayoutGroup.enabled = true;
			popupPositionTransform.pivot = new Vector2(0.5f, 0.5f);
		}
		else
		{
			centringLayoutGroup.enabled = false;
			Vector3 vector = ((!(PopupLocation == LOCATION_AT_MOUSE_CURSOR)) ? GameManager.Instance.getScreenTileCenter(PopupLocation.X, PopupLocation.Y) : Input.mousePosition);
			popupPositionTransform.pivot = new Vector2(0f, 0f);
			if (vector.x < (float)(Screen.width / 2))
			{
				if (vector.y < (float)(Screen.height / 2))
				{
					popupPositionTransform.pivot = new Vector2(0f, 0f);
				}
				else
				{
					popupPositionTransform.pivot = new Vector2(0f, 1f);
				}
			}
			else if (vector.y < (float)(Screen.height / 2))
			{
				popupPositionTransform.pivot = new Vector2(1f, 0f);
			}
			else
			{
				popupPositionTransform.pivot = new Vector2(1f, 1f);
			}
			popupPositionTransform.position = new Vector3(vector.x, vector.y, 0f);
			_ = popupPositionTransform.parent;
			ClampPopupPosition();
		}
		EnforceSuspensionContext();
		this.PopupID = PopupID;
		cancellationToken = cancelToken;
		this.onHide = onHide;
		base.canvasGroup.alpha = 0f;
		base.transform.SetAsLastSibling();
		base.gameObject.SetActive(value: true);
		Message.SetText("{{y|" + message + "}}");
		Message.GetComponent<LayoutElement>().minWidth = 0f;
		this.commandCallback = commandCallback;
		selectCallback = selectedItemCallback;
		controller.menuData = items ?? new List<QudMenuItem>();
		controller.bottomContextOptions = buttons;
		this.WantsSpecificPrompt = WantsSpecificPrompt;
		this.RestrictChars = RestrictChars;
		if (this.RestrictChars == "0123456789")
		{
			inputBox.contentType = TMP_InputField.ContentType.IntegerNumber;
		}
		else if (this.RestrictChars == "0123456789-")
		{
			inputBox.contentType = TMP_InputField.ContentType.IntegerNumber;
		}
		else
		{
			inputBox.contentType = TMP_InputField.ContentType.Standard;
		}
		if (string.IsNullOrEmpty(title))
		{
			TitleContainer.SetActive(value: false);
			Title.SetText("");
		}
		else
		{
			TitleContainer.SetActive(value: true);
			Title.SetText("{{W|" + title + "}}");
		}
		if (includeInput)
		{
			inputBox.gameObject.SetActive(value: true);
			if (!controller.inputFields.Contains(inputBox))
			{
				controller.inputFields.Add(inputBox);
			}
			inputBox.text = inputDefault ?? "";
			inputBox.onSubmit.RemoveAllListeners();
			inputBox.onSubmit.AddListener(OnInputSubmit);
			CapabilityManager.SuggestOnscreenKeyboard();
		}
		else
		{
			inputBox.gameObject.SetActive(value: false);
			if (controller.inputFields.Contains(inputBox))
			{
				controller.inputFields.Remove(inputBox);
			}
		}
		if (contextTitle != null || contextRender != null || afterRender != null)
		{
			contextContainer.SetActive(value: true);
			if (contextRender != null)
			{
				contextImage.gameObject.SetActive(value: true);
				contextImage.FromRenderable(contextRender);
			}
			else
			{
				contextImage.gameObject.SetActive(value: false);
			}
			if (afterRender != null)
			{
				afterImage.gameObject.SetActive(value: true);
				afterImage.FromRenderable(afterRender);
				if (afterImage.Background == The.Color.DarkBlack)
				{
					afterImage.Background = Color.clear;
				}
			}
			else
			{
				afterImage.gameObject.SetActive(value: false);
			}
			if (!string.IsNullOrEmpty(contextTitle))
			{
				contextText.gameObject.SetActive(value: true);
				contextText.SetText(contextTitle);
			}
			else
			{
				contextText.gameObject.SetActive(value: false);
			}
		}
		else
		{
			contextContainer.SetActive(value: false);
		}
		contextFrame.SetActive(showContextFrame);
		controller.UpdateElements(evenIfNotCurrent: true);
		controller.UpdateButtonLayout();
		bottomContextController?.RefreshButtons();
		Show();
		controller.Reselect(DefaultSelected);
		controller.Update();
		SizeContentToWidth();
		lastPopupID = PopupID;
	}

	public void SizeContentToWidth()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
		int num = 10;
		while (controller.GetComponent<ContentSizeWithMax>().MaximumPreferredSize.y != base.rectTransform.rect.height - 100f || controller.GetComponent<ContentSizeWithMax>().MaximumPreferredSize.x != 840f)
		{
			controller.GetComponent<ContentSizeWithMax>().MaximumPreferredSize = new Vector2(840f, base.rectTransform.rect.height - 100f);
			Canvas.ForceUpdateCanvases();
			num--;
			if (num == 0)
			{
				break;
			}
		}
		if (Message.text != "{{y|}}")
		{
			float num2 = controller.GetComponent<RectTransform>().rect.width - 80f;
			if (Message.preferredWidth < num2)
			{
				Message.GetComponent<LayoutElement>().minWidth = num2;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
		}
		base.canvasGroup.alpha = 1f;
		ClampPopupPosition();
	}

	public void ClampPopupPosition()
	{
		RectTransform rectTransform = popupPositionTransform;
		RectTransform rectTransform2 = popupPositionTransform.parent as RectTransform;
		if (rectTransform.pivot.y == 1f)
		{
			float num = 0f - rectTransform.anchoredPosition.y + rectTransform.rect.height;
			if (num > rectTransform2.rect.height)
			{
				rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + (num - rectTransform2.rect.height));
			}
		}
		else if (rectTransform.pivot.y == 0f)
		{
			float num2 = 0f - rectTransform.anchoredPosition.y - rectTransform.rect.height;
			if (num2 < 0f)
			{
				rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + num2);
			}
		}
	}

	public void OnActivateCommand(QudMenuItem command)
	{
		if (TutorialManager.AllowSelectedPopupCommand(PopupID, command) && !(command.command == "CmdOptions") && commandCallback != null)
		{
			commandCallback(command);
			commandCallback = null;
			Hide();
		}
	}

	public void OnSelect(QudMenuItem command)
	{
		if (TutorialManager.AllowSelectedPopupCommand(PopupID, command) && selectCallback != null)
		{
			selectCallback(command);
			selectCallback = null;
			Hide();
		}
	}

	public void OnInputSubmit(string value)
	{
		foreach (QudMenuItem bottomContextOption in controller.bottomContextOptions)
		{
			if (bottomContextOption.hotkey == "Submit" || bottomContextOption.hotkey == "Accept")
			{
				ControlManager.WaitForKeyup("Submit");
				ControlManager.WaitForKeyup("Accept");
				OnSelect(bottomContextOption);
				break;
			}
		}
	}

	public void EnforceSuspensionContext()
	{
		if (NavigationController.instance.activeContext != null && NavigationController.instance.activeContext != NavigationController.instance.suspensionContext)
		{
			MetricsManager.LogEditorWarning("In a popup but the activeContext isn't the suspension context! It was " + NavigationController.instance.activeContext.ToString());
			NavigationController.instance.activeContext = NavigationController.instance.suspensionContext;
		}
	}

	public void Update()
	{
		if (HideNextFrame > 0)
		{
			HideNextFrame--;
			if (HideNextFrame <= 0)
			{
				HideNextFrame = 0;
				base.canvasGroup.alpha = 0f;
				if (base.canvas.enabled)
				{
					base.canvas.enabled = false;
				}
				base.gameObject.SetActive(value: false);
			}
			return;
		}
		if (base.isActiveAndEnabled && cancellationToken != default(CancellationToken) && cancellationToken.IsCancellationRequested)
		{
			commandCallback(default(QudMenuItem));
			selectCallback = null;
			Hide();
			return;
		}
		if (!string.IsNullOrEmpty(RestrictChars))
		{
			for (int i = 0; i < inputBox.text.Length; i++)
			{
				if (!RestrictChars.Contains(inputBox.text[i]))
				{
					inputBox.text = inputBox.text.Replace($"{inputBox.text[i]}", "");
				}
			}
		}
		if ((inputBox.contentType == TMP_InputField.ContentType.IntegerNumber || inputBox.contentType == TMP_InputField.ContentType.DecimalNumber) && Input.GetKeyDown(UnityEngine.KeyCode.Space))
		{
			OnInputSubmit(inputBox.text);
			return;
		}
		if (WantsSpecificPrompt != null && base.isActiveAndEnabled)
		{
			if (!ControlManager.isCommandPressed("CmdOptions"))
			{
				if (holdTime > 0f)
				{
					inputBox.text = "";
				}
				holdTime = 0f;
				if (bottomContextController.buttons.Count > 0)
				{
					bottomContextController.buttons.Last().progressBar.gameObject.SetActive(value: false);
				}
			}
			else
			{
				holdTime += Time.deltaTime;
				if (holdTime >= 2f)
				{
					inputBox.text = WantsSpecificPrompt;
					OnInputSubmit(WantsSpecificPrompt);
					return;
				}
				SelectableTextMenuItem selectableTextMenuItem = bottomContextController.buttons.Last();
				RectTransform component = bottomContextController.buttons.Last().GetComponent<RectTransform>();
				selectableTextMenuItem.progressBar.gameObject.SetActive(value: true);
				selectableTextMenuItem.progressBar.sizeDelta = new Vector2(component.rect.width * (holdTime / 2f), component.rect.height);
				inputBox.text = WantsSpecificPrompt.Substring(0, (int)Math.Min(WantsSpecificPrompt.Length, (float)WantsSpecificPrompt.Length * (holdTime / 2f) + 1f));
			}
		}
		EnforceSuspensionContext();
		if (cancellationToken == default(CancellationToken) && controller.bottomContextOptions.Count == 0 && controller.menuData.Count == 0 && Input.anyKeyDown)
		{
			commandCallback(default(QudMenuItem));
			selectCallback = null;
			Hide();
		}
		foreach (QudMenuItem menuDatum in controller.menuData)
		{
			if (menuDatum.hotkey == null || !menuDatum.hotkey.StartsWith("char:"))
			{
				continue;
			}
			char c = ((menuDatum.hotkey.Length > 5) ? menuDatum.hotkey[5] : '\0');
			if (c != 0 && c != ' ' && (!inputBox.IsSelected() || c != '\b') && ControlManager.isCharDown(c))
			{
				if (commandCallback != null && TutorialManager.AllowSelectedPopupCommand(PopupID, menuDatum))
				{
					commandCallback(menuDatum);
					commandCallback = null;
					Hide();
				}
				return;
			}
		}
		ClampPopupPosition();
	}

	public void BackgroundClicked()
	{
		QudMenuItem command = bottomContextController.items.Where((QudMenuItem i) => i.hotkey == "Cancel" || i.hotkey == "Accept,Cancel").FirstOrDefault();
		if (command.hotkey == "Cancel" || command.hotkey == "Accept,Cancel")
		{
			OnActivateCommand(command);
		}
	}

	public override void Show()
	{
		HideNextFrame = 0;
		EnforceSuspensionContext();
		base.Show();
	}

	public override void Hide()
	{
		HideNextFrame = 2;
		if (wasPushed)
		{
			wasPushed = false;
		}
		if (base.raycaster != null && base.raycaster.enabled)
		{
			base.raycaster.enabled = false;
		}
		if (base.canvasGroup != null && base.canvasGroup.interactable)
		{
			base.canvasGroup.interactable = false;
		}
		if (onHide != null)
		{
			Action action = onHide;
			onHide = null;
			action();
		}
	}
}
