using XRL.UI;

namespace Qud.UI;

public class OptionsCheckboxRow : OptionsDataRow<bool>
{
	public OptionsCheckboxRow(GameOption Option)
		: base(Option)
	{
		Initialize(Options.GetOption(Option.ID) == "Yes");
	}
}
