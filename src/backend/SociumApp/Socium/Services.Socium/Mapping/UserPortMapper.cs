using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Mapping;

public static class UserPortMapper
{
    public static string ToDisplayName(UserPortModel? user)
    {
        if (user == null)
        {
            return UserPortTexts.DeletedUserName;
        }

        return user.IsDeleted ? $"{user.Name} {UserPortTexts.DeletedMark}" : user.Name;
    }
}
