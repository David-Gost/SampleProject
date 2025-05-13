using Hangfire.Dashboard;

namespace SampleProject.Middleware.Hangfire;

public class HangfireAuthorizationFilter:IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return true;
    }
}