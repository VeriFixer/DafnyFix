method AreElementsUnique(a: array<int>) returns (result: bool)
    requires a != null
    ensures result <==> forall i, j :: 0 <= i < a.Length && 0 <= j < a.Length && i != j ==> a[i] != a[j]
{
    result := true;
    for i := 0 to a.Length - 1
        invariant 0 <= i < a.Length
        invariant result ==> forall k, l :: 0 <= k < i && k < l < a.Length ==> a[k] != a[l]
        invariant !result ==> exists k, l :: 0 <= k < a.Length && 0 <= l < a.Length && k < l && a[k] == a[l]
    {
        for j := i + 1 to a.Length
            invariant i < j <= a.Length
            invariant result ==>
                (forall k, l :: 0 <= k < i && k < l < a.Length ==> a[k] != a[l]) &&
                (forall l :: i < l < j ==> a[i] != a[l])
            invariant !result ==> exists k, l :: 0 <= k < a.Length && 0 <= l < a.Length && k < l && a[k] == a[l]
        {
            if a[i] == a[j]
            {
                result := false;
                break;
            }
        }
    }
    result := result;
}