namespace mycms_shared.Infrastructure
{
    public enum CRUDOperation
    {
        CREATE, READ, UPDATE, DELETE
    }

    public class CRUDEvent<T>
    {
        public T Entity { get; set; }
        public CRUDOperation Operation { get; set; }
    }
}