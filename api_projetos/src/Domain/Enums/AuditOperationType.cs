namespace API_Gestao_Eventos.src.Domain.Enums
{
    public enum AuditOperationType
    {
        Insert = 0, //registro novo
        Update = 1, //registro alterado
        Delete = 2, //registro deletado
        LoginSuccess = 10, //acesso novo
        LoginFailure = 11, //acesso com falha
        PasswordChanged = 20, //senha alterada
        PasswordReset = 21 //senha resetada
    }
}
