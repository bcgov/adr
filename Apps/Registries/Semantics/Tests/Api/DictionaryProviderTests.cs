namespace Adr.Semantics.Tests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Abstractions;

    public class DictionaryProviderTests : IClassFixture<GlossaryApiFactory>
    {
        private readonly HttpClient _client;

        public DictionaryProviderTests(GlossaryApiFactory application)
        {
            _client = application.CreateClient();
        }

        [Theory]
        [InlineData("http://localhost/v1/Glossary")]
        [InlineData("http://localhost/v1/Glossary/")]
        public async Task DictionarySemanticLinkResolvesToTheReferencedGlossaryTerm(string baseUrl)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GlossaryBaseUrl"] = baseUrl,
                    ["DictionarySources:PublicBodies:Enabled"] = "true",
                    ["DictionarySources:PublicBodies:SourceUrl"] = "http://public-bodies/swagger/v1/swagger.json",
                })
                .Build();
            using var sourceClient = new HttpClient(new OpenApiHandler());
            var provider = new OpenApiProvider(
                NullLogger<OpenApiProvider>.Instance,
                new SourceClientFactory(sourceClient),
                configuration
            );

            var dictionary = Assert.Single(provider.GetAllDictionaries());
            var entry = Assert.Single(dictionary.Entries);
            var field = Assert.Single(entry.Fields);
            using var response = await _client.GetAsync(field.SemanticTermRef);

            response.EnsureSuccessStatusCode();
            var term = await response.Content.ReadFromJsonAsync<GlossaryResponseModel<GlossaryModel>>();
            Assert.Equal("access-control", term!.Payload.Name);
            Assert.Equal("a3ac060a-be57-4e70-af4c-bb7bd91bc7aa", term.Payload.StaticId);
        }

        private sealed class SourceClientFactory(HttpClient client) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => client;
        }

        private sealed class OpenApiHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            ) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"components":{"schemas":{"Example":{"allOf":[{},
                    {"properties":{"accessControl":{"x-bc-field":"accessControl",
                    "x-bc-semantic-ref":"a3ac060a-be57-4e70-af4c-bb7bd91bc7aa"}}}]}}}}
                    """),
            });
        }
    }
}
