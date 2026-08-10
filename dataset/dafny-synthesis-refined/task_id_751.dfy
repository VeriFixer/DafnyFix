method IsMinHeap(a: array<int>) returns (isMinHeap: bool)
    requires a != null
    requires forall i, j :: 0 <= i < j < a.Length ==> a[i] >= a[j]
    ensures isMinHeap <==> forall i, j :: 0 <= i < j < a.Length ==> a[i] <= a[j]
{
    var i := 0;
    var j := 1;
    while j < a.Length
        invariant 0 <= i < a.Length
        invariant 0 <= j < a.Length
        invariant forall k :: 0 <= k < i ==> a[k] <= a[j]
    {
        if 2 * j + 1 < a.Length && a[j] > a[2 * j + 1]
        {
            j := 2 * j + 1;
        }
        else if 2 * j + 2 < a.Length && a[j] > a[2 * j + 2]
        {
            j := 2 * j + 2;
        }
        else
        {
            i := i + 1;
            j := i + 1;
        }
    }
    isMinHeap := true;
}