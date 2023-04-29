program TypeCheck;
var 
  i, j, k: integer;
  p, q, r: real;

function Sqr (a: real) : real;
begin 
  Sqr := a * a;
end;

begin
  i := 1;
  p := 3.5;
  q := i + p;
  q := i + q;
  r := i;
  k := Sqr (i,j);
  k := Sqt (i);
  k := Sqr ("i");
  read (i,l);
  p := Sqr (i);
end.