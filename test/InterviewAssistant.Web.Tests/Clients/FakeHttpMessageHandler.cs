namespace InterviewAssistant.Web.Tests.Clients;

public class FakeHttpMessageHandler : HttpMessageHandler
{
private readonly HttpResponseMessage _fakeResponse;

public FakeHttpMessageHandler(HttpResponseMessage response)
{
  _fakeResponse = response;
}

protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
  return Task.FromResult(_fakeResponse);
}
}
