using System;
using System.Collections;
using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyBookTarget
{
    public string Title { get; set; } = string.Empty;
}

internal static class BookUI
{
    public static IDictionary Books = new Dictionary<string, object>(StringComparer.Ordinal);

    public static void Reset()
    {
        Books = new Dictionary<string, object>(StringComparer.Ordinal);
    }
}

internal sealed class DummyBookLineDataTarget
{
    public string text { get; set; } = string.Empty;
}

internal sealed class DummyFallbackBookLineDataTarget
{
    public string FallbackText { get; set; } = "book line fallback";
}

internal sealed class DummyBookLineTarget
{
    public bool OriginalExecuted { get; private set; }

    public DummyStatusContext context = new DummyStatusContext();

    public DummyUITextSkin text = new DummyUITextSkin();

    public void setData(object data)
    {
        OriginalExecuted = true;
        context.data = data;
        if (data is DummyFallbackBookLineDataTarget fallback)
        {
            text.SetText(fallback.FallbackText);
            return;
        }

        if (data is DummyBookLineDataTarget lineData)
        {
            text.SetText(lineData.text);
        }
    }
}

internal sealed class DummyBookScreenTarget
{
    public static DummyMenuOption PREV_PAGE = new DummyMenuOption("Previous Page", "UI:Navigate/left", "Previous Page");

    public static DummyMenuOption NEXT_PAGE = new DummyMenuOption("Next Page", "UI:Navigate/right", "Next Page");

    public static List<DummyMenuOption> getItemMenuOptions = new List<DummyMenuOption>
    {
        PREV_PAGE,
        NEXT_PAGE,
        new DummyMenuOption("Close book", "Cancel"),
    };

    public DummyUITextSkin titleText = new DummyUITextSkin();

    public bool OriginalExecuted { get; private set; }

    public static void ResetStaticMenuOptions()
    {
        PREV_PAGE = new DummyMenuOption("Previous Page", "UI:Navigate/left", "Previous Page");
        NEXT_PAGE = new DummyMenuOption("Next Page", "UI:Navigate/right", "Next Page");
        getItemMenuOptions = new List<DummyMenuOption>
        {
            PREV_PAGE,
            NEXT_PAGE,
            new DummyMenuOption("Close book", "Cancel"),
        };
    }

    public void showScreen(DummyBookTarget book, string sound = "book.wav", Action<int>? onShowPage = null, Action<int>? afterShowPage = null)
    {
        _ = sound;
        _ = onShowPage;
        _ = afterShowPage;
        OriginalExecuted = true;
        titleText.SetText(book.Title);
    }

    public void showScreen(string bookId, string sound = "book.wav", Action<int>? onShowPage = null, Action<int>? afterShowPage = null)
    {
        _ = sound;
        _ = onShowPage;
        _ = afterShowPage;
        OriginalExecuted = true;
        if (BookUI.Books.Contains(bookId) && BookUI.Books[bookId] is DummyBookTarget book)
        {
            titleText.SetText(book.Title);
        }
    }
}
