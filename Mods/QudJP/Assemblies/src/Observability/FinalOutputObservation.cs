namespace QudJP;

internal sealed class FinalOutputObservation
{
    internal FinalOutputObservation(
        string sink,
        string route,
        string? detail,
        string phase,
        string translationStatus,
        string markupStatus,
        string directMarkerStatus,
        string? sourceText,
        string? strippedText,
        string? translatedText,
        string finalText)
    {
        Sink = sink;
        Route = route;
        Detail = detail;
        Phase = phase;
        TranslationStatus = translationStatus;
        MarkupStatus = markupStatus;
        DirectMarkerStatus = directMarkerStatus;
        SourceText = sourceText;
        StrippedText = strippedText;
        TranslatedText = translatedText;
        FinalText = finalText;
    }

    internal string Sink { get; }

    internal string Route { get; }

    internal string? Detail { get; }

    internal string Phase { get; }

    internal string TranslationStatus { get; }

    internal string MarkupStatus { get; }

    internal string DirectMarkerStatus { get; }

    internal string? SourceText { get; }

    internal string? StrippedText { get; }

    internal string? TranslatedText { get; }

    internal string FinalText { get; }
}
