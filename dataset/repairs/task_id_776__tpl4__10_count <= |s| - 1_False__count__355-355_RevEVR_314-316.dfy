// task_id_776__tpl4__10_count <= |s| - 1_False__count.dfy

predicate IsVowel(c: char)
{
  c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u'
}

method CountVowelsAsNeighbors(s: string) returns (count: int)
  ensures count == |set i: int | 0 <= i < |s| - 1 && IsVowel(s[i]) && IsVowel(s[i + 1])|
{
  count := 0;
  if (count <= |s| - 1) == false {
    count := count + |s|;
  } else {
    for i := 0 to |s| - 1
      invariant 0 <= i <= |s| - 1
      invariant count == |set k: int | 0 <= k < i && IsVowel(s[k]) && IsVowel(s[k + 1])|
    {
      ghost var before := set k: int | 0 <= k < i && IsVowel(s[k]) && IsVowel(s[k + 1]);
      ghost var after := set k: int | 0 <= k < i + 1 && IsVowel(s[k]) && IsVowel(s[k + 1]);
      if IsVowel(s[i]) && IsVowel(s[i + 1]) {
        assert after == before + {i};
        count := count + 1;
      } else {
        assert after == before;
      }
    }
  }
}
