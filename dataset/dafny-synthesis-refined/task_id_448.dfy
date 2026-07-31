function a_val(k: nat): int
  decreases k
{
  if k <= 3 then 3
  else b_val(k-1)
}

function b_val(k: nat): int
  decreases k
{
  if k <= 3 then 0
  else c_val(k-1)
}

function c_val(k: nat): int
  decreases k
{
  if k <= 3 then 2
  else a_val(k-1) + b_val(k-1)
}

function sum_c(m: nat): int
  decreases m
{
  if m <= 3 then 0
  else sum_c(m-1) + c_val(m)
}

function Spec(n: nat): int
{
  if n == 0 then 0
  else if n == 1 then 3
  else if n == 2 then 3
  else if n == 3 then 5
  else 5 + sum_c(n)
}

method calSum(n:int) returns (res:int)
  requires n >= 0
  ensures res == Spec(n as nat)
  {   
    var a, b, c := 3, 0, 2;
    res := a + b + c;
    for i := 3 to n - 1
      invariant a >= 0 && b >= 0 && c >= 0 && res >= 0
      invariant a == a_val(i)
      invariant b == b_val(i)
      invariant c == c_val(i)
      invariant res == 5 + sum_c(i)
    {
      a, b, c := b, c, a + b;
      res := res + c;
    }
  }