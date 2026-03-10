using System;
using System.Collections.Generic;
using XRL.UI.Framework;

namespace Qud.UI;

[Serializable]
public abstract class OptionsDataRow : FrameworkDataElement
{
	public Func<bool> IsEnabled;

	public string CategoryId;

	public string Title;

	public string SearchWords;

	public string HelpText;

	protected HashSet<object> _observersSeen = new HashSet<object>();

	public bool ValueChangedSinceLastObserved(object obj)
	{
		if (_observersSeen.Contains(obj))
		{
			return false;
		}
		_observersSeen.Add(obj);
		return true;
	}

	protected void OnChange()
	{
		_observersSeen.Clear();
	}
}
