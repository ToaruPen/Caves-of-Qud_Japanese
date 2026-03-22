using TMPro;
using UnityEngine;

namespace XRL.UI;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextSkin : MonoBehaviour
{
	public enum Size
	{
		normal,
		header,
		subscript,
		topstatusbar,
		bottomstatusbar,
		xsmall,
		small,
		unset
	}

	public enum AdjustmentFactor
	{
		none,
		messageLog
	}

	public enum TextWrappingModeType
	{
		LegacyPaddingUpgradeMode
	}

	private RectTransform _rt;

	private TextMeshProUGUI _tmp;

	public Size style;

	public AdjustmentFactor adjustmentFactor;

	/// <summary>
	/// Call Apply() after setting this.
	/// </summary>
	[Multiline]
	public string text = "";

	public bool useBlockWrap = true;

	public int blockWrap = 72;

	protected string formattedText;

	public bool bold;

	public Color color = new Color(0.69f, 0.78f, 0.76f);

	public TextWrappingModeType textWrappingMode;

	protected bool _StripFormatting;

	private bool autoSizeSet;

	private string lasttext;

	public RectTransform rectTransform => _rt ?? (_rt = GetComponent<RectTransform>());

	private TextMeshProUGUI tmp
	{
		get
		{
			if (_tmp == null)
			{
				_tmp = GetComponent<TextMeshProUGUI>();
			}
			return _tmp;
		}
	}

	public float preferredHeight => Updated().preferredHeight;

	public float preferredWidth => Updated().preferredWidth;

	public bool StripFormatting
	{
		get
		{
			return _StripFormatting;
		}
		set
		{
			_StripFormatting = value;
			formattedText = null;
		}
	}

	private TextMeshProUGUI Updated()
	{
		Apply();
		return tmp;
	}

	public Vector2 GetPreferredValues(float MaxWidth, float MaxHeight)
	{
		return Updated().GetPreferredValues(MaxWidth, MaxHeight);
	}

	public Vector2 GetPreferredValues(string Text, float MaxWidth, float MaxHeight)
	{
		return Updated().GetPreferredValues(Text, MaxWidth, MaxHeight);
	}

	/// <summary>
	/// shortcut to set .<paramref name="text" /> then call .Apply()
	/// </summary>
	/// <param name="text" />
	public bool SetText(string text)
	{
		if (this.text == text)
		{
			return false;
		}
		this.text = text;
		formattedText = null;
		Apply();
		return true;
	}

	public void Apply()
	{
		if (!autoSizeSet)
		{
			autoSizeSet = true;
			tmp.vertexBufferAutoSizeReduction = false;
		}
		int num = 16;
		if (style == Size.xsmall)
		{
			num = 12;
		}
		if (style == Size.small)
		{
			num = 14;
		}
		if (style == Size.normal)
		{
			num = 16;
		}
		if (style == Size.header)
		{
			num = 24;
		}
		if (style == Size.subscript)
		{
			num = 8;
		}
		if (style == Size.topstatusbar)
		{
			num = 16;
		}
		if (style == Size.bottomstatusbar)
		{
			num = 14;
		}
		if (adjustmentFactor == AdjustmentFactor.messageLog)
		{
			num += Options.MessageLogLineSizeAdjustment;
		}
		if (formattedText == null || lasttext != text)
		{
			lasttext = text;
			formattedText = text.ToRTFCached(useBlockWrap ? blockWrap : (-1), StripFormatting);
		}
		if (tmp.text != formattedText)
		{
			tmp.text = formattedText;
		}
		if (style != Size.unset && tmp.fontSize != (float)num)
		{
			tmp.fontSize = num;
		}
		if (tmp.text != formattedText)
		{
			tmp.text = formattedText;
		}
		if (tmp.color != color)
		{
			tmp.color = color;
		}
		if (bold)
		{
			if ((tmp.fontStyle & FontStyles.Bold) == 0)
			{
				tmp.fontStyle += 1;
			}
		}
		else if ((tmp.fontStyle & FontStyles.Bold) != FontStyles.Normal)
		{
			tmp.fontStyle -= 1;
		}
		if (textWrappingMode == TextWrappingModeType.LegacyPaddingUpgradeMode && tmp.textWrappingMode == TextWrappingModes.Normal)
		{
			tmp.textWrappingMode = TextWrappingModes.PreserveWhitespace;
		}
	}

	public void Start()
	{
		Apply();
	}
}
