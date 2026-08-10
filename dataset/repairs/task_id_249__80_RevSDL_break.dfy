// task_id_249.dfy

predicate isUnique(arr: seq<int>)
{
  forall i, j :: 
    0 <= i < j < |arr| ==>
      arr[i] != arr[j]
}

function intersection(a1: seq<int>, a2: seq<int>): seq<int>
  requires isUnique(a1)
  requires isUnique(a2)
{
  if |a1| == 0 then
    []
  else if a1[0] in a2 then
    [a1[0]] + intersection(a1[1..], a2)
  else
    intersection(a1[1..], a2)
}

lemma UniquePrefix(s: seq<int>, k: nat)
  requires isUnique(s)
  requires k <= |s|
  ensures isUnique(s[..k])
{
}

lemma UniqueTail(s: seq<int>)
  requires isUnique(s)
  requires |s| > 0
  ensures isUnique(s[1..])
{
}

lemma SingletonIntersection(x: int, a2: seq<int>)
  requires isUnique(a2)
  ensures isUnique([x])
  ensures x in a2 ==> intersection([x], a2) == [x]
  ensures x !in a2 ==> intersection([x], a2) == []
{
  assert [x][1..] == [];
}

lemma intersection_concat_lemma(a1_before: seq<int>, a1_last_el_seq: seq<int>, a2: seq<int>)
  requires isUnique(a2)
  requires isUnique(a1_before)
  requires isUnique(a1_before + a1_last_el_seq)
  requires |a1_last_el_seq| == 1
  ensures intersection(a1_before + a1_last_el_seq, a2) == intersection(a1_before, a2) + intersection(a1_last_el_seq, a2)
  decreases |a1_before|
{
  if |a1_before| == 0 {
    assert a1_before == [];
    assert a1_before + a1_last_el_seq == a1_last_el_seq;
  } else {
    assert (a1_before + a1_last_el_seq)[0] == a1_before[0];
    assert (a1_before + a1_last_el_seq)[1..] == a1_before[1..] + a1_last_el_seq;
    UniqueTail(a1_before);
    UniqueTail(a1_before + a1_last_el_seq);
    intersection_concat_lemma(a1_before[1..], a1_last_el_seq, a2);
  }
}

method intersectionArray(array_nums1: array<int>, array_nums2: array<int>) returns (res: array<int>)
  requires isUnique(array_nums1[..])
  requires isUnique(array_nums2[..])
  ensures res[..] == intersection(array_nums1[..], array_nums2[..])
{
  var inter := new int[array_nums1.Length];
  var index := 0;
  for i := 0 to array_nums1.Length
    invariant index <= i
    invariant isUnique(array_nums1[..i])
    invariant intersection(array_nums1[..i], array_nums2[..]) == inter[..index]
  {
    UniquePrefix(array_nums1[..], i + 1);
    for j := 0 to array_nums2.Length
      invariant array_nums1[i] !in array_nums2[..j]
    {
      assert array_nums1[..i] + [array_nums1[i]] == array_nums1[..i + 1];
      if array_nums1[i] == array_nums2[j] {
        inter[index] := array_nums1[i];
        index := index + 1;
        break;
      }
    }
    intersection_concat_lemma(array_nums1[..i], [array_nums1[i]], array_nums2[..]);
  }
  assert array_nums1[..array_nums1.Length] == array_nums1[..];
  var result := new int[index];
  for i := 0 to index
    invariant result[..i] == inter[..i]
    modifies result
  {
    result[i] := inter[i];
  }
  res := result;
}

method arrayEquals(a1: array<int>, a2: array<int>) returns (equals: bool)
{
  if a1.Length != a2.Length {
    equals := false;
    return;
  }
  for i := 0 to a1.Length {
    if a1[i] != a2[i] {
      equals := false;
      return;
    }
  }
  equals := true;
}
