namespace LocaGuest.Api.Authorization;

public static class Permissions
{
    public const string PropertiesRead = "properties.read";
    public const string PropertiesWrite = "properties.write";

    public const string TenantsRead = "tenants.read";
    public const string TenantsWrite = "tenants.write";
    public const string TenantsDelete = "tenants.delete";

    public const string RoomsRead = "rooms.read";
    public const string RoomsWrite = "rooms.write";

    public const string ContractsRead = "contracts.read";
    public const string ContractsWrite = "contracts.write";

    public const string PaymentsRead = "payments.read";
    public const string PaymentsWrite = "payments.write";

    public const string DepositsRead = "deposits.read";
    public const string DepositsWrite = "deposits.write";

    public const string DocumentsRead = "documents.read";
    public const string DocumentsWrite = "documents.write";

    public const string TeamRead = "team.read";
    public const string TeamManage = "team.manage";
}
