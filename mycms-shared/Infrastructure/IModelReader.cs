using System;
using System.Linq;

namespace mycms_shared.Infrastructure
{
    public interface IModelReader<T> 
        : IDisposable, IQueryable<T> where T : class
    {
    }
}