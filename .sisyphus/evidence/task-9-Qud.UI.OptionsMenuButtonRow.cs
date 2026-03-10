using System.Reflection;
using XRL.UI;

namespace Qud.UI;

public class OptionsMenuButtonRow : OptionsDataRow<bool>
{
	public MethodInfo OnClick;

	public OptionsMenuButtonRow(GameOption Option)
		: base(Option)
	{
		OnClick = Option.OnClick;
	}
}
