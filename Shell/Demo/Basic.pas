program Basic;
const
   pi = 3.14;
   msg = "hello";
var
   i, j, k: integer;
   f: real;
begin 
   i := (3 + 4) * 2;
   f := -pi * sin (3.5) + length (msg);
   writeln ("i=", i, ", f=", f);
end.