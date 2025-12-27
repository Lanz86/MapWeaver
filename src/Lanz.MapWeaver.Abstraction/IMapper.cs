using System;
using System.Collections.Generic;
using System.Text;

namespace Lanz.MapWeaver.Abstraction;

public interface IMapper
{
    TDestination Map<TDestination>(object source);

    TDestination Map<TSource, TDestination>(TSource source);

    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
}
