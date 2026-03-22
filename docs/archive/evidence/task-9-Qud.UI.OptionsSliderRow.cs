using XRL.UI;

namespace Qud.UI;

public class OptionsSliderRow : OptionsDataRow<int>
{
	public int Min => Option.Min;

	public int Max => Option.Max;

	public int Increment => Option.Increment;

	public OptionsSliderRow(GameOption Option)
		: base(Option)
	{
		if (!int.TryParse(Options.GetOption(Option.ID), out var result) && !int.TryParse(Option.Default, out result))
		{
			result = Option.Min;
		}
		Initialize(result);
	}
}
