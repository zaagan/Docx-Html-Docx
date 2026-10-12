using Xunit;

// The converters drive OpenXmlPowerTools, HtmlToOpenXml and System.Drawing, all of
// which keep process-wide state; running the collections one at a time keeps results
// reproducible.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
