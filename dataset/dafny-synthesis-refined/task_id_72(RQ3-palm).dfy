method IsDifferenceOfTwoSquares(n: int) returns (result: bool)
    requires n >= 0
    ensures result <==> exists x, y :: (x * x - y * y == n)
{
    var x := 0;
    var y := 0;
    while x * x <= n
        invariant x * x <= n
        invariant result <==> exists y :: (x * x - y * y == n)
    {
        x := x + 1;
    }
    while y * y <= n
        invariant y * y <= n
        invariant result <==> exists x :: (x * x - y * y == n)
    {
        y := y + 1;
    }
    return result;
}