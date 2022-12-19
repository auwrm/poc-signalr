namespace TTSS.Infrastructure.Models
{
    public interface IDbModelBase
    {
    }

    public interface IDbModel : IDbModelBase
    {
    }

    public interface IDbModel<T> : IDbModelBase
    {
        T Id { get; set; }
    }
}
