using System;
using XRL.UI;

namespace Qud.UI;

public abstract class OptionsDataRow<T> : OptionsDataRow where T : IEquatable<T>
{
	public readonly GameOption Option;

	protected T _Value;

	protected T Initial;

	public T Value
	{
		get
		{
			return _Value;
		}
		set
		{
			ref T value2 = ref _Value;
			T other = value;
			if (value2.Equals(other))
			{
				return;
			}
			_Value = value;
			OnChange();
			if (Option.Restart)
			{
				if (value.Equals(Initial))
				{
					OptionsUI.RestartOptions.Remove(Option);
				}
				else
				{
					OptionsUI.RestartOptions.Add(Option);
				}
			}
		}
	}

	public OptionsDataRow(GameOption Option)
	{
		Id = Option.ID;
		this.Option = Option;
		Title = Option.DisplayText;
		CategoryId = Option.Category;
		SearchWords = Option.SearchKeywords + " " + Option.DisplayText + " " + Option.Category;
		IsEnabled = () => Option.Requires?.RequirementsMet ?? true;
		HelpText = Option.HelpText;
	}

	protected void Initialize(T Value)
	{
		_Value = Value;
		Initial = Value;
	}
}
