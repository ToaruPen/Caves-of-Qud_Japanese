using System.Collections.Generic;
using System.Threading.Tasks;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyCharGenBreadcrumb
{
    public string? Title { get; set; }
}

internal sealed class DummyCharGenMenuOption
{
    public string? Description { get; set; }

    public string? KeyDescription { get; set; }

    public string? InputCommand { get; set; }
}

internal sealed class DummyEmbarkBuilderModuleWindowDescriptor
{
    public string? title;
}

internal sealed class DummyCharGenModuleWindowTarget
{
    public string BreadcrumbTitle { get; set; } = string.Empty;

    public List<DummyCharGenMenuOption> MenuOptions { get; } = new List<DummyCharGenMenuOption>();

    public DummyCharGenBreadcrumb GetBreadcrumb()
    {
        return new DummyCharGenBreadcrumb { Title = BreadcrumbTitle };
    }

    public IEnumerable<DummyCharGenMenuOption> GetKeyMenuBar()
    {
        foreach (var option in MenuOptions)
        {
            yield return option;
        }
    }
}

internal sealed class DummyCharGenFrameworkScrollerTarget
{
    public string? LastTitle { get; private set; }

    public void BeforeShow(DummyEmbarkBuilderModuleWindowDescriptor? descriptor, IEnumerable<DummyFrameworkDataElement>? selections = null)
    {
        _ = selections;
        LastTitle = descriptor?.title;
    }
}

internal sealed class DummyCharGenCategoryMenuControllerTarget
{
    public string? LastTitle { get; private set; }

    public void setData(DummyFrameworkDataElement dataElement)
    {
        LastTitle = dataElement.Title;
    }
}

internal sealed class DummyCharGenCustomizePrefixMenuOption
{
    public string? Prefix { get; set; }

    public string? Description { get; set; }
}

internal sealed class DummyCharGenCustomizeWindowTarget
{
    public IEnumerable<DummyCharGenCustomizePrefixMenuOption> GetSelections()
    {
        yield return new DummyCharGenCustomizePrefixMenuOption
        {
            Prefix = "Gender: ",
            Description = "Male",
        };
        yield return new DummyCharGenCustomizePrefixMenuOption
        {
            Prefix = "Pronoun Set: ",
            Description = "<from gender>",
        };
        yield return new DummyCharGenCustomizePrefixMenuOption
        {
            Prefix = "Pet: ",
            Description = "<none>",
        };
    }

    public static async Task ShowNamePromptAsync()
    {
        await Task.Yield();
        DummyPopupTarget.ShowBlock("Enter name:");
    }

    public static async Task ShowChooseGenderAsync()
    {
        await Task.Yield();
        DummyPopupTarget.ShowOptionList(
            Title: "Choose Gender",
            Options: new List<string> { "<create new>" });
    }

    public static async Task ShowChoosePronounSetAsync()
    {
        await Task.Yield();
        DummyPopupTarget.ShowOptionList(
            Title: "Choose Pronoun Set",
            Options: new List<string> { "<from gender>", "<create new>" });
    }

    public static async Task ShowChoosePetAsync()
    {
        await Task.Yield();
        DummyPopupTarget.ShowOptionList(
            Title: "Choose Pet",
            Options: new List<string> { "<none>" });
    }
}
