using FloristHub.DAL.Entities;

namespace FloristHub.Services.Interfaces.Email;

public interface IAuthEmailNotifications
{
    void WelcomeRegistered(ApplicationUser user, Tenant tenant, string role);
}
