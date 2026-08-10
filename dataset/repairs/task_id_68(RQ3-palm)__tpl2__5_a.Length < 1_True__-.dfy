// task_id_68(RQ3-palm)__instrumented_helper.dfy

method IsMonotonic(a: array<int>) returns (result: bool)
  requires a != null
  ensures result <==> (forall i, j :: 0 <= i < j < a.Length ==> a[i] <= a[j]) || forall i, j :: 0 <= i < j < a.Length ==> a[i] >= a[j]
{
  if (a.Length < 1) == true {
    return true;
  }
  var increasing := true;
  var decreasing := true;
  for i := 0 to a.Length - 1
    invariant 0 <= i <= a.Length - 1
    invariant increasing <==> forall k, l :: 0 <= k < l <= i ==> a[k] <= a[l]
    invariant decreasing <==> forall k, l :: 0 <= k < l <= i ==> a[k] >= a[l]
  {
    if a[i] > a[i + 1] {
      increasing := false;
    } else if a[i] < a[i + 1] {
      decreasing := false;
    }
  }
  result := increasing || decreasing;
}
