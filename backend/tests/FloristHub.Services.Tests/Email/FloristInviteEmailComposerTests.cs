using FloristHub.DAL.Entities;
using FloristHub.Services.Email;

namespace FloristHub.Services.Tests.Email;

public class FloristInviteEmailComposerTests
{
    [Fact]
    public void Build_url_encodes_token_and_user_id()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var token = "abc+/=";
        var url = FloristInviteEmailComposer.BuildSetPasswordUrl("http://localhost:5173", id, token);
        Assert.Contains(Uri.EscapeDataString(token), url);
        Assert.StartsWith("http://localhost:5173/convite?", url);
    }

    [Fact]
    public void Compose_mentions_store_and_validity()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Flores da Lina", Slug = "flores-lina" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "florista@demo.pt",
            UserName = "florista@demo.pt",
            FullName = "Lina"
        };
        var (subject, html, url) = FloristInviteEmailComposer.Compose(
            tenant,
            user,
            "https://app.test",
            "token123");

        Assert.Contains("Flores da Lina", subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("72 horas", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/convite?", url, StringComparison.Ordinal);
        Assert.Contains("convite", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Definir palavra-passe", html, StringComparison.OrdinalIgnoreCase);
    }
}
