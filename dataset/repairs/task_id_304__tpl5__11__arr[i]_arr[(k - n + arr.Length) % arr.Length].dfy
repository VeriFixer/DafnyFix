// task_id_304__instrumented_helper.dfy

method FindElementAfterRotations(arr: array<int>, n: int, k: int)
    returns (result: int)
  requires n >= 1
  requires 0 <= k < arr.Length
  ensures result == arr[(k - n + arr.Length) % arr.Length]
{
  var i := 0;
  while i < k {
    i := i + 1;
  }
  return arr[(k - n + arr.Length) % arr.Length];
}
