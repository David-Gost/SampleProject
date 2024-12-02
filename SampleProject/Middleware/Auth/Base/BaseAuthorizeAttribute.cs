using Microsoft.AspNetCore.Authorization;

namespace SampleProject.Middleware.Auth.Base;

public class BaseAuthorizeAttribute:AuthorizeAttribute
{
    
    public BaseAuthorizeAttribute()
    {
        AuthenticationSchemes = "Bearer";
    }
}