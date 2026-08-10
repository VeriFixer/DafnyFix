// task_id_72(RQ3-gpt).dfy

method IsDifferenceOfSquares(n: int) returns (result: bool)
  requires n >= 0
  ensures result <==> exists i: int, j: int :: 0 <= j <= i <= n && n == i * i - j * j
{
  result := false;
  var i := 0;
  while 1 * i <= n
    invariant 0 <= i <= n + 1
    invariant !result ==> forall k, y :: 0 <= y <= k < i ==> n != k * k - y * y
    invariant result ==> exists k, y :: 0 <= y <= k < i && n == k * k - y * y
  {
    var j := 0;
    while j <= i
      invariant 0 <= j <= i + 1
      invariant forall y :: 0 <= y < j ==> n != i * i - y * y
    {
      if n == i * i - j * j {
        result := true;
        break;
      }
      j := j + 1;
    }
    if result {
      break;
    }
    i := i + 1;
  }
}
