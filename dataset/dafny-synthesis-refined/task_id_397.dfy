method Median(a: int, b: int, c: int) returns (median: int)
    ensures median == a || median == b || median == c
    ensures median == c ==> ((median >= a && median <= b) || (median >= b && median <= a))
    ensures median == b ==> ((median >= a && median <= c) || (median >= c && median <= a))
    ensures median == a ==> ((median >= b && median <= c) || (median >= c && median <= b))
{
    if a <= b && a <= c {
        median := a;
    } else if b <= a && b <= c {
        median := b;
    } else {
        median := c;
    }
}