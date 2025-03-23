using InterviewAssistant.Web.Components;
using InterviewAssistant.Web.Services;
using InterviewAssistant.Web.Clients;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

// ChatApiClient 등록
builder.Services.AddHttpClient<IChatApiClient, ChatApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// ChatService 등록
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();