method findUniqueElement(arr: array<int>) returns (uniqueElement: int)
  requires arr != null && arr.Length > 0
  ensures exists i :: 0 <= i < arr.Length && arr[i] == uniqueElement
{
  var low: int := 0;
  var high: int := arr.Length - 1;

  while low < high
    invariant 0 <= low <= high < arr.Length
    invariant 0 <= (low + high) / 2 < arr.Length
  {
    var mid: int := (low + high) / 2;

    // If mid is even and its next element is same as mid
    if mid % 2 == 0 && arr[mid] == arr[mid + 1] {
      low := mid + 2;
    }
    // If mid is odd and its previous element is same as mid
    else if mid % 2 == 1 && arr[mid] == arr[mid - 1] {
      low := mid + 1;
    }
    else {
      high := mid;
    }
  }

  uniqueElement := arr[low];
}