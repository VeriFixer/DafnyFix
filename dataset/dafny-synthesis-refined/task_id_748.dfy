function Spaced(s: string): string {
    if |s| == 0 then ""
    else if |s| == 1 then s
    else Spaced(s[..|s|-1])
         + (if IsCapitalLetter(s[|s|-1]) then [' ', s[|s|-1]] else [s[|s|-1]])
}

predicate IsCapitalLetter(c: char)
{
    65 <= c as int <= 90
}

method SpaceCapitalWords(s: string) returns (v: string)
    ensures v == Spaced(s)
{
    var s' : string := [s[0]];
    for i := 1 to |s|
    invariant 1 <= i <= |s|
    invariant s' == Spaced(s[..i])
    {
        assert s[..i+1][..i] == s[..i];  // helper assertion for the recursive call
        if IsCapitalLetter(s[i])
        {
            s' := s' + [' '] + [s[i]];
        }
        else 
        {
            s' := s' + [s[i]];
        }
    }
    assert s[..|s|] == s;   // helper assertion for the final result
    return s';
}