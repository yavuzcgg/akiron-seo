using System.Net;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using AkironSeo.Infrastructure.Services.GeoAdapters;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AkironSeo.IntegrationTests;

public class GeoAdapterTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task OpenAiSearchAdapter_WithoutKey_ShouldReturnNotConfigured()
    {
        var adapter = new OpenAiSearchAdapter(new HttpClient(), NullLogger<OpenAiSearchAdapter>.Instance);
        var result = await adapter.QueryEngineAsync("Acme Corp", "https://acme.com", "Best CRM software", apiKey: "");

        Assert.Equal("ChatGPT", result.EngineName);
        Assert.False(result.IsMentioned);
        Assert.Equal(DataSources.NotConfigured, result.DataSource);
    }

    [Fact]
    public async Task OpenAiSearchAdapter_WithValidResponse_ShouldDetectBrandMention()
    {
        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Top CRM solutions include Acme Corp (acme.com) for fast-growing businesses."
                    }
                }
            }
        };

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(mockResponse))
        });

        var httpClient = new HttpClient(handler);
        var adapter = new OpenAiSearchAdapter(httpClient, NullLogger<OpenAiSearchAdapter>.Instance);

        var result = await adapter.QueryEngineAsync("Acme Corp", "https://acme.com", "Best CRM software", apiKey: "sk-test-key");

        Assert.True(result.IsMentioned);
        Assert.Equal("Positive", result.Sentiment);
        Assert.Equal("https://acme.com", result.CitationUrl);
        Assert.Equal(DataSources.Live, result.DataSource);
    }

    [Fact]
    public async Task AnthropicAdapter_WithoutKey_ShouldReturnNotConfigured()
    {
        var adapter = new AnthropicAdapter(new HttpClient(), NullLogger<AnthropicAdapter>.Instance);
        var result = await adapter.QueryEngineAsync("Acme Corp", "https://acme.com", "Best CRM software", apiKey: "");

        Assert.Equal("Claude", result.EngineName);
        Assert.False(result.IsMentioned);
        Assert.Equal(DataSources.NotConfigured, result.DataSource);
    }

    [Fact]
    public async Task AnthropicAdapter_WithValidResponse_ShouldDetectBrandMention()
    {
        var mockResponse = new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = "Recommended brands are Acme Corp and others according to recent surveys."
                }
            }
        };

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(mockResponse))
        });

        var httpClient = new HttpClient(handler);
        var adapter = new AnthropicAdapter(httpClient, NullLogger<AnthropicAdapter>.Instance);

        var result = await adapter.QueryEngineAsync("Acme Corp", "https://acme.com", "Best CRM software", apiKey: "sk-ant-test-key");

        Assert.True(result.IsMentioned);
        Assert.Equal("Positive", result.Sentiment);
        Assert.Equal(DataSources.Live, result.DataSource);
    }
}
