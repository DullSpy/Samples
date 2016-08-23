using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(mvc_s1.Startup))]
namespace mvc_s1
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
