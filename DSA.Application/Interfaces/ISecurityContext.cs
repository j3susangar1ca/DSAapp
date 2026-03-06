namespace DSA.Application.Interfaces;

public interface ISecurityContext
{
    bool CurrentUserHasRole(string roleName);
}
