# DCCS.LocalizedString.NetStandard [![Build status](https://ci.appveyor.com/api/projects/status/nbenos1nau71747v?svg=true)](https://ci.appveyor.com/project/mgeramb/dccs-localizedstring-netstandard) [![NuGet Badge](https://buildstats.info/nuget/DCCS.LocalizedString.NetStandard)](https://www.nuget.org/packages/DCCS.LocalizedString.NetStandard/)
DCCS.LocalizedString.NetStandard provide a multilanguage string implementation and multilangue exceptions.

The accessor keys for the strings have to be defined as static objects which can be found by reflection. 
There exist furthermore a tools which can create resource files in C# projects for the required keys.

## Installation

Install [DCCS.LocalizedString.NetStandard](https://www.nuget.org/packages/DCCS.LocalizedString.NetStandard/) with NuGet:

    Install-Package DCCS.LocalizedString.NetStandard

Or via the .NET Core command line interface:

    dotnet add package DCCS.LocalizedString.NetStandard

Either commands, from Package Manager Console or .NET Core CLI, will download and install DCCS.LocalizedString.NetStandard and all required dependencies.

## Resource Creator Tool

This tools create resource file by using reflection out of your compiled .NET assembly

Install the tool in the package managment console, command prompt or powershell

    dotnet tool install --global DCCS.LocalizedString.ProjectResourceCreator

To run the tool 

    dotnet tool install --global DCCS.LocalizedString.ProjectResourceCreator

## Examples

Simple console application sample:
```csharp
using System;
using System.Diagnostics;
using System.Globalization;
using DCCS.LocalizedString.NetStandard;

class MyClass
{
    // Declaration of the key - Must be static in none generic classes
    static readonly LocalizedStringKey ThisIsASample = new LocalizedStringKey("This is a sample application");

    // If you want use runtime parameters, use the LocalizedFormatKey and specifiy the name of the provided parameters
    static readonly LocalizedFormatKey ThisIsASampleError = new LocalizedFormatKey("This is a sample error thrown at {0}", "Time Stamp");

    void Main()
    {
        ITranslationService translationService = new TranslationService(new ITranslationProviderService[] { new ResourceTranslationProvider() });
        ILocalizedString localizedString = translationService.Create(ThisIsASample);

        // Write out the message in the current UI culture:
        Console.WriteLine(localizedString.GetText(CultureInfo.CurrentUICulture));

        // Write out the message in the current UI culture:
        Console.WriteLine(localizedString.GetText(CultureInfo.CurrentUICulture));

        // Write out the message in english
        Trace.WriteLine(localizedString.GetText(CultureInfo.InvariantCulture));

        try
        {
            // To use runtimeparameters, specify a LocalizedFormatKey and a matching number of runtime parameters
            ILocalizedString localizedStringWithRuntimeParamters = translationService.Create(ThisIsASampleError, DateTime.Now);
            throw new LocalizedException(localizedStringWithRuntimeParamters);
        }
        catch (Exception e)
        {
            ILocalizedString localizedErrorMessage = LocalizedException.FindLocalizedMessage(e);
            if (localizedErrorMessage != null)
                Console.WriteLine(localizedErrorMessage.GetText(CultureInfo.CurrentUICulture));
            else
                Console.WriteLine($"Internal error {e.Message}");

            ILocalizedString createsAlwaysAUserFriendlyMessage = LocalizedException.CreateLocalizedMessage(translationService, e);
            // Will write out "Internal Error" if no LocalizedException was found in the exception hirachy
            Console.WriteLine(createsAlwaysAUserFriendlyMessage.GetText(CultureInfo.CurrentUICulture));
        }

        // Create a localized string out of an enum - Note: the enum must be marked with an [Translated] attribute
        ILocalizedString localizedEnumValue = translationService.Create(Colors.Red);
        Console.WriteLine(localizedEnumValue.GetText(CultureInfo.CurrentUICulture));
    }

    [Translated]
    enum Colors
    {
        Red,
        Blue,
        Green,
        [Translated("Dark Green")] // Provide default text, if the text of the enum value is not suitable
        GreenDark,
    }
}
```

## Contributing
All contributors are welcome to improve this project. The best way would be to start a bug or feature request and discuss the way you want find a solution with us.
After this process, please create a fork of the repository and open a pull request with your changes. Of course, if your changes are small, feel free to immediately start your pull request without previous discussion. 
