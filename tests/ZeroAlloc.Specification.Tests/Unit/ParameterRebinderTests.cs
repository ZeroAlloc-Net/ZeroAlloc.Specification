using System.Linq.Expressions;
using FluentAssertions;
using Xunit;
using ZeroAlloc.Specification.Internal;

namespace ZeroAlloc.Specification.Tests.Unit;

public class ParameterRebinderTests
{
    [Fact]
    public void Rebind_ReplacesParameterInBody()
    {
        Expression<Func<int, bool>> expr = x => x > 0;
        var newParam = Expression.Parameter(typeof(int), "y");

        var rebound = ParameterRebinder.ReplaceParameter(expr.Body, expr.Parameters[0], newParam);

        // Compile and verify the rebound expression works with the new parameter
        var lambda = Expression.Lambda<Func<int, bool>>(rebound, newParam).Compile();
        lambda(1).Should().BeTrue();
        lambda(-1).Should().BeFalse();
    }
}
