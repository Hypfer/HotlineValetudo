using HotlineValetudo;
using HotlineValetudo.Config;
using HotlineValetudo.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<SipOptions>(builder.Configuration.GetSection("Sip"));
builder.Services.Configure<TtsOptions>(builder.Configuration.GetSection("Tts"));
builder.Services.AddSingleton<CallSessionManager>();
builder.Services.AddSingleton<TtsService>();
builder.Services.AddSingleton<SipService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();