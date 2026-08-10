predicate IsSorted(a: array<int>)
    reads a
{
    forall p, q :: 0 <= p <= q < a.Length ==> a[p] <= a[q]
}

method Median(A: array<int>, B: array<int>) returns (m: int, ghost final_i: int, ghost final_j: int)
    requires A.Length == B.Length
    requires A.Length > 0
    requires IsSorted(A)
    requires IsSorted(B)
    ensures 0 <= final_i <= A.Length
    ensures 0 <= final_j <= B.Length
    ensures final_i + final_j == A.Length
    ensures forall x :: 0 <= x < final_i ==> A[x] <= m
    ensures forall y :: 0 <= y < final_j ==> B[y] <= m
    ensures forall x :: final_i <= x < A.Length ==> m <= A[x]
    ensures forall y :: final_j <= y < B.Length ==> m <= B[y]
{
    var i := 0;
    var j := 0;
    var k := 0;

    while i < A.Length && j < B.Length && k < (A.Length + B.Length) / 2
        invariant 0 <= i <= A.Length
        invariant 0 <= j <= B.Length
        invariant 0 <= k <= (A.Length + B.Length) / 2
        invariant k == i + j
        invariant k > 0 ==> (forall x :: 0 <= x < i ==> A[x] <= m)
        invariant k > 0 ==> (forall y :: 0 <= y < j ==> B[y] <= m)
        invariant k > 0 ==> (forall x :: i <= x < A.Length ==> m <= A[x])
        invariant k > 0 ==> (forall y :: j <= y < B.Length ==> m <= B[y])
    {
        if A[i] < B[j] {
            m := A[i];
            i := i + 1;
        } else {
            m := B[j];
            j := j + 1;
        }
        k := k + 1;
    }

    if i == A.Length {
        while j < B.Length && k < (A.Length + B.Length) / 2 
            invariant 0 <= i <= A.Length
            invariant 0 <= j <= B.Length
            invariant i == A.Length
            invariant 0 <= k <= (A.Length + B.Length) / 2
            invariant k == i + j
            invariant k > 0 ==> (forall x :: 0 <= x < i ==> A[x] <= m)
            invariant k > 0 ==> (forall y :: 0 <= y < j ==> B[y] <= m)
            invariant k > 0 ==> (forall x :: i <= x < A.Length ==> m <= A[x])
            invariant k > 0 ==> (forall y :: j <= y < B.Length ==> m <= B[y])
        {
            m := B[j];
            j := j + 1;
            k := k + 1;
        }
    } else if j == B.Length {
        while i < A.Length && k < (A.Length + B.Length) / 2 
            invariant 0 <= i <= A.Length
            invariant 0 <= j <= B.Length
            invariant j == B.Length
            invariant 0 <= k <= (A.Length + B.Length) / 2
            invariant k == i + j
            invariant k > 0 ==> (forall x :: 0 <= x < i ==> A[x] <= m)
            invariant k > 0 ==> (forall y :: 0 <= y < j ==> B[y] <= m)
            invariant k > 0 ==> (forall x :: i <= x < A.Length ==> m <= A[x])
            invariant k > 0 ==> (forall y :: j <= y < B.Length ==> m <= B[y])
        {
            m := A[i];
            i := i + 1;
            k := k + 1;
        }
    }

    final_i := i;
    final_j := j;
}