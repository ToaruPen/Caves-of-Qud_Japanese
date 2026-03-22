using UnityEngine;

namespace XRL.UI.Framework;

public class MainMenuOptionData : FrameworkDataElement
{
	public enum AlertMode
	{
		None,
		ModStatus
	}

	public bool Enabled = true;

	public string Text;

	public string Command;

	public AlertMode Alert;

	public KeyCode Shortcut;
}
