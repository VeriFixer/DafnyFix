// task_id_19__instrumented_helper.dfy

method ContainsDuplicate(a: array<int>) returns (result: bool)
  requires a != null
  ensures result ==> exists i, j :: 0 <= i < a.Length && 0 <= j < a.Length && i != j && a[i] == a[j]
  ensures !result ==> forall i, j :: 0 <= i < a.Length && 0 <= j < a.Length && i != j ==> a[i] != a[j]
{
  result := false;
  if !((0 == a.Length) == true) {
    for i := 0 to a.Length - 1
      invariant 0 <= i <= a.Length - 1
      invariant !result ==> forall x, y :: 0 <= x < i && 0 <= y < a.Length && x != y ==> a[x] != a[y]
    {
      for j := i + 1 to a.Length
        invariant i < j <= a.Length
        invariant !result ==> forall k :: i < k < j ==> a[i] != a[k]
      {
        if a[i] == a[j] {
          result := true;
          return;
        }
      }
    }
  }
}
