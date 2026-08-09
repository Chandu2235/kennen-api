using Microsoft.AspNetCore.Identity;

namespace Kennen.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }
}
