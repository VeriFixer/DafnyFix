// task_id_68(RQ3-gpt)__instrumented_helper.dfy

method IsMonotonic(a: array<int>) returns (result: bool)
  requires a != null
  ensures result <==> (forall i, j :: 0 <= i < j < a.Length ==> a[i] <= a[j]) || forall i, j :: 0 <= i < j < a.Length ==> a[i] >= a[j]
{
  if (0 < a.Length - 1) == false {
    return true;
  } else {
    result := true;
  }
  var isIncreasing := true;
  var isDecreasing := true;
  for i := 0 to a.Length - 1
    invariant 0 <= i < a.Length
    invariant isIncreasing <==> forall k, l :: 0 <= k < l <= i ==> a[k] <= a[l]
    invariant isDecreasing <==> forall k, l :: 0 <= k < l <= i ==> a[k] >= a[l]
  {
    if a[i] > a[i + 1] {
      isIncreasing := false;
    }
    if a[i] < a[i + 1] {
      isDecreasing := false;
    }
  }
  result := isIncreasing || isDecreasing;
}
