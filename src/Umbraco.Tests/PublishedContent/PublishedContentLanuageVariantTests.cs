﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Moq;
using NUnit.Framework;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Tests.Testing;
using Umbraco.Web;

namespace Umbraco.Tests.PublishedContent
{
    [TestFixture]
    [UmbracoTest(PluginManager = UmbracoTestOptions.PluginManager.PerFixture)]
    public class PublishedContentLanuageVariantTests : PublishedContentSnapshotTestBase
    {
        protected override void Compose()
        {
            base.Compose();

            Container.RegisterSingleton(_ => GetServiceContext());
        }

        protected ServiceContext GetServiceContext()
        {
            var serviceContext = TestObjects.GetServiceContextMock(Container);
            MockLocalizationService(serviceContext);
            return serviceContext;
        }

        private static void MockLocalizationService(ServiceContext serviceContext)
        {
            // Set up languages.
            // Spanish falls back to English and Italian to Spanish (and then to English).
            // French has no fall back.
            var languages = new List<Language>
                {
                    new Language("en-US") { Id = 1, CultureName = "English", IsDefaultVariantLanguage = true },
                    new Language("fr") { Id = 2, CultureName = "French" },
                    new Language("es") { Id = 3, CultureName = "Spanish" },
                    new Language("it") { Id = 4, CultureName = "Italian" },
                    new Language("de") { Id = 5, CultureName = "German" }
                };
            languages[2].FallbackLanguage = languages[0];
            languages[3].FallbackLanguage = languages[2];

            var localizationService = Mock.Get(serviceContext.LocalizationService);
            localizationService.Setup(x => x.GetAllLanguages()).Returns(languages);
            localizationService.Setup(x => x.GetLanguageByIsoCode(It.IsAny<string>()))
                .Returns((string c) => languages.SingleOrDefault(y => y.IsoCode == c));
        }

        internal override void PopulateCache(PublishedContentTypeFactory factory, SolidPublishedContentCache cache)
        {
            var props = new[]
                {
                    factory.CreatePropertyType("prop1", 1),
                };
            var contentType1 = factory.CreateContentType(1, "ContentType1", Enumerable.Empty<string>(), props);

            var prop1 = new SolidPublishedPropertyWithLanguageVariants
                {
                    Alias = "welcomeText",
                };
            prop1.SetSourceValue("en-US", "Welcome");
            prop1.SetValue("en-US", "Welcome");
            prop1.SetSourceValue("de", "Willkommen");
            prop1.SetValue("de", "Willkommen");

            cache.Add(new SolidPublishedContent(contentType1)
            {
                Id = 1,
                SortOrder = 0,
                Name = "Content 1",
                UrlSegment = "content-1",
                Path = "/1",
                Level = 1,
                Url = "/content-1",
                ParentId = -1,
                ChildIds = new int[] { },
                Properties = new Collection<IPublishedProperty>
                    {
                        prop1
                    }
            });
        }

        [Test]
        public void Can_Get_Content_For_Populated_Requested_Language()
        {
            var content = UmbracoContext.Current.ContentCache.GetAtRoot().First();
            var value = content.Value("welcomeText", "en-US");
            Assert.AreEqual("Welcome", value);
        }

        [Test]
        public void Can_Get_Content_For_Populated_Requested_Non_Default_Language()
        {
            var content = UmbracoContext.Current.ContentCache.GetAtRoot().First();
            var value = content.Value("welcomeText", "de");
            Assert.AreEqual("Willkommen", value);
        }

        [Test]
        public void Do_Not_Get_Content_For_Unpopulated_Requested_Language_Without_Fallback()
        {
            var content = UmbracoContext.Current.ContentCache.GetAtRoot().First();
            var value = content.Value("welcomeText", "fr");
            Assert.IsNull(value);
        }

        [Test]
        public void Can_Get_Content_For_Unpopulated_Requested_Language_With_Fallback()
        {
            var content = UmbracoContext.Current.ContentCache.GetAtRoot().First();
            var value = content.Value("welcomeText", "es");
            Assert.AreEqual("Welcome", value);
        }

        [Test]
        public void Can_Get_Content_For_Unpopulated_Requested_Language_With_Fallback_Over_Two_Levels()
        {
            var content = UmbracoContext.Current.ContentCache.GetAtRoot().First();
            var value = content.Value("welcomeText", "it");
            Assert.AreEqual("Welcome", value);
        }
    }
}
