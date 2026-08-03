using System.Reflection;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(DevKit.Screenshot.Avalonia.Sharp.Tests.HeadlessTestApp))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]
