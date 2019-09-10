using DCCS.LocalizedString.NetStandard.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleInjector;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace DCCS.LocalizedString.NetStandard.Tests
{
    [TestClass]
    public class LocalizerTest
    {

        [TestMethod]
        public void TestKey()
        {
            Assert.AreEqual(Assembly.GetExecutingAssembly().GetName().FullName, TestString.AssemblyName);
            Assert.AreEqual(GetType().FullName + "." + nameof(TestString), TestString.Key);
        }

        private static readonly LocalizedStringKey TestString = new LocalizedStringKey("Default");

        [TestMethod]
        public void TestKeyInGeneric()
        {
            try
            {
                string dummy = Test<int>.KeyInGeneric.Default;
                Assert.Fail("Exception not thrown");
            }
            catch (TypeInitializationException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(Exception));
            }
        }

        [TestMethod]
        public void TranslationServiceFallbackToDefault()
        {
            var mock = new Mock<ITranslationProviderService>();
            mock.Setup(foo => foo.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.InvariantCulture)).Returns(default(string));

            ITranslationService translationService = new TranslationService(new[] { mock.Object });

            ILocalizedString localizedString = translationService.Create(TestString);
            string result = localizedString.GetText(CultureInfo.InvariantCulture);
            Assert.AreEqual(TestString.Default, result);

            mock.VerifyAll();
        }

        [TestMethod]
        public void TranslationServiceNoFallbackGerman()
        {
            var mock = new Mock<ITranslationProviderService>();
            mock.Setup(foo => foo.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("de"))).Returns("german");

            ITranslationService translationService = new TranslationService(new[] { mock.Object });

            ILocalizedString localizedString = translationService.Create(TestString);
            string result = localizedString.GetText(CultureInfo.GetCultureInfo("de"));
            Assert.AreEqual("german", result);

            mock.VerifyAll();
        }

        [TestMethod]
        public void TranslationServiceFallbackToInvariant()
        {
            var mock = new Mock<ITranslationProviderService>();
            mock.Setup(foo => foo.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("de"))).Returns(default(string));
            mock.Setup(foo => foo.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("en"))).Returns(default(string));
            mock.Setup(foo => foo.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.InvariantCulture)).Returns("Invariant");

            ITranslationService translationService = new TranslationService(new[] { mock.Object });

            ILocalizedString localizedString = translationService.Create(TestString);
            string result = localizedString.GetText(CultureInfo.GetCultureInfo("de"));
            Assert.AreEqual("Invariant", result);

            mock.VerifyAll();
        }



        private static readonly LocalizedFormatKey TestFormat = new LocalizedFormatKey("First: {0} Second: {1}", "Value1", "Value2");

        [TestMethod]
        public void TestFormatKey()
        {
            var mock = new Mock<ITranslationProviderService>();
            mock.Setup(foo => foo.FindText(TestFormat.AssemblyName, TestFormat.Key, CultureInfo.GetCultureInfo("en"))).Returns("First: {0} Second: {1}");
            mock.Setup(foo => foo.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("en"))).Returns("english");

            var firstParameter = MyTest.Test1;

            ITranslationService translationService = new TranslationService(new[] { mock.Object });
            var secondParameter = translationService.Create(TestString);

            ILocalizedString localizedString = translationService.Create(TestFormat, firstParameter, secondParameter);
            string result = localizedString.GetText(CultureInfo.GetCultureInfo("en"));
            Assert.AreEqual("First: Test 1 Second: english", result);

            mock.VerifyAll();
        }

        [TestMethod]
        public void TestResourceLocalizeString()
        {
            // Please note, this is not a typical usage of this framework, this is only helpfull if you want use the Generated Resource Classes without the Key framework
            var localizedString = new ResourceLocalizedString((c) => Resources.DCCS_LocalizedString_NetStandard_Tests_LocalizerTest_TestString);
            Assert.AreEqual("Deutsch", localizedString.GetText(CultureInfo.GetCultureInfo("de")));
            Assert.AreEqual("English", localizedString.GetText(CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void TestResourceProvider()
        {
            ITranslationProviderService translationProvider = new ResourceTranslationProvider();
            Assert.AreEqual("Deutsch", translationProvider.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("de")));
            Assert.AreEqual("English", translationProvider.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.InvariantCulture));
            Assert.IsNull(translationProvider.FindText(TestString.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("es")));
        }

        [TestMethod]
        public void IntegrationTest()
        {
            Container container = new Container();
            container.Collection.Register<ITranslationProviderService>(new ResourceTranslationProvider());
            container.RegisterSingleton<ITranslationService, TranslationService>();

            ITranslationService translationService = container.GetInstance<ITranslationService>();

            ILocalizedString simpleLocalizedString = translationService.Create(TestString);
            Assert.AreEqual("Deutsch", simpleLocalizedString.GetText(CultureInfo.GetCultureInfo("de")));
            Assert.AreEqual("English", simpleLocalizedString.GetText(CultureInfo.GetCultureInfo("en")));

            ILocalizedString formatterString = translationService.Create(TestFormat, "x", simpleLocalizedString);

            Assert.AreEqual("Erster: x Zweiter: Deutsch", formatterString.GetText(CultureInfo.GetCultureInfo("de")));
            Assert.AreEqual("First: x Second: English", formatterString.GetText(CultureInfo.GetCultureInfo("en")));
            Assert.AreEqual("First: x Second: English", formatterString.GetText(CultureInfo.GetCultureInfo("es")));
        }

        [TestMethod]
        public void DataContractTestGerman()
        {
            var mock = new Mock<ITranslationProviderService>();
            mock.Setup(foo => foo.FindText(TestFormat.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("de"))).Returns("Deutsch");
            mock.Setup(foo => foo.FindText(TestFormat.AssemblyName, TestString.Key, CultureInfo.GetCultureInfo("en"))).Returns("English");
            var translationService = new TranslationService(new[] { mock.Object });

            ILocalizedString text = translationService.Create(TestString);
            var localizedContract = new LocalizedStringContract(text, CultureInfo.GetCultureInfo("de"));

            Assert.AreEqual("Deutsch", localizedContract.Text);
            Assert.AreEqual("English", localizedContract.Invariant);
            Assert.AreEqual("de", localizedContract.Language);

            var serializer = new DataContractJsonSerializer(typeof(LocalizedStringContract));
            LocalizedStringContract deserializedLocalizedStringContract;
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, localizedContract);
                stream.Position = 0;
                deserializedLocalizedStringContract = (LocalizedStringContract)serializer.ReadObject(stream);
            }
            Assert.AreEqual(localizedContract.Invariant, deserializedLocalizedStringContract.Invariant);
            Assert.AreEqual(localizedContract.Language, deserializedLocalizedStringContract.Language);
            Assert.AreEqual(localizedContract.Text, deserializedLocalizedStringContract.Text);
        }

        [TestMethod]
        public void UsageSearcherTest()
        {
            var entries = LocalizerUsageSearcher.GetLocalizerEntries(GetType().Assembly).ToDictionary(e => e.Key);
            Assert.IsTrue(entries.ContainsKey(TestString.Key));
            var entry = entries[TestString.Key];
            Assert.IsNotNull(entry.ParameterNames);
            Assert.AreEqual(0, entry.ParameterNames.Length);
            Assert.AreEqual(TestString.AssemblyName, entry.AssemblyName);
            Assert.AreEqual(TestString.Default, entry.Default);

            entry = entries[TestFormat.Key];
            Assert.IsNotNull(entry.ParameterNames);
            Assert.AreEqual(TestFormat.ParameterNames.Length, entry.ParameterNames.Length);
            Assert.AreEqual(TestFormat.AssemblyName, entry.AssemblyName);
            Assert.AreEqual(TestFormat.Default, entry.Default);
        }

        [Translated()]
        enum MyTest
        {
            Test1,
            [Translated("Test Second")]
            Test2,
        }

        [TestMethod]
        public void TestEnum()
        {
            var enumKeyTest1 = new LocalizedEnumKey(MyTest.Test1);
            Assert.AreEqual("Test 1", enumKeyTest1.Default);
            Assert.AreEqual(typeof(MyTest).FullName + ".Test1", enumKeyTest1.Key);
            Assert.AreEqual(typeof(MyTest).Assembly.FullName, enumKeyTest1.AssemblyName);

            var enumKeyTest2 = new LocalizedEnumKey(MyTest.Test2);
            Assert.AreEqual("Test Second", enumKeyTest2.Default);
            Assert.AreEqual(typeof(MyTest).FullName + ".Test2", enumKeyTest2.Key);
            Assert.AreEqual(typeof(MyTest).Assembly.FullName, enumKeyTest2.AssemblyName);

        }

        class Test<T>
        {
            public static readonly LocalizedStringKey KeyInGeneric = new LocalizedStringKey("My Test String");
        }
    }
}
