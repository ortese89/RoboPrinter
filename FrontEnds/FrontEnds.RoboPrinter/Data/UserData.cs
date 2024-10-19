namespace FrontEnds.RoboPrinter.Data;

public class UserData
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    //public int RoleId { get; set; }
    public List<string> Roles { get; set; }

    public UserData()
    {
        
    }

    public UserData(int userId, string userName, List<string> roles)
    {
        UserId = userId;
        UserName = userName;
        Roles = roles;
    }

    public void ClearData()
    {
        UserId = 0;
        UserName = null;
        Roles = null;
    }
}
