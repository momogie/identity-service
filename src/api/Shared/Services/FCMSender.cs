
using Microsoft.Extensions.Configuration;

namespace Shared;

public class FCMSender
{
    protected IConfiguration Configuration { get; set; }
    public FCMSender(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void Send(string token, string title, string description)
    {
        //HttpClient client = new HttpClient();
        //client.BaseAddress = new Uri(Configuration["FCM:Host"]);
    }
}
